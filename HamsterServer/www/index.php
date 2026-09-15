<?php

declare(strict_types=1);

function e(string $value): string
{
    return htmlspecialchars($value, ENT_QUOTES | ENT_SUBSTITUTE, 'UTF-8');
}

$extensions = get_loaded_extensions();
natcasesort($extensions);
$extensions = array_values($extensions);
$opcacheEnabled = filter_var(ini_get('opcache.enable'), FILTER_VALIDATE_BOOL);
$jitBuffer = (string) ini_get('opcache.jit_buffer_size');
$jitEnabled = $opcacheEnabled && $jitBuffer !== '' && $jitBuffer !== '0';

$serverInfo = [
    ['PHP', PHP_VERSION, 'Phiên bản runtime đang xử lý request'],
    ['SAPI', PHP_SAPI, 'Giao tiếp giữa Apache và PHP'],
    ['Web server', (string) ($_SERVER['SERVER_SOFTWARE'] ?? 'Unknown'), 'Phần mềm HTTP đang hoạt động'],
    ['Hệ điều hành', PHP_OS_FAMILY . ' · ' . (PHP_INT_SIZE * 8) . '-bit', 'Nền tảng PHP hiện tại'],
    ['OPcache', $opcacheEnabled ? ($jitEnabled ? 'Enabled · JIT' : 'Enabled') : 'Disabled', 'Bộ nhớ đệm bytecode PHP'],
    ['Memory limit', (string) ini_get('memory_limit'), 'Giới hạn bộ nhớ mỗi request'],
    ['Max execution', ((int) ini_get('max_execution_time')) . ' giây', 'Thời gian xử lý tối đa'],
    ['Múi giờ', date_default_timezone_get(), 'Múi giờ PHP đang sử dụng'],
];

$tests = [
    'ping' => ['HTTP & PHP', 'Độ trễ PHP tối thiểu', 'Đo overhead Apache, FastCGI và PHP bằng response 2 byte.', 'Baseline để so sánh cấu hình server', 1000, 20, 1],
    'noop' => ['Bootstrap', 'PHP + JSON tối giản', 'Nạp PHP và trả về một JSON nhỏ có header đo thời gian server.', 'Chi phí khởi tạo một request động', 1000, 20, 1],
    'hash' => ['CPU', 'Hiệu suất SHA-256', 'Thực hiện chuỗi phép băm để đo năng lực CPU đơn luồng của worker PHP.', 'Request/giây và PHP xử lý TB', 300, 8, 2],
    'json' => ['JSON', 'Encode và decode dữ liệu', 'Mô phỏng API tuần tự hóa danh sách đối tượng nhiều lần.', 'P95 khi xử lý payload có cấu trúc', 500, 12, 2],
    'arrays' => ['Mảng', 'Sắp xếp và tổng hợp', 'Đo callback, map, filter, sort và phép tổng hợp trên mảng PHP.', 'CPU và cấp phát bộ nhớ tạm', 400, 10, 2],
    'regex' => ['Regex', 'Chuỗi và biểu thức chính quy', 'Mô phỏng route, tìm kiếm và biến đổi chuỗi bằng PCRE.', 'Độ ổn định P95/P99', 500, 12, 2],
    'strings' => ['Chuỗi', 'Render chuỗi và template', 'Đo escape HTML, định dạng template, split, replace và ghép chuỗi.', 'Logic view và xử lý nội dung', 500, 12, 2],
    'serialize' => ['Serialize', 'PHP serialize và unserialize', 'Mô phỏng cache/session khi tuần tự hóa cấu trúc dữ liệu PHP lồng nhau.', 'CPU và kích thước dữ liệu tuần tự', 400, 10, 2],
    'math' => ['Toán học', 'Phép tính số thực', 'Chạy lượng giác, căn bậc hai và log để đo FPU của CPU.', 'Hiệu suất tính toán đơn luồng', 300, 8, 2],
    'datetime' => ['Ngày giờ', 'Parse và format DateTime', 'Tạo, chuyển múi giờ, điều chỉnh và định dạng đối tượng DateTimeImmutable.', 'Ứng dụng báo cáo và lịch biểu', 400, 10, 2],
    'compression' => ['Nén', 'Nén và giải nén zlib', 'Nén payload văn bản trong bộ nhớ để kiểm tra CPU và extension zlib.', 'Tỷ lệ nén và thời gian xử lý', 250, 8, 1],
    'stream' => ['Stream', 'PHP memory stream', 'Ghi, đọc và hash dữ liệu qua php://temp mà không truy cập ổ đĩa.', 'I/O nội bộ và sao chép bộ nhớ', 300, 8, 2],
    'filesystem' => ['Ổ đĩa', 'Temporary file I/O', 'Ghi, đọc và xác minh file tạm riêng cho từng request, sau đó tự động xóa.', 'Độ trễ filesystem và antivirus', 100, 4, 1],
    'wait' => ['I/O chờ', 'Mô phỏng tác vụ blocking', 'Giữ PHP worker trong 5 ms × intensity để mô phỏng API hoặc database chờ phản hồi.', 'Khả năng phục vụ đồng thời của worker', 300, 16, 1],
    'random' => ['Random', 'Entropy và token', 'Sinh random_bytes, mã hóa Base64 và HMAC như luồng tạo token bảo mật.', 'Chi phí entropy và crypto', 400, 10, 2],
    'mixed' => ['Tổng hợp', 'Workload ứng dụng hỗn hợp', 'Kết hợp hash, JSON, mảng và regex trong cùng một request.', 'Mô phỏng logic ứng dụng thực tế', 300, 10, 1],
    'payload' => ['Băng thông', 'Response động 16 KiB–1 MiB', 'Đo tốc độ truyền response được PHP tạo động qua FastCGI.', 'MiB/giây và độ trễ cuối đuôi', 300, 10, 1],
];
?>
<!doctype html>
<html lang="vi">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <meta name="color-scheme" content="dark">
    <title>XServer Performance Lab</title>
    <link rel="stylesheet" href="assets/style.css">
    <script src="assets/app.js" defer></script>
</head>
<body>
    <header class="hero">
        <div>
            <div class="brand"><span class="brand__mark">XS</span> XServer Performance Lab</div>
            <h1>Server overview &amp;<br><span>PHP benchmark</span></h1>
            <p>Kiểm tra cấu hình runtime, extension và hiệu suất Apache + PHP ngay trên trình duyệt. Không dùng framework hoặc dịch vụ bên ngoài.</p>
        </div>
        <div class="server-pill"><i></i><?= e((string) ($_SERVER['HTTP_HOST'] ?? 'localhost')) ?></div>
    </header>

    <main class="page">
        <nav class="tabs" aria-label="Các khu vực kiểm tra">
            <button class="tab-button is-active" type="button" data-tab="overview">Tổng quan</button>
            <?php foreach ($tests as $key => $test): ?>
                <button class="tab-button" type="button" data-tab="<?= e($key) ?>"><?= e($test[0]) ?></button>
            <?php endforeach; ?>
        </nav>

        <section class="tab-panel is-active" id="tab-overview" data-tab-panel="overview">
            <div class="section-title">
                <div><span class="eyebrow">Runtime snapshot</span><h2>Thông tin server và PHP</h2></div>
                <span class="ready-badge"><i></i> Online</span>
            </div>

            <div class="server-grid">
                <?php foreach ($serverInfo as [$label, $value, $note]): ?>
                    <article class="server-card">
                        <span><?= e($label) ?></span><strong title="<?= e($value) ?>"><?= e($value) ?></strong><small><?= e($note) ?></small>
                    </article>
                <?php endforeach; ?>
            </div>

            <div class="extensions-panel">
                <div class="section-title section-title--compact">
                    <div><span class="eyebrow">Loaded modules</span><h2>PHP Extensions <em><?= count($extensions) ?></em></h2></div>
                    <label class="search-box"><span>⌕</span><input id="extensionSearch" type="search" placeholder="Tìm extension…" autocomplete="off"></label>
                </div>
                <div class="extension-grid" id="extensionGrid">
                    <?php foreach ($extensions as $extension): ?>
                        <?php $version = phpversion($extension); ?>
                        <div class="extension-item" data-extension="<?= e(strtolower($extension)) ?>">
                            <i></i><span><?= e($extension) ?></span><small><?= $version !== false ? e((string) $version) : 'built-in' ?></small>
                        </div>
                    <?php endforeach; ?>
                </div>
                <p class="empty-state" id="extensionEmpty" hidden>Không tìm thấy extension phù hợp.</p>
            </div>
        </section>

        <?php foreach ($tests as $key => $test): ?>
            <section class="tab-panel test-panel" id="tab-<?= e($key) ?>" data-tab-panel="<?= e($key) ?>" data-profile="<?= e($key) ?>"
                     data-requests="<?= $test[4] ?>" data-concurrency="<?= $test[5] ?>" data-intensity="<?= $test[6] ?>">
                <div class="test-intro">
                    <div><span class="eyebrow"><?= e($test[0]) ?> test</span><h2><?= e($test[1]) ?></h2><p><?= e($test[2]) ?></p></div>
                    <div class="test-focus"><span>Chỉ số trọng tâm</span><strong><?= e($test[3]) ?></strong></div>
                </div>
            </section>
        <?php endforeach; ?>

        <section class="workbench" id="workbench" hidden>
            <div class="configuration-card">
                <div class="section-title section-title--compact">
                    <div><span class="eyebrow">Load configuration</span><h2 id="activeTestTitle">Thiết lập phép thử</h2></div>
                    <button id="resetButton" class="button button--ghost" type="button">Khuyến nghị</button>
                </div>
                <div class="form-grid">
                    <label><span>Tổng request</span><input id="requestCount" type="number" min="1" max="5000" value="500"></label>
                    <label><span>Đồng thời</span><input id="concurrency" type="number" min="1" max="64" value="10"></label>
                    <label id="intensityField"><span>Cường độ PHP</span><input id="intensity" type="number" min="1" max="20" value="1"></label>
                    <label id="payloadField" hidden><span>Payload</span><select id="payloadBytes"><option value="16384">16 KiB</option><option value="65536" selected>64 KiB</option><option value="262144">256 KiB</option><option value="1048576">1 MiB</option></select></label>
                </div>
                <label class="warmup"><input id="warmup" type="checkbox" checked><span>Warm-up OPcache và FastCGI trước khi đo</span></label>
                <div class="actions">
                    <button id="startButton" class="button button--primary" type="button">Chạy test</button>
                    <button id="stopButton" class="button button--danger" type="button" disabled>Dừng</button>
                    <button id="exportButton" class="button button--ghost" type="button" disabled>Xuất JSON</button>
                </div>
                <div class="progress-copy"><span id="progressLabel">Sẵn sàng</span><span id="progressValue">0 / 0</span></div>
                <div class="progress-track"><div id="progressBar"></div></div>
            </div>

            <div class="result-area">
                <div class="metric-grid">
                    <article class="metric metric--accent"><span>Thông lượng thành công</span><strong id="requestsPerSecond">—</strong><small>request/giây</small></article>
                    <article class="metric"><span>Trung bình (tất cả)</span><strong id="averageLatency">—</strong><small>mili giây</small></article>
                    <article class="metric"><span>P95</span><strong id="p95Latency">—</strong><small>mili giây</small></article>
                    <article class="metric"><span>P99</span><strong id="p99Latency">—</strong><small>mili giây</small></article>
                    <article class="metric"><span>Thành công</span><strong id="successRate">—</strong><small>tỷ lệ phản hồi</small></article>
                    <article class="metric"><span>Băng thông</span><strong id="throughput">—</strong><small>MiB/giây</small></article>
                </div>
                <div class="chart-card">
                    <div class="chart-heading"><strong>Độ trễ theo thứ tự hoàn thành</strong><span id="chartLegend">P50 — · P95 —</span></div>
                    <canvas id="latencyChart" height="230"></canvas>
                </div>
                <div class="details-card"><table><tbody>
                    <tr><th>Thời gian tổng</th><td id="totalDuration">—</td><th>Request lỗi</th><td id="failedRequests">—</td></tr>
                    <tr><th>RPS đã thử</th><td id="attemptedRequestsPerSecond">—</td><th>HTTP status</th><td id="httpStatusCounts">—</td></tr>
                    <tr><th>Nhỏ nhất</th><td id="minLatency">—</td><th>Lớn nhất</th><td id="maxLatency">—</td></tr>
                    <tr><th>P50</th><td id="p50Latency">—</td><th>Workload PHP TB</th><td id="serverAverage">—</td></tr>
                    <tr><th>Dữ liệu nhận</th><td id="transferredBytes">—</td><th>Concurrency</th><td id="usedConcurrency">—</td></tr>
                </tbody></table></div>
            </div>
        </section>

        <p class="safety-note">Benchmark có chủ đích tạo tải. Không chọn concurrency cao khi các website quan trọng đang hoạt động.</p>
    </main>
    <footer><span>XServer Performance Lab</span><span>Plain PHP · No Composer · No external requests</span></footer>
</body>
</html>
