<?php

declare(strict_types=1);

final class BenchmarkSuite
{
    /** @return array<string, string> */
    public static function profiles(): array
    {
        return [
            'noop' => 'Bootstrap PHP + JSON',
            'hash' => 'CPU SHA-256',
            'json' => 'JSON encode/decode',
            'arrays' => 'Array operations',
            'regex' => 'Regex and strings',
            'strings' => 'String rendering and templates',
            'serialize' => 'PHP serialize/unserialize',
            'math' => 'Floating-point math',
            'datetime' => 'DateTime parse/format',
            'compression' => 'Zlib compression',
            'stream' => 'In-memory stream I/O',
            'filesystem' => 'Temporary file I/O',
            'wait' => 'Blocking I/O simulation',
            'random' => 'Random bytes and tokens',
            'mixed' => 'Mixed application workload',
        ];
    }

    /** @return array{checksum: string, operations: int} */
    public static function run(string $profile, int $intensity): array
    {
        $intensity = max(1, min(20, $intensity));

        return match ($profile) {
            'noop' => ['checksum' => 'ready', 'operations' => 1],
            'hash' => self::hashWorkload($intensity),
            'json' => self::jsonWorkload($intensity),
            'arrays' => self::arrayWorkload($intensity),
            'regex' => self::regexWorkload($intensity),
            'strings' => self::stringWorkload($intensity),
            'serialize' => self::serializeWorkload($intensity),
            'math' => self::mathWorkload($intensity),
            'datetime' => self::dateTimeWorkload($intensity),
            'compression' => self::compressionWorkload($intensity),
            'stream' => self::streamWorkload($intensity),
            'filesystem' => self::filesystemWorkload($intensity),
            'wait' => self::waitWorkload($intensity),
            'random' => self::randomWorkload($intensity),
            'mixed' => self::mixedWorkload($intensity),
            default => throw new InvalidArgumentException('Unknown benchmark profile.'),
        };
    }

    /** @return array{checksum: string, operations: int} */
    private static function hashWorkload(int $intensity): array
    {
        $iterations = 200 * $intensity;
        $value = str_repeat('hamster-server-performance-', 128);
        for ($index = 0; $index < $iterations; $index++) {
            $value = hash('sha256', $value . $index, true);
        }

        return ['checksum' => bin2hex(substr($value, 0, 8)), 'operations' => $iterations];
    }

    /** @return array{checksum: string, operations: int} */
    private static function jsonWorkload(int $intensity): array
    {
        $records = [];
        for ($index = 0; $index < 40; $index++) {
            $records[] = [
                'id' => $index,
                'name' => "project-{$index}",
                'enabled' => ($index % 3) !== 0,
                'tags' => ['php', 'apache', 'benchmark'],
                'metrics' => ['latency' => $index * 1.25, 'requests' => $index * 100],
            ];
        }

        $encoded = '';
        for ($pass = 0; $pass < 8 * $intensity; $pass++) {
            $encoded = json_encode($records, JSON_THROW_ON_ERROR);
            $records = json_decode($encoded, true, 32, JSON_THROW_ON_ERROR);
        }

        return ['checksum' => hash('crc32b', $encoded), 'operations' => 16 * $intensity];
    }

    /** @return array{checksum: string, operations: int} */
    private static function arrayWorkload(int $intensity): array
    {
        $operations = 0;
        $checksum = 0;
        for ($pass = 0; $pass < 30 * $intensity; $pass++) {
            $values = range(1, 300);
            $values = array_map(static fn (int $value): int => ($value * 31 + $pass) % 997, $values);
            rsort($values, SORT_NUMERIC);
            $filtered = array_filter($values, static fn (int $value): bool => ($value % 3) === 0);
            $checksum += array_sum($filtered);
            $operations += count($values) * 3;
        }

        return ['checksum' => dechex($checksum), 'operations' => $operations];
    }

    /** @return array{checksum: string, operations: int} */
    private static function regexWorkload(int $intensity): array
    {
        $source = str_repeat(
            'GET /catalog/product-123?sort=popular HTTP/1.1 host=shop.test user=visitor@example.test ',
            20
        );
        $matches = [];
        $count = 0;
        $iterations = 80 * $intensity;
        for ($pass = 0; $pass < $iterations; $pass++) {
            $count += preg_match_all('/(?:product-|user=)([a-z0-9@.-]+)/i', $source, $matches);
            $source = strtr($source, ['popular' => 'recent', 'recent' => 'popular']);
        }

        return ['checksum' => dechex($count + strlen($source)), 'operations' => $iterations];
    }

    /** @return array{checksum: string, operations: int} */
    private static function stringWorkload(int $intensity): array
    {
        $source = 'hamster-server local php performance & security <benchmark>';
        $rendered = '';
        $iterations = 120 * $intensity;
        for ($pass = 0; $pass < $iterations; $pass++) {
            $title = ucwords(str_replace(['-', '_'], ' ', "article-{$pass}_{$source}"));
            $escaped = htmlspecialchars($title, ENT_QUOTES | ENT_SUBSTITUTE, 'UTF-8');
            $tokens = explode(' ', strtolower($escaped));
            $rendered = sprintf(
                '<article data-id="%d"><h2>%s</h2><p>%s</p></article>',
                $pass,
                $escaped,
                implode('|', array_reverse($tokens))
            );
        }

        return ['checksum' => hash('crc32b', $rendered), 'operations' => $iterations * 5];
    }

    /** @return array{checksum: string, operations: int} */
    private static function serializeWorkload(int $intensity): array
    {
        $payload = [
            'user' => ['id' => 42, 'name' => 'Hamster Server User', 'roles' => ['admin', 'developer']],
            'settings' => ['locale' => 'vi_VN', 'timezone' => 'Asia/Ho_Chi_Minh', 'dark' => true],
            'items' => array_map(
                static fn (int $id): array => ['id' => $id, 'sku' => "SKU-{$id}", 'price' => $id * 1.25],
                range(1, 60)
            ),
        ];
        $encoded = '';
        $iterations = 12 * $intensity;
        for ($pass = 0; $pass < $iterations; $pass++) {
            $encoded = serialize($payload);
            $decoded = unserialize($encoded, ['allowed_classes' => false]);
            if (!is_array($decoded)) {
                throw new RuntimeException('Could not unserialize benchmark payload.');
            }
            $payload = $decoded;
        }

        return ['checksum' => hash('crc32b', $encoded), 'operations' => $iterations * 2];
    }

    /** @return array{checksum: string, operations: int} */
    private static function mathWorkload(int $intensity): array
    {
        $value = 0.0;
        $iterations = 4_000 * $intensity;
        for ($index = 1; $index <= $iterations; $index++) {
            $angle = ($index % 360) * M_PI / 180;
            $value += sin($angle) * cos($angle / 2) + sqrt($index) / log($index + 1);
        }

        return ['checksum' => hash('crc32b', sprintf('%.10F', $value)), 'operations' => $iterations * 5];
    }

    /** @return array{checksum: string, operations: int} */
    private static function dateTimeWorkload(int $intensity): array
    {
        $utc = new DateTimeZone('UTC');
        $local = new DateTimeZone('Asia/Ho_Chi_Minh');
        $formatted = '';
        $iterations = 100 * $intensity;
        for ($pass = 0; $pass < $iterations; $pass++) {
            $date = DateTimeImmutable::createFromFormat(
                '!Y-m-d H:i:s',
                sprintf('2026-%02d-%02d %02d:%02d:%02d',
                    ($pass % 12) + 1,
                    ($pass % 28) + 1,
                    $pass % 24,
                    $pass % 60,
                    ($pass * 7) % 60),
                $utc
            );
            if ($date === false) {
                throw new RuntimeException('Could not parse benchmark date.');
            }
            $formatted = $date->setTimezone($local)->modify('+3 days')->format(DATE_ATOM);
        }

        return ['checksum' => hash('crc32b', $formatted), 'operations' => $iterations * 4];
    }

    /** @return array{checksum: string, operations: int} */
    private static function compressionWorkload(int $intensity): array
    {
        if (!function_exists('gzencode') || !function_exists('gzdecode')) {
            throw new RuntimeException('The zlib extension is required for this profile.');
        }

        $source = str_repeat('Hamster Server PHP compression benchmark: JSON CSS HTML JavaScript 0123456789' . "\n", 1_800);
        $compressed = '';
        $iterations = 3 * $intensity;
        for ($pass = 0; $pass < $iterations; $pass++) {
            $compressed = gzencode($source, 6);
            if ($compressed === false || gzdecode($compressed) !== $source) {
                throw new RuntimeException('Could not verify compressed benchmark payload.');
            }
        }

        return [
            'checksum' => hash('crc32b', $compressed),
            'operations' => $iterations * 2,
        ];
    }

    /** @return array{checksum: string, operations: int} */
    private static function streamWorkload(int $intensity): array
    {
        $payload = str_repeat('hamster-server-stream-0123456789abcdef', 2_048);
        $hash = '';
        $iterations = 8 * $intensity;
        for ($pass = 0; $pass < $iterations; $pass++) {
            $stream = fopen('php://temp/maxmemory:1048576', 'w+b');
            if ($stream === false) {
                throw new RuntimeException('Could not open temporary memory stream.');
            }
            try {
                fwrite($stream, $payload);
                rewind($stream);
                $contents = stream_get_contents($stream);
                if ($contents === false) {
                    throw new RuntimeException('Could not read temporary memory stream.');
                }
                $hash = hash('sha256', $contents, true);
            } finally {
                fclose($stream);
            }
        }

        return ['checksum' => bin2hex(substr($hash, 0, 8)), 'operations' => $iterations * 3];
    }

    /** @return array{checksum: string, operations: int} */
    private static function filesystemWorkload(int $intensity): array
    {
        $path = tempnam(sys_get_temp_dir(), 'xsb-');
        if ($path === false) {
            throw new RuntimeException('Could not create benchmark temporary file.');
        }

        $payload = str_repeat('hamster-server-file-0123456789abcdef', 1_024);
        $hash = '';
        $iterations = 2 * $intensity;
        try {
            for ($pass = 0; $pass < $iterations; $pass++) {
                $written = file_put_contents($path, $payload, LOCK_EX);
                $contents = file_get_contents($path);
                if ($written !== strlen($payload) || $contents === false) {
                    throw new RuntimeException('Could not verify benchmark temporary file.');
                }
                $hash = hash('sha256', $contents, true);
            }
        } finally {
            @unlink($path);
        }

        return ['checksum' => bin2hex(substr($hash, 0, 8)), 'operations' => $iterations * 3];
    }

    /** @return array{checksum: string, operations: int} */
    private static function waitWorkload(int $intensity): array
    {
        $microseconds = 5_000 * $intensity;
        usleep($microseconds);

        return ['checksum' => dechex($microseconds), 'operations' => 1];
    }

    /** @return array{checksum: string, operations: int} */
    private static function randomWorkload(int $intensity): array
    {
        $token = '';
        $iterations = 80 * $intensity;
        $key = 'hamster-server-benchmark-key';
        for ($pass = 0; $pass < $iterations; $pass++) {
            $bytes = random_bytes(32);
            $token = base64_encode($bytes) . '.' . hash_hmac('sha256', $bytes, $key);
        }

        return ['checksum' => hash('crc32b', $token), 'operations' => $iterations * 3];
    }

    /** @return array{checksum: string, operations: int} */
    private static function mixedWorkload(int $intensity): array
    {
        $hash = self::hashWorkload($intensity);
        $json = self::jsonWorkload($intensity);
        $arrays = self::arrayWorkload($intensity);
        $regex = self::regexWorkload($intensity);
        $checksum = hash('sha256', $hash['checksum'] . $json['checksum'] . $arrays['checksum'] . $regex['checksum']);

        return [
            'checksum' => substr($checksum, 0, 16),
            'operations' => $hash['operations'] + $json['operations'] + $arrays['operations'] + $regex['operations'],
        ];
    }
}
