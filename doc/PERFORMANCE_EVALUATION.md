# Đánh giá hiệu suất và độ ổn định

## Phạm vi

- Nhánh đánh giá: `dev`; kết quả soak test được giữ từ lần đánh giá connection pooling ban đầu.
- Runtime kiểm chứng: Apache 2.4.68 Win64, PHP 8.5.9 NTS FastCGI, 8 PHP worker.
- Thành phần liên quan: sinh cấu hình Apache/PHP, vòng đời worker và ứng dụng benchmark PHP trong `HamsterServer/www/`.
- Ngày đánh giá độc lập: 2026-09-08; cập nhật P1/P2 ngày 2026-09-10.

## Kết luận

Tái sử dụng kết nối FastCGI là tối ưu đúng hướng. `mod_proxy_fcgi` mặc định đóng kết nối sau mỗi request; bật `enablereuse=on` giảm chi phí TCP và số socket `TIME_WAIT`. Giới hạn `max` bằng số PHP worker cũng cần thiết để tránh Apache giữ nhiều kết nối hơn khả năng phục vụ của PHP.

Tuy nhiên, cấu hình hiện tại chưa đạt độ ổn định mong muốn khi kết hợp connection pooling với `PHP_FCGI_MAX_REQUESTS=1000`. Khi một PHP worker đạt giới hạn và thoát, Apache đôi lúc tái sử dụng socket vừa bị đóng, tạo HTTP 503. Đây là vấn đề cần xử lý trước khi coi tối ưu này sẵn sàng cho tải production.

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

Soak test trên server đóng gói:

| Cấu hình | Tải | Thông lượng | HTTP lỗi | FastCGI log |
|---|---:|---:|---:|---|
| `MaxRequests=1000` | 12.000 mixed, concurrency 32 | 1.863,70 req/s | 3 × 503 | `AH01067`, `AH01075`, Windows 10054 |
| `MaxRequests=1.000.000` | 12.000 mixed, concurrency 32 | 1.897,05 req/s | 0 | Không có lỗi mới |

Kết quả thứ hai cho thấy việc giảm/tắt recycle không làm suy giảm thông lượng trong phép thử này. Sai số 3/12.000 tương đương 0,025%; tỷ lệ nhỏ nhưng không thể chấp nhận nếu request có tác dụng phụ hoặc không được client tự động thử lại.

## Phát hiện kỹ thuật

### 1. Connection reuse xung đột với worker recycle — mức cao

`ApacheConfigWriter.FcgiReuse` sinh `enablereuse=on max=<Workers> ttl=15`, trong khi `ServerManager` luôn truyền `PHP_FCGI_MAX_REQUESTS`. `ttl` chỉ loại kết nối đã nhàn rỗi đủ lâu; nó không ngăn race condition khi worker đóng một kết nối đang được tái sử dụng liên tục.

### 2. Dashboard tính thông lượng gồm request lỗi — đã xử lý

Dashboard hiện tách thông lượng thành công và thông lượng đã thử, thống kê HTTP status/network error, đồng thời đưa latency của mọi request hoàn tất vào P50/P95/P99.

### 3. Chỉ số “PHP xử lý” chỉ đo workload — đã làm rõ

Tên trường, response header và nhãn UI đã đổi thành “Workload PHP” để phản ánh đúng phạm vi: chỉ đo phần tải có chủ đích, không đại diện cho tổng thời gian request.

### 4. Thiếu kiểm thử hồi quy cho pool — mức trung bình

Test hiện tại xác minh cú pháp Apache nhưng chưa kiểm tra `max` bám theo `Php.Workers`, chưa tạo tải qua lần recycle và chưa khẳng định không có HTTP 5xx.

## Đề xuất cải thiện

### Ưu tiên P0 — độ ổn định

1. Cho phép `Php.MaxRequests = 0` trong model, chuẩn hóa cấu hình và Settings UI. Dùng `0` cho Development khi bật connection reuse.
2. Chọn chính sách rõ ràng cho Production:
   - ưu tiên độ tin cậy: reuse bật, worker không tự recycle; theo dõi bộ nhớ và chủ động restart toàn PHP pool ở thời điểm an toàn;
   - cần recycle theo request: tắt reuse để tránh stale socket, chấp nhận thêm chi phí TCP.
3. Không xem một giá trị recycle rất lớn là cách khắc phục hoàn toàn; nó chỉ giảm tần suất race condition.
4. Bổ sung integration soak test với `MaxRequests` nhỏ để ép worker recycle, đếm riêng HTTP status và yêu cầu 0 lỗi.

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
