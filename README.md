# QLXeBus – Hệ thống GIS hỗ trợ quản lý, phân tích, thiết lập trạm xe buýt (Hà Nội)

Ứng dụng desktop **C# WinForms (.NET Framework 4.8)**, dựng bằng **Visual Studio 2019**, hiển thị bản đồ bằng **MapWinGIS** (nền OpenStreetMap) và lưu dữ liệu trong **PostgreSQL** (database `HQTCSDL`, quản lý bằng pgAdmin4).
Xây dựng theo báo cáo phân tích – thiết kế `docs/BaoCao_ThietKeHeThong.docx`.

## Chức năng (theo báo cáo)

| Quy trình | Chức năng |
|---|---|
| Quản lý trạm | Xem thông tin · tra cứu theo thuộc tính · tra cứu theo không gian (hộp / bán kính) · cập nhật thuộc tính |
| Quản lý tuyến | Xem tuyến + đường đi + danh sách trạm · tra cứu thuộc tính / không gian · cập nhật · gán trạm vào tuyến theo thứ tự |
| Đề xuất trạm mới | Chọn vùng (vẽ hộp hoặc theo quận/phường) → khu vực thiếu trạm → ứng viên → bảng so sánh → thêm trạm "Đề xuất" vào CSDL |
| Thống kê | Số trạm theo khu vực (bảng + biểu đồ) · trạm theo tuyến · phạm vi phục vụ (vẽ vùng đệm, % dân số tiếp cận) · xuất CSV · lưu B06.1/B06.2 |
| Quản trị | Người dùng, phân quyền theo nhóm, lịch sử đăng nhập, sao lưu (`pg_dump`) |

Phân quyền theo 3 nhóm (Người quản lý, Khách hàng, Quản trị viên) lấy từ bảng `quyen` / `quyen_nnd`; giao diện ẩn/khóa chức năng và tầng dữ liệu (`Repo`, `AdminRepo`) kiểm tra lại quyền trước mỗi thao tác ghi.

## Cài đặt (mỗi thành viên làm trên máy mình)

### 1. Công cụ
- Windows + **Visual Studio 2019** (workload *.NET desktop development*)
- **MapWinGIS 5.x** – tải bộ cài ở trang *Releases* của [MapWindow/MapWinGIS](https://github.com/MapWindow/MapWinGIS) và cài (đăng ký `MapWinGIS.ocx`). Cài bản **32-bit** nếu build cấu hình `x86` (mặc định), bản 64-bit nếu build `x64`.
- **PostgreSQL 12+** và pgAdmin4

### 2. Cơ sở dữ liệu
Trong pgAdmin4: tạo database `HQTCSDL` (UTF8) → *Query Tool* → mở và chạy lần lượt trong thư mục [`database/`](database):

1. `01_schema.sql` – bảng nghiệp vụ B01…B10, B06.1, B06.2
2. `02_security.sql` – bảng bảo mật B11…B15, thuộc tính `idNTao/tenNTao`, nhóm & quyền
3. `03_sample_data.sql` – dữ liệu **mẫu** (tọa độ gần đúng) để chạy thử; bỏ qua nếu bạn đã có dữ liệu thật

Nếu dữ liệu của bạn đã nằm sẵn trong HQTCSDL với tên bảng/cột khác, chỉ cần sửa các câu SQL trong `src/QLXeBus/Data/Repo.cs`.

### 3. Build & chạy
1. Mở `QLXeBus.sln` bằng VS2019, chọn cấu hình **Debug | x86**, *Restore NuGet* (Npgsql).
2. Nếu VS báo thiếu tham chiếu COM `MapWinGIS`: *Project ▸ Add Reference ▸ COM ▸ MapWinGIS* (và `AxMapWinGIS` sẽ được tạo tự động), rồi xóa tham chiếu cũ lỗi. Phải build bằng VS/MSBuild (không dùng `dotnet build`).
3. Chạy (F5). Ở màn hình đăng nhập bấm **Kết nối CSDL…** nhập máy chủ / cổng / `HQTCSDL` / tài khoản / mật khẩu PostgreSQL – lưu riêng từng máy tại `%AppData%\QLXeBus\dbconfig.ini` (không đưa lên git).

Tài khoản mặc định (tự tạo ở lần đăng nhập đầu khi bảng `nguoidung` trống – **đổi mật khẩu ngay**):

| Tên đăng nhập | Mật khẩu | Nhóm |
|---|---|---|
| `admin` | `admin@123` | Quản trị viên |
| `quanly` | `quanly@123` | Người quản lý |
| `khachhang` | `khachhang@123` | Khách hàng |

## Cấu trúc

```
QLXeBus.sln
database/            script SQL (schema, bảo mật, dữ liệu mẫu)
docs/                báo cáo thiết kế gốc, ghi chú kỹ thuật
src/QLXeBus/
  Data/              Db (kết nối Npgsql), Repo, AdminRepo
  Models/            thực thể
  Services/          AuthService (PBKDF2), Session, GeoUtil, CoverageAnalyzer, ReportExporter
  Map/               MapController – toàn bộ mã MapWinGIS nằm ở đây
  Forms/             giao diện (dựng bằng mã, không dùng Designer để dễ merge)
```

## Thuật toán đề xuất trạm (`CoverageAnalyzer`)
1. Khu dân cư / công trình trong vùng **thiếu trạm** nếu cách mọi trạm (kể cả trạm "Đề xuất") > bán kính phục vụ (mặc định 500 m).
2. Quét lưới (mặc định 100 m) trong vùng, loại điểm cách trạm hiện có / ứng viên khác < khoảng cách tối thiểu.
3. Chọn tham lam từng ứng viên có **điểm** cao nhất: *điểm = số dân tiếp cận mới + 500 × Σ trọng số loại công trình được phục vụ*; sau mỗi lần chọn, các đối tượng đã được phục vụ bị loại khỏi vòng sau.
4. Bảng so sánh gồm số khu, số dân, số công trình, điểm, khoảng cách tới trạm gần nhất.

## Điểm khác / bổ sung so với báo cáo
- `trambuyt.thutu`: thứ tự trạm trên tuyến (cần cho chức năng "Gán trạm vào tuyến").
- `trambuyt.matuyen/madoan/maphuongtien` cho phép NULL (trạm đề xuất chưa gán tuyến).
- `ls_dnhap.tendnhap`: lưu tên đăng nhập thử để truy vết đăng nhập sai; mật khẩu lưu dạng băm PBKDF2.
- Toạ độ lưu `viDo/kinhDo` (WGS84), **không cần PostGIS**; phép tra cứu không gian tính trong ứng dụng.

## Hạn chế hiện tại
- Chưa có hình học đường/ranh giới hành chính trong CSDL: ứng viên là điểm trên lưới (chưa "bám" vào đường); vùng theo quận/phường lấy theo khung bao các đối tượng thuộc đơn vị đó.
- Mã MapWinGIS được viết theo type library của MapWinGIS 5.x nhưng **chưa được chạy thử trên Windows** (môi trường tạo dự án không có VS/MapWinGIS). Phần dữ liệu, xác thực, phân tích đã được kiểm tra với PostgreSQL thật; phần còn lại mới kiểm tra ở mức biên dịch với bản giả lập API. Nếu gặp lỗi khi chạy bản đồ, xem `src/QLXeBus/Map/MapController.cs`.

## Đóng góp nhóm
Mỗi người làm trên một nhánh (`feature/...`), tạo Pull Request vào `main`. Không commit mật khẩu / `dbconfig.ini`. Khi đổi cấu trúc bảng, thêm script `database/NN_ten.sql` (idempotent) thay vì sửa script cũ.
