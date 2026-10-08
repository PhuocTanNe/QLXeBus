using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using QLXeBus.Data;
using QLXeBus.Models;
using QLXeBus.Services;

namespace QLXeBus.Forms
{
    public partial class MainForm
    {
        TabPage tabStats;
        readonly ComboBox cbStatKind = new ComboBox { Width = 230, DropDownStyle = ComboBoxStyle.DropDownList };
        readonly TextBox txtStatRoutes = new TextBox { Width = 150 };
        readonly NumericUpDown numStatRadius = new NumericUpDown { Width = 70, Minimum = 100, Maximum = 3000, Value = 500, Increment = 50 };
        readonly DataGridView gridStat = UI.Grid();
        readonly Chart chart = new Chart { Dock = DockStyle.Fill };
        readonly Label lblStatSummary = new Label { Dock = DockStyle.Bottom, Height = 50 };
        DataTable _statTable;

        void BuildStatsTab()
        {
            var kinds = new List<string>();
            if (Session.Can("TK_KHUVUC")) kinds.Add("Số lượng trạm theo khu vực");
            if (Session.Can("TK_TUYEN")) kinds.Add("Trạm theo tuyến xe buýt");
            if (Session.Can("TK_PHAMVI")) kinds.Add("Phạm vi phục vụ của trạm");
            if (kinds.Count == 0) return;
            tabStats = new TabPage("Thống kê");
            cbStatKind.DataSource = kinds;

            var pnl = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 112, Padding = new Padding(4) };
            pnl.Controls.Add(new Label { Text = "Thống kê", Width = 60, TextAlign = System.Drawing.ContentAlignment.MiddleLeft });
            pnl.Controls.Add(cbStatKind);
            pnl.Controls.Add(new Label { Text = "Mã tuyến (cách nhau dấu phẩy)", Width = 160, TextAlign = System.Drawing.ContentAlignment.MiddleLeft });
            pnl.Controls.Add(txtStatRoutes);
            pnl.Controls.Add(new Label { Text = "Bán kính phục vụ (m)", Width = 120, TextAlign = System.Drawing.ContentAlignment.MiddleLeft });
            pnl.Controls.Add(numStatRadius);
            pnl.Controls.Add(UI.Btn("Thống kê", 0, 0, 90, (s, e) => UI.Try(this, RunStat)));
            var btnExport = UI.Btn("Xuất báo cáo CSV…", 0, 0, 140, (s, e) => UI.Try(this, ExportStat));
            btnExport.Enabled = Session.Can("TK_XUATFILE");
            pnl.Controls.Add(btnExport);
            if (Session.Can("TK_PHAMVI"))
                pnl.Controls.Add(UI.Btn("Lưu phạm vi vào CSDL", 0, 0, 150, (s, e) => UI.Try(this, () =>
                {
                    int n = Repo.RecomputeCoverage((double)numStatRadius.Value, null);
                    UI.Info(this, "Đã cập nhật bảng phạm vi phục vụ (" + n + " dòng).");
                })));

            var chartArea = new ChartArea("main");
            chartArea.AxisX.Interval = 1;
            chartArea.AxisX.LabelStyle.Angle = -45;
            chart.ChartAreas.Add(chartArea);
            chart.Series.Add(new Series("s") { ChartType = SeriesChartType.Column });
            chart.Legends.Clear();

            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 200 };
            split.Panel1.Controls.Add(chart);
            split.Panel2.Controls.Add(gridStat);
            tabStats.Controls.Add(split);
            tabStats.Controls.Add(lblStatSummary);
            tabStats.Controls.Add(pnl);
            tabs.TabPages.Add(tabStats);
        }

        void BindChart(DataTable dt, string xCol, string yCol)
        {
            var s = chart.Series["s"];
            s.Points.Clear();
            foreach (DataRow r in dt.Rows) s.Points.AddXY(Convert.ToString(r[xCol]), Convert.ToDouble(r[yCol]));
            chart.ChartAreas[0].AxisY.Title = yCol;
        }

        void RunStat()
        {
            string kind = (string)cbStatKind.SelectedItem;
            lblStatSummary.Text = "";
            if (kind.StartsWith("Số lượng"))
            {
                _statTable = Repo.StatByDistrict();
                gridStat.DataSource = _statTable;
                var byQuan = _statTable.AsEnumerable().GroupBy(r => r.Field<string>("Quận/huyện"))
                                       .Select(g => new { Quan = g.Key, N = g.Sum(r => Convert.ToInt32(r["Số trạm"])) }).ToList();
                var dt = new DataTable(); dt.Columns.Add("Quận"); dt.Columns.Add("Số trạm", typeof(int));
                foreach (var g in byQuan) dt.Rows.Add(g.Quan, g.N);
                BindChart(dt, "Quận", "Số trạm");
                lblStatSummary.Text = "Tổng " + byQuan.Sum(g => g.N) + " trạm.";
            }
            else if (kind.StartsWith("Trạm theo tuyến"))
            {
                var ids = txtStatRoutes.Text.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                _statTable = Repo.StatByRoute(ids);
                gridStat.DataSource = _statTable;
                BindChart(_statTable, "Mã tuyến", "Số trạm");
                if (ids.Count > 0) lblStatSummary.Text = "Đã lọc " + ids.Count + " tuyến.";
            }
            else
            {
                double r = (double)numStatRadius.Value;
                int total, served;
                var cov = CoverageAnalyzer.StationCoverages(r, _stations, _residents, _facilities, out total, out served);
                _statTable = new DataTable();
                _statTable.Columns.Add("Mã trạm"); _statTable.Columns.Add("Tên trạm");
                _statTable.Columns.Add("Số khu dân cư", typeof(int)); _statTable.Columns.Add("Dân số tiếp cận", typeof(int)); _statTable.Columns.Add("Số công trình", typeof(int));
                foreach (var c in cov) _statTable.Rows.Add(c.Station.MaTram, c.Station.TenTram, c.SoKhu, c.DanSo, c.SoCongTrinh);
                gridStat.DataSource = _statTable;
                BindChart(_statTable, "Tên trạm", "Dân số tiếp cận");
                map.ShowCoverage(cov.Select(c => new LatLon(c.Station.ViDo, c.Station.KinhDo)), r);
                lblStatSummary.Text = string.Format("Bán kính {0:0} m: {1:N0}/{2:N0} dân được tiếp cận ({3:0.0}%) — không tính trùng giữa các trạm.",
                    r, served, total, total == 0 ? 0 : 100.0 * served / total);
            }
        }

        void ExportStat()
        {
            if (_statTable == null || _statTable.Rows.Count == 0) { UI.Info(this, "Hãy chạy thống kê trước khi xuất."); return; }
            using (var d = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", FileName = "baocao_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".csv" })
            {
                if (d.ShowDialog(this) != DialogResult.OK) return;
                ReportExporter.ToCsv(_statTable, d.FileName);
                UI.Info(this, "Đã xuất: " + d.FileName);
            }
        }
    }
}
