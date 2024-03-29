﻿using fekon_repository_api;
using fekon_repository_datamodel.DashboardModels;
using fekon_repository_datamodel.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace fekon_repository_dataservice.Services
{
    public class DashboardService : BaseService, IDashboardService
    {
        private readonly IAuthorService _authorService;
        private readonly IUserService _userService;
        private readonly IRepoService _repoService;
        public DashboardService(REPOSITORY_DEVContext context, IAuthorService authorService, IUserService userService, IRepoService repoService) 
            : base(context)
        {
            _authorService = authorService;
            _userService = userService;
            _repoService = repoService;
        }

        public async Task<IEnumerable<TotalRepositoryPerColl>> SetTotalRepoPerCollection()
        {
            IEnumerable<TotalRepositoryPerColl> countData = await (from r in _context.Repositories
                                                                   join t in _context.RefCollections on r.RefCollectionId equals t.RefCollectionId
                                                                   group r by new { t.RefCollectionId, t.CollName } into resGrp
                                                                   select new TotalRepositoryPerColl
                                                                   {
                                                                       Data = resGrp.Count(),
                                                                       CollName = resGrp.Key.CollName
                                                                   }).ToListAsync();

            return countData;
        }

        public async Task<IEnumerable<TotalRepositoryPerType>> SetTotalRepoPerType()
        {
            List<TotalRepositoryPerType> countData = await (from r in _context.Repositories
                                                            join c in _context.CollectionDs on r.CollectionDid equals c.CollectionDid
                                                            group r by new { c.CollectionDid, c.CollectionDname } into resGrp
                                                            select new TotalRepositoryPerType
                                                            {
                                                                Data = resGrp.Count(),
                                                                TypeName = resGrp.Key.CollectionDname
                                                            }).ToListAsync();
            return countData;
        }

        //Method ini seng support MySql ganti pake SP
        public async Task<IEnumerable<TotalRepositoryPerYearPublish>> SetTotalRepoPerYearPublish()
        {
            IEnumerable<TotalRepositoryPerYearPublish> countData = await (from d in _context.Repositories
                                                                          group d by d.PublishDate.Year into grp
                                                                          select new TotalRepositoryPerYearPublish
                                                                          {
                                                                              Year = grp.Key,
                                                                              Value = grp.Count()
                                                                          }).ToListAsync();

           

            return countData;
        }

        public IEnumerable<TotalRepositoryPerYearPublish> SetTotalRepoPerYearPublishWithSP(string connString)
        {
            IEnumerable<TotalRepositoryPerYearPublish> data = null;
            DataTable dt = GetDataBySp(connString, "spGetCountRepoPerYear");
            data = (from DataRow row in dt.Rows
                    select new TotalRepositoryPerYearPublish
                    {
                        Year = Convert.ToInt32(row["Tahun"]),
                        Value = Convert.ToInt32(row["CntTahun"])
                    }).ToList();
            return data;
        }

        public SummarySection SetDataSummary()
        {
            SummarySection summarySection = new();
            NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;
            NumberFormatInfo nfi2 = new CultureInfo("en-US", false).NumberFormat;
            nfi.NumberDecimalDigits = 0;
            nfi2.NumberDecimalDigits = 2;

            List<double> totSize = _context.FileDetails.Select(f => Convert.ToDouble(f.FileSize) * 0.000001).ToList();
            double fileSize = totSize.Sum();

            string totalAuthor = _authorService.GetTotalAuthor().ToString("N", nfi);
            string totalFileSize = $"{fileSize.ToString("N",nfi2)} MB";
            string totalUser = _userService.GetTotalUser().ToString("N", nfi);
            string totalRepos = _repoService.GetTotalRepository().ToString("N", nfi);

            string totalDownload = string.Empty, totalViews = string.Empty;

            summarySection.TotalAuthor = totalAuthor;
            summarySection.TotalFileSize = totalFileSize;
            summarySection.TotalUser = totalUser;
            summarySection.TotalRepository = totalRepos;
            summarySection.TopView = CalculateTopView(ref totalViews);
            summarySection.TopDownloads = CalculateTopDownload(ref totalDownload);
            summarySection.TotalDownload = totalDownload;
            summarySection.TotalViews = totalViews;

            return summarySection;
        }

        private List<SectionTopView> CalculateTopView(ref string totalViews)
        {
            decimal totalView = _context.RepoStatistics.Sum(r => r.LinkHitCount);
            totalViews = totalView.ToString();
            List<SectionTopView> data = (from r in _context.Repositories
                                         join s in _context.RepoStatistics on r.RepositoryId equals s.RepositoryId
                                         where s.LinkHitCount > 0
                                         orderby s.LinkHitCount descending
                                         select new SectionTopView
                                         {
                                             Title = r.Title,
                                             TotalView = Convert.ToInt32(s.LinkHitCount),
                                             PrcntView = (s.LinkHitCount / totalView) * 100
                                         }).Take(5).ToList();

            return data;
        }

        private List<SectionTopDownload> CalculateTopDownload(ref string totalDownloads)
        {
            decimal totalDownload = _context.DownloadStatistics.Count();
            totalDownloads = totalDownload.ToString();
            List<SectionTopDownload> data = (from r in _context.Repositories
                                             select new SectionTopDownload
                                             {
                                                 Title = r.Title,
                                                 TotalDownload = (from f in _context.FileDetails
                                                                  join d in _context.DownloadStatistics on f.FileDetailId equals d.FileDetailId
                                                                  where f.RepositoryId == r.RepositoryId
                                                                  select d.DownloadStatisticId).Count()
                                             } into topSelection
                                             where topSelection.TotalDownload > 0
                                             orderby topSelection.TotalDownload descending
                                             select topSelection
                        ).Take(5).ToList();

            foreach (var item in data)
            {
                decimal p = decimal.Divide(Convert.ToDecimal(item.TotalDownload), totalDownload);
                item.PrcntDownload = p * 100;
            }

            return data;
        }
    }
}
