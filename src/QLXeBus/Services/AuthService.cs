using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Security.Cryptography;
using QLXeBus.Data;
using QLXeBus.Models;

namespace QLXeBus.Services
{
    public static class AuthService
    {
        const int Iterations = 20000;

        public static string Hash(string password)
        {
            var salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
            using (var kdf = new Rfc2898DeriveBytes(password, salt, Iterations))
                return "pbkdf2$" + Iterations + "$" + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(kdf.GetBytes(32));
        }

        public static bool Verify(string password, string stored)
        {
            if (string.IsNullOrEmpty(stored)) return false;
            var p = stored.Split('$');
            if (p.Length != 4 || p[0] != "pbkdf2") return false;
            var salt = Convert.FromBase64String(p[2]);
            var expected = Convert.FromBase64String(p[3]);
            byte[] actual;
            using (var kdf = new Rfc2898DeriveBytes(password, salt, int.Parse(p[1]))) actual = kdf.GetBytes(expected.Length);
            int diff = 0;
            for (int i = 0; i < expected.Length; i++) diff |= expected[i] ^ actual[i];
            return diff == 0;
        }

        /// <summary>Tạo 3 tài khoản mặc định (mật khẩu: tên đăng nhập + "@123") nếu bảng nguoidung đang trống.</summary>
        public static void EnsureDefaultUsers()
        {
            var n = Db.Scalar("SELECT count(*) FROM nguoidung");
            if (Convert.ToInt64(n) > 0) return;
            Db.Exec("INSERT INTO nguoidung(idndung,tenndung,chucvu,donvi,tendnhap,matkhau,idnhom) VALUES (@i,@t,@c,@d,@u,@p,@g)",
                "@i", "ND001", "@t", "Quản trị hệ thống", "@c", "Quản trị viên", "@d", "Phòng CNTT", "@u", "admin", "@p", Hash("admin@123"), "@g", "QTV");
            Db.Exec("INSERT INTO nguoidung(idndung,tenndung,chucvu,donvi,tendnhap,matkhau,idnhom) VALUES (@i,@t,@c,@d,@u,@p,@g)",
                "@i", "ND002", "@t", "Cán bộ quản lý", "@c", "Chuyên viên", "@d", "Bộ phận quản lý", "@u", "quanly", "@p", Hash("quanly@123"), "@g", "NQL");
            Db.Exec("INSERT INTO nguoidung(idndung,tenndung,chucvu,donvi,tendnhap,matkhau,idnhom) VALUES (@i,@t,@c,@d,@u,@p,@g)",
                "@i", "ND003", "@t", "Khách hàng", "@c", "", "@d", "Bộ phận khách hàng", "@u", "khachhang", "@p", Hash("khachhang@123"), "@g", "KH");
        }

        static string LocalIp()
        {
            try
            {
                foreach (var a in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                    if (a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) return a.ToString();
            }
            catch { }
            return null;
        }

        /// <summary>Đăng nhập; ghi nhật ký B14 (cả thành công lẫn thất bại). Trả về thông báo lỗi hoặc null nếu OK.</summary>
        public static string Login(string username, string password)
        {
            username = (username ?? "").Trim();
            var dt = Db.Table(@"SELECT n.idndung, n.tenndung, n.matkhau, n.idnhom, n.trangthai, g.tennhom
                                 FROM nguoidung n JOIN n_ngdung g ON g.idnhom = n.idnhom
                                 WHERE lower(n.tendnhap) = lower(@u)", "@u", username);
            string ip = LocalIp(), device = Environment.MachineName;

            if (dt.Rows.Count == 0 || !Verify(password, Db.Str(dt.Rows[0]["matkhau"])))
            {
                LogLogin(dt.Rows.Count > 0 ? Db.Str(dt.Rows[0]["idndung"]) : null, username, ip, device, "Thất bại");
                return "Sai tên đăng nhập hoặc mật khẩu.";
            }
            var r = dt.Rows[0];
            if (Db.Str(r["trangthai"]) != TrangThai.HoatDong)
            {
                LogLogin(Db.Str(r["idndung"]), username, ip, device, "Thất bại");
                return "Tài khoản đã bị khóa.";
            }

            var perms = new List<string>();
            foreach (DataRow p in Db.Table(@"SELECT q.machucnang FROM quyen_nnd x JOIN quyen q ON q.idquyen = x.idquyen WHERE x.idnhom = @g",
                                           "@g", Db.Str(r["idnhom"])).Rows)
                perms.Add(Db.Str(p[0]));

            int logId = LogLogin(Db.Str(r["idndung"]), username, ip, device, "Thành công");
            Session.Start(new CurrentUser
            {
                Id = Db.Str(r["idndung"]), Ten = Db.Str(r["tenndung"]), TenDangNhap = username,
                IdNhom = Db.Str(r["idnhom"]), TenNhom = Db.Str(r["tennhom"])
            }, perms, logId);
            return null;
        }

        static int LogLogin(string idNd, string username, string ip, string device, string result)
        {
            return Convert.ToInt32(Db.Scalar(
                @"INSERT INTO ls_dnhap(idndung, tendnhap, diachiip, tenthietbi, ketqua) VALUES (@i,@u,@ip,@d,@k) RETURNING idlsdn",
                "@i", idNd, "@u", username, "@ip", ip, "@d", device, "@k", result));
        }

        public static void Logout()
        {
            try
            {
                if (Session.LoginLogId > 0)
                    Db.Exec("UPDATE ls_dnhap SET tgdangxuat = now() WHERE idlsdn = @i", "@i", Session.LoginLogId);
            }
            catch { }
            Session.Clear();
        }
    }
}
