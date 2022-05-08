using fekon_repository_api;
using fekon_repository_datamodel.MergeModels;
using fekon_repository_datamodel.Models;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Annot;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Extgstate;
using iText.Kernel.Pdf.Xobject;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace fekon_repository_dataservice.Services
{
    public class RepoService : BaseService, IRepoService
    {
        private readonly ILogger<RepoService> _logger;
        private readonly IAuthorService _authorService;
        private readonly IUserService _userService;
        private readonly IFileMonitoringService _fileMonitoringService;
        private readonly IGeneralService _generalService;

        public RepoService(REPOSITORY_DEVContext context, ILogger<RepoService> logger, IAuthorService authorService,
            IUserService userService, IFileMonitoringService fileMonitoringService, IGeneralService generalService)
            : base(context)
        {
            _logger = logger;
            _authorService = authorService;
            _userService = userService;
            _fileMonitoringService = fileMonitoringService;
            _generalService = generalService;
        }

        #region FOR DASHBOARD
        public IQueryable<Repository> GetRepositoriesForIndexPageAsync(string query)
        {
            IQueryable<Repository> repositories = _context.Repositories;

            if (!string.IsNullOrEmpty(query) || !string.IsNullOrWhiteSpace(query))
            {
                repositories = FilterRepoIdBySingleQueryParam(query, repositories);
            }

            repositories = repositories.Include(r => r.RepositoryDs)
                .ThenInclude(p => p.Author)
            .Include(p => p.RefCollection)
            .Include(c => c.CollectionD)
            .OrderBy(x => x.RepositoryId)
            .AsNoTracking();

            return repositories;
        }

        public IQueryable<MergeRepositoryPaging> GetRepositoryPagings(string title, string author, int? year, long? type, long? colld)
        {
            IQueryable<Repository> result = _context.Repositories;

            if (!string.IsNullOrWhiteSpace(title))
            {
                result = result.Where(t => t.Title.Contains(title));
            }

            if (year is not null)
            {
                result = result.Where(y => y.PublishDate.Year == year);
            }

            if (type is not null)
            {
                result = from r in result
                         join c in _context.RefCollections on r.RefCollectionId equals c.RefCollectionId
                         where c.RefCollectionId == type
                         select r;
            }

            if (colld is not null)
            {
                result = from r in result
                         join c in _context.CollectionDs on r.CollectionDid equals c.CollectionDid
                         where c.CollectionDid == colld
                         select r;
            }

            if (!string.IsNullOrWhiteSpace(author))
            {
                result = from r in result
                         join d in _context.RepositoryDs on r.RepositoryId equals d.RepositoryId
                         join a in _context.Authors on d.AuthorId equals a.AuthorId
                         where a.FirstName.Contains(author) || a.LastName.Contains(author)
                         select r;
            }

            IQueryable<MergeRepositoryPaging> pagingresult = from r in result
                                                             join c in _context.RefCollections on r.RefCollectionId equals c.RefCollectionId
                                                             join cd in _context.CollectionDs on r.CollectionDid equals cd.CollectionDid
                                                             orderby r.RepositoryId ascending
                                                             select new MergeRepositoryPaging
                                                             {
                                                                 RepositoryId = r.RepositoryId,
                                                                 AuthorName = _authorService.GetAuthorNameByRepositoryId(r.RepositoryId),
                                                                 CollName = cd.CollectionDname,
                                                                 PublishDate = r.PublishDate.ToString("dd-MMM-yyyy"),
                                                                 RefCollectionName = c.CollName,
                                                                 Title = r.Title
                                                             };

            return pagingresult;
        }

        public IQueryable<Repository> MoreSearchRepositoryDashboard(string title, string author, int? yearFrom, int? yearTo, long? type, long? colld)
        {
            IQueryable<Repository> result = _context.Repositories;

            if (!string.IsNullOrWhiteSpace(title))
            {
                result = result.Where(t => t.Title.Contains(title));
            }

            if (yearFrom is not null)
            {
                SetParamDateBetweenYear((int)yearFrom, yearTo ?? 0, out DateTime dtStart, out DateTime dtTo);
                if (yearTo is not null)
                {
                    result = result.Where(y => y.PublishDate >= dtStart && y.PublishDate <= dtTo);
                }
                else
                {
                    result = result.Where(y => y.PublishDate >= dtStart);
                }
            }

            if (type is not null)
            {
                result = from r in result
                         join c in _context.RefCollections on r.RefCollectionId equals c.RefCollectionId
                         where c.RefCollectionId == type
                         select r;
            }

            if (colld is not null)
            {
                result = from r in result
                         join c in _context.CollectionDs on r.CollectionDid equals c.CollectionDid
                         where c.CollectionDid == colld
                         select r;
            }

            if (!string.IsNullOrWhiteSpace(author))
            {
                result = from r in result
                         join d in _context.RepositoryDs on r.RepositoryId equals d.RepositoryId
                         join a in _context.Authors on d.AuthorId equals a.AuthorId
                         where a.FirstName.Contains(author) || a.LastName.Contains(author)
                         select r;
            }

            return result.Include(r => r.RepositoryDs)
                .ThenInclude(p => p.Author)
            .Include(p => p.RefCollection)
            .Include(c => c.CollectionD)
            .OrderByDescending(x => x.RepositoryId)
            .AsNoTracking();
        }

        public Repository GetRepositoryByRepoId(long id)
        {
            return _context.Repositories
                 .Include(p => p.PublisherNavigation)
                 .Include(c => c.RefCollection)
                 .Include(c => c.CollectionD)
                 .Include(f => f.FileDetails)
                 .Where(r => r.RepositoryId == id).FirstOrDefault();
        }

        public MergeRepoViewDashboard GetRepositoryForDetailById(long id)
        {
            Repository repo = _context.Repositories
                 .Include(p => p.RepositoryDs).ThenInclude(a => a.Author)
                 .Include(m => m.Communitiy)
                 .Include(c => c.RefCollection)
                 .Include(c => c.CollectionD)
                 .Include(s => s.RepoStatistics)
                 .Include(pb => pb.PublisherNavigation)
                 .Include(f => f.FileDetails).ThenInclude(d => d.DownloadStatistics)
                 .Include(f => f.FileDetails).ThenInclude(ft => ft.RefRepositoryFileType)
                 .Where(r => r.RepositoryId == id).FirstOrDefault();

            if (repo is null)
                return null;

            string empUploader = _context.RefEmployees.Where(e => e.UserId == repo.UsrCreate).Select(e => e.EmployeeName).FirstOrDefault();
            IEnumerable<string> repoKeyword = _context.RepositoryKeywords.Where(x => x.RepostioryId == id)
                .Join(_context.RefKeywords, r => r.RefKeywordId, rk => rk.RefKeywordId, (r, rk) => rk.KeywordName);

            MergeRepoViewDashboard merge = new()
            {
                Repository = repo,
                UploadBy = empUploader,
                Keywords = repoKeyword
            };

            return merge;
        }

        public async Task<string> CreateNewRepoAsync(Repository repository, List<RepoFile> files, List<long> authorIds, List<string> keywords)
        {
            string resultMsg = string.Empty;
            if (IsValidInput(repository, files, ref resultMsg, "ADD"))
            {
                List<FileDetail> fileDetails = new();
                try
                {
                    repository.UploadDate = DateTime.Now;
                    _context.Add(repository);

                    foreach (long authId in authorIds)
                    {
                        RepositoryD rd = new()
                        {
                            AuthorId = authId
                        };
                        repository.RepositoryDs.Add(rd);
                    }

                    repository.RepositoryKeywords = CreateKeywordForRepo(keywords);

                    fileDetails = UploadFile(files, (long)repository.RefCollectionId);
                    foreach (FileDetail file in fileDetails)
                    {
                        repository.FileDetails.Add(file);
                    }

                    await _context.SaveChangesAsync();
                    await _userService.AddUserActHist(repository.UsrCreate, $"New Submision with ID : {repository.RepositoryId}", "Submit New Repository");
                }
                catch (Exception ex)
                {
                    resultMsg = ex.InnerException.Message is not null ? ex.InnerException.Message : ex.Message;
                    _logger.LogError(resultMsg);
                    if (fileDetails.Any())
                    {
                        if (Directory.Exists(fileDetails.FirstOrDefault().FilePath))
                        {
                            string[] fileToDelete = Directory.GetFiles(fileDetails.FirstOrDefault().FilePath);
                            if (fileToDelete.Length > 0)
                            {
                                DeleteFolder(fileToDelete, fileDetails.FirstOrDefault().FilePath);
                            }
                        }
                    }
                }
            }
            return resultMsg;
        }

        public async Task<string> EditRepoAsync(Repository repository, List<RepoFile> files, List<long> authorIds, string userEdit, List<string> keywords)
        {
            string resultMsg = string.Empty;
            if (IsValidInput(repository, files, ref resultMsg))
            {
                files = files.Where(f => f.FormFile is not null).ToList();
                if (files.Any())
                {
                    foreach (RepoFile repofile in files)
                    {
                        FileDetail updateFileDetail = (from f in _context.FileDetails
                                                       join ft in _context.RefRepositoryFileTypes on f.RefRepositoryFileTypeId equals ft.RefRepositoryFileTypeId
                                                       where f.RepositoryId == repository.RepositoryId && ft.RepositoryFileTypeCode == repofile.FileTypeCode
                                                       select f).FirstOrDefault();

                        if (updateFileDetail is not null)
                        {
                            string dir = updateFileDetail.FilePath;
                            if (Directory.Exists(dir))
                            {
                                string[] filesToDelete = Directory.GetFiles(dir, updateFileDetail.FileName);
                                if (filesToDelete.Length > 0)
                                {
                                    DeleteFolder(filesToDelete, dir, true);
                                }
                            }

                            List<FileDetail> fileDetails = UploadFile(files, (long)repository.RefCollectionId, updateFileDetail, existingPath: dir);
                            foreach (FileDetail fd in fileDetails)
                            {
                                _context.FileDetails.Update(fd);
                            }
                        }
                        else
                        {
                            string existingpath = _context.FileDetails.Where(r => r.RepositoryId == repository.RepositoryId).Select(f => f.FilePath).FirstOrDefault();
                            List<FileDetail> fileDetails = UploadFile(files, (long)repository.RefCollectionId, existingPath: existingpath);
                            foreach (FileDetail fd in fileDetails)
                            {
                                repository.FileDetails.Add(fd);
                            }
                        }
                    }
                }

                List<RepositoryKeyword> listRepoKeyword = (from r in _context.RepositoryKeywords
                                                           where r.RepostioryId == repository.RepositoryId
                                                           select r).ToList();

                List<string> repoKeywordId = listRepoKeyword.Select(a => a.RefKeywordId.ToString()).ToList();
                bool compareKeyword = false;

                if (repoKeywordId.Count == keywords.Count)
                    compareKeyword = repoKeywordId.Except(keywords).Any();
                else
                    compareKeyword = true;

                if (compareKeyword || !listRepoKeyword.Any())
                {
                    repository.RepositoryKeywords = CreateKeywordForRepo(keywords, listRepoKeyword);
                }

                List<RepositoryD> repositoryDs = _context.RepositoryDs.Where(f => f.RepositoryId.Equals(repository.RepositoryId)).ToList();
                List<long> authors = repositoryDs.Select(l => l.AuthorId).ToList();

                if (!CheckIsAuthorSameForEdit(authors, authorIds))
                {
                    foreach (RepositoryD rd in repositoryDs)
                    {
                        _context.RepositoryDs.Remove(rd);
                    }

                    foreach (long autid in authorIds)
                    {
                        RepositoryD newRd = new()
                        {
                            AuthorId = autid
                        };
                        repository.RepositoryDs.Add(newRd);
                    }
                }

                _context.Update(repository);
                await _context.SaveChangesAsync();
                await _userService.AddUserActHist(userEdit, $"Update Repository with ID : {repository.RepositoryId}", "Update Repository");
            }
            return resultMsg;
        }

        public async Task DeleteRepoAsync(long id)
        {
            Repository repository = _context.Repositories.Find(id);
            List<RepositoryD> rd = _context.RepositoryDs.Where(r => r.RepositoryId.Equals(id)).ToList();
            List<FileDetail> fileDetails = _context.FileDetails.Where(f => f.RepositoryId == repository.RepositoryId).ToList();
            RepoStatistic repoStat = _context.RepoStatistics.Where(r => r.RepositoryId == repository.RepositoryId).FirstOrDefault();

            string repoTitle = repository.Title;
            string userid = repository.UsrCreate;

            if (repoStat is not null)
                _context.RepoStatistics.Remove(repoStat);

            foreach (RepositoryD rds in rd)
                _context.RepositoryDs.Remove(rds);

            string dir = fileDetails.FirstOrDefault().FilePath;
            string[] files = Directory.GetFiles(dir);
            DeleteFolder(files, dir);

            foreach (FileDetail fd in fileDetails)
            {
                List<FileMonitoringResult> listFileMonitoringRes = _fileMonitoringService.GetMonitoringResultByFileDetailId(fd.FileDetailId);
                if (listFileMonitoringRes.Any())
                {
                    for (int i = 0; i < listFileMonitoringRes.Count; i++)
                    {
                        FileMonitoringHist fmh = _fileMonitoringService.GetFileMonitoringHistObjById((long)listFileMonitoringRes[i].FileMonitoringHistId);
                        _context.FileMonitoringResults.Remove(listFileMonitoringRes[i]);
                        if (fmh.TotalFileProblem > 0)
                        {
                            fmh.TotalFileProblem -= 1;
                        }
                        _context.Update(fmh);
                    }
                }

                List<DownloadStatistic> ds = _context.DownloadStatistics.Where(d => d.FileDetailId == fd.FileDetailId).ToList();
                foreach (DownloadStatistic dsItem in ds)
                {
                    _context.DownloadStatistics.Remove(dsItem);
                }
                _context.FileDetails.Remove(fd);
            }

            _context.Repositories.Remove(repository);
            await _context.SaveChangesAsync();
        }

        public string DeleteRepositoryFile(long id)
        {
            string res = string.Empty;
            FileDetail fileDetail = _context.FileDetails.Find(id);
            if (CheckIsLastRepositoryFile((long)fileDetail.RepositoryId))
            {
                res = "Cannot Delete Last File of this Repository";
            }
            else
            {
                if (Directory.Exists(fileDetail.FilePath))
                {
                    string[] files = Directory.GetFiles(fileDetail.FilePath, fileDetail.FileName);
                    if (files.Length > 0)
                    {
                        try
                        {
                            DeleteFolder(files, fileDetail.FilePath, true);
                            List<FileMonitoringResult> listFileMonitoringRes = _fileMonitoringService.GetMonitoringResultByFileDetailId(fileDetail.FileDetailId);
                            List<DownloadStatistic> downloadStatistics = _context.DownloadStatistics.Where(d => d.FileDetailId == fileDetail.FileDetailId).ToList();

                            for (int i = 0; i < listFileMonitoringRes.Count; i++)
                            {
                                FileMonitoringHist fmh = _fileMonitoringService.GetFileMonitoringHistObjById((long)listFileMonitoringRes[i].FileMonitoringHistId);
                                _context.FileMonitoringResults.Remove(listFileMonitoringRes[i]);
                                if (fmh.TotalFileProblem > 0)
                                {
                                    fmh.TotalFileProblem -= 1;
                                }
                                _context.Update(fmh);
                            }

                            for (int i = 0; i < downloadStatistics.Count; i++)
                            {
                                _context.DownloadStatistics.Remove(downloadStatistics[i]);
                            }

                            _context.FileDetails.Remove(fileDetail);
                            _context.SaveChanges();
                        }
                        catch (Exception e)
                        {
                            res = e.Message;
                        }
                    }
                    else
                    {
                        res = $"File {fileDetail.FileName} on folder {fileDetail.FilePath} is Not Exist";
                    }
                }
                else
                {
                    res = $"Folder Path {fileDetail.FilePath} is Not Exist";
                }
            }
            return res;
        }

        public IEnumerable<CurrentFileInfo> GetCurrentFileInfos(long repoid)
        {
            List<CurrentFileInfo> repofileinfo = (from r in _context.FileDetails
                                                  join ft in _context.RefRepositoryFileTypes on r.RefRepositoryFileTypeId equals ft.RefRepositoryFileTypeId
                                                  where r.RepositoryId == repoid
                                                  select new CurrentFileInfo
                                                  {
                                                      FileDetailId = r.FileDetailId,
                                                      FileName = r.FileName,
                                                      FileSize = r.FileSize,
                                                      OriginalName = r.OriginFileName,
                                                      FileType = ft.RepositoryFileTypeName,
                                                      Path = r.FilePath
                                                  }).ToList();

            foreach (CurrentFileInfo item in repofileinfo)
                item.FileStatus = CheckFileStatus(item.Path);

            return repofileinfo;
        }

        public bool CheckRepoHasFilePerType(long repoid, long typeid)
        {
            return _context.FileDetails.Where(f => f.RepositoryId == repoid && f.RefRepositoryFileTypeId == typeid).Any();
        }

        #region REPOSITORY REPORT
        public DataTable GetDataReportStatPeryear(int year)
        {
            IConfigurationBuilder builder = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                            .AddEnvironmentVariables();
            IConfiguration config = builder.Build();

            string conString = config.GetConnectionString("RepoAssasins");

            DataTable dt = null;

            //using (SqlConnection con = new(conString))
            //{
            //    con.Open();
            //    using (SqlCommand cmd = new("spReportStatisticPerYear"))
            //    {
            //        cmd.Connection = con;
            //        cmd.CommandType = CommandType.StoredProcedure;
            //        cmd.Parameters.Add("@PublishYear", SqlDbType.Int).Value = year;
            //        using SqlDataAdapter sda = new(cmd);
            //        dt = new();
            //        sda.Fill(dt);
            //    }
            //    con.Close();
            //}
            return dt;
        }

        public async Task<Dictionary<int, int>> GetListYearPublishAsync()
        {
            return await _context.Repositories.Select(y => y.PublishDate.Year).Distinct().ToDictionaryAsync(d => d);
        }
        #endregion

        #endregion

        #region FOR MAIN
        public IQueryable<Repository> GetRepositoriesForIndexHomePageAsync()
        {
            IQueryable<Repository> repositories = _context.Repositories.OrderByDescending(r => r.UploadDate)
             .Include(r => r.RepositoryDs)
                 .ThenInclude(p => p.Author)
             .Include(s => s.RepoStatistics)
             .Take(20)
             .AsNoTracking();

            return repositories;
        }

        public async Task<MergeRepoView> GetRepoByIdAsync(long id)
        {
            Repository rep = await _context.Repositories.FindAsync(id);
            if (rep is null)
                return null;

            IEnumerable<Author> listAuthors = _authorService.GetListAuthorByReposId(id);
            IEnumerable<FileDetail> fileDetails = _context.FileDetails.Where(f => f.RepositoryId == id).Include(f => f.RefRepositoryFileType);
            IEnumerable<RefKeyword> refKeywords = await _generalService.GetRepositoryKeywordByRepoId(id);
            MergeRepoView repoView = new()
            {
                repository = rep,
                authors = listAuthors,
                fileDetails = fileDetails,
                RefCollName = _context.RefCollections.Where(c => c.RefCollectionId == rep.RefCollectionId).FirstOrDefault().CollName,
                CollDName = _context.CollectionDs.Where(c => c.CollectionDid == rep.CollectionDid).FirstOrDefault().CollectionDname,
                Keywords = refKeywords.Select(r => r.KeywordName)
            };
            InsertRepoStatistic(rep.RepositoryId);

            return repoView;
        }

        public async Task<FileDetail> GetFileDetailForViewAsync(long id)
        {
            return await _context.FileDetails.FindAsync(id);
        }

        public FileDetail GetFileDetailByFileName(string fname)
        {
            return _context.FileDetails.Where(f => f.FileName == fname).FirstOrDefault();
        }

        public Dictionary<string, int> GetCountRepositoryOfRangeYear(List<string> year)
        {
            Dictionary<string, int> listGrpYear = new();
            foreach (string item in year)
            {
                int yearStart = Convert.ToInt32(item[..4]);
                int yearEnd = Convert.ToInt32(item.Substring(item.Length - 5, 5));

                SetParamDateBetweenYear(yearStart, yearEnd, out DateTime dtStart, out DateTime dtEnd);
                List<DateTime> listDt = (from r in _context.Repositories
                                         where r.PublishDate >= dtStart && r.PublishDate <= dtEnd
                                         select r.PublishDate).ToList();

                var data = (from r in listDt
                            group r by r.Year into grp
                            select new
                            {
                                Cnt = grp.Count(),
                                Id = grp.Key
                            });

                int totalCnt = data.Sum(r => r.Cnt);
                listGrpYear.Add(item, totalCnt);
            }

            return listGrpYear;
        }

        public IQueryable<Repository> MoreSearchResult(string title, long? type, long? colld, int? year, int? yearTo, string author)
        {
            IQueryable<Repository> result = _context.Repositories;

            if (!string.IsNullOrWhiteSpace(title))
            {
                result = result.Where(t => t.Title.Contains(title));
            }

            if (year is not null)
            {
                SetParamDateBetweenYear((int)year, yearTo ?? 0, out DateTime dtStart, out DateTime dtTo);
                if (yearTo is not null)
                {
                    result = result.Where(y => y.PublishDate >= dtStart && y.PublishDate <= dtTo);
                }
                else
                {
                    result = result.Where(y => y.PublishDate >= dtStart);
                }
            }

            if (type is not null)
            {
                result = from r in result
                         join c in _context.RefCollections on r.RefCollectionId equals c.RefCollectionId
                         where c.RefCollectionId == type
                         select r;
            }

            if (colld is not null)
            {
                result = from r in result
                         join c in _context.CollectionDs on r.CollectionDid equals c.CollectionDid
                         where c.CollectionDid == colld
                         select r;
            }

            if (!string.IsNullOrWhiteSpace(author))
            {
                result = from r in result
                         join d in _context.RepositoryDs on r.RepositoryId equals d.RepositoryId
                         join a in _context.Authors on d.AuthorId equals a.AuthorId
                         where a.FirstName.Contains(author) || a.LastName.Contains(author)
                         select r;
            }

            result = result.Include(d => d.RepositoryDs).ThenInclude(a => a.Author)
                .Include(s => s.RepoStatistics)
                .OrderByDescending(r => r.RepositoryId)
                .AsNoTracking();

            return result;
        }

        public void InsertDownloadStat(long fileid, string userid, bool issuccess, string errMsg = "")
        {
            DownloadStatistic downloadStatistic;
            if (issuccess)
            {
                downloadStatistic = new()
                {
                    FileDetailId = fileid,
                    UserId = userid,
                    DownloadDate = DateTime.Now,
                    DownloadStatus = issuccess
                };

                long repoId = _context.FileDetails.Where(f => f.FileDetailId == fileid).Select(r => (long)r.RepositoryId).FirstOrDefault();
                RepoStatistic rs = _context.RepoStatistics.Where(r => r.RepositoryId == repoId).FirstOrDefault();
                rs.DownloadCount += 1;

                _context.Update(rs);
                _context.Add(downloadStatistic);
            }
            else
            {
                downloadStatistic = new()
                {
                    FileDetailId = fileid,
                    UserId = userid,
                    DownloadDate = DateTime.Now,
                    DownloadStatus = issuccess,
                    ErrorMsg = errMsg
                };

                _context.Add(downloadStatistic);
            }

            _context.SaveChanges();
        }

        public IEnumerable<KeywordsGrouping> GetGroupingKeywordForSideMenu()
        {
            IQueryable<KeywordsGrouping> data = (from r in _context.Repositories
                                                 join rk in _context.RepositoryKeywords on r.RepositoryId equals rk.RepostioryId
                                                 join k in _context.RefKeywords on rk.RefKeywordId equals k.RefKeywordId
                                                 group r by new { k.RefKeywordId, k.KeywordName, k.KeywordCode } into grp
                                                 orderby grp.Count() descending
                                                 select new KeywordsGrouping
                                                 {
                                                     KeywordName = grp.Key.KeywordName,
                                                     KeywordCode = grp.Key.KeywordCode,
                                                     Count = grp.Count()
                                                 }).Take(5);

            return data;
        }
        #endregion

        #region PRIVATE METHOD
        private string CheckFileStatus(string path)
        {
            string resMsg = "Ok";
            if (Directory.Exists(path))
            {
                string[] filesToDelete = Directory.GetFiles(path);
                if (filesToDelete.Length < 1)
                {
                    resMsg = "No File Exist";
                }
            }
            else
            {
                resMsg = "Folder Is Not Exist";
            }
            return resMsg;
        }

        private static bool IsValidInput(Repository rep, List<RepoFile> files, ref string msg, string saveMode = "")
        {
            bool res = true;
            if (string.IsNullOrWhiteSpace(rep.Title))
            {
                msg = "Please input Title";
                res = false;
                return res;
            }
            else if (rep.PublishDate.ToString() == string.Empty)
            {
                msg = "Please Input Publish Date";
                res = false;
                return res;
            }
            else if (rep.RefCollectionId == null)
            {
                msg = "Please Select Collection Type";
                res = false;
                return res;
            }

            List<bool> statusAtached = new();
            foreach (RepoFile item in files.Where(r => r.FormFile is not null))
            {
                if (item.FormFile is null)
                    statusAtached.Add(false);

                string ext = System.IO.Path.GetExtension(item.FormFile.FileName);
                if (ext != ".pdf" && !string.IsNullOrEmpty(ext))
                {
                    msg = $"Please Select File with .PDF Format for {item.FileTypeName} File";
                    res = false;
                    return res;
                }
            }

            if (statusAtached.Any())
            {
                if (!statusAtached.Contains(true))
                {
                    msg = "Please Attach Repository File";
                    res = false;
                    return res;
                }
            }

            return res;
        }

        private List<FileDetail> UploadFile(List<RepoFile> files, long collectionId, FileDetail fileDetail = null, string existingPath = "")
        {
            string collCode = _context.RefCollections.Where(c => c.RefCollectionId.Equals(collectionId)).FirstOrDefault().CollCode;
            IConfigurationBuilder builder = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                            .AddEnvironmentVariables();
            IConfiguration config = builder.Build();
            string filePath = config["UploadPath"];

            string finalPath = string.Empty;
            if (string.IsNullOrEmpty(existingPath))
            {
                DirectoryInfo di = CreateDirectrory(filePath, collCode);
                di.Create();
                finalPath = di.ToString();
            }
            else
            {
                finalPath = existingPath;
            }

            List<FileDetail> fileDetails = new();
            foreach (RepoFile item in files.Where(f => f.FormFile is not null))
            {
                Guid g = Guid.NewGuid();
                string originFilename = item.FormFile.FileName;
                string newFname = g.ToString() + System.IO.Path.GetExtension(item.FormFile.FileName);
                string fullPath = System.IO.Path.Combine(finalPath, newFname);
                long fileSize = item.FormFile.Length;
                WatermarkPDF(item.FormFile, fullPath);
                //using FileStream stream = new(fullPath, FileMode.Create);
                long fileTypeId = _generalService.GetRefRepositoryFileTypeByCode(item.FileTypeCode).RefRepositoryFileTypeId;

                if (fileDetail is null)
                {
                    fileDetail = new();
                }

                fileDetail.FileName = newFname;
                fileDetail.FilePath = finalPath;
                fileDetail.FileExt = System.IO.Path.GetExtension(item.FormFile.FileName);
                fileDetail.FileSize = fileSize.ToString();
                fileDetail.OriginFileName = originFilename;
                fileDetail.RefRepositoryFileTypeId = fileTypeId;
                fileDetails.Add(fileDetail);
            }

            return fileDetails;
        }

        private static void WatermarkPDF(IFormFile pdfFile, string fullpath)
        {
            float watermarkTrimmingRectangleWidth = 300;
            float watermarkTrimmingRectangleHeight = 300;

            float formWidth = 600;
            float formHeight = 600;
            float formXOffset = 0;
            float formYOffset = 0;

            float xTranslation = 50;
            float yTranslation = 33;

            double rotationInRads = Math.PI / 3.5;

            PdfFont font = PdfFontFactory.CreateFont();
            float fontSize = 60;

            Stream stream = pdfFile.OpenReadStream();

            PdfDocument pdfDoc = new(new PdfReader(stream), new PdfWriter(fullpath));
            var numberOfPages = pdfDoc.GetNumberOfPages();
            PdfPage page = null;

            for (var i = 1; i <= numberOfPages; i++)
            {
                page = pdfDoc.GetPage(i);
                Rectangle ps = page.GetPageSize();

                //Center the annotation
                float bottomLeftX = ps.GetWidth() / 2 - watermarkTrimmingRectangleWidth / 2;
                float bottomLeftY = ps.GetHeight() / 2 - watermarkTrimmingRectangleHeight / 2;
                Rectangle watermarkTrimmingRectangle = new(bottomLeftX, bottomLeftY, watermarkTrimmingRectangleWidth, watermarkTrimmingRectangleHeight);

                PdfWatermarkAnnotation watermark = new(watermarkTrimmingRectangle);

                //Apply linear algebra rotation math
                //Create identity matrix
                AffineTransform transform = new();//No-args constructor creates the identity transform
                                                                  //Apply translation
                transform.Translate(xTranslation, yTranslation);
                //Apply rotation
                transform.Rotate(rotationInRads);

                PdfFixedPrint fixedPrint = new();
                watermark.SetFixedPrint(fixedPrint);
                //Create appearance
                Rectangle formRectangle = new(formXOffset, formYOffset, formWidth, formHeight);

                //Observation: font XObject will be resized to fit inside the watermark rectangle
                PdfFormXObject form = new(formRectangle);
                PdfExtGState gs1 = new PdfExtGState().SetFillOpacity(0.2f);
                PdfCanvas canvas = new(form, pdfDoc);

                float[] transformValues = new float[6];
                transform.GetMatrix(transformValues);
                canvas.SaveState()
                    .BeginText().SetColor(DeviceRgb.BLACK, true).SetExtGState(gs1)
                    .SetTextMatrix(transformValues[0], transformValues[1], transformValues[2], transformValues[3], transformValues[4], transformValues[5])
                    .SetFontAndSize(font, fontSize)
                    .ShowText("Repository FEB UNPATTI")
                    .EndText()
                    .RestoreState();

                canvas.Release();

                watermark.SetAppearance(PdfName.N, new PdfAnnotationAppearance(form.GetPdfObject()));
                watermark.SetFlags(PdfAnnotation.PRINT);

                page.AddAnnotation(watermark);

            }
            page?.Flush();
            pdfDoc.Close();

            //return pdfDoc;
        }

        private static void DeleteFolder(string[] files, string dir, bool fileOnly = false)
        {
            foreach (string item in files)
            {
                if (File.Exists(item))
                {
                    File.Delete(item);
                }
            }

            if (!fileOnly)
            {
                string[] fileAfterDel = Directory.GetFiles(dir);
                if (fileAfterDel.Length <= 0)
                {
                    if (Directory.Exists(dir))
                    {
                        Directory.Delete(dir);
                    }
                }
            }
        }

        private void InsertRepoStatistic(long repoid)
        {
            RepoStatistic stat = _context.RepoStatistics.Where(r => r.RepositoryId == repoid).FirstOrDefault();

            if (stat is not null)
            {
                stat.LinkHitCount++;
                _context.Update(stat);
            }
            else
            {
                stat = new()
                {
                    RepositoryId = repoid,
                    DownloadCount = 0,
                    LinkHitCount = 1
                };
                _context.Add(stat);
            }
            _context.SaveChanges();
        }

        private static bool CheckIsAuthorSameForEdit(List<long> listRD, List<long> authorsnew)
        {
            if (listRD.Count == authorsnew.Count)
            {
                bool compare = listRD.Except(authorsnew).Any();
                if (compare)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return false;
            }
        }

        private bool CheckIsLastRepositoryFile(long repoid)
        {
            bool islast = false;
            int count = _context.FileDetails.Where(f => f.RepositoryId == repoid).Count();
            if (count <= 1)
            {
                islast = true;
            }
            return islast;
        }

        private List<RepositoryKeyword> CreateKeywordForRepo(List<string> keywords, List<RepositoryKeyword> repositoryKeywords = null)
        {
            if (repositoryKeywords is null)
            {
                repositoryKeywords = new();
            }
            else
            {
                //Remove yg lama
                foreach (RepositoryKeyword rk in repositoryKeywords)
                {
                    _context.RepositoryKeywords.Remove(rk);
                }
            }

            List<RefKeyword> listKeyword = new();
            foreach (string keyword in keywords)
            {
                if (!long.TryParse(keyword, out long num)) //untuk buat keyword baru kalo inputan tag baru dari halaman
                {
                    RefKeyword newKeyword = _generalService.GetRefKeywordObjByCode(keyword);
                    if (newKeyword is null)
                    {
                        newKeyword = _generalService.CreateNewKeyword(keyword);
                        _context.Add(newKeyword);
                    }
                    listKeyword.Add(newKeyword);
                }
                else
                {
                    RefKeyword refkeyword = _generalService.GetRefKeywordObjById(num);
                    if (refkeyword is null)
                    {
                        refkeyword = _generalService.CreateNewKeyword(num.ToString());
                        _context.Add(keyword);
                    }
                    listKeyword.Add(refkeyword);
                }
            }

            foreach (RefKeyword key in listKeyword)
            {
                RepositoryKeyword rk = new()
                {
                    RefKeyword = key
                };
                repositoryKeywords.Add(rk);
            }

            return repositoryKeywords;
        }
        #endregion
    }
}
