(() => {
    'use strict';

    const $ = (id) => document.getElementById(id);
    const tabButtons = [...document.querySelectorAll('[data-tab]')];
    const tabPanels = [...document.querySelectorAll('[data-tab-panel]')];
    const workbench = $('workbench');
    let currentProfile = 'ping';
    let currentDefaults = { requests: 1000, concurrency: 20, intensity: 1 };
    let activeController = null;
    let lastReport = null;
    let chartValues = [];

    const controls = {
        requests: $('requestCount'), concurrency: $('concurrency'), intensity: $('intensity'),
        intensityField: $('intensityField'), payloadField: $('payloadField'), payloadBytes: $('payloadBytes'),
        warmup: $('warmup'), start: $('startButton'), stop: $('stopButton'), export: $('exportButton'),
        reset: $('resetButton'), progressLabel: $('progressLabel'), progressValue: $('progressValue'),
        progressBar: $('progressBar')
    };

    function activateTab(name, updateHash = true) {
        const selectedPanel = tabPanels.find((panel) => panel.dataset.tabPanel === name) || tabPanels[0];
        const selectedName = selectedPanel.dataset.tabPanel;
        tabPanels.forEach((panel) => panel.classList.toggle('is-active', panel === selectedPanel));
        tabButtons.forEach((button) => {
            const active = button.dataset.tab === selectedName;
            button.classList.toggle('is-active', active);
            button.setAttribute('aria-selected', String(active));
            if (active) {
                button.scrollIntoView({
                    behavior: updateHash ? 'smooth' : 'auto',
                    block: 'nearest',
                    inline: 'nearest'
                });
            }
        });

        const isTest = Boolean(selectedPanel.dataset.profile);
        workbench.hidden = !isTest;
        if (isTest) {
            currentProfile = selectedPanel.dataset.profile;
            currentDefaults = {
                requests: Number(selectedPanel.dataset.requests),
                concurrency: Number(selectedPanel.dataset.concurrency),
                intensity: Number(selectedPanel.dataset.intensity)
            };
            $('activeTestTitle').textContent = selectedPanel.querySelector('h2').textContent;
            controls.intensityField.hidden = currentProfile === 'ping' || currentProfile === 'payload';
            controls.payloadField.hidden = currentProfile !== 'payload';
            applyDefaults();
        }
        if (updateHash) history.replaceState(null, '', `#${selectedName}`);
    }

    function applyDefaults() {
        controls.requests.value = String(currentDefaults.requests);
        controls.concurrency.value = String(currentDefaults.concurrency);
        controls.intensity.value = String(currentDefaults.intensity);
        controls.payloadBytes.value = '65536';
        controls.warmup.checked = true;
    }

    function clampInput(input) {
        const value = Number.parseInt(input.value, 10);
        const safe = Number.isFinite(value) ? value : Number(input.min);
        input.value = String(Math.max(Number(input.min), Math.min(Number(input.max), safe)));
        return Number(input.value);
    }

    function requestUrl(profile, intensity, payloadBytes, sequence) {
        const nonce = `${Date.now()}-${sequence}-${Math.random().toString(16).slice(2)}`;
        if (profile === 'ping') return `api/ping.php?n=${nonce}`;
        if (profile === 'payload') return `api/payload.php?bytes=${payloadBytes}&n=${nonce}`;
        return `api/benchmark.php?profile=${encodeURIComponent(profile)}&intensity=${intensity}&n=${nonce}`;
    }

    async function issueRequest(url, signal) {
        const startedAt = performance.now();
        try {
            const response = await fetch(url, { cache: 'no-store', signal });
            const body = await response.arrayBuffer();
            const serverTime = Number.parseFloat(
                response.headers.get('X-Benchmark-Workload-Time-Ms') ||
                response.headers.get('X-Benchmark-Time-Ms') || '');
            return {
                ok: response.ok,
                status: response.status,
                elapsed: performance.now() - startedAt,
                bytes: body.byteLength,
                serverTime: response.ok && Number.isFinite(serverTime) ? serverTime : null
            };
        } catch (error) {
            if (error.name === 'AbortError') throw error;
            return {
                ok: false,
                status: 0,
                elapsed: performance.now() - startedAt,
                bytes: 0,
                serverTime: null,
                error: error instanceof Error ? error.message : String(error)
            };
        }
    }

    async function warmUp(profile, intensity, payloadBytes, concurrency, signal) {
        const count = Math.min(20, Math.max(4, concurrency * 2));
        controls.progressLabel.textContent = `Warm-up ${count} request…`;
        const results = await Promise.all(Array.from({ length: count }, (_, index) =>
            issueRequest(requestUrl(profile, intensity, payloadBytes, `warmup-${index}`), signal)
        ));
        const failure = results.find((result) => !result.ok);
        if (failure) throw new Error(failure.status ? `HTTP ${failure.status}` : 'Lỗi mạng khi warm-up');
    }

    function setRunning(running) {
        workbench.querySelectorAll('input, select').forEach((input) => { input.disabled = running; });
        tabButtons.forEach((button) => { button.disabled = running; });
        controls.start.disabled = running;
        controls.stop.disabled = !running;
        controls.reset.disabled = running;
        controls.export.disabled = running || lastReport === null;
    }

    function updateProgress(done, total, errors, startedAt) {
        const elapsedSeconds = Math.max((performance.now() - startedAt) / 1000, 0.001);
        controls.progressValue.textContent = `${done.toLocaleString('vi-VN')} / ${total.toLocaleString('vi-VN')}`;
        controls.progressBar.style.width = `${((done / total) * 100).toFixed(2)}%`;
        controls.progressLabel.textContent = errors
            ? `${(done / elapsedSeconds).toFixed(1)} req/s · ${errors} lỗi`
            : `${(done / elapsedSeconds).toFixed(1)} req/s`;
    }

    function percentile(sorted, ratio) {
        if (!sorted.length) return 0;
        const index = (sorted.length - 1) * ratio;
        const lower = Math.floor(index);
        const fraction = index - lower;
        return sorted[lower + 1] === undefined
            ? sorted[lower]
            : sorted[lower] + fraction * (sorted[lower + 1] - sorted[lower]);
    }

    function summarize(total, concurrency, elapsedMs, results, stopped) {
        const successfulResults = results.filter((result) => result.ok);
        const latencies = results.map((result) => result.elapsed);
        const sorted = [...latencies].sort((a, b) => a - b);
        const serverTimes = successfulResults.map((result) => result.serverTime).filter(Number.isFinite);
        const bytes = results.reduce((sum, result) => sum + result.bytes, 0);
        const seconds = Math.max(elapsedMs / 1000, 0.001);
        const completed = results.length;
        const failed = completed - successfulResults.length;
        const statusCounts = results.reduce((counts, result) => {
            const key = result.status === 0 ? 'network_error' : String(result.status);
            counts[key] = (counts[key] || 0) + 1;
            return counts;
        }, {});
        return {
            generated_at: new Date().toISOString(), profile: currentProfile, stopped,
            requested: total, completed, successful: successfulResults.length, failed, concurrency,
            duration_ms: elapsedMs,
            requests_per_second: successfulResults.length / seconds,
            attempted_requests_per_second: completed / seconds,
            status_counts: statusCounts,
            latency_scope: 'all_completed_requests',
            transferred_bytes: bytes, throughput_mib_per_second: bytes / 1048576 / seconds,
            latency_ms: {
                min: sorted[0] || 0,
                average: latencies.length ? latencies.reduce((sum, value) => sum + value, 0) / latencies.length : 0,
                p50: percentile(sorted, .5), p95: percentile(sorted, .95), p99: percentile(sorted, .99),
                max: sorted.at(-1) || 0
            },
            average_php_workload_time_ms: serverTimes.length
                ? serverTimes.reduce((sum, value) => sum + value, 0) / serverTimes.length
                : null,
            samples_ms: latencies
        };
    }

    const formatMs = (value) => `${value.toFixed(value < 10 ? 2 : 1)} ms`;

    function formatStatusCounts(counts) {
        return Object.entries(counts)
            .sort(([left], [right]) => left.localeCompare(right, undefined, { numeric: true }))
            .map(([status, count]) => `${status === 'network_error' ? 'Network' : `HTTP ${status}`}: ${count}`)
            .join(' · ') || '—';
    }

    function renderReport(report) {
        $('requestsPerSecond').textContent = report.requests_per_second.toFixed(1);
        $('averageLatency').textContent = report.latency_ms.average.toFixed(2);
        $('p95Latency').textContent = report.latency_ms.p95.toFixed(2);
        $('p99Latency').textContent = report.latency_ms.p99.toFixed(2);
        $('successRate').textContent = report.completed
            ? `${((report.successful / report.completed) * 100).toFixed(1)}%` : '0%';
        $('throughput').textContent = report.throughput_mib_per_second.toFixed(2);
        $('totalDuration').textContent = `${(report.duration_ms / 1000).toFixed(2)} giây`;
        $('failedRequests').textContent = report.failed.toLocaleString('vi-VN');
        $('attemptedRequestsPerSecond').textContent = report.attempted_requests_per_second.toFixed(1);
        $('httpStatusCounts').textContent = formatStatusCounts(report.status_counts);
        $('minLatency').textContent = formatMs(report.latency_ms.min);
        $('maxLatency').textContent = formatMs(report.latency_ms.max);
        $('p50Latency').textContent = formatMs(report.latency_ms.p50);
        $('serverAverage').textContent = report.average_php_workload_time_ms === null
            ? 'Không áp dụng' : formatMs(report.average_php_workload_time_ms);
        $('transferredBytes').textContent = `${(report.transferred_bytes / 1048576).toFixed(2)} MiB`;
        $('usedConcurrency').textContent = String(report.concurrency);
        $('chartLegend').textContent = `P50 ${formatMs(report.latency_ms.p50)} · P95 ${formatMs(report.latency_ms.p95)}`;
        chartValues = report.samples_ms;
        drawChart();
    }

    function drawChart() {
        const canvas = $('latencyChart');
        if (!canvas || workbench.hidden) return;
        const context = canvas.getContext('2d');
        const ratio = window.devicePixelRatio || 1;
        const width = canvas.clientWidth;
        const height = canvas.clientHeight;
        canvas.width = Math.round(width * ratio);
        canvas.height = Math.round(height * ratio);
        context.scale(ratio, ratio);
        context.clearRect(0, 0, width, height);
        context.strokeStyle = 'rgba(148, 163, 184, .15)';
        for (let line = 1; line < 5; line++) {
            const y = height / 5 * line;
            context.beginPath(); context.moveTo(0, y); context.lineTo(width, y); context.stroke();
        }
        if (chartValues.length < 2) return;
        const stride = Math.max(1, Math.ceil(chartValues.length / Math.max(2, Math.floor(width))));
        const points = chartValues.filter((_, index) => index % stride === 0);
        const sorted = [...chartValues].sort((a, b) => a - b);
        const ceiling = Math.max(percentile(sorted, .99) * 1.25, 1);
        const gradient = context.createLinearGradient(0, 0, width, 0);
        gradient.addColorStop(0, '#38bdf8'); gradient.addColorStop(1, '#34d399');
        context.beginPath();
        points.forEach((value, index) => {
            const x = index / (points.length - 1) * width;
            const y = height - Math.min(value / ceiling, 1) * (height - 8) - 4;
            index ? context.lineTo(x, y) : context.moveTo(x, y);
        });
        context.strokeStyle = gradient; context.lineWidth = 1.7; context.stroke();
    }

    async function startBenchmark() {
        const total = clampInput(controls.requests);
        const concurrency = Math.min(clampInput(controls.concurrency), total);
        const intensity = clampInput(controls.intensity);
        const payloadBytes = Number(controls.payloadBytes.value);
        const controller = new AbortController();
        activeController = controller;
        lastReport = null;
        setRunning(true);
        controls.progressBar.style.width = '0%';
        controls.progressValue.textContent = `0 / ${total.toLocaleString('vi-VN')}`;

        try {
            if (controls.warmup.checked) await warmUp(currentProfile, intensity, payloadBytes, concurrency, controller.signal);
            const results = [];
            let errors = 0, nextIndex = 0, completed = 0, lastUiUpdate = 0;
            const startedAt = performance.now();

            async function worker() {
                while (!controller.signal.aborted) {
                    const index = nextIndex++;
                    if (index >= total) return;
                    let counted = false;
                    try {
                        const result = await issueRequest(
                            requestUrl(currentProfile, intensity, payloadBytes, index), controller.signal);
                        results.push(result);
                        if (!result.ok) errors++;
                        counted = true;
                    } catch (error) {
                        if (error.name === 'AbortError') return;
                        throw error;
                    } finally {
                        if (counted) {
                            completed++;
                            const now = performance.now();
                            if (now - lastUiUpdate > 80 || completed === total) {
                                updateProgress(completed, total, errors, startedAt); lastUiUpdate = now;
                            }
                        }
                    }
                }
            }

            await Promise.all(Array.from({ length: concurrency }, worker));
            lastReport = summarize(total, concurrency, performance.now() - startedAt, results, controller.signal.aborted);
            renderReport(lastReport);
            controls.progressLabel.textContent = controller.signal.aborted ? 'Đã dừng · kết quả một phần' : 'Hoàn tất';
        } catch (error) {
            controls.progressLabel.textContent = error.name === 'AbortError' ? 'Đã dừng khi warm-up' : `Lỗi: ${error.message}`;
        } finally {
            activeController = null;
            setRunning(false);
        }
    }

    function exportReport() {
        if (!lastReport) return;
        const data = { ...lastReport };
        delete data.samples_ms;
        const url = URL.createObjectURL(new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' }));
        const link = document.createElement('a');
        link.href = url; link.download = `xserver-${lastReport.profile}-${Date.now()}.json`; link.click();
        URL.revokeObjectURL(url);
    }

    tabButtons.forEach((button) => button.addEventListener('click', () => activateTab(button.dataset.tab)));
    controls.start.addEventListener('click', startBenchmark);
    controls.stop.addEventListener('click', () => activeController?.abort());
    controls.export.addEventListener('click', exportReport);
    controls.reset.addEventListener('click', applyDefaults);
    window.addEventListener('resize', drawChart);

    $('extensionSearch').addEventListener('input', (event) => {
        const query = event.target.value.trim().toLowerCase();
        let visible = 0;
        document.querySelectorAll('[data-extension]').forEach((item) => {
            const show = item.dataset.extension.includes(query);
            item.hidden = !show;
            if (show) visible++;
        });
        $('extensionEmpty').hidden = visible !== 0;
    });

    const initialTab = location.hash.slice(1);
    activateTab(tabPanels.some((panel) => panel.dataset.tabPanel === initialTab) ? initialTab : 'overview', false);
})();
