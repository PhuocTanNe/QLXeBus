using System;
using System.Collections.Generic;
using System.Linq;
using QLXeBus.Models;

namespace QLXeBus.Services
{
    public class AnalysisParams
    {
        public double RadiusM = 500;          // bán kính phục vụ của một trạm
        public double GridStepM = 100;        // bước lưới quét ứng viên
        public double MinSpacingM = 250;      // khoảng cách tối thiểu tới trạm hiện có / ứng viên khác
        public int TopN = 5;                  // số ứng viên cần chọn
        public double FacilityBonus = 500;    // điểm cộng cho mỗi công trình được phục vụ (nhân trọng số loại công trình)
        public Bounds Region;                 // vùng phân tích
    }

    public class UnderservedResult
    {
        public List<Residential> Residents = new List<Residential>();      // khu dân cư thiếu trạm trong vùng
        public List<Facility> Facilities = new List<Facility>();           // công trình chưa được phục vụ
        public int TotalResidentsInRegion, TotalPopulationInRegion, UnderservedPopulation;
        public double CoveragePercent
        {
            get { return TotalPopulationInRegion == 0 ? 100.0 : 100.0 * (TotalPopulationInRegion - UnderservedPopulation) / TotalPopulationInRegion; }
        }
    }

    public class StationCoverage
    {
        public Station Station;
        public int SoKhu, DanSo, SoCongTrinh;
    }

    /// <summary>Phân tích phạm vi phục vụ, khu vực thiếu trạm và tìm vị trí ứng viên trạm mới.</summary>
    public static class CoverageAnalyzer
    {
        static IEnumerable<Station> Active(IEnumerable<Station> all)
        {
            // trạm "Đề xuất" cũng được tính là sẽ phục vụ để tránh đề xuất trùng
            return all.Where(s => s.TrangThai != TrangThai.Ngung);
        }

        static double NearestStation(double lat, double lon, List<Station> st, double cos)
        {
            double best = double.MaxValue;
            for (int i = 0; i < st.Count; i++)
            {
                double d = GeoUtil.FastDistance(lat, lon, st[i].ViDo, st[i].KinhDo, cos);
                if (d < best) best = d;
            }
            return best;
        }

        /// <summary>Bước 2 của quy trình đề xuất: xác định khu dân cư/công trình trong vùng nằm ngoài phạm vi phục vụ của mọi trạm.</summary>
        public static UnderservedResult FindUnderserved(AnalysisParams p, IEnumerable<Station> stations,
                                                        IEnumerable<Residential> residents, IEnumerable<Facility> facilities)
        {
            var st = Active(stations).ToList();
            double cos = Math.Cos((p.Region.MinLat + p.Region.MaxLat) / 2 * Math.PI / 180);
            var r = new UnderservedResult();
            foreach (var k in residents.Where(k => p.Region.Contains(k.ViDo, k.KinhDo)))
            {
                r.TotalResidentsInRegion++;
                r.TotalPopulationInRegion += k.DanSo;
                if (NearestStation(k.ViDo, k.KinhDo, st, cos) > p.RadiusM)
                {
                    r.Residents.Add(k);
                    r.UnderservedPopulation += k.DanSo;
                }
            }
            foreach (var c in facilities.Where(c => p.Region.Contains(c.ViDo, c.KinhDo)))
                if (NearestStation(c.ViDo, c.KinhDo, st, cos) > p.RadiusM) r.Facilities.Add(c);
            return r;
        }

        /// <summary>
        /// Bước 3-4: quét lưới trong vùng, chọn tham lam TopN vị trí làm tăng nhiều nhất số dân (và công trình) được phục vụ.
        /// Điểm = số dân tiếp cận mới + FacilityBonus × Σ trọng số công trình mới được phục vụ.
        /// </summary>
        public static List<Candidate> FindCandidates(AnalysisParams p, IEnumerable<Station> stations, UnderservedResult under)
        {
            var result = new List<Candidate>();
            if (under.Residents.Count == 0 && under.Facilities.Count == 0) return result;

            var st = Active(stations).ToList();
            var chosen = new List<Station>(st);
            var rs = new List<Residential>(under.Residents);
            var fs = new List<Facility>(under.Facilities);
            double midLat = (p.Region.MinLat + p.Region.MaxLat) / 2;
            double cos = Math.Cos(midLat * Math.PI / 180);

            double step = p.GridStepM;
            double w = (p.Region.MaxLon - p.Region.MinLon) * GeoUtil.MPerDegLonEq * cos;
            double h = (p.Region.MaxLat - p.Region.MinLat) * GeoUtil.MPerDegLat;
            while ((w / step + 1) * (h / step + 1) > 40000) step *= 1.5;    // giới hạn kích thước lưới
            double dLat = step / GeoUtil.MPerDegLat, dLon = step / (GeoUtil.MPerDegLonEq * cos);

            for (int rank = 1; rank <= p.TopN && (rs.Count > 0 || fs.Count > 0); rank++)
            {
                double bestScore = 0, bestLat = 0, bestLon = 0;
                for (double lat = p.Region.MinLat; lat <= p.Region.MaxLat; lat += dLat)
                    for (double lon = p.Region.MinLon; lon <= p.Region.MaxLon; lon += dLon)
                    {
                        if (NearestStation(lat, lon, chosen, cos) < p.MinSpacingM) continue;
                        double score = 0;
                        for (int i = 0; i < rs.Count; i++)
                            if (GeoUtil.FastDistance(lat, lon, rs[i].ViDo, rs[i].KinhDo, cos) <= p.RadiusM) score += rs[i].DanSo;
                        for (int i = 0; i < fs.Count; i++)
                            if (GeoUtil.FastDistance(lat, lon, fs[i].ViDo, fs[i].KinhDo, cos) <= p.RadiusM) score += p.FacilityBonus * fs[i].TrongSo;
                        if (score > bestScore) { bestScore = score; bestLat = lat; bestLon = lon; }
                    }
                if (bestScore <= 0) break;

                var newRes = rs.Where(k => GeoUtil.FastDistance(bestLat, bestLon, k.ViDo, k.KinhDo, cos) <= p.RadiusM).ToList();
                var newFac = fs.Where(c => GeoUtil.FastDistance(bestLat, bestLon, c.ViDo, c.KinhDo, cos) <= p.RadiusM).ToList();
                result.Add(new Candidate
                {
                    Hang = rank, ViDo = Math.Round(bestLat, 6), KinhDo = Math.Round(bestLon, 6),
                    SoKhuMoi = newRes.Count, DanSoMoi = newRes.Sum(k => k.DanSo), SoCongTrinhMoi = newFac.Count,
                    Diem = Math.Round(bestScore, 1),
                    KcTramGanNhat = (int)Math.Round(NearestStation(bestLat, bestLon, st, cos))
                });
                rs.RemoveAll(k => newRes.Contains(k));
                fs.RemoveAll(c => newFac.Contains(c));
                chosen.Add(new Station { ViDo = bestLat, KinhDo = bestLon });
            }
            return result;
        }

        /// <summary>Thống kê phạm vi phục vụ: theo từng trạm và tổng thể (dân số được tiếp cận, không đếm trùng).</summary>
        public static List<StationCoverage> StationCoverages(double radiusM, IEnumerable<Station> stations,
            IEnumerable<Residential> residents, IEnumerable<Facility> facilities,
            out int totalPopulation, out int servedPopulation)
        {
            var res = residents.ToList();
            var fac = facilities.ToList();
            var served = new HashSet<string>();
            var list = new List<StationCoverage>();
            foreach (var s in Active(stations))
            {
                var c = new StationCoverage { Station = s };
                foreach (var k in res)
                    if (GeoUtil.Distance(s.ViDo, s.KinhDo, k.ViDo, k.KinhDo) <= radiusM) { c.SoKhu++; c.DanSo += k.DanSo; served.Add(k.MaKhu); }
                foreach (var f in fac)
                    if (GeoUtil.Distance(s.ViDo, s.KinhDo, f.ViDo, f.KinhDo) <= radiusM) c.SoCongTrinh++;
                list.Add(c);
            }
            totalPopulation = res.Sum(k => k.DanSo);
            servedPopulation = res.Where(k => served.Contains(k.MaKhu)).Sum(k => k.DanSo);
            return list;
        }
    }
}
