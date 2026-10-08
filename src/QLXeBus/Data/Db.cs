using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using Npgsql;

namespace QLXeBus.Data
{
    /// <summary>Cấu hình kết nối PostgreSQL. Mỗi máy lưu riêng trong %AppData%\QLXeBus\dbconfig.ini (không đưa lên git).</summary>
    public class DbConfig
    {
        public string Host = "localhost";
        public int Port = 5432;
        public string Database = "HQTCSDL";
        public string Username = "postgres";
        public string Password = "";
        public string PgDumpPath = "pg_dump";

        public string ConnectionString
        {
            get
            {
                return new NpgsqlConnectionStringBuilder
                {
                    Host = Host, Port = Port, Database = Database, Username = Username, Password = Password,
                    Timeout = 10, CommandTimeout = 120
                }.ConnectionString;
            }
        }
    }

    public static class Db
    {
        static DbConfig _cfg;

        public static string ConfigPath
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QLXeBus", "dbconfig.ini");
            }
        }

        public static DbConfig Config
        {
            get { return _cfg ?? (_cfg = Load()); }
            set { _cfg = value; }
        }

        static string App(string key, string def)
        {
            var v = ConfigurationManager.AppSettings[key];
            return string.IsNullOrWhiteSpace(v) ? def : v;
        }

        static DbConfig Load()
        {
            var c = new DbConfig
            {
                Host = App("Host", "localhost"),
                Database = App("Database", "HQTCSDL"),
                Username = App("Username", "postgres"),
                PgDumpPath = App("PgDumpPath", "pg_dump")
            };
            int port;
            if (int.TryParse(App("Port", "5432"), out port)) c.Port = port;

            if (File.Exists(ConfigPath))
            {
                foreach (var line in File.ReadAllLines(ConfigPath))
                {
                    int i = line.IndexOf('=');
                    if (i <= 0) continue;
                    string k = line.Substring(0, i).Trim(), v = line.Substring(i + 1).Trim();
                    switch (k)
                    {
                        case "Host": c.Host = v; break;
                        case "Port": if (int.TryParse(v, out port)) c.Port = port; break;
                        case "Database": c.Database = v; break;
                        case "Username": c.Username = v; break;
                        case "Password": c.Password = v; break;
                        case "PgDumpPath": c.PgDumpPath = v; break;
                    }
                }
            }
            return c;
        }

        public static void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
            var c = Config;
            File.WriteAllLines(ConfigPath, new[]
            {
                "Host=" + c.Host, "Port=" + c.Port, "Database=" + c.Database,
                "Username=" + c.Username, "Password=" + c.Password, "PgDumpPath=" + c.PgDumpPath
            });
        }

        public static NpgsqlConnection Open()
        {
            var cn = new NpgsqlConnection(Config.ConnectionString);
            cn.Open();
            return cn;
        }

        /// <summary>Tham số truyền theo cặp: "@ten", giá trị, "@ten2", giá trị2 ...</summary>
        static void Bind(NpgsqlCommand cmd, object[] kv)
        {
            for (int i = 0; i + 1 < kv.Length; i += 2)
                cmd.Parameters.AddWithValue((string)kv[i], kv[i + 1] ?? DBNull.Value);
        }

        public static DataTable Table(string sql, params object[] kv)
        {
            using (var cn = Open())
            using (var cmd = new NpgsqlCommand(sql, cn))
            {
                Bind(cmd, kv);
                var dt = new DataTable();
                using (var rd = cmd.ExecuteReader()) dt.Load(rd);
                return dt;
            }
        }

        public static object Scalar(string sql, params object[] kv)
        {
            using (var cn = Open())
            using (var cmd = new NpgsqlCommand(sql, cn))
            {
                Bind(cmd, kv);
                var r = cmd.ExecuteScalar();
                return r == DBNull.Value ? null : r;
            }
        }

        public static int Exec(string sql, params object[] kv)
        {
            using (var cn = Open())
            using (var cmd = new NpgsqlCommand(sql, cn))
            {
                Bind(cmd, kv);
                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>Chạy nhiều lệnh trong một transaction.</summary>
        public static void InTransaction(Action<NpgsqlConnection, NpgsqlTransaction> work)
        {
            using (var cn = Open())
            using (var tx = cn.BeginTransaction())
            {
                try { work(cn, tx); tx.Commit(); }
                catch { tx.Rollback(); throw; }
            }
        }

        public static int Exec(NpgsqlConnection cn, NpgsqlTransaction tx, string sql, params object[] kv)
        {
            using (var cmd = new NpgsqlCommand(sql, cn, tx))
            {
                Bind(cmd, kv);
                return cmd.ExecuteNonQuery();
            }
        }

        public static string Str(object o) { return o == null || o == DBNull.Value ? null : Convert.ToString(o); }
        public static double Dbl(object o) { return o == null || o == DBNull.Value ? 0 : Convert.ToDouble(o); }
        public static int Int(object o) { return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o); }
        public static int? IntN(object o) { return o == null || o == DBNull.Value ? (int?)null : Convert.ToInt32(o); }
    }
}
