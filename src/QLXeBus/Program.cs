using System;
using System.Windows.Forms;
using QLXeBus.Forms;

namespace QLXeBus
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            while (true)
            {
                using (var login = new LoginForm())
                    if (login.ShowDialog() != DialogResult.OK) return;
                var main = new MainForm();
                Application.Run(main);
                if (!main.LoggedOut) return;        // Đăng xuất → quay lại màn hình đăng nhập
            }
        }
    }
}
