using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using QLXeBus.Data;
using QLXeBus.MapView;
using QLXeBus.Models;
using QLXeBus.Services;

namespace QLXeBus.Forms
{
    public partial class MainForm
    {
        TabPage tabRoutes;
        readonly DataGridView gridRoutes = UI.Grid(), gridRouteStations = UI.Grid();
        readonly TextBox txtRtName = new TextBox { Width = 160 };
        readonly ComboBox cbRtStatus = new ComboBox { Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
        List<RouteInfo> _routes = new List<RouteInfo>();

        void BuildRouteTab()
        {
            if (!Session.Can("TUYEN_XEM")) return;
            tabRoutes = new TabPage("Tuyến");

            var pnl = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 112, Padding = new Padding(4) };
            pnl.Controls.Add(new Label { Text = "Tên/mã", Width = 55, TextAlign = System.Drawing.ContentAlignment.MiddleLeft });
            pnl.Controls.Add(txtRtName);
            pnl.Controls.Add(new Label { Text = "Trạng thái", Width = 65, TextAlign = System.Drawing.ContentAlignment.MiddleLeft });
            pnl.Controls.Add(cbRtStatus);
            cbRtStatus.DataSource = new[] { "(tất cả)" }.Concat(TrangThai.Chung).ToList();

            var btnSearch = UI.Btn("Tra cứu thuộc tính", 0, 0, 130, (s, e) => FilterRoutes(null));
            btnSearch.Enabled = Session.Can("TUYEN_TC_TT");
            var btnBox = UI.Btn("Tuyến qua vùng chọn…", 0, 0, 150, (s, e) =>
                SetTool(MapTool.SelectBox, Pending.None, b =>
                {
                    var ids = new HashSet<string>(_stations.Where(x => x.MaTuyen != null && b.Contains(x.ViDo, x.KinhDo)).Select(x => x.MaTuyen));
                    map.ShowArea(b);
                    FilterRoutes(ids);
                    SetTool(MapTool.Pan, Pending.None, null, "Sẵn sàng");
                }, "Kéo hộp trên bản đồ: tìm các tuyến có trạm trong vùng"));
            btnBox.Enabled = Session.Can("TUYEN_TC_KG");
            var btnAll = UI.Btn("Hiện tất cả", 0, 0, 90, (s, e) => { txtRtName.Text = ""; cbRtStatus.SelectedIndex = 0; BindRoutes(_routes); });
            var btnEdit = UI.Btn("Sửa tuyến…", 0, 0, 100, (s, e) => EditSelectedRoute());
            btnEdit.Enabled = Session.Can("TUYEN_SUA");
            var btnAssign = UI.Btn("Gán trạm vào tuyến…", 0, 0, 150, (s, e) => AssignToSelectedRoute());
            btnAssign.Enabled = Session.Can("TUYEN_GAN_TRAM");
            foreach (var b in new Control[] { btnSearch, btnBox, btnAll, btnEdit, btnAssign }) pnl.Controls.Add(b);

            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
            split.Panel1.Controls.Add(gridRoutes);
            split.Panel2.Controls.Add(gridRouteStations);
            gridRoutes.SelectionChanged += (s, e) => ShowSelectedRoute();
            gridRoutes.CellDoubleClick += (s, e) => EditSelectedRoute();

            tabRoutes.Controls.Add(split);
            tabRoutes.Controls.Add(pnl);
            tabs.TabPages.Add(tabRoutes);
        }

        RouteInfo SelectedRoute()
        {
            return gridRoutes.CurrentRow == null ? null : gridRoutes.CurrentRow.DataBoundItem as RouteInfo;
        }

        void ReloadRouteTab()
        {
            if (tabRoutes == null) return;
            _routes = Repo.Routes();
            BindRoutes(_routes);
        }

        void BindRoutes(IList<RouteInfo> list)
        {
            gridRoutes.DataSource = new BindingList<RouteInfo>(list.ToList());
            lblMsg.Text = list.Count + " tuyến";
        }

        void FilterRoutes(HashSet<string> idsInArea)
        {
            string kw = txtRtName.Text.Trim().ToLowerInvariant();
            string tt = cbRtStatus.SelectedIndex > 0 ? (string)cbRtStatus.SelectedItem : null;
            BindRoutes(_routes.Where(r =>
                (kw == "" || r.MaTuyen.ToLowerInvariant().Contains(kw) || (r.TenTuyen ?? "").ToLowerInvariant().Contains(kw)) &&
                (tt == null || r.TrangThai == tt) && (idsInArea == null || idsInArea.Contains(r.MaTuyen))).ToList());
        }

        /// <summary>Xem thông tin tuyến: danh sách trạm theo thứ tự và đường đi trên bản đồ.</summary>
        void ShowSelectedRoute()
        {
            var r = SelectedRoute();
            if (r == null || map == null) { gridRouteStations.DataSource = null; return; }
            UI.Try(this, () =>
            {
                var sts = Repo.StationsOfRoute(r.MaTuyen);
                gridRouteStations.DataSource = new BindingList<Station>(sts);
                map.ShowRoute(sts);
                if (sts.Count > 0)
                    map.GoTo(new Bounds(sts.Min(s => s.ViDo), sts.Min(s => s.KinhDo), sts.Max(s => s.ViDo), sts.Max(s => s.KinhDo)).Expand(500));
            });
        }

        void EditSelectedRoute()
        {
            var r = SelectedRoute();
            if (r == null || !Session.Can("TUYEN_SUA")) return;
            using (var f = new RouteEditForm(r)) if (f.ShowDialog(this) == DialogResult.OK) ReloadData();
        }

        void AssignToSelectedRoute()
        {
            var r = SelectedRoute();
            if (r == null) return;
            using (var f = new AssignStationsForm(r)) if (f.ShowDialog(this) == DialogResult.OK) ReloadData();
        }
    }
}
