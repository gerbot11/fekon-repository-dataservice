using fekon_repository_api;
using fekon_repository_datamodel.MergeModels;
using fekon_repository_datamodel.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace fekon_repository_dataservice.Services
{
    public class DiscoverService : BaseService, IDiscoverService
    {
        private readonly ICollectionService _collectionService;
        public DiscoverService(REPOSITORY_DEVContext context, ICollectionService collectionService)
            : base(context)
        {
            _collectionService = collectionService;
        }

        public IQueryable<Repository> GeneralSearch(string query, string category = "", long subcategory = 0)
        {
            IQueryable<Repository> rep = _context.Repositories.OrderByDescending(a => a.RepositoryId);

            if (!string.IsNullOrEmpty(category))
            {
                rep = from r in rep
                      join c in _context.RefCollections on r.RefCollectionId equals c.RefCollectionId
                      where c.CollCode == category
                      select r;
            }

            if (subcategory > 0)
                rep = rep.Where(r => r.CollectionDid == subcategory);


            IQueryable<long> listrepo = (from r in rep
                                         join repd in (from rd in _context.RepositoryDs
                                                       join a in _context.Authors on rd.AuthorId equals a.AuthorId
                                                       select new
                                                       {
                                                           rd.RepositoryId,
                                                           a.LastName,
                                                           a.FirstName
                                                       }) on r.RepositoryId equals repd.RepositoryId
                                         //      //join repk in (from rk in _context.RepositoryKeywords
                                         //      //              join refk in _context.RefKeywords on rk.RefKeywordId equals refk.RefKeywordId
                                         //      //              select new
                                         //      //              {
                                         //      //                  rk.RepostioryId,
                                         //      //                  refk.KeywordName
                                         //      //              }) on r.RepositoryId equals repk.RepostioryId
                                         where r.Title.Contains(query) || r.Description.Contains(query) || repd.FirstName.Contains(query) || repd.LastName.Contains(query) //|| repk.KeywordName.Contains(query)
                                         select r.RepositoryId).Distinct();


            rep = rep.Include(r => r.RepositoryDs).ThenInclude(a => a.Author)
                     .Include(s => s.RepoStatistics)
                     .Include(c => c.CollectionD)
                     .Include(r => r.RefCollection)
                     .AsNoTracking();

            rep = rep.Where(x => listrepo.Contains(x.RepositoryId));

            //rep = rep.OrderByDescending(r => r.RepositoryId)
            //    .Include(r => r.RepositoryDs).ThenInclude(a => a.Author)
            //         .Include(s => s.RepoStatistics)
            //         .Include(c => c.CollectionD)
            //         .Include(r => r.RefCollection)
            //         .AsNoTracking();

            return rep;
        }

        public IQueryable<MergeAuthorGrouping> DiscoverAuthor(string query, string category, long subcategory, char? startChar)
        {
            IQueryable<Author> authors = _context.Authors;

            RefCollection refColl = _collectionService.FindRefCollectionByCode(category);
            CollectionD colld = _collectionService.FindCollectionDById(subcategory);

            if (refColl is null || colld is null)
                return null;

            long rcId = refColl.RefCollectionId;
            long colldId = colld.CollectionDid;

            if (startChar is not null)
            {
                authors = authors
                    .Where(r => r.FirstName.Substring(0, 1) == startChar.ToString());
            }

            if (!string.IsNullOrEmpty(query))
            {
                authors = authors.Where(a => a.FirstName.Contains(query) || a.LastName.Contains(query));
            }

            IQueryable<MergeAuthorGrouping> finalRes = from a in authors
                                                       join r in _context.RepositoryDs on a.AuthorId equals r.AuthorId
                                                       join rep in _context.Repositories on r.RepositoryId equals rep.RepositoryId
                                                       orderby a.FirstName ascending
                                                       where rep.RefCollectionId == rcId && rep.CollectionDid == colldId
                                                       group a by new { a.AuthorId, a.FirstName, a.LastName } into grpRes
                                                       where grpRes.Count() > 0
                                                       select new MergeAuthorGrouping
                                                       {
                                                           Id = grpRes.Key.AuthorId,
                                                           Name = string.IsNullOrEmpty(grpRes.Key.LastName) ? $"{ grpRes.Key.FirstName }" : $"{grpRes.Key.LastName}, {grpRes.Key.FirstName}",
                                                           RepoCount = grpRes.Count()
                                                       };

            return finalRes;
        }

        public IQueryable<MergeAuthorGrouping> DiscoverMoreAuthor(string query, string isAdvisior, char? startChar)
        {
            IQueryable<Author> authors = _context.Authors.Where(a => a.IsAdvisor == isAdvisior);
            if (!string.IsNullOrWhiteSpace(query))
            {
                authors = authors.Where(a => a.FirstName.Contains(query) || a.LastName.Contains(query));
            }

            if (startChar is not null)
            {
                authors = authors
                    .Where(r => r.FirstName.Substring(0, 1) == startChar.ToString());
            }

            IQueryable<MergeAuthorGrouping> authorRepo = (from a in authors
                                                          join rd in _context.RepositoryDs on a.AuthorId equals rd.AuthorId
                                                          join r in _context.Repositories on rd.RepositoryId equals r.RepositoryId
                                                          group a by new { a.AuthorId, a.FirstName, a.LastName } into grpRes
                                                          orderby grpRes.Count() descending
                                                          where grpRes.Count() > 0
                                                          select new MergeAuthorGrouping
                                                          {
                                                              Id = grpRes.Key.AuthorId,
                                                              Name = string.IsNullOrEmpty(grpRes.Key.LastName) ? $"{ grpRes.Key.FirstName }" : $"{grpRes.Key.LastName}, {grpRes.Key.FirstName}",
                                                              RepoCount = grpRes.Count()
                                                          });

            return authorRepo;
        }

        public IQueryable<DateTime> DiscoverPublishDt(string category, long subcategory, DateTime dateFrom, DateTime dateTo)
        {
            IQueryable<DateTime> dtRes = (from r in _context.Repositories
                                          join c in _context.RefCollections on r.RefCollectionId equals c.RefCollectionId
                                          where r.PublishDate >= dateFrom && r.PublishDate <= dateTo && (c.CollCode.Equals(category) && r.CollectionDid.Equals(subcategory))
                                          orderby r.PublishDate ascending
                                          select r.PublishDate).Distinct();

            return dtRes;
        }

        public IQueryable<KeywordsGrouping> DiscoverKeyword(string query)
        {
            IQueryable<RefKeyword> refKeywords = _context.RefKeywords;
            if (!string.IsNullOrEmpty(query))
                refKeywords = refKeywords.Where(r => r.KeywordName.Contains(query));

            IQueryable<KeywordsGrouping> data = (from r in _context.Repositories
                                                 join rk in _context.RepositoryKeywords on r.RepositoryId equals rk.RepostioryId
                                                 join k in refKeywords on rk.RefKeywordId equals k.RefKeywordId
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
    }
}
