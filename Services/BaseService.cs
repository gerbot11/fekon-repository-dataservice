using fekon_repository_datamodel.Models;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.IO;

namespace fekon_repository_dataservice.Services
{
    public abstract class BaseService
    {
        public readonly REPOSITORY_DEVContext _context;
        public BaseService(REPOSITORY_DEVContext context)
        {
            _context = context;
        }

        public static DataTable GetDataBySp(string conString, string spName)
        {
            using MySqlConnection con = new(conString);

            if (con.State == ConnectionState.Closed) 
                con.Open();
            
            DataTable dt = new();
            using (MySqlCommand cmd = new(spName, con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                using MySqlDataAdapter sda = new(cmd);
                sda.Fill(dt);
            }
            con.Close();

            return dt;
        }

        public static DirectoryInfo CreateDirectrory(string rootpath, string childpath)
        {
            string subDir = Path.Combine(rootpath, childpath, DateTime.Now.ToString("yyyyMMdd"));
            int cntFolder;
            string finalPath;
            if (Directory.Exists(subDir))
            {
                cntFolder = Directory.GetDirectories(subDir).Length + 1;
                finalPath = Path.Combine(subDir, "_" + cntFolder.ToString());
            }
            else
            {
                cntFolder = 1;
                finalPath = Path.Combine(subDir, "_" + cntFolder.ToString());
            }

            DirectoryInfo di = Directory.CreateDirectory(finalPath);
            return di;
        }

        //Pakai ini par filter Tahun, entah untuk select tahun MySql dan EF Core seng bisa (harus full date)
        public static void SetParamDateBetweenYear(int yearStart, int yearTo, out DateTime dtStart, out DateTime dtTo)
        {
            dtStart = new(yearStart, 1, 1);

            if (yearTo is 0)
                dtTo = new(DateTime.Now.Year, 12, 31);
            else 
                dtTo = new(yearTo, 12, 31);
        }
    }
}
