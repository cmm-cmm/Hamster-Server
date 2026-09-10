<?php

declare(strict_types=1);

$allowedSizes = [16_384, 65_536, 262_144, 1_048_576];
$requestedSize = filter_input(INPUT_GET, 'bytes', FILTER_VALIDATE_INT);
$size = is_int($requestedSize) && in_array($requestedSize, $allowedSizes, true)
    ? $requestedSize
    : 65_536;

$startedAt = hrtime(true);
$chunk = 'Hamster Server-PHP-payload-0123456789abcdefghijklmnopqrstuvwxyz' . "\n";
$payload = substr(str_repeat($chunk, (int) ceil($size / strlen($chunk))), 0, $size);
$elapsedMs = (hrtime(true) - $startedAt) / 1_000_000;
$formattedTime = number_format($elapsedMs, 3, '.', '');

header('Content-Type: application/octet-stream');
header('Content-Length: ' . $size);
header('Cache-Control: no-store, no-cache, must-revalidate, max-age=0');
header('X-Content-Type-Options: nosniff');
header('Server-Timing: php-workload;dur=' . $formattedTime);
header('X-Benchmark-Workload-Time-Ms: ' . $formattedTime);
echo $payload;
