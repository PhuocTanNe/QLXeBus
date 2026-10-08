using System.Data;
using System.IO;
using System.Text;

namespace QLXeBus.Services
{
    public static class ReportExporter
    {
        static string Esc(string s)
        {
            s = s ?? "";
            if (s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0) s = "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }

        /// <summary>Xuất CSV UTF-8 có BOM (mở trực tiếp bằng Excel không lỗi tiếng Việt).</summary>
        public static void ToCsv(DataTable dt, string path)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < dt.Columns.Count; i++) sb.Append(i > 0 ? "," : "").Append(Esc(dt.Columns[i].ColumnName));
            sb.AppendLine();
            foreach (DataRow r in dt.Rows)
            {
                for (int i = 0; i < dt.Columns.Count; i++) sb.Append(i > 0 ? "," : "").Append(Esc(System.Convert.ToString(r[i], System.Globalization.CultureInfo.InvariantCulture)));
                sb.AppendLine();
            }
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        }
    }
}
