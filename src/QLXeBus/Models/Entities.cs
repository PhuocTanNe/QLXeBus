using System;
using System.ComponentModel;

namespace QLXeBus.Models
{
    public static class TrangThai
    {
        public const string HoatDong = "Hoạt động";
        public const string DeXuat = "Đề xuất";
        public const string Ngung = "Ngừng hoạt động";
        public static readonly string[] Tram = { HoatDong, DeXuat, Ngung };
        public static readonly string[] Chung = { HoatDong, Ngung };
    }

    public class Station
    {
        [DisplayName("Mã trạm")] public string MaTram { get; set; }
        [DisplayName("Tên trạm")] public string TenTram { get; set; }
        [DisplayName("Tuyến")] public string TenTuyen { get; set; }
        [DisplayName("Thứ tự")] public int? ThuTu { get; set; }
        [DisplayName("Loại trạm")] public string TenLoai { get; set; }
        [DisplayName("Phường/xã")] public string TenPhuong { get; set; }
        [DisplayName("Quận/huyện")] public string TenQuan { get; set; }
        [DisplayName("Vĩ độ")] public double ViDo { get; set; }
        [DisplayName("Kinh độ")] public double KinhDo { get; set; }
        [DisplayName("Trạng thái")] public string TrangThai { get; set; }

        [Browsable(false)] public string MaTuyen { get; set; }
        [Browsable(false)] public string MaLoaiTram { get; set; }
        [Browsable(false)] public string MaDoan { get; set; }
        [Browsable(false)] public string MaPhuongTien { get; set; }
        [Browsable(false)] public string MaPhuong { get; set; }
        [Browsable(false)] public string MaQuan { get; set; }
    }

    public class RouteInfo
    {
        [DisplayName("Mã tuyến")] public string MaTuyen { get; set; }
        [DisplayName("Tên tuyến")] public string TenTuyen { get; set; }
        [DisplayName("Số trạm")] public int SoTram { get; set; }
        [DisplayName("Trạng thái")] public string TrangThai { get; set; }
        [DisplayName("Mô tả")] public string MoTa { get; set; }
        [DisplayName("Người tạo")] public string TenNTao { get; set; }
        [DisplayName("Cập nhật")] public DateTime NgayCapNhat { get; set; }
    }

    public class Residential
    {
        public string MaKhu { get; set; }
        public string TenKhu { get; set; }
        public string TenPhuong { get; set; }
        public string MaPhuong { get; set; }
        public string MaQuan { get; set; }
        public double ViDo { get; set; }
        public double KinhDo { get; set; }
        public int DanSo { get; set; }
    }

    public class Facility
    {
        public string MaCongTrinh { get; set; }
        public string TenCongTrinh { get; set; }
        public string TenLoai { get; set; }
        public string MaPhuong { get; set; }
        public string MaQuan { get; set; }
        public double ViDo { get; set; }
        public double KinhDo { get; set; }
        public int QuyMo { get; set; }
        public double TrongSo { get; set; }
    }

    public class Candidate
    {
        [DisplayName("Hạng")] public int Hang { get; set; }
        [DisplayName("Vĩ độ")] public double ViDo { get; set; }
        [DisplayName("Kinh độ")] public double KinhDo { get; set; }
        [DisplayName("Số khu dân cư được phục vụ mới")] public int SoKhuMoi { get; set; }
        [DisplayName("Số dân tiếp cận mới")] public int DanSoMoi { get; set; }
        [DisplayName("Số công trình được phục vụ")] public int SoCongTrinhMoi { get; set; }
        [DisplayName("Điểm tối ưu")] public double Diem { get; set; }
        [DisplayName("Cách trạm gần nhất (m)")] public int KcTramGanNhat { get; set; }
    }

    public class LookupItem
    {
        public string Ma { get; set; }
        public string Ten { get; set; }
        public override string ToString() { return Ten; }
    }

    public class CurrentUser
    {
        public string Id { get; set; }
        public string Ten { get; set; }
        public string TenDangNhap { get; set; }
        public string IdNhom { get; set; }
        public string TenNhom { get; set; }
    }
}
