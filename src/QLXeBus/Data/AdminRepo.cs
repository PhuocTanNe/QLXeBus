using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using QLXeBus.Services;

namespace QLXeBus.Data
{
    /// <summary>Chức năng của Quản trị viên: người dùng, phân quyền, lịch sử đăng nhập, sao lưu.</summary>
    public static class AdminRepo
    {
        public static DataTable Users()
        {
            return Db.Table(@"SELECT n.idndung AS ""Mã"", n.tenndung AS ""Họ tên"", n.tendnhap AS ""Tên đăng nhập"", g.tennhom AS ""Nhóm"",
                                     n.chucvu AS ""Chức vụ"", n.donvi AS ""Đơn vị"", n.trangthai AS ""Trạng thái""
                              FROM nguoidung n JOIN n_ngdung g ON g.idnhom = n.idnhom ORDER BY n.idndung");
        }

        public static DataTable Groups() { return Db.Table("SELECT idnhom, tennhom FROM n_ngdung ORDER BY idnhom"); }

        public static DataRow GetUser(string id)
        {
            var dt = Db.Table("SELECT * FROM nguoidung WHERE idndung=@i", "@i", id);
            return dt.Rows.Count == 0 ? null : dt.Rows[0];
        }

        public static void SaveUser(string id, string ten, string chucVu, string donVi, string tenDn, string password, string idNhom, string trangThai)
        {
            if (id == null)
            {
                Session.Require("QT_THEM_ND");
                if (string.IsNullOrEmpty(password)) throw new ArgumentException("Cần nhập mật khẩu cho người dùng mới.");
                string nid = Repo.NextId("nguoidung", "idndung", "ND", 3);
                Db.Exec(@"INSERT INTO nguoidung(idndung,tenndung,chucvu,donvi,tendnhap,matkhau,idnhom,trangthai) VALUES (@i,@t,@c,@d,@u,@p,@g,@s)",
                    "@i", nid, "@t", ten, "@c", chucVu, "@d", donVi, "@u", tenDn, "@p", AuthService.Hash(password), "@g", idNhom, "@s", trangThai);
            }
            else
            {
                Session.Require("QT_SUA_ND");
                Db.Exec("UPDATE nguoidung SET tenndung=@t, chucvu=@c, donvi=@d, tendnhap=@u, idnhom=@g, trangthai=@s WHERE idndung=@i",
                    "@i", id, "@t", ten, "@c", chucVu, "@d", donVi, "@u", tenDn, "@g", idNhom, "@s", trangThai);
                if (!string.IsNullOrEmpty(password))
                    Db.Exec("UPDATE nguoidung SET matkhau=@p WHERE idndung=@i", "@i", id, "@p", AuthService.Hash(password));
            }
        }

        public static void DeleteUser(string id)
        {
            Session.Require("QT_XOA_ND");
            if (id == Session.User.Id) throw new InvalidOperationException("Không thể xóa tài khoản đang đăng nhập.");
            Db.Exec("DELETE FROM nguoidung WHERE idndung=@i", "@i", id);
        }

        public static DataTable Permissions() { return Db.Table("SELECT idquyen, tenquyen, machucnang, loaithaotac FROM quyen ORDER BY idquyen"); }

        public static HashSet<string> GroupPermissionIds(string idNhom)
        {
            var s = new HashSet<string>();
            foreach (DataRow r in Db.Table("SELECT idquyen FROM quyen_nnd WHERE idnhom=@g", "@g", idNhom).Rows) s.Add(Db.Str(r[0]));
            return s;
        }

        public static void SaveGroupPermissions(string idNhom, IEnumerable<string> idQuyen)
        {
            Session.Require("QT_PHANQUYEN");
            Db.InTransaction((cn, tx) =>
            {
                Db.Exec(cn, tx, "DELETE FROM quyen_nnd WHERE idnhom=@g", "@g", idNhom);
                foreach (var q in idQuyen)
                    Db.Exec(cn, tx, "INSERT INTO quyen_nnd(idnhom,idquyen) VALUES (@g,@q)", "@g", idNhom, "@q", q);
            });
        }

        public static DataTable LoginHistory(int top)
        {
            Session.Require("QT_LICHSU");
            return Db.Table(@"SELECT l.idlsdn AS ""Mã"", coalesce(n.tenndung, '') AS ""Người dùng"", l.tendnhap AS ""Tên đăng nhập"",
                                     l.tgdangnhap AS ""Đăng nhập"", l.tgdangxuat AS ""Đăng xuất"", l.diachiip AS ""IP"",
                                     l.tenthietbi AS ""Thiết bị"", l.ketqua AS ""Kết quả""
                              FROM ls_dnhap l LEFT JOIN nguoidung n ON n.idndung = l.idndung
                              ORDER BY l.idlsdn DESC LIMIT @n", "@n", top);
        }

        public static DataTable Backups()
        {
            return Db.Table(@"SELECT s.idsaoluu AS ""Mã"", s.thoigian AS ""Thời gian"", coalesce(n.tenndung,'') AS ""Người thực hiện"",
                                     s.filepath AS ""Tệp"", s.kichthuoc AS ""Kích thước (byte)"", s.loaisaoluu AS ""Loại""
                              FROM saoluu s LEFT JOIN nguoidung n ON n.idndung = s.idndung ORDER BY s.idsaoluu DESC");
        }

        /// <summary>Sao lưu bằng pg_dump (định dạng custom, khôi phục bằng pg_restore) và ghi bảng B15.</summary>
        public static string Backup(string folder)
        {
            Session.Require("QT_SAOLUU");
            var cfg = Db.Config;
            Directory.CreateDirectory(folder);
            string file = Path.Combine(folder, cfg.Database + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".backup");

            var psi = new ProcessStartInfo
            {
                FileName = cfg.PgDumpPath,
                Arguments = string.Format("-h \"{0}\" -p {1} -U \"{2}\" -F c -f \"{3}\" \"{4}\"", cfg.Host, cfg.Port, cfg.Username, file, cfg.Database),
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true
            };
            psi.EnvironmentVariables["PGPASSWORD"] = cfg.Password;
            string err;
            using (var p = Process.Start(psi))
            {
                err = p.StandardError.ReadToEnd();
                p.WaitForExit();
                if (p.ExitCode != 0) throw new InvalidOperationException("pg_dump lỗi: " + err);
            }
            Db.Exec("INSERT INTO saoluu(idndung, filepath, kichthuoc, loaisaoluu) VALUES (@u,@f,@k,'Thủ công')",
                "@u", Session.User.Id, "@f", file, "@k", new FileInfo(file).Length);
            return file;
        }
    }
}
