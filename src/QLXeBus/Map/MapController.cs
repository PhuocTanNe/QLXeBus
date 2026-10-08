using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using AxMapWinGIS;
using MapWinGIS;
using QLXeBus.Models;
using QLXeBus.Services;

namespace QLXeBus.MapView
{
    public enum MapTool { Pan, ZoomIn, ZoomOut, PickPoint, SelectBox }

    /// <summary>
    /// Bọc điều khiển MapWinGIS (AxMap): nền OpenStreetMap, vẽ lớp dữ liệu từ CSDL (lat/lon WGS84, tự đổi sang Web Mercator)
    /// và các lớp phủ (kết quả tra cứu, phạm vi phục vụ, khu thiếu trạm, ứng viên...). Toàn bộ mã dùng MapWinGIS tập trung ở đây.
    /// </summary>
    public class MapController
    {
        public AxMap Map { get; private set; }

        public event Action<LatLon, double> PointPicked;    // PickPoint: nhấp chuột (vị trí, dung sai 12 px quy ra mét)
        public event Action<Bounds> BoxSelected;           // SelectBox: kéo hộp
        public event Action<LatLon> CursorMoved;           // hiển thị toạ độ

        public bool ShowResidential = true, ShowFacilities = true, ShowRoutes = true, ShowStationNames = false;

        List<Station> _stations = new List<Station>();
        List<Residential> _residents = new List<Residential>();
        List<Facility> _facilities = new List<Facility>();

        readonly List<int> _baseLayers = new List<int>();
        readonly Dictionary<string, int> _overlayHandles = new Dictionary<string, int>();
        readonly Dictionary<string, Func<Shapefile>> _overlayBuilders = new Dictionary<string, Func<Shapefile>>();
        readonly List<string> _overlayOrder = new List<string>();

        public MapController(AxMap map)
        {
            Map = map;
            Map.Projection = tkMapProjection.PROJECTION_GOOGLE_MERCATOR;
            Map.Tiles.Provider = tkTileProvider.OpenStreetMap;
            Map.Tiles.Visible = true;
            Map.SendMouseDown = true;
            Map.SendMouseMove = true;
            Map.SendSelectBoxFinal = true;
            Map.CursorMode = tkCursorMode.cmPan;

            Map.MouseDownEvent += OnMouseDown;
            Map.MouseMoveEvent += OnMouseMove;
            Map.SelectBoxFinal += OnSelectBoxFinal;
            GoTo(new Bounds(20.93, 105.72, 21.10, 105.92));      // trung tâm Hà Nội
        }

        // ---------------------------------------------------------------- công cụ & toạ độ
        public void SetTool(MapTool t)
        {
            switch (t)
            {
                case MapTool.Pan: Map.CursorMode = tkCursorMode.cmPan; break;
                case MapTool.ZoomIn: Map.CursorMode = tkCursorMode.cmZoomIn; break;
                case MapTool.ZoomOut: Map.CursorMode = tkCursorMode.cmZoomOut; break;
                case MapTool.PickPoint: Map.CursorMode = tkCursorMode.cmNone; break;
                case MapTool.SelectBox: Map.CursorMode = tkCursorMode.cmSelection; break;
            }
            Tool = t;
        }
        public MapTool Tool { get; private set; }

        public LatLon PixelToLatLon(int px, int py)
        {
            double x = 0, y = 0, lon = 0, lat = 0;
            Map.PixelToProj(px, py, ref x, ref y);
            Map.ProjToDegrees(x, y, ref lon, ref lat);
            return new LatLon(lat, lon);
        }

        /// <summary>Số mét tương ứng với n điểm ảnh tại vị trí hiện tại (dùng làm dung sai khi chọn trạm).</summary>
        public double PixelsToMeters(int px, int py, int n)
        {
            var a = PixelToLatLon(px, py);
            var b = PixelToLatLon(px + n, py);
            return GeoUtil.Distance(a.Lat, a.Lon, b.Lat, b.Lon);
        }

        public void GoTo(Bounds b)
        {
            var ext = new Extents();
            ext.SetBounds(b.MinLon, b.MinLat, 0, b.MaxLon, b.MaxLat, 0);
            Map.SetGeographicExtents(ext);
        }

        public void ZoomToLatLon(double lat, double lon, double widthKm)
        {
            Map.SetGeographicExtents2(lon, lat, widthKm);
        }

        void OnMouseDown(object sender, _DMapEvents_MouseDownEvent e)
        {
            if (Tool == MapTool.PickPoint && e.button == 1 && PointPicked != null)
                PointPicked(PixelToLatLon(e.x, e.y), PixelsToMeters(e.x, e.y, 12));
        }

        void OnMouseMove(object sender, _DMapEvents_MouseMoveEvent e)
        {
            if (CursorMoved != null) CursorMoved(PixelToLatLon(e.x, e.y));
        }

        void OnSelectBoxFinal(object sender, _DMapEvents_SelectBoxFinalEvent e)
        {
            if (Tool != MapTool.SelectBox || BoxSelected == null) return;
            var a = PixelToLatLon(e.left, e.top);
            var b = PixelToLatLon(e.right, e.bottom);
            BoxSelected(new Bounds(a.Lat, a.Lon, b.Lat, b.Lon));
        }

        // ---------------------------------------------------------------- tạo shapefile trong bộ nhớ
        static uint Ole(Color c) { return (uint)ColorTranslator.ToOle(c); }

        Shapefile NewShapefile(ShpfileType type, params string[] textFields)
        {
            var sf = new Shapefile();
            if (!sf.CreateNew("", type)) throw new InvalidOperationException("Không tạo được lớp bản đồ: " + sf.get_ErrorMsg(sf.LastErrorCode));
            var gp = new GeoProjection();
            gp.CopyFrom(Map.GeoProjection);
            sf.GeoProjection = gp;
            sf.Selectable = false;
            foreach (var f in textFields) sf.EditAddField(f, FieldType.STRING_FIELD, 0, 100);
            return sf;
        }

        Shape NewShape(ShpfileType type, IEnumerable<LatLon> pts)
        {
            var shp = new Shape();
            shp.Create(type);
            foreach (var p in pts)
            {
                double x = 0, y = 0;
                Map.DegreesToProj(p.Lon, p.Lat, ref x, ref y);
                shp.AddPoint(x, y);
            }
            return shp;
        }

        int AddPoint(Shapefile sf, double lat, double lon, params string[] values)
        {
            int idx = sf.EditAddShape(NewShape(ShpfileType.SHP_POINT, new[] { new LatLon(lat, lon) }));
            for (int i = 0; i < values.Length; i++) sf.EditCellValue(i, idx, values[i]);
            return idx;
        }

        void StylePoints(Shapefile sf, tkPointShapeType shape, float size, Color fill, Color line)
        {
            var o = sf.DefaultDrawingOptions;
            o.PointShape = shape;
            o.PointSize = size;
            o.FillColor = Ole(fill);
            o.LineColor = Ole(line);
        }

        void AddLabel(Shapefile sf, string text, double lat, double lon, Color color)
        {
            double x = 0, y = 0;
            Map.DegreesToProj(lon, lat, ref x, ref y);
            sf.Labels.AddLabel(text, x, y, 0, -1, 0, 0);
        }

        void StyleLabels(Shapefile sf, Color color, int size)
        {
            var lb = sf.Labels;
            lb.FontSize = size;
            lb.FontColor = Ole(color);
            lb.HaloVisible = true;
            lb.HaloColor = Ole(Color.White);
            lb.OffsetY = 10;
            lb.AvoidCollisions = true;
            lb.Visible = true;
        }

        // ---------------------------------------------------------------- lớp dữ liệu nền
        public void SetData(List<Station> stations, List<Residential> residents, List<Facility> facilities)
        {
            _stations = stations; _residents = residents; _facilities = facilities;
            Rebuild();
        }

        /// <summary>Dựng lại toàn bộ lớp (nền + lớp phủ). Giữ nguyên khung nhìn.</summary>
        public void Rebuild()
        {
            Map.LockWindow(tkLockMode.lmLock);
            try
            {
                Map.RemoveAllLayers();
                _baseLayers.Clear();
                _overlayHandles.Clear();

                if (ShowResidential && _residents.Count > 0)
                {
                    var sf = NewShapefile(ShpfileType.SHP_POINT, "Ten");
                    foreach (var k in _residents) AddPoint(sf, k.ViDo, k.KinhDo, k.TenKhu);
                    StylePoints(sf, tkPointShapeType.ptShapeRegular, 7, Color.Orange, Color.DarkOrange);
                    _baseLayers.Add(Map.AddLayer(sf, true));
                }
                if (ShowFacilities && _facilities.Count > 0)
                {
                    var sf = NewShapefile(ShpfileType.SHP_POINT, "Ten");
                    foreach (var c in _facilities) AddPoint(sf, c.ViDo, c.KinhDo, c.TenCongTrinh);
                    StylePoints(sf, tkPointShapeType.ptShapeFlag, 9, Color.MediumPurple, Color.Indigo);
                    _baseLayers.Add(Map.AddLayer(sf, true));
                }
                if (ShowRoutes)
                {
                    var sf = NewShapefile(ShpfileType.SHP_POLYLINE, "Tuyen");
                    foreach (var g in _stations.Where(s => s.MaTuyen != null && s.TrangThai != TrangThai.Ngung).GroupBy(s => s.MaTuyen))
                    {
                        var pts = g.OrderBy(s => s.ThuTu ?? int.MaxValue).Select(s => new LatLon(s.ViDo, s.KinhDo)).ToList();
                        if (pts.Count < 2) continue;
                        int idx = sf.EditAddShape(NewShape(ShpfileType.SHP_POLYLINE, pts));
                        sf.EditCellValue(0, idx, g.Key);
                    }
                    var o = sf.DefaultDrawingOptions;
                    o.LineColor = Ole(Color.RoyalBlue);
                    o.LineWidth = 3;
                    _baseLayers.Add(Map.AddLayer(sf, true));
                }
                AddStationLayer(TrangThai.Ngung, Color.Gray, Color.DimGray, 7);
                AddStationLayer(TrangThai.HoatDong, Color.LimeGreen, Color.DarkGreen, 10);
                AddStationLayer(TrangThai.DeXuat, Color.DeepPink, Color.DarkMagenta, 11);

                foreach (var name in _overlayOrder) BuildOverlay(name);
            }
            finally { Map.LockWindow(tkLockMode.lmUnlock); }
            Map.Redraw();
        }

        void AddStationLayer(string status, Color fill, Color line, float size)
        {
            var list = _stations.Where(s => s.TrangThai == status).ToList();
            if (list.Count == 0) return;
            var sf = NewShapefile(ShpfileType.SHP_POINT, "Ten");
            foreach (var s in list)
            {
                AddPoint(sf, s.ViDo, s.KinhDo, s.TenTram);
                if (ShowStationNames) AddLabel(sf, s.TenTram, s.ViDo, s.KinhDo, line);
            }
            StylePoints(sf, tkPointShapeType.ptShapeCircle, size, fill, line);
            if (ShowStationNames) StyleLabels(sf, Color.Black, 8);
            _baseLayers.Add(Map.AddLayer(sf, true));
        }

        // ---------------------------------------------------------------- lớp phủ
        void SetOverlay(string name, Func<Shapefile> builder)
        {
            _overlayBuilders[name] = builder;
            if (!_overlayOrder.Contains(name)) _overlayOrder.Add(name);
            BuildOverlay(name);
            Map.Redraw();
        }

        void BuildOverlay(string name)
        {
            int h;
            if (_overlayHandles.TryGetValue(name, out h)) { Map.RemoveLayer(h); _overlayHandles.Remove(name); }
            Func<Shapefile> b;
            if (!_overlayBuilders.TryGetValue(name, out b)) return;
            var sf = b();
            if (sf != null) _overlayHandles[name] = Map.AddLayer(sf, true);
        }

        public void ClearOverlay(string name)
        {
            int h;
            if (_overlayHandles.TryGetValue(name, out h)) { Map.RemoveLayer(h); _overlayHandles.Remove(name); }
            _overlayBuilders.Remove(name);
            _overlayOrder.Remove(name);
            Map.Redraw();
        }

        public void ClearAllOverlays()
        {
            foreach (var n in _overlayOrder.ToList()) ClearOverlay(n);
        }

        public const string OvHighlight = "highlight", OvRoute = "route", OvCoverage = "coverage", OvUnder = "under",
                            OvCandidates = "candidates", OvArea = "area";

        /// <summary>Làm nổi các trạm kết quả tra cứu (vòng vàng).</summary>
        public void ShowHighlight(IList<Station> stations)
        {
            var copy = stations.ToList();
            SetOverlay(OvHighlight, () =>
            {
                if (copy.Count == 0) return null;
                var sf = NewShapefile(ShpfileType.SHP_POINT, "Ten");
                foreach (var s in copy)
                {
                    AddPoint(sf, s.ViDo, s.KinhDo, s.TenTram);
                    AddLabel(sf, s.TenTram, s.ViDo, s.KinhDo, Color.Black);
                }
                StylePoints(sf, tkPointShapeType.ptShapeCircle, 16, Color.FromArgb(255, 235, 59), Color.OrangeRed);
                sf.DefaultDrawingOptions.FillTransparency = 0.6f;
                StyleLabels(sf, Color.Black, 9);
                return sf;
            });
        }

        /// <summary>Làm nổi một tuyến: đường nối các trạm theo thứ tự.</summary>
        public void ShowRoute(IList<Station> ordered)
        {
            var pts = ordered.Select(s => new LatLon(s.ViDo, s.KinhDo)).ToList();
            var names = ordered.ToList();
            SetOverlay(OvRoute, () =>
            {
                if (pts.Count < 2) return null;
                var sf = NewShapefile(ShpfileType.SHP_POLYLINE, "Ten");
                sf.EditAddShape(NewShape(ShpfileType.SHP_POLYLINE, pts));
                sf.DefaultDrawingOptions.LineColor = Ole(Color.Red);
                sf.DefaultDrawingOptions.LineWidth = 5;
                return sf;
            });
            ShowHighlight(names);
        }

        public void ShowCoverage(IEnumerable<LatLon> centers, double radiusM)
        {
            var c = centers.ToList();
            SetOverlay(OvCoverage, () =>
            {
                if (c.Count == 0) return null;
                var sf = NewShapefile(ShpfileType.SHP_POLYGON, "Ten");
                foreach (var p in c) sf.EditAddShape(NewShape(ShpfileType.SHP_POLYGON, GeoUtil.Circle(p.Lat, p.Lon, radiusM, 48)));
                var o = sf.DefaultDrawingOptions;
                o.FillColor = Ole(Color.DodgerBlue);
                o.FillTransparency = 0.25f;
                o.LineColor = Ole(Color.RoyalBlue);
                o.LineWidth = 1;
                return sf;
            });
        }

        public void ShowUnderserved(IEnumerable<Residential> residents, IEnumerable<Facility> facilities)
        {
            var rs = residents.ToList();
            var fs = facilities.ToList();
            SetOverlay(OvUnder, () =>
            {
                if (rs.Count + fs.Count == 0) return null;
                var sf = NewShapefile(ShpfileType.SHP_POINT, "Ten");
                foreach (var k in rs) { AddPoint(sf, k.ViDo, k.KinhDo, k.TenKhu); AddLabel(sf, k.TenKhu, k.ViDo, k.KinhDo, Color.Red); }
                foreach (var f in fs) { AddPoint(sf, f.ViDo, f.KinhDo, f.TenCongTrinh); AddLabel(sf, f.TenCongTrinh, f.ViDo, f.KinhDo, Color.Red); }
                StylePoints(sf, tkPointShapeType.ptShapeCross, 12, Color.Red, Color.DarkRed);
                StyleLabels(sf, Color.DarkRed, 8);
                return sf;
            });
        }

        public void ShowCandidates(IEnumerable<Candidate> candidates)
        {
            var cs = candidates.ToList();
            SetOverlay(OvCandidates, () =>
            {
                if (cs.Count == 0) return null;
                var sf = NewShapefile(ShpfileType.SHP_POINT, "Hang");
                foreach (var c in cs) { AddPoint(sf, c.ViDo, c.KinhDo, c.Hang.ToString()); AddLabel(sf, "#" + c.Hang, c.ViDo, c.KinhDo, Color.Black); }
                StylePoints(sf, tkPointShapeType.ptShapeStar, 16, Color.Gold, Color.DarkRed);
                StyleLabels(sf, Color.Black, 10);
                return sf;
            });
        }

        /// <summary>Khung vùng phân tích/tra cứu (hình chữ nhật).</summary>
        public void ShowArea(Bounds b)
        {
            var ring = new List<LatLon>
            {
                new LatLon(b.MinLat, b.MinLon), new LatLon(b.MaxLat, b.MinLon), new LatLon(b.MaxLat, b.MaxLon),
                new LatLon(b.MinLat, b.MaxLon), new LatLon(b.MinLat, b.MinLon)
            };
            SetOverlay(OvArea, () =>
            {
                var sf = NewShapefile(ShpfileType.SHP_POLYGON, "Ten");
                sf.EditAddShape(NewShape(ShpfileType.SHP_POLYGON, ring));
                var o = sf.DefaultDrawingOptions;
                o.FillVisible = false;
                o.LineColor = Ole(Color.Crimson);
                o.LineWidth = 2;
                return sf;
            });
        }

        public void ShowCircleArea(LatLon center, double radiusM)
        {
            SetOverlay(OvArea, () =>
            {
                var sf = NewShapefile(ShpfileType.SHP_POLYGON, "Ten");
                sf.EditAddShape(NewShape(ShpfileType.SHP_POLYGON, GeoUtil.Circle(center.Lat, center.Lon, radiusM, 64)));
                var o = sf.DefaultDrawingOptions;
                o.FillVisible = false;
                o.LineColor = Ole(Color.Crimson);
                o.LineWidth = 2;
                return sf;
            });
        }
    }
}
