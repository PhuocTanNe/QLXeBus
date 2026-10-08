using System;
using System.Collections.Generic;

namespace QLXeBus.Services
{
    public struct LatLon
    {
        public double Lat, Lon;
        public LatLon(double lat, double lon) { Lat = lat; Lon = lon; }
    }

    /// <summary>Hình chữ nhật theo độ (WGS84).</summary>
    public class Bounds
    {
        public double MinLat, MinLon, MaxLat, MaxLon;

        public Bounds(double minLat, double minLon, double maxLat, double maxLon)
        {
            MinLat = Math.Min(minLat, maxLat); MaxLat = Math.Max(minLat, maxLat);
            MinLon = Math.Min(minLon, maxLon); MaxLon = Math.Max(minLon, maxLon);
        }

        public bool Contains(double lat, double lon)
        {
            return lat >= MinLat && lat <= MaxLat && lon >= MinLon && lon <= MaxLon;
        }

        public Bounds Expand(double meters)
        {
            double dLat = meters / GeoUtil.MPerDegLat;
            double dLon = meters / (GeoUtil.MPerDegLonEq * Math.Cos((MinLat + MaxLat) / 2 * Math.PI / 180));
            return new Bounds(MinLat - dLat, MinLon - dLon, MaxLat + dLat, MaxLon + dLon);
        }

        public override string ToString()
        {
            return string.Format("({0:0.0000}, {1:0.0000}) - ({2:0.0000}, {3:0.0000})", MinLat, MinLon, MaxLat, MaxLon);
        }
    }

    public static class GeoUtil
    {
        public const double MPerDegLat = 110574.0;
        public const double MPerDegLonEq = 111320.0;
        const double R = 6371008.8;

        /// <summary>Khoảng cách haversine (m).</summary>
        public static double Distance(double lat1, double lon1, double lat2, double lon2)
        {
            double p1 = lat1 * Math.PI / 180, p2 = lat2 * Math.PI / 180;
            double dp = p2 - p1, dl = (lon2 - lon1) * Math.PI / 180;
            double a = Math.Sin(dp / 2) * Math.Sin(dp / 2) + Math.Cos(p1) * Math.Cos(p2) * Math.Sin(dl / 2) * Math.Sin(dl / 2);
            return 2 * R * Math.Asin(Math.Min(1, Math.Sqrt(a)));
        }

        /// <summary>Khoảng cách xấp xỉ phẳng (m), đủ chính xác ở phạm vi đô thị và nhanh hơn haversine; cosLat = cos(vĩ độ trung tâm).</summary>
        public static double FastDistance(double lat1, double lon1, double lat2, double lon2, double cosLat)
        {
            double dy = (lat2 - lat1) * MPerDegLat;
            double dx = (lon2 - lon1) * MPerDegLonEq * cosLat;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>Đa giác tròn bán kính r(m), đỉnh xếp theo chiều kim đồng hồ, đã đóng kín (đỉnh đầu = đỉnh cuối).</summary>
        public static List<LatLon> Circle(double lat, double lon, double radiusM, int segments)
        {
            var pts = new List<LatLon>();
            double cos = Math.Cos(lat * Math.PI / 180);
            for (int i = 0; i <= segments; i++)
            {
                double ang = -2 * Math.PI * (i % segments) / segments;
                pts.Add(new LatLon(lat + radiusM * Math.Sin(ang) / MPerDegLat, lon + radiusM * Math.Cos(ang) / (MPerDegLonEq * cos)));
            }
            return pts;
        }
    }
}
