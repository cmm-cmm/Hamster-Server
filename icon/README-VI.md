# Hamster Server — Concept 01 / Hamster Stack

Bộ nhận diện dựng lại bằng vector từ mẫu 01 đã chọn: đầu hamster màu cam, hai tầng máy chủ và đèn trạng thái. Giữ tinh thần và bố cục phác thảo; đường nét và chữ được chuẩn hóa thành màu phẳng để xuất sắc nét, có thể khác nhẹ ảnh ý tưởng.

## Dùng nhanh

- Website nền sáng: `svg/hamster-server-c01-horizontal-color.svg`.
- Website nền tối: `svg/hamster-server-c01-horizontal-reverse.svg`.
- Icon ứng dụng: `icons/hamster-server.ico` (16/20/24/32/40/48/64/128/256 px).
- Biểu tượng vector: `svg/hamster-server-c01-icon-color.svg`.
- Website: dùng các file trong `favicon/`.

## Bố cục và biến thể

`horizontal`: biểu tượng và chữ nằm ngang; `stacked`: biểu tượng trên chữ; `icon`: biểu tượng riêng.

Mỗi bố cục có `color` (nền sáng), `reverse` (nền tối), `black`, `white`, `orange` (một màu), `dark-bg` (nền đen than cố định). SVG và PNG không có hậu tố `dark-bg` đều có nền trong suốt. Phần lông trắng trong bản màu là màu kem cố định, không phải vùng trong suốt. Bản đơn sắc dùng vùng rỗng trong suốt.

PNG logo ngang rộng 4096 px, logo dọc 2048 px, biểu tượng 1024 × 1024 px. Các PNG trong `icons/` có kích thước thực ghi trong tên. ICO có đủ 9 kích thước, nền trong suốt. Favicon nhỏ bỏ chấm sáng mắt để giảm chi tiết.

## Màu

| Vai trò | HEX |
|---|---|
| Cam chính theo website | #D9711A |
| Khung máy chủ, mắt, mũi, chữ | #17191E |
| Má, tai, nền sáng gợi ý | #F7F5F1 |
| Đèn trạng thái xanh | #22A06B |
| Trắng | #FFFFFF |

## Quy tắc sử dụng

Giữ nguyên tỷ lệ; chừa khoảng trống ít nhất bằng chiều cao một tầng máy chủ quanh logo. Logo có chữ nên dùng từ 180 px chiều rộng. Icon đầy đủ từ 32 px; dùng bản tối giản trong `icons/` ở 16–24 px. Dùng reverse/white trên nền tối, color/black trên nền sáng. Không kéo giãn, đổi thứ tự màu, thêm hiệu ứng nổi hoặc bóng đổ.

Tất cả SVG là vector thật. Chữ đã chuyển thành path, không cần cài font, không chứa ảnh raster nhúng. Chữ được dựng với DejaVu Sans Bold; giấy phép đi kèm trong `licenses/`. Có thể mở SVG bằng Inkscape, Illustrator hoặc trình duyệt.

`docs/integration.html` là ví dụ tích hợp logo và favicon. Đường dẫn favicon/webmanifest giả định file ở thư mục gốc website; hãy sửa đường dẫn nếu đặt trong thư mục con. Android icons là loại `any`, không phải maskable. Chỉ có ảnh chia sẻ xã hội và file `dark-bg` chứa nền cố định.
