using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using QLXeBus.Data;
using QLXeBus.MapView;
using QLXeBus.Models;
using QLXeBus.Services;

namespace QLXeBus.Forms
{
    /// <summary>Quy trình "Đề xuất trạm xe mới": chọn vùng → khu thiếu trạm → ứng viên → so sánh → đề xuất.</summary>
    public partial class MainForm
    {
        TabPage tabProposal;
        readonly ComboBox cbPrQuan = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
        readonly ComboBox cbPrPhuong = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
        readonly NumericUpDown numPrRadius = new NumericUpDown { Width = 70, Minimum = 100, Maximum = 3000, Value = 500, Increment = 50 };
        readonly NumericUpDown numPrStep = new NumericUpDown { Width = 70, Minimum = 20, Maximum = 1000, Value = 100, Increment = 10 };
        readonly NumericUpDown numPrSpacing = new NumericUpDown { Width = 70, Minimum = 0, Maximum = 2000, Value = 250, Increment = 50 };
        readonly NumericUpDown numPrTop = new NumericUpDown { Width = 70, Minimum = 1, Maximum = 20, Value = 5 };
        readonly Label lblRegion = new Label { AutoSize = false, Width = 470, Height = 34, Text = "Chưa chọn vùng phân tích." };
        readonly Label lblPrSummary = new Label { Dock = DockStyle.Bottom, Height = 70 };
        readonly DataGridView gridCand = UI.Grid();
        readonly Button btnStep2 = new Button(), btnStep3 = new Button(), btnStep5 = new Button();

        Bounds _region;
        UnderservedResult _under;
        List<Candidate> _cands = new List<Candidate>();

        void BuildProposalTab()
        {
            if (!Session.Can("DX_VUNG")) return;
            tabProposal = new TabPage("Đề xuất trạm");

            var pnl = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 262, Padding = new Padding(4) };
            Action<string, Control, int> add = (t, c, w) =>
            {
                pnl.Controls.Add(new Label { Text = t, Width = w, TextAlign = System.Drawing.ContentAlignment.MiddleLeft });
                pnl.Controls.Add(c);
            };
            pnl.Controls.Add(new Label { Text = "1. Chọn vùng cần phân tích", Width = 460, Font = new System.Drawing.Font(Font, System.Drawing.FontStyle.Bold) });
            pnl.Controls.Add(UI.Btn("Vẽ hộp trên bản đồ…", 0, 0, 160, (s, e) =>
                SetTool(MapTool.SelectBox, Pending.None, b =>
                {
                    _region = b; map.ShowArea(b); ResetAnalysis(); SetTool(MapTool.Pan, Pending.None, null, "Sẵn sàng");
                }, "Kéo hộp trên bản đồ để chọn vùng phân tích")));
            pnl.SetFlowBreak(pnl.Controls[pnl.Controls.Count - 1], true);
            add("Quận/huyện", cbPrQuan, 75); add("Phường/xã", cbPrPhuong, 70);
            pnl.Controls.Add(UI.Btn("Dùng đơn vị hành chính", 0, 0, 170, (s, e) => UI.Try(this, UseAdminRegion)));
            pnl.Controls.Add(lblRegion);
            cbPrQuan.SelectedIndexChanged += (s, e) =>
            {
                cbPrPhuong.DataSource = All(Repo.Phuong((string)cbPrQuan.SelectedValue), "(tất cả)");
                cbPrPhuong.DisplayMember = "Ten"; cbPrPhuong.ValueMember = "Ma";
            };

            pnl.Controls.Add(new Label { Text = "Tham số", Width = 460, Font = new System.Drawing.Font(Font, System.Drawing.FontStyle.Bold) });
            add("Bán kính phục vụ (m)", numPrRadius, 120); add("Bước lưới (m)", numPrStep, 80);
            add("Cách trạm tối thiểu (m)", numPrSpacing, 135); add("Số ứng viên", numPrTop, 70);

            btnStep2.Text = "2. Xác định khu vực thiếu trạm"; btnStep2.Width = 220; btnStep2.Height = 28; btnStep2.Enabled = Session.Can("DX_THIEU");
            btnStep2.Click += (s, e) => UI.Try(this, FindUnderserved);
            btnStep3.Text = "3-4. Tìm & so sánh ứng viên"; btnStep3.Width = 220; btnStep3.Height = 28; btnStep3.Enabled = Session.Can("DX_UNGVIEN");
            btnStep3.Click += (s, e) => UI.Try(this, FindCandidates);
            btnStep5.Text = "5. Đề xuất trạm tại ứng viên đã chọn…"; btnStep5.Width = 270; btnStep5.Height = 28; btnStep5.Enabled = Session.Can("DX_DEXUAT");
            btnStep5.Click += (s, e) => UI.Try(this, ProposeSelected);
            pnl.Controls.Add(btnStep2); pnl.Controls.Add(btnStep3); pnl.Controls.Add(btnStep5);

            tabProposal.Controls.Add(gridCand);
            tabProposal.Controls.Add(lblPrSummary);
            tabProposal.Controls.Add(pnl);
            tabs.TabPages.Add(tabProposal);
            cbPrQuan.DisplayMember = "Ten"; cbPrQuan.ValueMember = "Ma";
        }

        void ReloadProposalLookups()
        {
            if (tabProposal == null) return;
            cbPrQuan.DataSource = All(Repo.Quan(), "(tất cả)");
        }

        void ResetAnalysis()
        {
            _under = null; _cands = new List<Candidate>(); gridCand.DataSource = null;
            lblRegion.Text = "Vùng: " + _region;
            lblPrSummary.Text = "";
        }

        AnalysisParams Params()
        {
            if (_region == null) throw new InvalidOperationException("Hãy chọn vùng cần phân tích (bước 1).");
            return new AnalysisParams
            {
                Region = _region, RadiusM = (double)numPrRadius.Value, GridStepM = (double)numPrStep.Value,
                MinSpacingM = (double)numPrSpacing.Value, TopN = (int)numPrTop.Value
            };
        }

        void UseAdminRegion()
        {
            string q = (string)cbPrQuan.SelectedValue, p = (string)cbPrPhuong.SelectedValue;
            if (q == null && p == null) throw new InvalidOperationException("Chọn quận/huyện hoặc phường/xã.");
            Func<string, string, bool> inUnit = (maPhuong, maQuan) => p != null ? maPhuong == p : maQuan == q;
            var pts = new List<LatLon>();
            pts.AddRange(_residents.Where(k => inUnit(k.MaPhuong, k.MaQuan)).Select(k => new LatLon(k.ViDo, k.KinhDo)));
            pts.AddRange(_facilities.Where(k => inUnit(k.MaPhuong, k.MaQuan)).Select(k => new LatLon(k.ViDo, k.KinhDo)));
            pts.AddRange(_stations.Where(k => inUnit(k.MaPhuong, k.MaQuan)).Select(k => new LatLon(k.ViDo, k.KinhDo)));
            if (pts.Count == 0) throw new InvalidOperationException("Đơn vị hành chính này chưa có dữ liệu để xác định vùng.");
            // Dữ liệu chưa có ranh giới hành chính: vùng = khung bao các đối tượng thuộc đơn vị, nới thêm một bán kính phục vụ.
            _region = new Bounds(pts.Min(x => x.Lat), pts.Min(x => x.Lon), pts.Max(x => x.Lat), pts.Max(x => x.Lon)).Expand((double)numPrRadius.Value);
            map.ShowArea(_region);
            map.GoTo(_region.Expand(500));
            ResetAnalysis();
        }

        void FindUnderserved()
        {
            var p = Params();
            _under = CoverageAnalyzer.FindUnderserved(p, _stations, _residents, _facilities);
            map.ShowCoverage(_stations.Where(s => s.TrangThai != TrangThai.Ngung && p.Region.Expand(p.RadiusM).Contains(s.ViDo, s.KinhDo))
                                      .Select(s => new LatLon(s.ViDo, s.KinhDo)), p.RadiusM);
            map.ShowUnderserved(_under.Residents, _under.Facilities);

            var dt = new DataTable();
            dt.Columns.Add("Khu dân cư / công trình"); dt.Columns.Add("Phường/xã"); dt.Columns.Add("Dân số", typeof(int));
            foreach (var k in _under.Residents) dt.Rows.Add(k.TenKhu, k.TenPhuong, k.DanSo);
            foreach (var c in _under.Facilities) dt.Rows.Add(c.TenCongTrinh + " (" + c.TenLoai + ")", "", 0);
            gridCand.DataSource = dt;

            lblPrSummary.Text = string.Format(
                "Vùng có {0} khu dân cư, {1:N0} dân; thiếu trạm: {2} khu ({3:N0} dân, {4:0.0}% dân số vùng chưa được phục vụ trong {5:0} m) và {6} công trình.",
                _under.TotalResidentsInRegion, _under.TotalPopulationInRegion, _under.Residents.Count, _under.UnderservedPopulation,
                100 - _under.CoveragePercent, p.RadiusM, _under.Facilities.Count);
        }

        void FindCandidates()
        {
            var p = Params();
            if (_under == null) FindUnderserved();
            _cands = CoverageAnalyzer.FindCandidates(p, _stations, _under);
            gridCand.DataSource = new BindingList<Candidate>(_cands);
            if (!Session.Can("DX_SOSANH"))
                foreach (DataGridViewColumn c in gridCand.Columns)
                    c.Visible = c.DataPropertyName == "Hang" || c.DataPropertyName == "ViDo" || c.DataPropertyName == "KinhDo";
            map.ShowCandidates(_cands);
            lblPrSummary.Text = _cands.Count == 0
                ? "Không tìm được ứng viên: vùng đã được phục vụ đủ hoặc ràng buộc khoảng cách quá chặt."
                : string.Format("Tìm được {0} ứng viên. Điểm = số dân tiếp cận mới + {1:0} × Σ trọng số công trình được phục vụ. " +
                                "Chọn một dòng rồi bấm bước 5 để đề xuất.", _cands.Count, p.FacilityBonus);
        }

        void ProposeSelected()
        {
            var c = gridCand.CurrentRow == null ? null : gridCand.CurrentRow.DataBoundItem as Candidate;
            if (c == null) throw new InvalidOperationException("Hãy chạy bước 3-4 và chọn một ứng viên trong bảng.");
            var loai = Repo.LoaiTram();
            var s = new Station
            {
                TenTram = "Trạm đề xuất " + c.Hang, ViDo = c.ViDo, KinhDo = c.KinhDo, TrangThai = TrangThai.DeXuat,
                MaLoaiTram = loai.Count > 0 ? loai[0].Ma : null
            };
            using (var f = new StationEditForm(s, true, false))
            {
                if (f.ShowDialog(this) != DialogResult.OK) return;
                if (Session.Can("TK_PHAMVI")) Repo.RecomputeCoverage((double)numPrRadius.Value, f.Result.MaTram);
                UI.Info(this, "Đã thêm trạm " + f.Result.MaTram + " (trạng thái Đề xuất) vào CSDL.");
            }
            ReloadData();
        }
    }
}
