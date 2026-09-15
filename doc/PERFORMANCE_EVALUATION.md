# Đánh giá hiệu suất và độ ổn định

## Phạm vi

- Nhánh đánh giá: `dev`; kết quả soak test được giữ từ lần đánh giá connection pooling ban đầu.
- Runtime kiểm chứng: Apache 2.4.68 Win64, PHP 8.5.9 NTS FastCGI, 8 PHP worker.
- Thành phần liên quan: sinh cấu hình Apache/PHP, vòng đời worker và ứng dụng benchmark PHP trong `HamsterServer/www/`.
- Ngày đánh giá độc lập: 2026-09-08; cập nhật P1/P2 ngày 2026-09-10; xử lý P0 ngày 2026-09-15.

## Kết luận

Tái sử dụng kết nối FastCGI là tối ưu đúng hướng. `mod_proxy_fcgi` mặc định đóng kết nối sau mỗi request; bật `enablereuse=on` giảm chi phí TCP và số socket `TIME_WAIT`. Giới hạn `max` bằng số PHP worker cũng cần thiết để tránh Apache giữ nhiều kết nối hơn khả năng phục vụ của PHP.

Nguyên nhân mất ổn định đã được xác định và xử lý: xung đột nằm ở việc **đồng thời** bật connection pooling và cho worker tự recycle theo số request. Từ 2026-09-15 hai thứ này loại trừ nhau trong cấu hình sinh ra:

- `Php.MaxRequests = 0` (mặc định mới) — worker không tự recycle, và `ApacheConfigWriter.FcgiReuse` phát `enablereuse=on max=<Workers> ttl=15`.
- `Php.MaxRequests > 0` — người dùng chủ động chọn recycle, và cấu hình sinh ra **không** còn tham số reuse, tức trở về hành vi mặc định không pooling của Apache. Chậm hơn, nhưng không bao giờ gặp stale socket.

Trước đây `Normalize()` kẹp `MaxRequests` vào khoảng `1..1_000_000`, nên đặt `0` trong JSON bị ép thành `1` — recycle sau **mỗi** request, tức trường hợp xấu nhất của chính lỗi này. Giờ `0` là giá trị hợp lệ và là mặc định; config cũ còn mang đúng default cũ (`1000`) được nâng lên `0` một lần khi load.

Tài liệu tham khảo:

- [Apache mod_proxy_fcgi — connection reuse](https://httpd.apache.org/docs/2.4/mod/mod_proxy_fcgi.html)
- [Apache mod_proxy — worker pool, max và ttl](https://httpd.apache.org/docs/current/mod/mod_proxy.html)
- [PHP-FPM reference — giá trị 0 xử lý request không giới hạn](https://github.com/php/php-src/blob/master/sapi/fpm/www.conf.in)

## Kết quả kiểm chứng

| Phép kiểm tra | Kết quả |
|---|---|
| `dotnet build HamsterServer.slnx -c Release --no-restore` | Thành công, 0 cảnh báo/lỗi |
| `dotnet test HamsterServer.slnx -c Release --no-restore` | 51/51 test qua |
| `dotnet format HamsterServer.slnx --verify-no-changes --no-restore` | Thành công |
| Apache config test | `Syntax OK` |
| PHP lint 8.2, 8.3, 8.4, 8.5 | Toàn bộ file benchmark hợp lệ |
| JavaScript syntax check | Thành công |

Cập nhật 2026-09-15 (sau khi xử lý P0):

| Phép kiểm tra | Kết quả |
|---|---|
| `dotnet build HamsterServer.slnx` | Thành công, 0 cảnh báo/lỗi |
| `dotnet test --filter "TestCategory!=Integration&TestCategory!=Soak"` | 68/68 test qua |
| `dotnet test --filter "TestCategory=Integration"` | 5/5 test qua |
| `dotnet format --verify-no-changes` | Thành công |
| `httpd -t` với cấu hình **có** reuse (`MaxRequests=0`) | `Syntax OK` |
| `httpd -t` với cấu hình **không** reuse (`MaxRequests>0`) | `Syntax OK` |
| `dotnet test --filter "TestCategory=Soak"` | 5/5 test qua, 0 lỗi 5xx — xem bảng dưới |

Soak test trên server đóng gói:

| Cấu hình | Tải | Thông lượng | HTTP lỗi | FastCGI log |
|---|---:|---:|---:|---|
| `MaxRequests=1000` | 12.000 mixed, concurrency 32 | 1.863,70 req/s | 3 × 503 | `AH01067`, `AH01075`, Windows 10054 |
| `MaxRequests=1.000.000` | 12.000 mixed, concurrency 32 | 1.897,05 req/s | 0 | Không có lỗi mới |

Kết quả thứ hai cho thấy việc giảm/tắt recycle không làm suy giảm thông lượng trong phép thử này. Sai số 3/12.000 tương đương 0,025%; tỷ lệ nhỏ nhưng không thể chấp nhận nếu request có tác dụng phụ hoặc không được client tự động thử lại.

## Phát hiện kỹ thuật

### 1. Connection reuse xung đột với worker recycle — đã xử lý

`ApacheConfigWriter.FcgiReuse` sinh `enablereuse=on max=<Workers> ttl=15`, trong khi `ServerManager` luôn truyền `PHP_FCGI_MAX_REQUESTS`. `ttl` chỉ loại kết nối đã nhàn rỗi đủ lâu; nó không ngăn race condition khi worker đóng một kết nối đang được tái sử dụng liên tục.

Đã xử lý bằng cách cho hai cơ chế loại trừ nhau: `FcgiReuse` chỉ phát tham số pooling khi `Php.MaxRequests == 0`. Đồng thời phát hiện thêm một lỗi làm vấn đề nặng hơn — `Normalize()` kẹp `MaxRequests` vào `1..1_000_000`, nên đặt `0` (ý nghĩa "không giới hạn" của PHP) bị ép thành `1`, tức recycle sau **mỗi** request trong khi pooling vẫn bật. Nay `0` là giá trị hợp lệ, là mặc định, và config cũ mang đúng default cũ `1000` được nâng lên `0` một lần.

### 2. Dashboard tính thông lượng gồm request lỗi — đã xử lý

Dashboard hiện tách thông lượng thành công và thông lượng đã thử, thống kê HTTP status/network error, đồng thời đưa latency của mọi request hoàn tất vào P50/P95/P99.

### 3. Chỉ số “PHP xử lý” chỉ đo workload — đã làm rõ

Tên trường, response header và nhãn UI đã đổi thành “Workload PHP” để phản ánh đúng phạm vi: chỉ đo phần tải có chủ đích, không đại diện cho tổng thời gian request.

### 4. Thiếu kiểm thử hồi quy cho pool — đã xử lý một phần

Test hiện tại xác minh cú pháp Apache nhưng chưa kiểm tra `max` bám theo `Php.Workers`, chưa tạo tải qua lần recycle và chưa khẳng định không có HTTP 5xx.

Đã bổ sung trong `HamsterServer.Tests`:

- `WriteAllVHosts_PinsFcgiPoolSizeToWorkerCount` — `max` bám đúng `Php.Workers` (4/8/32).
- `WriteAllVHosts_EnablesFcgiReuseWhenWorkersNeverRecycle` — cả vhost mặc định và vhost custom đều có pooling khi `MaxRequests == 0`.
- `WriteAllVHosts_OmitsFcgiReuseWhenWorkerRecyclingIsEnabled` — không còn `enablereuse` khi recycle bật, và directive không để lại dấu cách thừa.
- `BundledApache_AcceptsConfigurationWithWorkerRecycling` — `httpd -t` chấp nhận cả biến thể không pooling.
- `Load_NormalizesMaxRequestsWithoutClampingZeroUp`, `Load_UpgradesLegacyDefaultMaxRequestsToNeverRecycle`, `NewConfiguration_DefaultsToNeverRecyclingWorkers` — hành vi chuẩn hóa và migrate.

Soak test tạo tải thật qua nhiều chu kỳ recycle cũng đã có — xem mục 5.

### 5. Soak test tự động — đã bổ sung

`HamsterServer.Tests/FastCgiSoakTests.cs` dựng nguyên bộ Apache + php-cgi từ thư mục output của app, bắn 12.000 request qua `SocketsHttpHandler` ở concurrency 32 và đếm riêng 5xx với lỗi transport. Đặt trong `[TestCategory("Soak")]` nên **không** chạy cùng `dotnet test` mặc định:

```powershell
dotnet test HamsterServer.slnx --filter "TestCategory=Soak"
```

Kết quả ngày 2026-09-15 (4 worker, loopback, cùng máy đánh giá):

| Cấu hình | Tải | Thông lượng | HTTP 5xx | Lỗi transport |
|---|---:|---:|---:|---:|
| `MaxRequests=0` (mặc định mới, có pooling) | 12.000, concurrency 32 | 16.626 req/s | **0** | 0 |
| `MaxRequests=200` (recycle bật, không pooling) | 12.000, concurrency 32 | 5.502 req/s | **0** | 0 |

Hai điểm rút ra:

1. Cấu hình `MaxRequests=200` với 4 worker buộc mỗi worker recycle khoảng 15 lần trong một lần chạy — ép race condition mạnh hơn nhiều so với `MaxRequests=1000` đã tạo ra 3 × 503 trước đây — mà vẫn không có lỗi nào. Việc bỏ pooling khi recycle bật là đủ để đóng race.
2. Chênh lệch thông lượng khoảng 3× cho thấy vì sao `0` phải là mặc định, chứ không phải chỉ là một lựa chọn.

Lưu ý khi chạy: `ServerManager.Dispose()` kill mọi `httpd`/`php-cgi` thuộc cùng installation, nên phải đóng app trước. Nếu chạy lặp lại để so sánh, `php-cgi` được restart giữa các test vì mỗi test dựng stack riêng.

### 6. Chủ động restart pool theo bộ nhớ — đã bổ sung

Đây là đối trọng còn thiếu của `MaxRequests = 0`: khi worker không bao giờ tự recycle thì không còn cơ chế nào thu hồi bộ nhớ rò rỉ. Việc dọn dẹp chuyển từ **đếm số request** sang **đo bộ nhớ thật**.

Cấu hình: `php.memoryRestartThreshold` (MB, mặc định `1024`, `0` = tắt), chỉnh trong **Settings → PHP → FastCGI → "Restart pool above (MB)"**.

Cơ chế trong `ServerManager`:

- Quét bằng `System.Threading.Timer`, cộng `WorkingSet64` của mọi tiến trình `php-cgi` thuộc installation này (`GetPhpPoolWorkingSetBytes`). Chu kỳ **thích ứng**: 60 giây khi bộ nhớ còn xa ngưỡng, rút xuống 15 giây khi đã đạt 80% ngưỡng.
- Khi vượt ngưỡng, **chờ pool rảnh rồi mới restart** — không cắt ngang request nào. Nếu sau 5 phút vẫn bận thì bỏ qua, chu kỳ sau xử lý tiếp.
- Trong lúc chờ, **poll mỗi 250 ms** chứ không phải vài giây. Đây là điểm mấu chốt: máy có tải đều vẫn có khe hở giữa các request, và poll 5 giây đi thẳng qua chúng. Đo với tải liên tục 1 request/200 ms, bản poll dày vẫn bắt được khe hở và recycle với **0 request lỗi**; bản poll 5 giây sẽ hết cả cửa sổ chờ mà không làm được gì.
- Bảo vệ: nghỉ 10 phút sau mỗi lần restart thành công; tối đa 3 lần/giờ, vượt quá thì watchdog tự dừng và ghi log hướng dẫn nâng ngưỡng — tránh vòng lặp restart khi ngưỡng đặt thấp hơn mức pool dùng lúc nghỉ. **Chỉ lần restart thật sự mới bị tính vào hạn mức này**: bản đầu tính ngay khi phát hiện vượt ngưỡng, nên một pool chỉ đơn giản là đang bận sẽ dùng hết hạn mức mà chưa recycle lần nào, rồi watchdog tự tắt trong một giờ — đúng ngược với mục đích của nó.
- Sau 3 chu kỳ liên tiếp không bắt được khe hở nào, log nâng từ INFO lên WARN, nêu rõ mức bộ nhớ hiện tại và khuyên người dùng tự restart PHP hoặc nâng ngưỡng.

**Cách xác định "rảnh" — một sai lầm đã bị test bắt được.** Ban đầu cơ chế đếm số kết nối TCP `ESTABLISHED` tới cổng FastCGI và coi `0` là rảnh. Test `MemoryWatchdogReadings_ReflectTheRunningPool` cho thấy pool **không bao giờ** trở về rảnh: `ttl=15` của `mod_proxy` chỉ loại kết nối cũ **khi Apache lấy ra dùng lại**, nó không có luồng nền đóng kết nối, nên với `enablereuse=on` các socket nằm `ESTABLISHED` vô thời hạn. Một pool đã từng phục vụ traffic sẽ vĩnh viễn trông như đang bận và watchdog không bao giờ chạy.

Cơ chế hiện tại đọc `BusyWorkers` từ `mod_status` — chính xác vì đó là số worker Apache đang thực sự xử lý request:

```
GET http://127.0.0.1:<port>/server-status?auto
```

`WriteHttpdConf` vì vậy sinh thêm block `<Location /server-status>` với `SetHandler server-status` và `Require ip 127.0.0.1` / `::1`. Endpoint **luôn chỉ mở cho loopback**, kể cả khi `allowLanAccess` mở mọi thứ khác ra mạng. `ExtendedStatus` cố ý để tắt: `BusyWorkers` đã có trong output cơ bản và không tốn chi phí.

Kiểm chứng (ngày 2026-09-15, 4 worker):

| Phép kiểm tra | Kết quả |
|---|---|
| Đọc working set của pool | 177-178 MB / 4 worker (~44 MB mỗi worker) |
| Đọc trạng thái khi đang có request | bận — đúng |
| Đọc trạng thái sau khi ngừng traffic | rảnh trong vòng vài giây — đúng |
| Restart end-to-end với ngưỡng 128 MB | PID `11060` → `25268` sau 61 giây, log `Recycling the PHP pool to release 180 MB` |
| Restart **khi đang có tải liên tục** (1 req/200 ms) | PID `12312` → `16432`; 402 request trong 84 giây, **0 lỗi 5xx, 0 lỗi transport** |

## Đề xuất cải thiện

### Ưu tiên P0 — độ ổn định

1. ~~Cho phép `Php.MaxRequests = 0` trong model, chuẩn hóa cấu hình và Settings UI. Dùng `0` cho Development khi bật connection reuse.~~ — **đã xử lý**: `PhpSettings.MaxRequests` mặc định `0`, `Normalize()` không còn kẹp `0` lên `1`, `nudMaxRequests.Minimum` về `0`, nút "Apply mode defaults" đặt `0` cho cả hai mode.
2. ~~Chọn chính sách rõ ràng cho Production.~~ — **đã xử lý trọn vẹn**: `FcgiReuse` gắn với `MaxRequests`, nên hai nhánh trong đề xuất gốc được chọn bằng đúng một thiết lập; và nhánh "theo dõi bộ nhớ, chủ động restart toàn PHP pool ở thời điểm an toàn" nay đã có — xem mục 6.
3. Không xem một giá trị recycle rất lớn là cách khắc phục hoàn toàn; nó chỉ giảm tần suất race condition. — vẫn đúng, và vì vậy giải pháp chọn `0` chứ không phải một giá trị lớn.
4. ~~Bổ sung integration soak test với `MaxRequests` nhỏ để ép worker recycle, đếm riêng HTTP status và yêu cầu 0 lỗi.~~ — **đã xử lý**: `FastCgiSoakTests`, xem "Phát hiện kỹ thuật" mục 4 và 5.

### Ưu tiên P1 — độ chính xác benchmark

1. Đã hiển thị riêng `attempted req/s`, `successful req/s`, status-code breakdown và network error.
2. Đã ghi latency của mọi request hoàn tất và báo P50/P95/P99 trên toàn bộ tập này.
3. Đã đổi nhãn và schema thành “Workload PHP”.
4. Xuất thêm phiên bản Apache/PHP, OPcache/JIT, worker count, MaxRequests, CPU và RAM vào JSON để kết quả có thể tái lập.
5. Dùng công cụ tải ngoài trình duyệt như ApacheBench, `wrk` hoặc k6 cho capacity test. Dashboard trình duyệt phù hợp với kiểm tra nhanh, nhưng browser có thể giới hạn số kết nối HTTP/1.1 thực tế thấp hơn concurrency đã nhập.

### Ưu tiên P2 — ma trận hiệu suất

- So sánh tối thiểu ba lần và dùng median cho mỗi cấu hình.
- Kiểm tra concurrency `1, 8, 32, 64, 128`, cả HTTP/1.1 và HTTPS/HTTP/2.
- Thử tải đồng thời trên nhiều virtual host để xác nhận pool vẫn bị giới hạn đúng.
- Theo dõi CPU, working set của `httpd/php-cgi`, số kết nối `Established/TIME_WAIT`, HTTP 5xx và P99.
- Chạy soak test dài hơn tổng `Workers × MaxRequests` ít nhất 3–5 lần để buộc nhiều chu kỳ recycle.

## Tiêu chí chấp nhận đề xuất

- Không có HTTP 5xx hoặc exception trong soak test có chủ đích ép recycle.
- Không tăng dần `TIME_WAIT`, working set hoặc số process ngoài giới hạn cấu hình.
- P95/P99 không suy giảm quá 10% qua ba lần chạy cùng điều kiện.
- Báo cáo benchmark phân biệt rõ attempt, success, failure và thời gian workload/server/client.
