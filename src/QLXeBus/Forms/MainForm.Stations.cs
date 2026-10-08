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
        TabPage tabStations;
        readonly DataGridView gridStations = UI.Grid();
        readonly TextBox txtStName = new TextBox { Width = 150 };
        readonly ComboBox cbStRoute = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
        readonly ComboBox cbStType = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
        readonly ComboBox cbStDistrict = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
        readonly ComboBox cbStStatus = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
        readonly NumericUpDown numRadius = new NumericUpDown { Width = 70, Minimum = 50, Maximum = 20000, Value = 500, Increment = 50 };
        readonly Label lblStCount = new Label { AutoSize = true };

        static List<LookupItem> All(List<LookupItem> l, string text)
        {
            l.Insert(0, new LookupItem { Ma = null, Ten = text });
            return l;
        }

        void BuildStationTab()
        {
            if (!Session.Can("TRAM_XEM")) return;
            tabStations = new TabPage("Trạm");

            var pnl = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 190, Padding = new Padding(4), WrapContents = true };
            Action<string, Control> add = (t, c) =>
            {
                pnl.Controls.Add(new Label { Text = t, Width = 70, TextAlign = System.Drawing.ContentAlignment.MiddleLeft });
                pnl.Controls.Add(c);
            };
            add("Tên/mã", txtStName); add("Tuyến", cbStRoute); add("Loại trạm", cbStType); add("Quận/huyện", cbStDistrict); add("Trạng thái", cbStStatus);
            var btnSearch = UI.Btn("Tra cứu thuộc tính", 0, 0, 130, (s, e) => SearchStationsByAttribute());
            btnSearch.Enabled = Session.Can("TRAM_TC_TT");
            var btnReset = UI.Btn("Hiện tất cả", 0, 0, 90, (s, e) => { ResetStationFilter(); ShowStationResults(_stations, "Tất cả trạm"); });
            pnl.Controls.Add(btnSearch); pnl.Controls.Add(btnReset);

            var btnBox = UI.Btn("Tra cứu theo hộp…", 0, 0, 130, (s, e) =>
                SetTool(MapTool.SelectBox, Pending.None, b =>
                {
                    var found = _stations.Where(x => b.Contains(x.ViDo, x.KinhDo)).ToList();
                    map.ShowArea(b);
                    ShowStationResults(found, found.Count + " trạm trong vùng chọn");
                    SetTool(MapTool.Pan, Pending.None, null, "Sẵn sàng");
                }, "Kéo chuột trên bản đồ để vẽ vùng tra cứu"));
            var btnRad = UI.Btn("Theo bán kính (m):", 0, 0, 130, (s, e) =>
                SetTool(MapTool.PickPoint, Pending.RadiusSearch, null, "Nhấp vào tâm vùng tra cứu"));
            btnBox.Enabled = btnRad.Enabled = Session.Can("TRAM_TC_KG");
            pnl.Controls.Add(btnBox); pnl.Controls.Add(btnRad); pnl.Controls.Add(numRadius);

            var btnView = UI.Btn(Session.Can("TRAM_SUA") ? "Xem / sửa trạm…" : "Xem thông tin…", 0, 0, 130, (s, e) =>
            {
                var st = SelectedStation();
                if (st != null) { map.ShowHighlight(new List<Station> { st }); OpenStation(st); }
            });
            var btnZoom = UI.Btn("Phóng tới trạm", 0, 0, 110, (s, e) =>
            {
                var st = SelectedStation();
                if (st != null) { map.ShowHighlight(new List<Station> { st }); map.ZoomToLatLon(st.ViDo, st.KinhDo, 1.0); }
            });
            pnl.Controls.Add(btnView); pnl.Controls.Add(btnZoom); pnl.Controls.Add(lblStCount);

            gridStations.CellDoubleClick += (s, e) => { var st = SelectedStation(); if (st != null) OpenStation(st); };
            gridStations.CellClick += (s, e) => { var st = SelectedStation(); if (st != null) map.ShowHighlight(new List<Station> { st }); };

            tabStations.Controls.Add(gridStations);
            tabStations.Controls.Add(pnl);
            tabs.TabPages.Add(tabStations);
        }

        Station SelectedStation()
        {
            return gridStations.CurrentRow == null ? null : gridStations.CurrentRow.DataBoundItem as Station;
        }

        void SelectInGrid(string maTram)
        {
            foreach (DataGridViewRow r in gridStations.Rows)
            {
                var s = r.DataBoundItem as Station;
                if (s != null && s.MaTram == maTram) { r.Selected = true; gridStations.CurrentCell = r.Cells[0]; return; }
            }
        }

        void ResetStationFilter()
        {
            txtStName.Text = "";
            cbStRoute.SelectedIndex = cbStType.SelectedIndex = cbStDistrict.SelectedIndex = cbStStatus.SelectedIndex = 0;
        }

        /// <summary>Gọi sau khi nạp lại dữ liệu từ CSDL: cập nhật danh sách chọn và các bảng.</summary>
        void OnDataReloaded()
        {
            if (tabStations != null)
            {
                cbStRoute.DataSource = All(Repo.Tuyen(), "(tất cả)"); cbStRoute.DisplayMember = "Ten"; cbStRoute.ValueMember = "Ma";
                cbStType.DataSource = All(Repo.LoaiTram(), "(tất cả)"); cbStType.DisplayMember = "Ten"; cbStType.ValueMember = "Ma";
                cbStDistrict.DataSource = All(Repo.Quan(), "(tất cả)"); cbStDistrict.DisplayMember = "Ten"; cbStDistrict.ValueMember = "Ma";
                cbStStatus.DataSource = new[] { "(tất cả)" }.Concat(TrangThai.Tram).ToList();
                ShowStationResults(_stations, "Tất cả trạm");
            }
            ReloadRouteTab();
            ReloadProposalLookups();
        }

        void ShowStationResults(IList<Station> list, string message)
        {
            if (tabStations == null) return;
            gridStations.DataSource = new BindingList<Station>(list.ToList());
            lblStCount.Text = list.Count + " kết quả";
            lblMsg.Text = message;
            map.ShowHighlight(list.Count == _stations.Count ? new List<Station>() : list);
        }

        void SearchStationsByAttribute()
        {
            string kw = txtStName.Text.Trim().ToLowerInvariant();
            string tuyen = (string)cbStRoute.SelectedValue, loai = (string)cbStType.SelectedValue, quan = (string)cbStDistrict.SelectedValue;
            string tt = cbStStatus.SelectedIndex > 0 ? (string)cbStStatus.SelectedItem : null;
            var found = _stations.Where(s =>
                (kw == "" || (s.TenTram ?? "").ToLowerInvariant().Contains(kw) || s.MaTram.ToLowerInvariant().Contains(kw)) &&
                (tuyen == null || s.MaTuyen == tuyen) && (loai == null || s.MaLoaiTram == loai) &&
                (quan == null || s.MaQuan == quan) && (tt == null || s.TrangThai == tt)).ToList();
            ShowStationResults(found, found.Count + " trạm thỏa điều kiện");
            if (found.Count > 0)
                map.GoTo(new Bounds(found.Min(s => s.ViDo), found.Min(s => s.KinhDo), found.Max(s => s.ViDo), found.Max(s => s.KinhDo)).Expand(300));
        }
    }
}
