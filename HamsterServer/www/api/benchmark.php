<?php

declare(strict_types=1);

require_once dirname(__DIR__) . '/src/BenchmarkSuite.php';

header('Content-Type: application/json; charset=utf-8');
header('Cache-Control: no-store, no-cache, must-revalidate, max-age=0');
header('X-Content-Type-Options: nosniff');

$profile = strtolower(trim((string) ($_GET['profile'] ?? 'mixed')));
$intensity = filter_input(INPUT_GET, 'intensity', FILTER_VALIDATE_INT, [
    'options' => ['default' => 1, 'min_range' => 1, 'max_range' => 20],
]);
$intensity = is_int($intensity) ? $intensity : 1;

if (!array_key_exists($profile, BenchmarkSuite::profiles())) {
    http_response_code(400);
    echo json_encode(['ok' => false, 'error' => 'Unsupported benchmark profile.'], JSON_THROW_ON_ERROR);
    exit;
}

try {
    $startedAt = hrtime(true);
    $result = BenchmarkSuite::run($profile, $intensity);
    $elapsedMs = (hrtime(true) - $startedAt) / 1_000_000;
    $formattedTime = number_format($elapsedMs, 3, '.', '');

    header('Server-Timing: php;dur=' . $formattedTime);
    header('X-Benchmark-Time-Ms: ' . $formattedTime);
    echo json_encode([
        'ok' => true,
        'profile' => $profile,
        'intensity' => $intensity,
        'operations' => $result['operations'],
        'checksum' => $result['checksum'],
        'server_ms' => round($elapsedMs, 3),
        'peak_memory_bytes' => memory_get_peak_usage(true),
    ], JSON_THROW_ON_ERROR | JSON_UNESCAPED_SLASHES);
} catch (Throwable) {
    http_response_code(500);
    echo json_encode(['ok' => false, 'error' => 'Benchmark execution failed.'], JSON_THROW_ON_ERROR);
}
