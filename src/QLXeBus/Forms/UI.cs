using System;
using System.Drawing;
using System.Windows.Forms;

namespace QLXeBus.Forms
{
    /// <summary>Tiện ích dựng giao diện bằng mã (không dùng Designer để dễ merge khi làm nhóm).</summary>
    public static class UI
    {
        public static void Info(IWin32Window o, string msg) { MessageBox.Show(o, msg, "QLXeBus", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        public static void Error(IWin32Window o, string msg) { MessageBox.Show(o, msg, "QLXeBus", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        public static bool Confirm(IWin32Window o, string msg) { return MessageBox.Show(o, msg, "QLXeBus", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes; }

        /// <summary>Chạy hành động; lỗi (kể cả thiếu quyền, lỗi DB) hiện hộp thoại thay vì làm sập ứng dụng.</summary>
        public static bool Try(IWin32Window o, Action a)
        {
            try { a(); return true; }
            catch (UnauthorizedAccessException ex) { MessageBox.Show(o, ex.Message, "Không đủ quyền", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            catch (Exception ex) { Error(o, ex.Message); }
            return false;
        }

        public static Label Lbl(string text, int x, int y, int w = 110)
        {
            return new Label { Text = text, Left = x, Top = y + 3, Width = w, AutoSize = false };
        }

        public static TextBox Txt(int x, int y, int w, bool password = false)
        {
            return new TextBox { Left = x, Top = y, Width = w, UseSystemPasswordChar = password };
        }

        public static ComboBox Combo(int x, int y, int w, bool dropDownList = true)
        {
            return new ComboBox { Left = x, Top = y, Width = w, DropDownStyle = dropDownList ? ComboBoxStyle.DropDownList : ComboBoxStyle.DropDown };
        }

        public static Button Btn(string text, int x, int y, int w = 100, EventHandler click = null)
        {
            var b = new Button { Text = text, Left = x, Top = y, Width = w, Height = 28 };
            if (click != null) b.Click += click;
            return b;
        }

        public static DataGridView Grid()
        {
            return new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells, BackgroundColor = SystemColors.Window,
                AutoGenerateColumns = true
            };
        }

        public static Form Dialog(string title, int w, int h)
        {
            return new Form
            {
                Text = title, ClientSize = new Size(w, h), StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, ShowInTaskbar = false
            };
        }
    }
}
