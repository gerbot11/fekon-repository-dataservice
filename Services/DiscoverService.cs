using fekon_repository_api;
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
            IQueryable<Repository> rep;

            if (string.IsNullOrWhiteSpace(query) || string.IsNullOrEmpty(query))
                return null;
            else
            {
                rep = _context.Repositories.Include(r => r.RepositoryDs)
                                 .ThenInclude(a => a.Author)
                             .Include(c => c.RefCollection)
                             .Include(s => s.RepoStatistics)
                             .Where(c => c.Title.Contains(query) || c.Description.Contains(query)
                             || c.RepositoryDs.Select(a => a.Author.FirstName).Contains(query) || c.RepositoryDs.Select(a => a.Author.LastName).Contains(query))
                             .OrderBy(r => r.PublishDate)
                             .AsNoTracking();

                if (!string.IsNullOrEmpty(category))
                    rep = rep.Where(r => r.RefCollection.CollCode == category);

                if (subcategory > 0)
                    rep = rep.Where(r => r.CollectionDid == subcategory);
            }

            return !rep.Any() ? null : rep;
        }

        public IQueryable<Author> DiscoverAuthor(string category, long subcategory, char? startChar)
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
                    .Where(r => r.FirstName.Substring(0, 1) == startChar.ToString())
                    .OrderBy(o => o.FirstName)
                    .AsNoTracking();
            }

            IQueryable<Author> finalRes = from a in authors
                                          join r in _context.RepositoryDs on a.AuthorId equals r.AuthorId
                                          join rep in _context.Repositories on r.RepositoryId equals rep.RepositoryId
                                          where rep.RefCollectionId == rcId && rep.CollectionDid == colldId
                                          select a;

            return finalRes;
        }

        public IQueryable<DateTime> DiscoverPublishDt(string category, long subcategory, DateTime dateFrom, DateTime dateTo)
        {
            IQueryable<DateTime> dtRes = (from r in _context.Repositories
                                          join c in _context.RefCollections on r.RefCollectionId equals c.RefCollectionId
                                          where r.PublishDate >= dateFrom && r.PublishDate <= dateTo && (c.CollCode.Equals(category) && r.CollectionDid.Equals(subcategory))
                                          orderby r.PublishDate ascending
                                          select r.PublishDate).Distinct();

            return !dtRes.Any() ? null : dtRes;
        }
    }
}
