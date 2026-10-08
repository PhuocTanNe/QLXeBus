using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AxMapWinGIS;
using QLXeBus.Data;
using QLXeBus.MapView;
using QLXeBus.Models;
using QLXeBus.Services;

namespace QLXeBus.Forms
{
    /// <summary>Cửa sổ chính: bản đồ MapWinGIS (phải) + các tab chức năng theo quyền (trái).</summary>
    public partial class MainForm : Form
    {
        enum Pending { None, Identify, RadiusSearch }

        readonly AxMap axMap = new AxMap();
        MapController map;
        readonly TabControl tabs = new TabControl { Dock = DockStyle.Fill };
        readonly ToolStripStatusLabel lblCoord = new ToolStripStatusLabel { Spring = true, TextAlign = ContentAlignment.MiddleRight };
        readonly ToolStripStatusLabel lblMsg = new ToolStripStatusLabel("Sẵn sàng");

        List<Station> _stations = new List<Station>();
        List<Residential> _residents = new List<Residential>();
        List<Facility> _facilities = new List<Facility>();
        Pending _pending = Pending.None;
        Action<Bounds> _onBox;                  // hành động khi người dùng kéo hộp trên bản đồ

        public bool LoggedOut { get; private set; }

        public MainForm()
        {
            Text = "QLXeBus - Hệ thống GIS quản lý trạm xe buýt Hà Nội";
            Width = 1280; Height = 800; StartPosition = FormStartPosition.CenterScreen;

            var split = new SplitContainer { Dock = DockStyle.Fill, FixedPanel = FixedPanel.Panel1 };
            split.Panel1.Controls.Add(tabs);
            ((ISupportInitialize)axMap).BeginInit();
            axMap.Dock = DockStyle.Fill;
            split.Panel2.Controls.Add(axMap);
            ((ISupportInitialize)axMap).EndInit();
            split.SplitterDistance = 500;

            // Thứ tự Add: control Fill trước, thanh Top sau (thanh Add sau được dock trước).
            Controls.Add(split);
            var tool = BuildToolbar();
            var menu = BuildMenu();
            var status = new StatusStrip();
            status.Items.Add(lblMsg);
            status.Items.Add(new ToolStripStatusLabel("  |  " + Session.User.Ten + " (" + Session.User.TenNhom + ")"));
            status.Items.Add(lblCoord);
            Controls.Add(tool);
            Controls.Add(menu);
            Controls.Add(status);
            MainMenuStrip = menu;

            BuildStationTab();
            BuildRouteTab();
            BuildProposalTab();
            BuildStatsTab();

            Shown += (s, e) => OnFirstShown();
            FormClosing += (s, e) => { if (!LoggedOut) AuthService.Logout(); };
        }

        void OnFirstShown()
        {
            if (!UI.Try(this, () =>
            {
                map = new MapController(axMap);
                map.PointPicked += Map_PointPicked;
                map.BoxSelected += Map_BoxSelected;
                map.CursorMoved += p => lblCoord.Text = string.Format("Vĩ độ {0:0.00000}, Kinh độ {1:0.00000}", p.Lat, p.Lon);
            })) return;
            ReloadData();
        }

        // ---------------------------------------------------------------- menu & toolbar
        MenuStrip BuildMenu()
        {
            var ms = new MenuStrip();
            var sys = new ToolStripMenuItem("Hệ thống");
            sys.DropDownItems.Add(new ToolStripMenuItem("Làm mới dữ liệu", null, (s, e) => ReloadData(), Keys.F5));
            sys.DropDownItems.Add("Đăng xuất", null, (s, e) => { AuthService.Logout(); LoggedOut = true; Close(); });
            sys.DropDownItems.Add("Thoát", null, (s, e) => Close());
            ms.Items.Add(sys);

            if (new[] { "QT_THEM_ND", "QT_SUA_ND", "QT_XOA_ND", "QT_PHANQUYEN", "QT_LICHSU", "QT_SAOLUU" }.Any(Session.Can))
                ms.Items.Add(new ToolStripMenuItem("Quản trị", null, (s, e) => { using (var f = new UserAdminForm()) f.ShowDialog(this); }));

            var help = new ToolStripMenuItem("Trợ giúp");
            help.DropDownItems.Add("Giới thiệu", null, (s, e) => UI.Info(this,
                "QLXeBus - Hệ thống GIS hỗ trợ quản lý, phân tích, thiết lập trạm xe buýt (Hà Nội)\nVisual Studio 2019 · MapWinGIS · PostgreSQL (HQTCSDL)"));
            ms.Items.Add(help);
            return ms;
        }

        ToolStrip BuildToolbar()
        {
            var ts = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };
            ts.Items.Add(ToolBtn("Kéo bản đồ", () => SetTool(MapTool.Pan, Pending.None, null, "Kéo chuột để di chuyển bản đồ")));
            ts.Items.Add(ToolBtn("Phóng to", () => SetTool(MapTool.ZoomIn, Pending.None, null, "Nhấp/kéo hộp để phóng to")));
            ts.Items.Add(ToolBtn("Thu nhỏ", () => SetTool(MapTool.ZoomOut, Pending.None, null, "Nhấp để thu nhỏ")));
            ts.Items.Add(ToolBtn("Toàn cảnh Hà Nội", () => map.GoTo(new Bounds(20.93, 105.72, 21.10, 105.92))));
            ts.Items.Add(new ToolStripSeparator());
            ts.Items.Add(ToolBtn("Chọn trạm", () => SetTool(MapTool.PickPoint, Pending.Identify, null, "Nhấp vào một trạm để xem thông tin")));
            ts.Items.Add(ToolBtn("Xóa kết quả trên bản đồ", () => map.ClearAllOverlays()));
            ts.Items.Add(new ToolStripSeparator());

            var layers = new ToolStripDropDownButton("Lớp hiển thị");
            layers.DropDownItems.Add(LayerItem("Khu dân cư", true, v => map.ShowResidential = v));
            layers.DropDownItems.Add(LayerItem("Công trình", true, v => map.ShowFacilities = v));
            layers.DropDownItems.Add(LayerItem("Tuyến", true, v => map.ShowRoutes = v));
            layers.DropDownItems.Add(LayerItem("Tên trạm", false, v => map.ShowStationNames = v));
            ts.Items.Add(layers);
            return ts;
        }

        ToolStripButton ToolBtn(string text, Action a)
        {
            var b = new ToolStripButton(text) { DisplayStyle = ToolStripItemDisplayStyle.Text };
            b.Click += (s, e) => { if (map != null) UI.Try(this, a); };
            return b;
        }

        ToolStripMenuItem LayerItem(string text, bool on, Action<bool> set)
        {
            var it = new ToolStripMenuItem(text) { CheckOnClick = true, Checked = on };
            it.CheckedChanged += (s, e) => { if (map == null) return; set(it.Checked); map.Rebuild(); };
            return it;
        }

        /// <summary>Chọn công cụ bản đồ và hành động đi kèm (nhấp điểm / kéo hộp).</summary>
        void SetTool(MapTool tool, Pending pending, Action<Bounds> onBox, string hint)
        {
            map.SetTool(tool);
            _pending = pending;
            _onBox = onBox;
            lblMsg.Text = hint;
        }

        // ---------------------------------------------------------------- dữ liệu
        void ReloadData()
        {
            UI.Try(this, () =>
            {
                Cursor = Cursors.WaitCursor;
                try
                {
                    _stations = Repo.Stations();
                    _residents = Repo.Residentials();
                    _facilities = Repo.Facilities();
                    map.SetData(_stations, _residents, _facilities);
                    OnDataReloaded();
                    lblMsg.Text = string.Format("{0} trạm · {1} khu dân cư · {2} công trình", _stations.Count, _residents.Count, _facilities.Count);
                }
                finally { Cursor = Cursors.Default; }
            });
        }

        // ---------------------------------------------------------------- sự kiện bản đồ
        void Map_PointPicked(LatLon p, double tolM)
        {
            if (_pending == Pending.None) return;
            if (_pending == Pending.Identify)
            {
                double tol = Math.Max(30, tolM);
                Station best = null; double bd = double.MaxValue;
                foreach (var s in _stations)
                {
                    double d = GeoUtil.Distance(p.Lat, p.Lon, s.ViDo, s.KinhDo);
                    if (d < bd) { bd = d; best = s; }
                }
                if (best == null || bd > tol) { lblMsg.Text = "Không có trạm tại vị trí này."; return; }
                ShowStation(best);
            }
            else if (_pending == Pending.RadiusSearch)
            {
                double r = (double)numRadius.Value;
                var found = _stations.Where(s => GeoUtil.Distance(p.Lat, p.Lon, s.ViDo, s.KinhDo) <= r).ToList();
                map.ShowCircleArea(p, r);
                ShowStationResults(found, string.Format("{0} trạm trong bán kính {1:0} m", found.Count, r));
                _pending = Pending.None; map.SetTool(MapTool.Pan);
            }
        }

        void Map_BoxSelected(Bounds b)
        {
            var a = _onBox;
            if (a != null) a(b);
        }

        /// <summary>Xem thông tin một trạm: chọn trong danh sách, làm nổi trên bản đồ và mở hộp thông tin.</summary>
        void ShowStation(Station s)
        {
            map.ShowHighlight(new List<Station> { s });
            if (tabStations != null) { tabs.SelectedTab = tabStations; SelectInGrid(s.MaTram); }
            OpenStation(s);
        }

        void OpenStation(Station s)
        {
            bool canEdit = Session.Can("TRAM_SUA");
            using (var f = new StationEditForm(Repo.GetStation(s.MaTram) ?? s, false, !canEdit))
                if (f.ShowDialog(this) == DialogResult.OK) ReloadData();
        }
    }
}
