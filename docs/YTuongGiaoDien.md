# Ý tưởng giao diện – Hệ thống GIS hỗ trợ quản lý, phân tích, thiết lập trạm xe buýt

Căn cứ: báo cáo phân tích – thiết kế (3 nhóm người dùng, 4 quy trình, 24 quyền chức năng). Giao diện desktop WinForms + bản đồ MapWinGIS.
Ký hiệu: ✅ đã có trong code · 🆕 đề xuất bổ sung.

## 1. Nguyên tắc
1. **Bản đồ là trung tâm** (khoảng 70% màn hình); mọi chức năng nghiệp vụ nằm ở bảng bên trái, kết quả luôn hiện lên bản đồ ("tra cứu → danh sách + highlight", đúng mô tả quy trình).
2. **Một màn hình chính duy nhất**, chuyển chức năng bằng tab; hộp thoại chỉ dùng để nhập/sửa.
3. **Giao diện theo quyền**: chức năng không có quyền thì ẩn hoặc làm mờ, không để người dùng bấm rồi mới báo lỗi.
4. **Quy trình đề xuất trạm dạng từng bước (wizard)** để người dùng không bị lạc giữa 5 bước.

## 2. Bố cục màn hình chính
```
┌──────────────────────────────────────────────────────────────────────────────────────┐
│ Hệ thống ▾   Quản trị ▾   Trợ giúp ▾                                                  │ menu
├──────────────────────────────────────────────────────────────────────────────────────┤
│ [✋Kéo] [🔍+] [🔍-] [⌖Toàn cảnh] │ [🚏Chọn trạm] [🧹Xóa kết quả] │ Lớp hiển thị ▾  [Ô tìm nhanh…] │ toolbar
├───────────────────────────┬──────────────────────────────────────────────────────────┤
│ [Trạm][Tuyến][Đề xuất][TK]│                                                          │
│ ┌───────────────────────┐ │                    BẢN ĐỒ (MapWinGIS + OSM)               │
│ │  Bộ lọc / thao tác    │ │                                                          │
│ ├───────────────────────┤ │          ● trạm   ━ tuyến   ▪ khu dân cư   ⚑ công trình  │
│ │  Bảng kết quả         │ │                                                ┌───────┐ │
│ │  (chọn dòng ⇄ bản đồ) │ │                                                │Chú giải│ │
│ ├───────────────────────┤ │                                                └───────┘ │
│ │  Chi tiết / tóm tắt   │ │   ⊕ 21.02850, 105.85420            tỷ lệ ───── 500 m     │
├───────────────────────────┴──────────────────────────────────────────────────────────┤
│ 13 trạm · 14 khu dân cư │ Người quản lý: Cán bộ quản lý │ Tọa độ con trỏ            │ status
└──────────────────────────────────────────────────────────────────────────────────────┘
```
- Thanh bên trái kéo rộng/hẹp được; có nút thu gọn để xem bản đồ toàn màn hình 🆕.
- **Chú giải** (góc bản đồ) 🆕: trạm hoạt động (xanh lá), đề xuất (hồng), ngừng (xám); tuyến (xanh dương); khu dân cư (cam); công trình (tím); khu thiếu trạm (✕ đỏ); ứng viên (★ vàng).
- **Ô tìm nhanh** 🆕: gõ tên trạm/tuyến → gợi ý → bay tới trên bản đồ.

## 3. Các màn hình theo quy trình

### 3.1 Đăng nhập ✅
Tên đăng nhập, mật khẩu, nút "Kết nối CSDL…". 🆕 Nút "Xem với tư cách khách" cho nhóm Khách hàng (không cần tài khoản nếu muốn công khai bản đồ tra cứu).

### 3.2 Tab Trạm (Quy trình 1)
```
Tên/mã [________]  Tuyến [▾]  Loại [▾]  Quận [▾]  Trạng thái [▾]
[Tra cứu thuộc tính] [Hiện tất cả]
[Vẽ hộp…] [Theo bán kính (m): 500]            ← tra cứu không gian
┌────────────────────────────────────────────┐
│ Mã   │ Tên trạm   │ Tuyến │ Loại │ Quận … │   ← nhấp dòng: highlight vàng trên bản đồ
└────────────────────────────────────────────┘
[Xem / sửa trạm…] [Phóng tới trạm]            12 kết quả
```
- Nhấp trạm trên bản đồ (công cụ "Chọn trạm") → mở **thẻ thông tin** (tên, tuyến, loại, đoạn đường, tọa độ, số khu dân cư/công trình được phục vụ) ✅/🆕 (hiện là hộp thoại, có thể đổi thành popup nổi cạnh trạm).
- Nhóm Khách hàng: chỉ có bộ lọc + bảng + xem; ẩn nút sửa.

### 3.3 Tab Tuyến (Quy trình 2)
- Bảng tuyến phía trên; chọn tuyến → bảng trạm theo thứ tự phía dưới, đường đi tô **đỏ dày** trên bản đồ và tự phóng vừa tuyến ✅.
- "Tuyến qua vùng chọn": kéo hộp → lọc các tuyến có trạm trong hộp ✅.
- **Hộp thoại Gán trạm** ✅: 2 danh sách (trên tuyến / chưa thuộc tuyến), nút ◄ ► ▲ ▼ đổi thứ tự. 🆕 Cho phép **nhấp trạm trên bản đồ để thêm vào tuyến** và kéo-thả đổi thứ tự.

### 3.4 Tab Đề xuất trạm (Quy trình 3) – wizard 5 bước
```
 ①Vùng ─── ②Khu thiếu trạm ─── ③Ứng viên ─── ④So sánh ─── ⑤Đề xuất
  ●          ○                  ○              ○            ○
```
| Bước | Màn hình | Kết quả trên bản đồ |
|---|---|---|
| ① Chọn vùng | Vẽ hộp, hoặc chọn Quận/Phường | khung đỏ nét liền |
| ② Khu thiếu trạm | Tham số bán kính phục vụ; thẻ tóm tắt "x khu · y dân · z% chưa phục vụ" | vùng đệm xanh quanh trạm hiện có + dấu ✕ đỏ ở khu thiếu trạm |
| ③ Ứng viên | Tham số bước lưới, khoảng cách tối thiểu, số ứng viên | ★ vàng đánh số #1…#n |
| ④ So sánh | Bảng: hạng, số khu, số dân, số công trình, điểm, cách trạm gần nhất; 🆕 thanh ngang thể hiện điểm | chọn dòng → ★ tương ứng nhấp nháy + vẽ vòng phục vụ của riêng nó |
| ⑤ Đề xuất | Hộp thoại nhập thuộc tính trạm mới (tên, loại, đoạn đường…), trạng thái mặc định "Đề xuất" | trạm hồng xuất hiện, ứng viên khác mờ đi |

- Nút "Tiếp / Quay lại", bước chưa đủ điều kiện thì mờ 🆕 (hiện là 3 nút rời).
- 🆕 Chế độ **"trước / sau"**: tô màu phủ sóng trước và sau khi thêm trạm đề xuất để thấy hiệu quả.

### 3.5 Tab Thống kê (Quy trình 4)
```
Loại thống kê [Số trạm theo khu vực ▾]  Bán kính [500]  [Thống kê] [Xuất CSV] [Lưu phạm vi]
┌─────────────── biểu đồ cột ───────────────┐
├─────────────── bảng chi tiết ──────────────┤
Tóm tắt: 112.000/170.500 dân được tiếp cận (65,7%)
```
- 🆕 Bản đồ chuyên đề (choropleth) tô màu quận theo số trạm khi có ranh giới hành chính; thẻ KPI lớn ở đầu tab (tổng trạm, tổng tuyến, % dân số được phục vụ).
- Xuất báo cáo: CSV ✅; 🆕 thêm PDF/Excel kèm ảnh bản đồ.

### 3.6 Quản trị (nhóm Quản trị viên) ✅
Cửa sổ riêng 4 tab: **Người dùng** (thêm/sửa/xóa/đặt lại MK) · **Phân quyền** (chọn nhóm → tick quyền) · **Lịch sử đăng nhập** (🆕 tô đỏ các lần thất bại, lọc theo ngày) · **Sao lưu** (nút sao lưu, danh sách bản đã tạo, 🆕 lịch tự động).

## 4. Giao diện theo nhóm người dùng
| Nhóm | Thấy gì |
|---|---|
| Khách hàng | Tab Trạm, Tuyến (chỉ tra cứu/xem). Không có Đề xuất, Thống kê, Quản trị. 🆕 Giao diện đơn giản hơn: ô tìm kiếm lớn, "trạm gần tôi" |
| Người quản lý | Đủ 4 tab; được sửa trạm, tuyến, đề xuất, xuất báo cáo |
| Quản trị viên | Menu Quản trị; không cần tab nghiệp vụ |

## 5. Phong cách trực quan
- Nền sáng, màu nhấn xanh dương cho hành động chính, đỏ cho cảnh báo/khu thiếu trạm; phông Segoe UI 9–10pt.
- Mọi kết quả tra cứu dùng **một kiểu làm nổi duy nhất** (vòng vàng) để người dùng quen mắt.
- Trạng thái trạm luôn có **màu + hình dạng** (không chỉ màu) để dễ phân biệt.
- Thông báo kết quả ở thanh trạng thái thay vì hộp thoại khi không cần xác nhận.

## 6. Thứ tự phát triển gợi ý
1. (Dễ) Chú giải bản đồ, ô tìm nhanh, tô đỏ đăng nhập thất bại, thanh điểm trong bảng so sánh.
2. (Vừa) Wizard 5 bước, popup thông tin trạm, nhấp bản đồ để gán trạm.
3. (Khó, cần dữ liệu) Bản đồ chuyên đề theo quận, trước/sau phủ sóng, xuất PDF kèm bản đồ.
