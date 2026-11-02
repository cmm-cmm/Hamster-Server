# Hamster Server — Danh mục icon và logo

Tài liệu này dành cho người thiết kế. Nó liệt kê mọi hình ảnh mà ứng dụng đang hiển thị: logo, icon ứng dụng, chấm trạng thái và các glyph trên toolbar/menu/nút. Mỗi mục ghi rõ hình đang được dựng từ đâu, hiện ở đâu, màu gì và cần kích thước bao nhiêu. Tên định danh (`tsbStartAll`, `UiTheme.Glyph.Play`…) được giữ nguyên tiếng Anh để tra trong code.

Phiên bản ứng dụng: **1.4.1**. Nguồn sự thật cho màu và glyph: `HamsterServer/UiTheme.cs`. Toàn bộ việc gán icon cho cửa sổ chính nằm trong `HamsterServerForm.InitToolbarIcons`.

---

## 1. Nguyên tắc của hệ thống icon hiện tại

| Nguyên tắc | Chi tiết |
|---|---|
| **Giao diện Windows gốc** | Ứng dụng dùng control chuẩn của WinForms (`MenuStrip`, `ToolStrip`, `ListView`…), không có control tự vẽ kiểu "card". Icon chỉ là một `Image` gắn vào control chuẩn. |
| **Bộ SVG riêng, không dùng font** | Mọi icon thao tác là **dữ liệu path SVG** trên lưới 24 px trong `UiTheme.Glyph` (lề 2 px, nét 1,5 px, đầu nét bo tròn, không tô kín). `SvgPath.Parse` đổi path sang `GraphicsPath`, rồi `UiTheme.CreateGlyph` vẽ nét với màu theo nghĩa. Không còn phụ thuộc font Segoe Fluent Icons / MDL2, nên icon giống nhau trên mọi máy Windows. Repo **không có file icon rời**: bản vẽ nằm ngay trong code. |
| **Kiểm tra** | `IconTests` đảm bảo mọi glyph đọc được, nằm trong lề an toàn và không bị cắt ở 16–48 px. Đặt `HAMSTER_GLYPH_SHEET=<đường dẫn>.png` khi chạy test để xuất bảng xem toàn bộ icon. |
| **Kích thước** | Glyph và chấm trạng thái được dựng ở `UiTheme.IconSize`: **16 px ở 100 %**, nhân theo DPI màn hình chính rồi làm tròn về số chẵn (20 px ở 125 %, 24 px ở 150 %, 32 px ở 200 %). Nét = 1,5 × (kích thước / 24), tối thiểu 1,25 px để icon 16 px không bị nhạt. |
| **Màu mang nghĩa, không trang trí** | Xanh lá = chạy/bắt đầu, đỏ = dừng/lỗi/xoá, hổ phách = đang chuyển trạng thái hoặc có rủi ro, xanh dương = thao tác trung tính, xám = cấu hình/không hoạt động. |
| **Menu cấp cao không có icon** | `File`, `Service`, `Tools`, `Help` cố ý không có icon, đúng quy ước thanh menu của Windows. |
| **Nút hộp thoại không có icon** | `OK`, `Cancel`, `Close`, `Save`, `Browse…` để trống. Chỉ các nút *hành động* mới có glyph. |

---

## 2. Logo và icon ứng dụng

Logo hiện tại là **Concept 01 — "Hamster Stack"**: đầu hamster màu cam đặt trên hai tầng máy chủ màu than, mỗi tầng có một đèn trạng thái (trên xanh lá, dưới cam). Bộ nhận diện gốc nằm nguyên vẹn trong `icon/` ở gốc repo (SVG vector thật, PNG, ICO, favicon, ảnh xem trước). `icon/SHA256SUMS.txt` kiểm được từng file, và `.gitattributes` giữ `icon/**` đúng từng byte. Quy tắc dùng logo: `icon/README-VI.md`.

| Vai trò | Hex | Trong code |
|---|---|---|
| Cam chính | `#D9711A` | `--orange` của website |
| Than: thân máy chủ, mắt, mũi, chữ | `#17191E` | `UiTheme.BrandInk` |
| Ivory: má, tai trong, nền sáng | `#F7F5F1` | `UiTheme.BrandIvory`, nền sáng của website |
| Đèn xanh | `#22A06B` | — |

### 2.1 File trong chương trình

| File | Lấy từ | Dùng ở đâu |
|---|---|---|
| `HamsterServer/server.ico` | `icon/icons/hamster-server.ico`: 9 frame PNG 16/20/24/32/40/48/64/128/256 px, 26 KB; 16–24 px là bản rút gọn | `<ApplicationIcon>` của exe, icon cửa sổ chính (bản sao nhúng trong `HamsterServerForm.resx` — **thay ICO thì phải thay cả chỗ này**), khay hệ thống, logo About |
| `HamsterServer/www/favicon.ico` | `icon/favicon/favicon.ico` | Dashboard `www/` |
| `HamsterServer/www/assets/logo.svg` | `icon/svg/hamster-server-c01-icon-reverse.svg` | Logo trên header dashboard (nền tối) |
| `site/favicon.*`, `site/apple-touch-icon.png`, `site/assets/favicon-*.png`, `site/assets/icon-*.png` | `icon/favicon/` | Website |
| `site/assets/brand/*.svg` | `icon/svg/` (icon và horizontal, bản `color` và `reverse`) | Header, footer, thẻ tải về, demo của website |
| `site/assets/og-image.png` | Dựng từ logo ngang `reverse` + ảnh cửa sổ chính | Ảnh chia sẻ mạng xã hội |

### 2.2 Logo xuất hiện ở đâu

| Vị trí | Kích thước hiển thị | Cách lấy hình |
|---|---|---|
| Icon file exe (Explorer, Start menu, shortcut) | 16 → 256 px | Windows tự chọn frame từ `server.ico` |
| Thanh tiêu đề cửa sổ chính | 16 px (×DPI) | Bản màu **có chấm trạng thái**, xem §3 |
| Nút trên taskbar, khay hệ thống | 16–48 px (×DPI) | Có chấm trạng thái; bản **reverse** khi taskbar tối, xem §3 |
| Hộp thoại **About** (`picLogo`) | 64 px logic, lấy frame gấp đôi rồi thu nhỏ | `new Icon(appIcon, px, px).ToBitmap()`, dùng icon gốc **không** có chấm trạng thái |
| Các hộp thoại phụ | — | `ShowIcon = false`, nên không hiện logo |
| Dashboard `www/index.php` | favicon + 40 px trên header | `favicon.ico`, `assets/logo.svg` |

### 2.3 Nền tối: bản reverse

Thanh máy chủ màu than của bản màu gần như biến mất trên nền tối. Vì vậy bộ nhận diện có bản `reverse` với thanh màu ivory, còn mọi chi tiết khác giữ nguyên.

- **Website:** mọi logo đặt trong `<picture>`. Mặc định (nền tối) dùng `reverse`, `prefers-color-scheme: light` dùng `color`.
- **Chương trình:** `server.ico` chỉ có bản màu, nên `UiTheme.ToReverseLogo` tự đổi các điểm ảnh màu than ở dải dưới (từ 58 % chiều cao, ngay dưới mũi) sang ivory và giữ nguyên alpha. `ToReverseLogo_TurnsOnlyTheServerBarsIvory` kiểm ở 16–64 px rằng chỉ thanh máy chủ đổi màu: mắt, mũi và đèn LED giữ nguyên.

---

## 3. Chấm trạng thái trên icon (overlay)

`HamsterServerForm.BuildStatusIcon` dựng lại icon ở đúng các frame của `server.ico` (16/20/24/32/40/48/64 px) mỗi khi trạng thái tổng thay đổi, nên ở 125–200 % DPI Windows không phải co giãn ảnh:

- Đường kính chấm: `max(5, 38 % × kích thước)`, đặt ở **góc dưới-phải**, cách mép 1 px. Vị trí này trùng đèn LED cam của tầng máy chủ dưới, nên chấm thay chỗ cho đèn đó.
- Viền: vòng **trắng** dày 2 px quanh chấm (halo) để tách chấm khỏi logo.
- Áp dụng cho: icon cửa sổ, nút taskbar, icon khay hệ thống.
- **Theo màu taskbar.** Thanh tiêu đề luôn sáng nên giữ bản màu. Khay và nút taskbar (`WM_SETICON`, `ICON_BIG`) dùng bản reverse khi `HKCU\…\Themes\Personalize\SystemUsesLightTheme` = 0, kể cả khi thiếu giá trị này. Đổi theme lúc đang chạy thì `SystemEvents.UserPreferenceChanged` dựng lại icon.

| Trạng thái | Điều kiện | Màu | Hex |
|---|---|---|---|
| Running | Apache **và** PHP đều đang chạy | `TaskbarDotRunning` | `#27AE60` |
| Transition | Đang khởi động/dừng, hoặc chỉ một dịch vụ chạy | `TaskbarDotTransition` | `#F39C12` |
| Error | Có dịch vụ ở trạng thái lỗi | `TaskbarDotError` | `#E74C3C` |
| Stopped | Cả hai đều dừng | `TaskbarDotStopped` | `#95A5A6` |

---

## 4. Bảng màu ngữ nghĩa (`UiTheme.cs`)

| Token | Hex | Ý nghĩa / nơi dùng |
|---|---|---|
| `StatusRunning` | `#008000` | Bắt đầu, đang chạy, thêm mới, HTTPS |
| `StatusTransition` | `#B45F00` | Restart, đang chuyển trạng thái, thư mục, thao tác có rủi ro |
| `StatusError` | `#C00000` | Dừng, xoá, thoát, lỗi |
| `StatusStopped` | `SystemColors.GrayText` | Đã dừng, mục bị tắt |
| `AccentAction` | `#005AB4` | Thao tác trung tính (vhost, port, tải PHP, About…) |
| `AccentHttp` | `#005AB4` | HTTP, trình duyệt, proxy |
| `AccentHttps` | `#008000` | Port HTTPS |
| `AccentNeutral` | `#5A5A5A` | Cài đặt, xoá log, thu nhỏ |
| `LogApache` | `#9C2777` | Mọi thứ thuộc Apache (menu, viewer cấu hình, log). Tách khỏi `#005AB4` từ bản này, chọn bằng đo đạc: tương phản 7,1:1 và khác biệt ΔE ≥ 13 với mọi token ở cả thị giác thường lẫn mù màu đỏ/lục/lam. |
| `LogPhp` | `#7030A0` | Mọi thứ thuộc PHP (menu, viewer cấu hình, log) |
| `FieldError` | `#FFEBEB` | Nền ô nhập sai |
| `FieldWarning` | `#FFF8DC` | Nền ô nhập có cảnh báo |

Khi một dòng `ListView` đang được chọn, `UiTheme.OnHighlight` làm sáng ba màu trạng thái để đọc được trên nền `SystemColors.Highlight`. Bộ icon mới cần giữ được độ tương phản trên cả nền trắng lẫn nền xanh chọn dòng.

---

## 5. Danh mục glyph (`UiTheme.Glyph`)

Mỗi glyph là một chuỗi path SVG trong `UiTheme.Glyph`. Cột "Thay cho" là codepoint Segoe Fluent mà glyph đó thay thế, giữ lại để tra lịch sử. Cột "Số chỗ dùng" đếm số lần gán trong code.

| Tên trong code | Thay cho | Ý nghĩa | Số chỗ dùng |
|---|---|---|---|
| `Play` | `U+E768` | Start | 6 |
| `Stop` | `U+E71A` | Stop | 6 |
| `Refresh` | `U+E72C` | Restart / làm mới / cập nhật | 5 |
| `Settings` | `U+E713` | Cài đặt, cấu hình dịch vụ | 4 |
| `World` | `U+E774` | Trình duyệt, virtual host đang phục vụ | 4 |
| `Folder` | `U+E8B7` | Mở thư mục | 4 |
| `Server` | `U+E839` | Apache (submenu) | 1 |
| `Code` | `U+E943` | PHP (submenu) | 1 |
| `Page` | `U+E7C3` | Xem file cấu hình | 2 |
| `Link` | `U+E71B` | Virtual Hosts, vhost dạng proxy | 3 |
| `Ports` | `U+E8AB` | Ports (hai mũi tên ngược chiều) | 2 |
| `Download` | `U+E896` | Trình quản lý phiên bản PHP | 2 |
| `Add` | `U+E710` | Thêm / cài đặt | 3 |
| `Edit` | `U+E70F` | Sửa | 2 |
| `Delete` | `U+E74D` | Xoá | 3 |
| `Lock` | `U+E72E` | Port HTTPS | 2 |
| `Unlock` | `U+E785` | Port HTTP | 2 |
| `Clear` | `U+E894` | Xoá log | 1 |
| `Minimize` | `U+E921` | Ẩn xuống khay | 1 |
| `Exit` | `U+E7E8` | Thoát | 1 |
| `Info` | `U+E946` | About | 1 |
| `Accept` | `U+E8FB` | Bật extension cho vhost | 1 |
| `Star` | `U+E734` | Bộ extension khuyến nghị | 1 |
| `SelectAll` | `U+E8B3` | Chọn tất cả | 1 |
| `ClearSelection` | `U+E8E6` | Bỏ chọn tất cả | 1 |
| `Wrench` | `U+E90F` | Áp dụng mặc định theo chế độ | 1 |
| `Warning` | `U+E7BA` | Cảnh báo | **0 — đã khai báo nhưng chưa dùng** |

---

## 6. Kiểm kê theo từng màn hình

Định dạng: **Control** · nhãn hiển thị · glyph · màu.

### 6.1 Cửa sổ chính — toolbar

| Control | Nhãn | Glyph | Màu |
|---|---|---|---|
| `tsbStartAll` | Start All | Play | StatusRunning |
| `tsbStopAll` | Stop All | Stop | StatusError |
| `tsbRestart` | Restart | Refresh | StatusTransition |
| `tsbSettings` | Settings | Settings | AccentNeutral |
| `tsbVHosts` | Virtual Hosts | Link | AccentAction |
| `tsbPorts` | Ports | Ports | AccentAction |
| `tsbOpenWww` | Document Root | Folder | StatusTransition |
| `tsbBrowser` | Browser | World | AccentHttp |

### 6.2 Cửa sổ chính — menu

| Menu | Control | Nhãn | Glyph | Màu |
|---|---|---|---|---|
| File | `mnuFileOpenWww` | Open Document Root | Folder | StatusTransition |
| File | `mnuFileOpenLogs` | Open Log Folder | Folder | StatusTransition |
| File | `mnuFileHide` | Hide to Tray | Minimize | AccentNeutral |
| File | `mnuFileExit` | Exit | Exit | StatusError |
| Service | `mnuSvcStartAll` | Start All | Play | StatusRunning |
| Service | `mnuSvcStopAll` | Stop All | Stop | StatusError |
| Service | `mnuSvcRestartAll` | Restart All | Refresh | StatusTransition |
| Service | `mnuSvcApache` (submenu) | Apache | Server | LogApache |
| Service › Apache | `mnuSvcApacheStart` / `Stop` | Start / Stop | Play / Stop | StatusRunning / StatusError |
| Service | `mnuSvcPhp` (submenu) | PHP-CGI | Code | LogPhp |
| Service › PHP-CGI | `mnuSvcPhpStart` / `Stop` | Start / Stop | Play / Stop | StatusRunning / StatusError |
| Tools | `mnuToolsSettings` | Settings… | Settings | AccentNeutral |
| Tools | `mnuToolsPhpVersions` | PHP Version Manager… | Download | AccentAction |
| Tools | `mnuToolsVHosts` | Virtual Hosts… | Link | AccentAction |
| Tools | `mnuToolsPorts` | Ports… | Ports | AccentAction |
| Tools | `mnuToolsApacheConfig` | Apache Configuration… | Page | LogApache |
| Tools | `mnuToolsPhpConfig` | PHP Configuration… | Page | LogPhp |
| Tools | `mnuToolsBrowser` | Open in Browser | World | AccentHttp |
| Help | `mnuHelpAbout` | About Hamster Server | Info | AccentAction |

### 6.3 Cửa sổ chính — danh sách Services

| Control | Nhãn | Glyph | Màu |
|---|---|---|---|
| `btnSvcStart` + menu chuột phải `ctxSvcStart` | Start | Play | StatusRunning |
| `btnSvcStop` + `ctxSvcStop` | Stop | Stop | StatusError |
| `btnSvcConfig` + `ctxSvcConfig` | Configure… / Configure Apache… / Configure PHP… | Settings | AccentNeutral |
| `btnPhpVersions` | Manage… | Download | AccentAction |
| Icon đầu dòng (`_statusIcons`) | Stopped / Running / Starting·Stopping / Error | chấm tròn | StatusStopped / StatusRunning / StatusTransition / StatusError |

### 6.4 Cửa sổ chính — khung Log và thanh trạng thái

| Control | Nhãn | Glyph | Màu |
|---|---|---|---|
| `btnClearLog` | Clear | Clear | AccentNeutral |
| `btnOpenLog` | Open Folder | Folder | StatusTransition |
| `lblGlobalStatus` | Running / Partially running / Stopped / Error | chấm tròn | theo `UiTheme.StatusColor` |

### 6.5 Menu khay hệ thống (tray)

| Control | Nhãn | Glyph | Màu |
|---|---|---|---|
| `_trayShowHide` | Show / Hide | Minimize | AccentNeutral |
| `_trayStartAll` | Start All | Play | StatusRunning |
| `_trayStopAll` | Stop All | Stop | StatusError |
| `_trayBrowser` | Open Browser | World | AccentHttp |
| `_trayExit` | Exit | Exit | StatusError |

### 6.6 Hộp thoại phụ

| Hộp thoại | Control | Nhãn | Glyph | Màu |
|---|---|---|---|---|
| PHP Version Manager | `btnRefresh` | Check for updates | Refresh | AccentAction |
| | `btnInstall` | Install | Add | StatusRunning |
| | `btnUpdate` | Update | Download | StatusTransition |
| | `btnUse` | Use this version | Accept | StatusRunning |
| | `btnCopyExtensions` | Copy extensions | Code | LogPhp |
| | `btnDelete` | Remove | Delete | StatusError |
| | `btnCleanup` | Remove old patches | Clear | StatusError |
| | `btnUpdateActive` (vùng tóm tắt) | Update to x.y.z / Switch to x.y.z | Download | StatusTransition |
| | Icon dòng | Đang dùng · đang dùng nhưng có bản mới · đã cài · có thể cài | chấm · Download · chấm · Download | StatusRunning · StatusTransition · StatusStopped · AccentAction |
| Cửa sổ chính | `lblStatusPhpUpdate` (thanh trạng thái) | PHP x.y.z available | Download | StatusTransition |
| Ports | `btnAdd` / `btnEdit` / `btnRemove` | Add… / Edit… / Remove | Add / Edit / Delete | StatusRunning / AccentAction / StatusError |
| | `btnRecheck` / `btnSysRefresh` | Check availability / Refresh | Refresh | AccentAction |
| | `btnSysRelevant` | Hamster & conflicts only | Warning | StatusTransition |
| | Tab icons | Apache ports · System ports · (có xung đột) | Ports · Server · Warning | LogApache · AccentNeutral · StatusError |
| | Icon dòng System ports | Hamster Server · chặn Hamster · chương trình khác | chấm · Warning · chấm | StatusRunning · StatusError · StatusStopped |
| | Menu chuột phải System ports | Open in browser / Open program folder / Copy details | World / Folder / Page | AccentHttp / StatusTransition / AccentNeutral |
| | Menu chuột phải `ctxEdit` / `ctxRemove` | Edit… / Remove | Edit / Delete | AccentAction / StatusError |
| | Icon dòng (`_portIcons`) | HTTP · HTTPS · HTTP tắt · HTTPS tắt | Unlock · Lock · Unlock · Lock | AccentHttp · AccentHttps · StatusStopped · StatusStopped |
| Virtual Hosts | `btnAdd` / `btnEdit` / `btnDelete` | Add… / Edit… / Delete | Add / Edit / Delete | StatusRunning / AccentAction / StatusError |
| | Menu chuột phải `ctxEdit` / `ctxDelete` | Edit… / Delete | Edit / Delete | AccentAction / StatusError |
| | `btnToggle` (+ menu chuột phải) | Enable / Disable | Play / Stop | StatusRunning / StatusError |
| | `btnBrowse` / `btnOpenFolder` (+ menu chuột phải) | Open in browser / Open folder | World / Folder | AccentHttp / StatusTransition |
| | Icon dòng (`_vhostIcons`) | Đang bật · Đã tắt · Proxy (Hyperf) | World · World · Link | StatusRunning · StatusStopped · AccentHttp |
| VHost Dialog | `btnEnableExtensions` | Enable extensions | Accept | AccentAction |
| Settings | Tab icons | Apache · PHP · General | Server · Code · Settings | LogApache · LogPhp · AccentNeutral |
| Settings › PHP | `btnOpenPhpConfig` | Open PHP Configuration… | Page | LogPhp |
| Settings › General | `btnApplyModeDefaults` | Apply mode defaults | Wrench | AccentNeutral |
| PHP Configuration | `btnPhpSaveRestart` | Save & restart PHP | Refresh | StatusTransition |
| PHP Configuration › Extensions (toolbar) | Recommended | Recommended | Star | AccentAction |
| | Check all | Check all | SelectAll | AccentAction |
| | Uncheck all | Uncheck all | ClearSelection | AccentNeutral |
| About | `picLogo` | — | logo ứng dụng | — |

---

## 7. Chỗ còn thiếu hoặc chưa nhất quán

Các điểm dưới đây là đầu vào cho đợt thiết kế. Không mục nào là lỗi chức năng.

| # | Vấn đề | Đề xuất |
|---|---|---|
| 1 | ~~Menu khay hệ thống không có icon.~~ | **Đã làm** (§6.5). |
| 2 | ~~Menu chuột phải trong Ports và Virtual Hosts không có icon.~~ | **Đã làm** (§6.6). |
| 3 | ~~Glyph luôn là 16 px, không theo DPI.~~ | **Đã làm**: dựng theo `UiTheme.IconSize`. Còn lại: đổi màn hình có DPI khác lúc đang chạy thì Windows sẽ co giãn ảnh. |
| 4 | `Glyph.Warning` đã khai báo nhưng không dùng. | Dùng cho cảnh báo trong dialog (RAM vượt ngưỡng, `post_max_size` nhỏ hơn `upload_max_filesize`) hoặc xoá. |
| 5 | ~~`server.png` (931 KB) không được dùng.~~ | **Đã làm**: bỏ, master mới nằm trong `icon/`. |
| 6 | ~~Dashboard `www/` chưa có favicon.~~ | **Đã làm** (§2.1). Dashboard cũng đổi tên từ "XServer Performance Lab" thành "Hamster Server Performance Lab". |
| 7 | ~~Icon ứng dụng dùng chung một hình cho mọi kích thước.~~ | **Đã làm**: ICO Concept 01 có bản rút gọn cho 16–24 px. Còn lại: bộ nhận diện chưa có ICO `reverse`, nên app tự suy ra (§2.3). |
| 8 | ~~`Apache` và `AccentHttp`/`AccentAction` trùng màu `#005AB4`.~~ | **Đã làm**: `LogApache` = `#9C2777`. |

---

## 8. Danh sách cần bàn giao (nếu thay bộ icon)

| Hạng mục | Kích thước / định dạng | Ghi chú |
|---|---|---|
| Logo master | SVG, kèm PNG 1024 px | Nền trong suốt |
| `server.ico` | 16, 20, 24, 32, 40, 48, 64, 128, 256 px; frame 256 dạng PNG | 16–32 px dùng bản giản lược; chừa góc dưới-phải cho chấm trạng thái |
| Logo About | Dùng lại frame 128 px của ICO | Hiển thị 64 px logic |
| Favicon dashboard | `favicon.ico` 16/32/48, `apple-touch-icon.png` 180 px | |
| Glyph thao tác | Path SVG một màu trên lưới 24 px, đưa thẳng vào `UiTheme.Glyph` | Không cần PNG: app tự vẽ ở mọi kích thước. Phải là nét (không tô) để tô màu lúc chạy |
| Chấm trạng thái | Giữ cách vẽ bằng code | Chỉ cần chốt 4 màu ở §3 |

Khi thay glyph, chỉ cần sửa chuỗi path trong `UiTheme.Glyph`, rồi chạy `IconTests` (kèm `HAMSTER_GLYPH_SHEET` để xem bằng mắt). Mọi nơi dùng đều đi qua hai điểm này, và vị trí gán cho cửa sổ chính nằm gọn trong `HamsterServerForm.InitToolbarIcons`.
