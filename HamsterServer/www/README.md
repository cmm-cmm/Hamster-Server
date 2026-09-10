# Hamster Server Performance Lab

Ứng dụng PHP thuần hiển thị thông tin server, PHP, toàn bộ extension đang nạp và đo hiệu suất Apache/FastCGI/PHP của Hamster Server. Mỗi workload có một tab chuyên biệt. Project không dùng Composer, framework, database hoặc dịch vụ bên ngoài.

## Sử dụng

1. Build và chạy Hamster Server, sau đó bật Apache và PHP.
2. Mở `http://localhost/` hoặc địa chỉ/port đã cấu hình.
3. Chọn workload, tổng request và mức đồng thời rồi nhấn **Bắt đầu benchmark**.
4. Chạy mỗi cấu hình ít nhất ba lần và so sánh median; đóng ứng dụng nền nếu cần kết quả ổn định.

Các phép thử gồm:

- **Nền tảng:** HTTP/PHP baseline, PHP bootstrap và response payload.
- **CPU:** SHA-256, toán học số thực, random/entropy và workload hỗn hợp.
- **Dữ liệu:** JSON, PHP serialize, mảng, regex, chuỗi/template và ngày giờ.
- **I/O:** zlib, memory stream, temporary file và tác vụ blocking mô phỏng API/database.

Nút **Xuất JSON** lưu kết quả tóm tắt để so sánh giữa phiên bản PHP hoặc cấu hình Hamster Server. Bài test ổ đĩa chỉ sử dụng file tạm riêng cho request và luôn cố gắng xóa file sau khi hoàn tất.

## Diễn giải

- **Request thành công/giây** là thông lượng chính; **RPS đã thử** cho biết tổng tốc độ phản hồi kể cả lỗi.
- **P95/P99** bao gồm mọi request đã hoàn tất, kể cả HTTP/network error, để không che khuất tail latency.
- **Workload PHP TB** lấy từ header do endpoint trả về và chỉ đo phần workload có chủ đích, không bao gồm thời gian mạng/trình duyệt.
- **HTTP status** tách số phản hồi theo mã; lỗi không có response được ghi là `Network`.
- Baseline và payload giúp tách overhead web server khỏi workload ứng dụng.

Không chạy mức đồng thời cao trên máy chủ công khai hoặc trong lúc các website quan trọng đang hoạt động. Các endpoint giới hạn intensity và payload nhưng benchmark vẫn có chủ đích tạo tải CPU/mạng.
