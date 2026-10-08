# Ghi chú kiến trúc

## Luồng dữ liệu
`PostgreSQL (HQTCSDL)` → `Db/Repo` (Npgsql, SQL tham số hóa) → danh sách đối tượng trong bộ nhớ (`Station`, `Residential`, `Facility`) →
`MapController` dựng shapefile **trong bộ nhớ** (đổi WGS84 → Web Mercator bằng `Map.DegreesToProj`) và thêm vào `AxMap`.
Mọi lớp phủ kết quả (làm nổi trạm, tuyến, vùng đệm, khu thiếu trạm, ứng viên, khung vùng chọn) là các "overlay" được dựng lại sau mỗi lần `Rebuild()`.

## Phân quyền
`Session.Can(maChucNang)` (giao diện) và `Session.Require(...)` (tầng dữ liệu). Mã chức năng nằm ở `quyen.machucnang`
(`TRAM_XEM`, `TRAM_SUA`, `TUYEN_GAN_TRAM`, `DX_DEXUAT`, `TK_XUATFILE`, `QT_PHANQUYEN`, ...). Muốn thêm chức năng: thêm dòng vào `quyen`, gán cho nhóm trong `quyen_nnd`, rồi kiểm tra mã đó trong mã nguồn.

## Ma trận quyền dữ liệu (báo cáo 3.1.2)
Thực thi qua quyền chức năng: Người quản lý sửa B01 (tuyến) qua `TUYEN_SUA`, thêm/sửa B06 (trạm) qua `DX_DEXUAT`/`TRAM_SUA`, ghi B06.1/B06.2 qua `TK_PHAMVI`;
Khách hàng chỉ đọc B01–B06; Quản trị viên quản lý B11–B15.
