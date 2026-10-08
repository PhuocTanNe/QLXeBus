using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using QLXeBus.Data;
using QLXeBus.Services;

namespace QLXeBus.Forms
{
    /// <summary>Quản trị: người dùng, phân quyền theo nhóm, lịch sử đăng nhập, sao lưu.</summary>
    public class UserAdminForm : Form
    {
        readonly TabControl tabs = new TabControl { Dock = DockStyle.Fill };
        readonly DataGridView gridUsers = UI.Grid(), gridHist = UI.Grid(), gridBak = UI.Grid();
        readonly ComboBox cbGroup = UI.Combo(80, 8, 220);
        readonly CheckedListBox lstPerm = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true };
        DataTable _perms;

        public UserAdminForm()
        {
            Text = "Quản trị hệ thống";
            Width = 900; Height = 560; StartPosition = FormStartPosition.CenterParent;

            // --- Người dùng
            var tpUsers = new TabPage("Người dùng");
            var pnlU = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(6) };
            pnlU.Controls.Add(UI.Btn("Thêm", 0, 0, 80, (s, e) => EditUser(null)));
            pnlU.Controls.Add(UI.Btn("Sửa / đặt lại MK", 0, 0, 130, (s, e) => EditUser(SelectedId(gridUsers))));
            pnlU.Controls.Add(UI.Btn("Xóa", 0, 0, 80, (s, e) => DeleteUser()));
            tpUsers.Controls.Add(gridUsers); tpUsers.Controls.Add(pnlU);

            // --- Phân quyền
            var tpPerm = new TabPage("Phân quyền");
            var pnlP = new Panel { Dock = DockStyle.Top, Height = 44 };
            pnlP.Controls.AddRange(new Control[] { UI.Lbl("Nhóm", 8, 8, 70), cbGroup, UI.Btn("Lưu phân quyền", 320, 6, 130, (s, e) => SavePerm()) });
            tpPerm.Controls.Add(lstPerm); tpPerm.Controls.Add(pnlP);
            cbGroup.SelectedIndexChanged += (s, e) => LoadPerm();

            // --- Lịch sử
            var tpHist = new TabPage("Lịch sử đăng nhập");
            tpHist.Controls.Add(gridHist);

            // --- Sao lưu
            var tpBak = new TabPage("Sao lưu");
            var pnlB = new Panel { Dock = DockStyle.Top, Height = 44 };
            pnlB.Controls.Add(UI.Btn("Sao lưu ngay (pg_dump)…", 8, 6, 200, (s, e) => Backup()));
            tpBak.Controls.Add(gridBak); tpBak.Controls.Add(pnlB);

            if (Session.Can("QT_THEM_ND") || Session.Can("QT_SUA_ND") || Session.Can("QT_XOA_ND")) tabs.TabPages.Add(tpUsers);
            if (Session.Can("QT_PHANQUYEN")) tabs.TabPages.Add(tpPerm);
            if (Session.Can("QT_LICHSU")) tabs.TabPages.Add(tpHist);
            if (Session.Can("QT_SAOLUU")) tabs.TabPages.Add(tpBak);
            Controls.Add(tabs);

            Load += (s, e) => UI.Try(this, Reload);
        }

        void Reload()
        {
            gridUsers.DataSource = AdminRepo.Users();
            if (Session.Can("QT_LICHSU")) gridHist.DataSource = AdminRepo.LoginHistory(500);
            gridBak.DataSource = AdminRepo.Backups();
            if (cbGroup.DataSource == null)
            {
                var g = AdminRepo.Groups();
                cbGroup.DisplayMember = "tennhom"; cbGroup.ValueMember = "idnhom"; cbGroup.DataSource = g;
                _perms = AdminRepo.Permissions();
                foreach (DataRow r in _perms.Rows)
                    lstPerm.Items.Add(Db.Str(r["idquyen"]) + " [" + Db.Str(r["loaithaotac"]) + "] " + Db.Str(r["tenquyen"]));
                LoadPerm();
            }
        }

        static string SelectedId(DataGridView g)
        {
            return g.CurrentRow == null ? null : Convert.ToString(g.CurrentRow.Cells[0].Value);
        }

        void EditUser(string id)
        {
            if (id == null && !Session.Can("QT_THEM_ND")) { UI.Error(this, "Bạn không có quyền thêm người dùng."); return; }
            using (var f = new UserEditForm(id)) if (f.ShowDialog(this) == DialogResult.OK) UI.Try(this, Reload);
        }

        void DeleteUser()
        {
            var id = SelectedId(gridUsers);
            if (id == null || !UI.Confirm(this, "Xóa người dùng " + id + "?")) return;
            if (UI.Try(this, () => AdminRepo.DeleteUser(id))) UI.Try(this, Reload);
        }

        void LoadPerm()
        {
            if (cbGroup.SelectedValue == null || _perms == null) return;
            var have = AdminRepo.GroupPermissionIds((string)cbGroup.SelectedValue);
            for (int i = 0; i < _perms.Rows.Count; i++) lstPerm.SetItemChecked(i, have.Contains(Db.Str(_perms.Rows[i]["idquyen"])));
        }

        void SavePerm()
        {
            var ids = new List<string>();
            for (int i = 0; i < _perms.Rows.Count; i++) if (lstPerm.GetItemChecked(i)) ids.Add(Db.Str(_perms.Rows[i]["idquyen"]));
            if (UI.Try(this, () => AdminRepo.SaveGroupPermissions((string)cbGroup.SelectedValue, ids)))
                UI.Info(this, "Đã lưu. Người dùng cần đăng nhập lại để quyền mới có hiệu lực.");
        }

        void Backup()
        {
            using (var d = new FolderBrowserDialog { Description = "Chọn thư mục lưu bản sao lưu" })
            {
                if (d.ShowDialog(this) != DialogResult.OK) return;
                string file = null;
                Cursor = Cursors.WaitCursor;
                var ok = UI.Try(this, () => file = AdminRepo.Backup(d.SelectedPath));
                Cursor = Cursors.Default;
                if (ok) { UI.Info(this, "Đã sao lưu: " + file); UI.Try(this, Reload); }
            }
        }
    }

    public class UserEditForm : Form
    {
        readonly string _id;
        readonly TextBox txtTen = UI.Txt(120, 15, 250), txtCv = UI.Txt(120, 47, 250), txtDv = UI.Txt(120, 79, 250),
                         txtDn = UI.Txt(120, 111, 250), txtPw = UI.Txt(120, 143, 250, true);
        readonly ComboBox cbNhom = UI.Combo(120, 175, 250), cbTt = UI.Combo(120, 207, 150);

        public UserEditForm(string id)
        {
            _id = id;
            Text = id == null ? "Thêm người dùng" : "Sửa người dùng " + id;
            ClientSize = new System.Drawing.Size(400, 300);
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; StartPosition = FormStartPosition.CenterParent;
            cbNhom.DataSource = AdminRepo.Groups(); cbNhom.DisplayMember = "tennhom"; cbNhom.ValueMember = "idnhom";
            cbTt.DataSource = Models.TrangThai.Chung.ToList();
            if (id != null)
            {
                var r = AdminRepo.GetUser(id);
                txtTen.Text = Db.Str(r["tenndung"]); txtCv.Text = Db.Str(r["chucvu"]); txtDv.Text = Db.Str(r["donvi"]);
                txtDn.Text = Db.Str(r["tendnhap"]); cbNhom.SelectedValue = Db.Str(r["idnhom"]); cbTt.SelectedItem = Db.Str(r["trangthai"]);
            }
            Controls.AddRange(new Control[]
            {
                UI.Lbl("Họ tên", 15, 15), txtTen, UI.Lbl("Chức vụ", 15, 47), txtCv, UI.Lbl("Đơn vị", 15, 79), txtDv,
                UI.Lbl("Tên đăng nhập", 15, 111), txtDn, UI.Lbl(id == null ? "Mật khẩu" : "Mật khẩu mới", 15, 143), txtPw,
                UI.Lbl("Nhóm", 15, 175), cbNhom, UI.Lbl("Trạng thái", 15, 207), cbTt,
                new Label { Left = 120, Top = 235, Width = 260, ForeColor = System.Drawing.Color.Gray, Text = id == null ? "" : "(để trống nếu không đổi mật khẩu)" },
                UI.Btn("Lưu", 200, 260, 80, (a, b) => Save()), UI.Btn("Hủy", 290, 260, 80, (a, b) => DialogResult = DialogResult.Cancel)
            });
        }

        void Save()
        {
            if (string.IsNullOrWhiteSpace(txtTen.Text) || string.IsNullOrWhiteSpace(txtDn.Text)) { UI.Error(this, "Cần nhập họ tên và tên đăng nhập."); return; }
            if (UI.Try(this, () => AdminRepo.SaveUser(_id, txtTen.Text.Trim(), txtCv.Text.Trim(), txtDv.Text.Trim(), txtDn.Text.Trim(),
                    txtPw.Text, (string)cbNhom.SelectedValue, (string)cbTt.SelectedItem)))
                DialogResult = DialogResult.OK;
        }
    }
}
