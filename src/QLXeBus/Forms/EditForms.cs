using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using QLXeBus.Data;
using QLXeBus.Models;

namespace QLXeBus.Forms
{
    public static class Num
    {
        public static string Fmt(double v) { return v.ToString("0.000000", CultureInfo.InvariantCulture); }
        public static bool TryParse(string s, out double v)
        {
            return double.TryParse((s ?? "").Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out v);
        }
    }

    /// <summary>Xem / cập nhật thuộc tính trạm; cũng dùng để nhập trạm mới khi đề xuất.</summary>
    public class StationEditForm : Form
    {
        readonly Station _s;
        readonly bool _isNew;
        readonly TextBox txtMa = UI.Txt(130, 15, 120), txtTen = UI.Txt(130, 47, 280), txtY = UI.Txt(130, 175, 120), txtX = UI.Txt(130, 207, 120),
                         txtTuyen = UI.Txt(130, 239, 280);
        readonly ComboBox cbLoai = UI.Combo(130, 79, 280), cbDoan = UI.Combo(130, 111, 280), cbPt = UI.Combo(130, 143, 280), cbTt = UI.Combo(130, 271, 160);

        static List<LookupItem> WithBlank(List<LookupItem> l)
        {
            l.Insert(0, new LookupItem { Ma = null, Ten = "(không)" });
            return l;
        }

        public StationEditForm(Station s, bool isNew, bool readOnly)
        {
            _s = s; _isNew = isNew;
            Text = isNew ? "Trạm đề xuất mới" : (readOnly ? "Thông tin trạm" : "Cập nhật trạm");
            ClientSize = new System.Drawing.Size(430, 360);
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; StartPosition = FormStartPosition.CenterParent;

            cbLoai.DataSource = Repo.LoaiTram(); cbLoai.DisplayMember = "Ten"; cbLoai.ValueMember = "Ma";
            cbDoan.DataSource = WithBlank(Repo.DoanDuong()); cbDoan.DisplayMember = "Ten"; cbDoan.ValueMember = "Ma";
            cbPt.DataSource = WithBlank(Repo.PhuongTien()); cbPt.DisplayMember = "Ten"; cbPt.ValueMember = "Ma";
            cbTt.DataSource = TrangThai.Tram.ToList();

            txtMa.Text = isNew ? "(tự sinh)" : s.MaTram; txtMa.ReadOnly = true;
            txtTen.Text = s.TenTram;
            if (s.MaLoaiTram != null) cbLoai.SelectedValue = s.MaLoaiTram;
            if (s.MaDoan != null) cbDoan.SelectedValue = s.MaDoan; else cbDoan.SelectedIndex = 0;
            if (s.MaPhuongTien != null) cbPt.SelectedValue = s.MaPhuongTien; else cbPt.SelectedIndex = 0;
            txtY.Text = Num.Fmt(s.ViDo); txtX.Text = Num.Fmt(s.KinhDo);
            txtTuyen.Text = s.TenTuyen ?? "(chưa gán tuyến)"; txtTuyen.ReadOnly = true;
            cbTt.SelectedItem = s.TrangThai ?? TrangThai.HoatDong;

            var ok = UI.Btn("Lưu", 240, 315, 80, (a, b) => Save());
            var cancel = UI.Btn(readOnly ? "Đóng" : "Hủy", 330, 315, 80, (a, b) => DialogResult = DialogResult.Cancel);
            Controls.AddRange(new Control[]
            {
                UI.Lbl("Mã trạm", 15, 15), txtMa, UI.Lbl("Tên trạm", 15, 47), txtTen, UI.Lbl("Loại trạm", 15, 79), cbLoai,
                UI.Lbl("Đoạn đường", 15, 111), cbDoan, UI.Lbl("Phương tiện", 15, 143), cbPt,
                UI.Lbl("Vĩ độ", 15, 175), txtY, UI.Lbl("Kinh độ", 15, 207), txtX, UI.Lbl("Tuyến", 15, 239), txtTuyen,
                UI.Lbl("Trạng thái", 15, 271), cbTt
            });
            if (readOnly)
            {
                foreach (Control c in Controls) if (c is TextBox || c is ComboBox) c.Enabled = false;
            }
            else Controls.Add(ok);
            Controls.Add(cancel);
        }

        public Station Result { get { return _s; } }

        void Save()
        {
            double y, x;
            if (string.IsNullOrWhiteSpace(txtTen.Text)) { UI.Error(this, "Tên trạm không được để trống."); return; }
            if (!Num.TryParse(txtY.Text, out y) || y < -90 || y > 90) { UI.Error(this, "Vĩ độ không hợp lệ (-90..90)."); return; }
            if (!Num.TryParse(txtX.Text, out x) || x < -180 || x > 180) { UI.Error(this, "Kinh độ không hợp lệ (-180..180)."); return; }
            _s.TenTram = txtTen.Text.Trim();
            _s.MaLoaiTram = (string)cbLoai.SelectedValue;
            _s.MaDoan = (string)cbDoan.SelectedValue;
            _s.MaPhuongTien = (string)cbPt.SelectedValue;
            _s.ViDo = y; _s.KinhDo = x;
            _s.TrangThai = (string)cbTt.SelectedItem;
            if (UI.Try(this, () =>
            {
                if (_isNew) _s.MaTram = Repo.InsertStation(_s); else Repo.UpdateStation(_s);
            })) DialogResult = DialogResult.OK;
        }
    }

    public class RouteEditForm : Form
    {
        readonly RouteInfo _r;
        readonly TextBox txtMa = UI.Txt(110, 15, 100), txtTen = UI.Txt(110, 47, 290), txtMoTa = UI.Txt(110, 79, 290);
        readonly ComboBox cbTt = UI.Combo(110, 143, 150);

        public RouteEditForm(RouteInfo r)
        {
            _r = r;
            Text = "Cập nhật tuyến";
            ClientSize = new System.Drawing.Size(420, 240);
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; StartPosition = FormStartPosition.CenterParent;
            txtMa.Text = r.MaTuyen; txtMa.ReadOnly = true;
            txtTen.Text = r.TenTuyen;
            txtMoTa.Text = r.MoTa; txtMoTa.Multiline = true; txtMoTa.Height = 56;
            cbTt.DataSource = TrangThai.Chung.ToList(); cbTt.SelectedItem = r.TrangThai ?? TrangThai.HoatDong;
            Controls.AddRange(new Control[]
            {
                UI.Lbl("Mã tuyến", 15, 15, 90), txtMa, UI.Lbl("Tên tuyến", 15, 47, 90), txtTen, UI.Lbl("Mô tả", 15, 79, 90), txtMoTa,
                UI.Lbl("Trạng thái", 15, 143, 90), cbTt,
                UI.Btn("Lưu", 230, 195, 80, (a, b) => Save()), UI.Btn("Hủy", 320, 195, 80, (a, b) => DialogResult = DialogResult.Cancel)
            });
        }

        void Save()
        {
            if (string.IsNullOrWhiteSpace(txtTen.Text)) { UI.Error(this, "Tên tuyến không được để trống."); return; }
            _r.TenTuyen = txtTen.Text.Trim(); _r.MoTa = txtMoTa.Text.Trim(); _r.TrangThai = (string)cbTt.SelectedItem;
            if (UI.Try(this, () => Repo.UpdateRoute(_r))) DialogResult = DialogResult.OK;
        }
    }

    /// <summary>Gán trạm vào tuyến và xác định thứ tự phục vụ.</summary>
    public class AssignStationsForm : Form
    {
        readonly RouteInfo _route;
        readonly ListBox lstIn = new ListBox { Left = 15, Top = 40, Width = 260, Height = 330, DisplayMember = "Text" };
        readonly ListBox lstOut = new ListBox { Left = 385, Top = 40, Width = 260, Height = 330, DisplayMember = "Text" };

        class Item
        {
            public Station S;
            public string Text { get { return S.MaTram + " - " + S.TenTram + (S.MaTuyen != null && S.TenTuyen != null ? "  [" + S.MaTuyen + "]" : ""); } }
        }

        public AssignStationsForm(RouteInfo route)
        {
            _route = route;
            Text = "Gán trạm vào tuyến " + route.MaTuyen + " - " + route.TenTuyen;
            ClientSize = new System.Drawing.Size(660, 420);
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; StartPosition = FormStartPosition.CenterParent;

            foreach (var s in Repo.Stations().Where(s => s.TrangThai != TrangThai.Ngung))
            {
                if (s.MaTuyen == route.MaTuyen) lstIn.Items.Add(new Item { S = s });
                else lstOut.Items.Add(new Item { S = s });
            }
            Controls.AddRange(new Control[]
            {
                new Label { Left = 15, Top = 15, Width = 260, Text = "Trạm trên tuyến (theo thứ tự)" }, lstIn,
                new Label { Left = 385, Top = 15, Width = 260, Text = "Trạm chưa thuộc tuyến này (tuyến khác sẽ bị chuyển)" }, lstOut,
                UI.Btn("◄ Thêm", 285, 120, 90, (a, b) => MoveItem(lstOut, lstIn)),
                UI.Btn("Gỡ ►", 285, 155, 90, (a, b) => MoveItem(lstIn, lstOut)),
                UI.Btn("▲ Lên", 285, 215, 90, (a, b) => Shift(-1)),
                UI.Btn("▼ Xuống", 285, 250, 90, (a, b) => Shift(1)),
                UI.Btn("Lưu", 470, 380, 80, (a, b) => Save()), UI.Btn("Hủy", 560, 380, 80, (a, b) => DialogResult = DialogResult.Cancel)
            });
        }

        static void MoveItem(ListBox from, ListBox to)
        {
            var it = from.SelectedItem;
            if (it == null) return;
            from.Items.Remove(it); to.Items.Add(it); to.SelectedItem = it;
        }

        void Shift(int d)
        {
            int i = lstIn.SelectedIndex, j = i + d;
            if (i < 0 || j < 0 || j >= lstIn.Items.Count) return;
            var it = lstIn.Items[i];
            lstIn.Items.RemoveAt(i); lstIn.Items.Insert(j, it); lstIn.SelectedIndex = j;
        }

        void Save()
        {
            var ids = lstIn.Items.Cast<Item>().Select(i => i.S.MaTram).ToList();
            if (UI.Try(this, () => Repo.AssignStations(_route.MaTuyen, ids))) DialogResult = DialogResult.OK;
        }
    }
}
