<?php

declare(strict_types=1);

header('Content-Type: application/json; charset=utf-8');
header('Cache-Control: no-store, no-cache, must-revalidate, max-age=0');
header('X-Content-Type-Options: nosniff');

$opcacheEnabled = filter_var(ini_get('opcache.enable'), FILTER_VALIDATE_BOOL);
$jitBuffer = (string) ini_get('opcache.jit_buffer_size');

echo json_encode([
    'php_version' => PHP_VERSION,
    'sapi' => PHP_SAPI,
    'os_family' => PHP_OS_FAMILY,
    'architecture' => (PHP_INT_SIZE * 8) . '-bit',
    'memory_limit' => (string) ini_get('memory_limit'),
    'max_execution_time' => (int) ini_get('max_execution_time'),
    'opcache_enabled' => $opcacheEnabled,
    'jit_enabled' => $opcacheEnabled && $jitBuffer !== '' && $jitBuffer !== '0',
    'extension_count' => count(get_loaded_extensions()),
    'server_time' => gmdate(DATE_ATOM),
], JSON_THROW_ON_ERROR | JSON_UNESCAPED_SLASHES);
