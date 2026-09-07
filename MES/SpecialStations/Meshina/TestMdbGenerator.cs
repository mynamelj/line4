using System.Data.OleDb;
using System.IO;
using System.Runtime.InteropServices;

namespace MES.SpecialStations.Meshina
{
    /// <summary>供线外啮合工位联调时模拟啮合设备生成结果文件。</summary>
    public sealed class TestMdbGenerator
    {
        private readonly string dataDirectory;
        private readonly string provider;

        public TestMdbGenerator(string dataDirectory, string provider)
        {
            this.dataDirectory = dataDirectory;
            this.provider = provider;
        }

        public string Create()
        {
            Directory.CreateDirectory(dataDirectory);
            string fileName = $"194-001_Unknown_955555555555555555_{DateTime.Now:yyyyMMdd_HHmmss}.mdb";
            string path = Path.Combine(dataDirectory, fileName);
            if (File.Exists(path))
                throw new IOException("同一秒内已生成测试MDB，请等待一秒后重试");

            CreateDatabase(path);
            try
            {
                using var connection = new OleDbConnection($"Provider={provider};Data Source={path};");
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "CREATE TABLE TJSHEET (FileN TEXT(50), FileID TEXT(50), MeaTime DATETIME,"
                    + " Fi DECIMAL(10,2), fii DECIMAL(10,2), Fr DECIMAL(10,2),"
                    + " aave DECIMAL(10,3), amax DECIMAL(10,3), amin DECIMAL(10,3),"
                    + " rm DECIMAL(10,3), rmmax DECIMAL(10,3), rmmin DECIMAL(10,3),"
                    + " WKave DECIMAL(10,3), WKmax DECIMAL(10,3), WKmin DECIMAL(10,3),"
                    + " Mave DECIMAL(10,3), Mmax DECIMAL(10,3), Mmin DECIMAL(10,3),"
                    + " dave1 DECIMAL(10,3), dmax1 DECIMAL(10,3), dmin1 DECIMAL(10,3),"
                    + " TwoDimCode TEXT(100), diff DECIMAL(10,3), Cylindricity DECIMAL(10,2), Result TEXT(10))";
                command.ExecuteNonQuery();

                command.CommandText = "INSERT INTO TJSHEET (FileN,FileID,MeaTime,Fi,fii,Fr,aave,amax,amin,"
                    + "rm,rmmax,rmmin,WKave,WKmax,WKmin,Mave,Mmax,Mmin,dave1,dmax1,dmin1,"
                    + "TwoDimCode,diff,Cylindricity,Result) VALUES ("
                    + $"'194-001','Unknown',#{DateTime.Now:MM/dd/yyyy HH:mm:ss}#," 
                    + "1.11,1.22,1.33,1.444,1.555,1.666,1.777,1.888,1.999,"
                    + "2.111,2.222,2.333,2.444,2.555,2.666,2.777,2.888,2.999,"
                    + "'955555555555555555',3.333,3.44,'PASS')";
                command.ExecuteNonQuery();
                return path;
            }
            catch
            {
                try { if (File.Exists(path)) File.Delete(path); }
                catch { /* 保留原始建表/写入异常。 */ }
                throw;
            }
        }

        private void CreateDatabase(string path)
        {
            Type catalogType = Type.GetTypeFromProgID("ADOX.Catalog");
            if (catalogType == null)
                throw new InvalidOperationException("未安装ADOX/Access数据库引擎，无法生成测试MDB");
            object catalog = Activator.CreateInstance(catalogType);
            object activeConnection = null;
            try
            {
                catalogType.InvokeMember("Create", System.Reflection.BindingFlags.InvokeMethod, null, catalog,
                    new object[] { $"Provider={provider};Data Source={path};Jet OLEDB:Engine Type=5;" });
                activeConnection = catalogType.InvokeMember("ActiveConnection",
                    System.Reflection.BindingFlags.GetProperty, null, catalog, null);
                activeConnection?.GetType().InvokeMember("Close", System.Reflection.BindingFlags.InvokeMethod,
                    null, activeConnection, null);
            }
            finally
            {
                if (activeConnection != null && Marshal.IsComObject(activeConnection))
                    Marshal.FinalReleaseComObject(activeConnection);
                if (catalog != null && Marshal.IsComObject(catalog)) Marshal.FinalReleaseComObject(catalog);
            }
        }

    }
}
