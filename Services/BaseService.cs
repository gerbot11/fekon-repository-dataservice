using fekon_repository_datamodel.Models;
using fekon_repository_datamodel.ModelService;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

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

        public IQueryable<Repository> FilterRepoIdBySingleQueryParam(string q, IQueryable<Repository> repositories, bool isTitleOnly = false, bool isNameOnly = false)
        {
            string[] listParam = SetQueryParamToArray(q);
            List<long> distinctId = new();

            if (isTitleOnly)
            {
                IQueryable<long> matchFilter = (from r in repositories
                                                where r.Title.Contains(q)
                                                select r.RepositoryId).Distinct();
                if (matchFilter.Any())
                {
                    distinctId.AddRange(matchFilter);
                }
                else
                {
                    foreach (string item in listParam)
                    {
                        IQueryable<long> filter = (from r in repositories
                                                   where r.Title.Contains(item)
                                                   select r.RepositoryId).Distinct();

                        distinctId.AddRange(filter);
                        distinctId = distinctId.Distinct().ToList();
                    }
                }
            }
            else if (isNameOnly)
            {
                IQueryable<long> matchFilter = (from r in repositories
                                                join d in _context.RepositoryDs on r.RepositoryId equals d.RepositoryId
                                                join a in _context.Authors on d.AuthorId equals a.AuthorId
                                                where a.FirstName.Contains(q) || a.LastName.Contains(q)
                                                select r.RepositoryId).Distinct();
                if (matchFilter.Any())
                {
                    distinctId.AddRange(matchFilter);
                }
                else
                {
                    foreach (string item in listParam)
                    {
                        IQueryable<long> filter = (from r in repositories
                                                   join d in _context.RepositoryDs on r.RepositoryId equals d.RepositoryId
                                                   join a in _context.Authors on d.AuthorId equals a.AuthorId
                                                   where a.FirstName.Contains(item) || a.LastName.Contains(item)
                                                   select r.RepositoryId).Distinct();

                        distinctId.AddRange(filter);
                        distinctId = distinctId.Distinct().ToList();
                    }
                }
            }
            else
            {
                IQueryable<long> matchFilter = (from r in repositories
                                                join d in _context.RepositoryDs on r.RepositoryId equals d.RepositoryId
                                                join a in _context.Authors on d.AuthorId equals a.AuthorId
                                                where a.FirstName.Contains(q) || a.LastName.Contains(q) || r.Title.Contains(q) || r.Description.Contains(q)
                                                select r.RepositoryId).Distinct();
                if (matchFilter.Any())
                {
                    distinctId.AddRange(matchFilter);
                }
                else
                {
                    foreach (string item in listParam)
                    {
                        IEnumerable<SearchResultModel> filter = (from r in repositories
                                                                 join d in _context.RepositoryDs on r.RepositoryId equals d.RepositoryId
                                                                 join a in _context.Authors on d.AuthorId equals a.AuthorId
                                                                 where a.FirstName.Contains(item) || a.LastName.Contains(item) || r.Title.Contains(item) || r.Description.Contains(item)
                                                                 select new SearchResultModel
                                                                 {
                                                                     RepoId = r.RepositoryId,
                                                                     Title = r.Title.ToLower(),
                                                                     Desc = r.Description.ToLower()
                                                                 }).Distinct();

                        var result = filter.Where(f => listParam.Any(p => f.Title.Contains(p))).Select(x => x.RepoId);

                        distinctId.AddRange(result.Except(distinctId));
                        distinctId = distinctId.Distinct().ToList();
                    }
                }
            }
            
            return repositories.Where(r => distinctId.Contains(r.RepositoryId));
        }

        private static string[] SetQueryParamToArray(string query)
        {
            query = query.Trim().ToLower();
            string[] listParam = query.Split(' ').ToArray();
            return listParam;
        }
    }
}
