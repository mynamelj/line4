using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;

namespace MdbReadTest
{
    public sealed class DatabaseSnapshot
    {
        public string Path { get; set; }
        public string Provider { get; set; }
        public DateTime LastWriteTime { get; set; }
        public List<DataTable> Tables { get; } = new List<DataTable>();
    }

    public static class DatabaseReader
    {
        public static DatabaseSnapshot ReadLatest(string directory, string provider)
        {
            var file = new DirectoryInfo(directory).GetFiles("*.mdb", SearchOption.TopDirectoryOnly)
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .ThenByDescending(f => f.CreationTimeUtc).ThenBy(f => f.Name).FirstOrDefault();
            if (file == null) throw new FileNotFoundException("目录内没有 MDB 文件。");
            return ReadFile(file.FullName, provider);
        }

        public static DatabaseSnapshot ReadFile(string path, string provider)
        {
            if (string.IsNullOrWhiteSpace(provider))
            {
                provider = new[] { "Microsoft.ACE.OLEDB.12.0", "Microsoft.ACE.OLEDB.16.0", "Microsoft.Jet.OLEDB.4.0" }
                    .FirstOrDefault(p => Type.GetTypeFromProgID(p) != null);
                if (provider == null)
                    throw new InvalidOperationException("未找到32位 Access/Jet驱动，请安装32位 Access 数据库引擎。");
            }
            var file = new FileInfo(path);
            var stamp = file.LastWriteTimeUtc;
            var length = file.Length;
            var result = new DatabaseSnapshot { Path = file.FullName, Provider = provider, LastWriteTime = file.LastWriteTime };
            var builder = new OleDbConnectionStringBuilder { Provider = provider, DataSource = file.FullName };
            builder["Mode"] = "Read";
            builder["Persist Security Info"] = false;
            using (var connection = new OleDbConnection(builder.ConnectionString))
            {
                connection.Open();
                var schema = connection.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, new object[] { null, null, null, "TABLE" });
                if (schema == null) throw new InvalidDataException("无法获取数据库表结构。");
                foreach (DataRow row in schema.Rows)
                {
                    string name = Convert.ToString(row["TABLE_NAME"]);
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT * FROM [" + name.Replace("]", "]]") + "]";
                        command.CommandTimeout = 5;
                        using (var reader = command.ExecuteReader())
                        {
                            var table = new DataTable(name);
                            table.Load(reader);
                            result.Tables.Add(table);
                        }
                    }
                }
            }
            file.Refresh();
            if (!file.Exists || file.Length != length || file.LastWriteTimeUtc != stamp)
                throw new IOException("MDB在读取期间发生变化，请等待写入完成后重试。");
            return result;
        }
    }
}
