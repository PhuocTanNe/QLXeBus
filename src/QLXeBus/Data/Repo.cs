using System;
using System.Collections.Generic;
using System.Data;
using QLXeBus.Models;
using QLXeBus.Services;

namespace QLXeBus.Data
{
    /// <summary>Truy cập dữ liệu nghiệp vụ. Mọi thao tác ghi đều kiểm tra quyền của người dùng hiện tại.</summary>
    public static class Repo
    {
        // ---------------------------------------------------------------- danh mục
        static List<LookupItem> Lookup(string sql)
        {
            var l = new List<LookupItem>();
            foreach (DataRow r in Db.Table(sql).Rows) l.Add(new LookupItem { Ma = Db.Str(r[0]), Ten = Db.Str(r[1]) });
            return l;
        }
        public static List<LookupItem> Tuyen() { return Lookup("SELECT matuyen, matuyen || ' - ' || tentuyen FROM tuyenbuyt ORDER BY matuyen"); }
        public static List<LookupItem> LoaiTram() { return Lookup("SELECT maloaitram, tenloaitram FROM loaitram ORDER BY maloaitram"); }
        public static List<LookupItem> Quan() { return Lookup("SELECT maquan, tenquan FROM quanhuyen ORDER BY tenquan"); }
        public static List<LookupItem> Phuong(string maQuan)
        {
            var dt = maQuan == null
                ? Db.Table("SELECT maphuong, tenphuong FROM phuongxa ORDER BY tenphuong")
                : Db.Table("SELECT maphuong, tenphuong FROM phuongxa WHERE maquan = @q ORDER BY tenphuong", "@q", maQuan);
            var l = new List<LookupItem>();
            foreach (DataRow r in dt.Rows) l.Add(new LookupItem { Ma = Db.Str(r[0]), Ten = Db.Str(r[1]) });
            return l;
        }
        public static List<LookupItem> PhuongTien() { return Lookup("SELECT maphuongtien, maphuongtien || ' - ' || coalesce(bienso,'') FROM phuongtien ORDER BY maphuongtien"); }
        public static List<LookupItem> DoanDuong() { return Lookup("SELECT madoan, madoan || ' - ' || tenduong FROM doanduong ORDER BY madoan"); }

        // ---------------------------------------------------------------- trạm
        const string StationSql = @"
            SELECT t.matram, t.tentram, t.matuyen, tb.tentuyen, t.thutu, t.maloaitram, lt.tenloaitram,
                   t.madoan, t.maphuongtien, p.maphuong, p.tenphuong, q.maquan, q.tenquan,
                   t.vido, t.kinhdo, t.trangthai
            FROM trambuyt t
            LEFT JOIN tuyenbuyt tb ON tb.matuyen = t.matuyen
            LEFT JOIN loaitram lt ON lt.maloaitram = t.maloaitram
            LEFT JOIN doanduong d ON d.madoan = t.madoan
            LEFT JOIN phuongxa p ON p.maphuong = d.maphuong
            LEFT JOIN quanhuyen q ON q.maquan = p.maquan ";

        static Station ToStation(DataRow r)
        {
            return new Station
            {
                MaTram = Db.Str(r["matram"]), TenTram = Db.Str(r["tentram"]),
                MaTuyen = Db.Str(r["matuyen"]), TenTuyen = Db.Str(r["tentuyen"]), ThuTu = Db.IntN(r["thutu"]),
                MaLoaiTram = Db.Str(r["maloaitram"]), TenLoai = Db.Str(r["tenloaitram"]),
                MaDoan = Db.Str(r["madoan"]), MaPhuongTien = Db.Str(r["maphuongtien"]),
                MaPhuong = Db.Str(r["maphuong"]), TenPhuong = Db.Str(r["tenphuong"]),
                MaQuan = Db.Str(r["maquan"]), TenQuan = Db.Str(r["tenquan"]),
                ViDo = Db.Dbl(r["vido"]), KinhDo = Db.Dbl(r["kinhdo"]), TrangThai = Db.Str(r["trangthai"])
            };
        }

        public static List<Station> Stations()
        {
            var l = new List<Station>();
            foreach (DataRow r in Db.Table(StationSql + " ORDER BY t.matuyen, t.thutu, t.matram").Rows) l.Add(ToStation(r));
            return l;
        }

        public static Station GetStation(string maTram)
        {
            var dt = Db.Table(StationSql + " WHERE t.matram = @m", "@m", maTram);
            return dt.Rows.Count == 0 ? null : ToStation(dt.Rows[0]);
        }

        public static string NextId(string table, string col, string prefix, int digits)
        {
            // lấy số lớn nhất trong các mã dạng <prefix><số>
            var o = Db.Scalar("SELECT coalesce(max(substring(" + col + " from " + (prefix.Length + 1) + ")::int), 0) FROM " + table +
                              " WHERE " + col + " ~ ('^' || @p || '[0-9]+$')", "@p", prefix);
            int n = Convert.ToInt32(o) + 1;
            return prefix + n.ToString().PadLeft(digits, '0');
        }

        public static void UpdateStation(Station s)
        {
            Session.Require("TRAM_SUA");
            Db.Exec(@"UPDATE trambuyt SET tentram=@ten, maloaitram=@lt, madoan=@dd, maphuongtien=@pt, vido=@y, kinhdo=@x,
                        trangthai=@tt, ngaycapnhat=now() WHERE matram=@m",
                "@ten", s.TenTram, "@lt", s.MaLoaiTram, "@dd", s.MaDoan, "@pt", s.MaPhuongTien,
                "@y", s.ViDo, "@x", s.KinhDo, "@tt", s.TrangThai, "@m", s.MaTram);
        }

        /// <summary>Thêm trạm mới (dùng cho đề xuất trạm). Trả về mã trạm.</summary>
        public static string InsertStation(Station s)
        {
            Session.Require("DX_DEXUAT");
            string id = NextId("trambuyt", "matram", "T", 4);
            Db.Exec(@"INSERT INTO trambuyt(matram,matuyen,maloaitram,madoan,maphuongtien,tentram,vido,kinhdo,trangthai,thutu,idntao,tenntao)
                      VALUES (@m,@tuyen,@lt,@dd,@pt,@ten,@y,@x,@tt,NULL,@u,@un)",
                "@m", id, "@tuyen", s.MaTuyen, "@lt", s.MaLoaiTram, "@dd", s.MaDoan, "@pt", s.MaPhuongTien, "@ten", s.TenTram,
                "@y", s.ViDo, "@x", s.KinhDo, "@tt", s.TrangThai, "@u", Session.User.Id, "@un", Session.User.Ten);
            return id;
        }

        // ---------------------------------------------------------------- tuyến
        public static List<RouteInfo> Routes()
        {
            var dt = Db.Table(@"SELECT t.matuyen, t.tentuyen, t.mota, t.trangthai, t.tenntao, t.ngaycapnhat,
                                       (SELECT count(*) FROM trambuyt s WHERE s.matuyen = t.matuyen) AS sotram
                                FROM tuyenbuyt t ORDER BY t.matuyen");
            var l = new List<RouteInfo>();
            foreach (DataRow r in dt.Rows)
                l.Add(new RouteInfo
                {
                    MaTuyen = Db.Str(r["matuyen"]), TenTuyen = Db.Str(r["tentuyen"]), MoTa = Db.Str(r["mota"]),
                    TrangThai = Db.Str(r["trangthai"]), TenNTao = Db.Str(r["tenntao"]), SoTram = Db.Int(r["sotram"]),
                    NgayCapNhat = r["ngaycapnhat"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(r["ngaycapnhat"])
                });
            return l;
        }

        public static void UpdateRoute(RouteInfo t)
        {
            Session.Require("TUYEN_SUA");
            Db.Exec("UPDATE tuyenbuyt SET tentuyen=@t, mota=@m, trangthai=@s, ngaycapnhat=now() WHERE matuyen=@id",
                "@t", t.TenTuyen, "@m", t.MoTa, "@s", t.TrangThai, "@id", t.MaTuyen);
        }

        public static List<Station> StationsOfRoute(string maTuyen)
        {
            var l = new List<Station>();
            foreach (DataRow r in Db.Table(StationSql + " WHERE t.matuyen = @m ORDER BY t.thutu, t.matram", "@m", maTuyen).Rows) l.Add(ToStation(r));
            return l;
        }

        /// <summary>Gán danh sách trạm (theo thứ tự) vào tuyến; các trạm của tuyến không còn trong danh sách bị gỡ khỏi tuyến.</summary>
        public static void AssignStations(string maTuyen, IList<string> orderedStationIds)
        {
            Session.Require("TUYEN_GAN_TRAM");
            Db.InTransaction((cn, tx) =>
            {
                Db.Exec(cn, tx, "UPDATE trambuyt SET matuyen = NULL, thutu = NULL, ngaycapnhat = now() WHERE matuyen = @t", "@t", maTuyen);
                for (int i = 0; i < orderedStationIds.Count; i++)
                    Db.Exec(cn, tx, "UPDATE trambuyt SET matuyen = @t, thutu = @n, ngaycapnhat = now() WHERE matram = @m",
                        "@t", maTuyen, "@n", (short)(i + 1), "@m", orderedStationIds[i]);
                Db.Exec(cn, tx, "UPDATE tuyenbuyt SET ngaycapnhat = now() WHERE matuyen = @t", "@t", maTuyen);
            });
        }

        // ---------------------------------------------------------------- dữ liệu phân tích
        public static List<Residential> Residentials()
        {
            var l = new List<Residential>();
            foreach (DataRow r in Db.Table(@"SELECT k.makhu, k.tenkhu, p.maphuong, p.maquan, p.tenphuong, k.vido, k.kinhdo, k.danso
                                              FROM khudancu k JOIN phuongxa p ON p.maphuong = k.maphuong
                                              WHERE k.trangthai = 'Hoạt động' ORDER BY k.makhu").Rows)
                l.Add(new Residential
                {
                    MaKhu = Db.Str(r["makhu"]), TenKhu = Db.Str(r["tenkhu"]), TenPhuong = Db.Str(r["tenphuong"]),
                    MaPhuong = Db.Str(r["maphuong"]), MaQuan = Db.Str(r["maquan"]),
                    ViDo = Db.Dbl(r["vido"]), KinhDo = Db.Dbl(r["kinhdo"]), DanSo = Db.Int(r["danso"])
                });
            return l;
        }

        public static List<Facility> Facilities()
        {
            var l = new List<Facility>();
            foreach (DataRow r in Db.Table(@"SELECT c.macongtrinh, c.tencongtrinh, lc.tenloaicongtrinh, c.maphuong, p.maquan, c.vido, c.kinhdo, c.quymo, lc.trongso
                                              FROM congtrinh c JOIN loaicongtrinh lc ON lc.maloaicongtrinh = c.maloaicongtrinh
                                              JOIN phuongxa p ON p.maphuong = c.maphuong
                                              WHERE c.trangthai = 'Hoạt động' ORDER BY c.macongtrinh").Rows)
                l.Add(new Facility
                {
                    MaCongTrinh = Db.Str(r["macongtrinh"]), TenCongTrinh = Db.Str(r["tencongtrinh"]), TenLoai = Db.Str(r["tenloaicongtrinh"]),
                    MaPhuong = Db.Str(r["maphuong"]), MaQuan = Db.Str(r["maquan"]),
                    ViDo = Db.Dbl(r["vido"]), KinhDo = Db.Dbl(r["kinhdo"]), QuyMo = Db.Int(r["quymo"]), TrongSo = Db.Dbl(r["trongso"])
                });
            return l;
        }

        /// <summary>Tính lại B06.1/B06.2 cho các trạm (hoặc một trạm) theo bán kính phục vụ.</summary>
        public static int RecomputeCoverage(double radiusM, string onlyStation)
        {
            Session.Require("TK_PHAMVI");
            var stations = Stations();
            var res = Residentials();
            var fac = Facilities();
            int inserted = 0;
            Db.InTransaction((cn, tx) =>
            {
                if (onlyStation == null)
                {
                    Db.Exec(cn, tx, "DELETE FROM ctpvkhudancu");
                    Db.Exec(cn, tx, "DELETE FROM ctpvcongtrinh");
                }
                else
                {
                    Db.Exec(cn, tx, "DELETE FROM ctpvkhudancu WHERE matram=@m", "@m", onlyStation);
                    Db.Exec(cn, tx, "DELETE FROM ctpvcongtrinh WHERE matram=@m", "@m", onlyStation);
                }
                foreach (var s in stations)
                {
                    if (s.TrangThai == TrangThai.Ngung) continue;
                    if (onlyStation != null && s.MaTram != onlyStation) continue;
                    foreach (var k in res)
                    {
                        double d = GeoUtil.Distance(s.ViDo, s.KinhDo, k.ViDo, k.KinhDo);
                        if (d <= radiusM)
                        {
                            Db.Exec(cn, tx, "INSERT INTO ctpvkhudancu(matram,makhu,khoangcachm) VALUES (@t,@k,@d)", "@t", s.MaTram, "@k", k.MaKhu, "@d", Math.Round(d, 2));
                            inserted++;
                        }
                    }
                    foreach (var c in fac)
                    {
                        double d = GeoUtil.Distance(s.ViDo, s.KinhDo, c.ViDo, c.KinhDo);
                        if (d <= radiusM)
                        {
                            Db.Exec(cn, tx, "INSERT INTO ctpvcongtrinh(matram,macongtrinh,khoangcachm) VALUES (@t,@c,@d)", "@t", s.MaTram, "@c", c.MaCongTrinh, "@d", Math.Round(d, 2));
                            inserted++;
                        }
                    }
                }
            });
            return inserted;
        }

        // ---------------------------------------------------------------- thống kê
        public static DataTable StatByDistrict()
        {
            Session.Require("TK_KHUVUC");
            return Db.Table(@"SELECT coalesce(q.tenquan,'(Chưa xác định)') AS ""Quận/huyện"",
                                     coalesce(p.tenphuong,'(Chưa xác định)') AS ""Phường/xã"",
                                     count(t.matram) AS ""Số trạm"",
                                     count(*) FILTER (WHERE t.trangthai = 'Hoạt động') AS ""Đang hoạt động"",
                                     count(*) FILTER (WHERE t.trangthai = 'Đề xuất') AS ""Đề xuất""
                              FROM trambuyt t
                              LEFT JOIN doanduong d ON d.madoan = t.madoan
                              LEFT JOIN phuongxa p ON p.maphuong = d.maphuong
                              LEFT JOIN quanhuyen q ON q.maquan = p.maquan
                              GROUP BY q.tenquan, p.tenphuong ORDER BY 1, 2");
        }

        public static DataTable StatByRoute(IList<string> maTuyen)
        {
            Session.Require("TK_TUYEN");
            var all = maTuyen == null || maTuyen.Count == 0;
            var sql = @"SELECT tb.matuyen AS ""Mã tuyến"", tb.tentuyen AS ""Tên tuyến"", count(t.matram) AS ""Số trạm"",
                               coalesce(string_agg(t.tentram, ' → ' ORDER BY t.thutu), '') AS ""Danh sách trạm""
                        FROM tuyenbuyt tb LEFT JOIN trambuyt t ON t.matuyen = tb.matuyen "
                      + (all ? "" : "WHERE tb.matuyen = ANY(@l) ")
                      + "GROUP BY tb.matuyen, tb.tentuyen ORDER BY tb.matuyen";
            return all ? Db.Table(sql) : Db.Table(sql, "@l", new List<string>(maTuyen).ToArray());
        }
    }
}
