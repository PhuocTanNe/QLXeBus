using System;
using System.Windows.Forms;
using QLXeBus.Data;
using QLXeBus.Services;

namespace QLXeBus.Forms
{
    public class LoginForm : Form
    {
        readonly TextBox txtUser = UI.Txt(130, 60, 200);
        readonly TextBox txtPass = UI.Txt(130, 92, 200, true);
        readonly Label lblInfo = new Label { Left = 20, Top = 20, Width = 330, Height = 30, Text = "Hệ thống GIS hỗ trợ quản lý, phân tích, thiết lập trạm xe buýt Hà Nội" };

        public LoginForm()
        {
            Text = "Đăng nhập - QLXeBus";
            ClientSize = new System.Drawing.Size(360, 195);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;

            var btnLogin = UI.Btn("Đăng nhập", 130, 130, 95, (s, e) => DoLogin());
            var btnDb = UI.Btn("Kết nối CSDL…", 232, 130, 98, (s, e) => { using (var f = new DbConfigForm()) f.ShowDialog(this); });
            AcceptButton = btnLogin;
            Controls.AddRange(new Control[]
            {
                lblInfo, UI.Lbl("Tên đăng nhập", 20, 60), txtUser, UI.Lbl("Mật khẩu", 20, 92), txtPass, btnLogin, btnDb,
                new Label { Left = 20, Top = 168, Width = 330, ForeColor = System.Drawing.Color.Gray, Text = "Lần đầu: admin / admin@123 (xem README)" }
            });
        }

        void DoLogin()
        {
            Cursor = Cursors.WaitCursor;
            try
            {
                AuthService.EnsureDefaultUsers();
                var err = AuthService.Login(txtUser.Text, txtPass.Text);
                if (err != null) { UI.Error(this, err); txtPass.SelectAll(); txtPass.Focus(); return; }
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                UI.Error(this, "Không kết nối được CSDL (" + Db.Config.Database + "@" + Db.Config.Host + "): " + ex.Message +
                               "\n\nBấm \"Kết nối CSDL…\" để kiểm tra cấu hình và chắc chắn đã chạy các script trong thư mục database/.");
            }
            finally { Cursor = Cursors.Default; }
        }
    }

    public class DbConfigForm : Form
    {
        readonly TextBox host = UI.Txt(130, 15, 220), port = UI.Txt(130, 47, 80), db = UI.Txt(130, 79, 220),
                         user = UI.Txt(130, 111, 220), pass = UI.Txt(130, 143, 220, true), dump = UI.Txt(130, 175, 220);

        public DbConfigForm()
        {
            Text = "Cấu hình kết nối PostgreSQL";
            ClientSize = new System.Drawing.Size(380, 270);
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            var c = Db.Config;
            host.Text = c.Host; port.Text = c.Port.ToString(); db.Text = c.Database; user.Text = c.Username; pass.Text = c.Password; dump.Text = c.PgDumpPath;
            Controls.AddRange(new Control[]
            {
                UI.Lbl("Máy chủ", 15, 15), host, UI.Lbl("Cổng", 15, 47), port, UI.Lbl("Cơ sở dữ liệu", 15, 79), db,
                UI.Lbl("Tài khoản", 15, 111), user, UI.Lbl("Mật khẩu", 15, 143), pass, UI.Lbl("Đường dẫn pg_dump", 15, 175), dump,
                UI.Btn("Kiểm tra", 130, 215, 90, (s, e) => Test()),
                UI.Btn("Lưu", 230, 215, 60, (s, e) => { Apply(); Db.Save(); DialogResult = DialogResult.OK; }),
                UI.Btn("Hủy", 295, 215, 60, (s, e) => DialogResult = DialogResult.Cancel),
                new Label { Left = 15, Top = 248, Width = 350, ForeColor = System.Drawing.Color.Gray, Text = "Lưu tại %AppData%\\QLXeBus\\dbconfig.ini (riêng từng máy)." }
            });
        }

        void Apply()
        {
            var c = Db.Config;
            c.Host = host.Text.Trim(); c.Database = db.Text.Trim(); c.Username = user.Text.Trim(); c.Password = pass.Text; c.PgDumpPath = dump.Text.Trim();
            int p; c.Port = int.TryParse(port.Text, out p) ? p : 5432;
        }

        void Test()
        {
            Apply();
            if (UI.Try(this, () => { using (Db.Open()) { } })) UI.Info(this, "Kết nối thành công.");
        }
    }
}
