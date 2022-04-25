using fekon_repository_api;
using fekon_repository_datamodel.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fekon_repository_dataservice.Services
{
    public class BrowseService : BaseService, IBrowseService
    {
        public BrowseService(REPOSITORY_DEVContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Repository>> BrowseReposByCategoryAsync(string category, long subcategory)
        {
            IEnumerable<Repository> repositories = await _context.Repositories.OrderByDescending(r => r.RepositoryId)
                .Include(r => r.RepositoryDs)
                    .ThenInclude(p => p.Author)
                .Include(c => c.CollectionD).Where(c => c.CollectionDid == subcategory)
                .Include(s => s.RepoStatistics)
                .Include(rc => rc.RefCollection).Where(rc => rc.RefCollection.CollCode == category)
                .OrderByDescending(o => o.RepositoryId)
                .Take(20)
                .AsNoTracking()
                .ToListAsync();

            return repositories;
        }

        public IQueryable<Repository> AuthorBrowseResult(long authorId, string query, string category = "", long subcategory = 0)
        {
            IQueryable<Repository> repositories = from r in _context.Repositories
                                                  join d in _context.RepositoryDs on r.RepositoryId equals d.RepositoryId
                                                  where d.AuthorId == authorId
                                                  select r;

            if (!string.IsNullOrEmpty(query) || !string.IsNullOrWhiteSpace(query))
                repositories = repositories.Where(r => r.Title.Contains(query) || r.Description.Contains(query));

            if (!string.IsNullOrEmpty(category))
                repositories = from r in repositories
                               join c in _context.RefCollections on r.RefCollectionId equals c.RefCollectionId
                               where c.CollCode == category
                               select r;

            if (subcategory is not 0)
                repositories = repositories.Where(r => r.CollectionDid == subcategory);

            repositories = repositories.Include(r => r.RepositoryDs)
                .ThenInclude(p => p.Author)
            .Include(s => s.RepoStatistics)
            .OrderByDescending(o => o.RepositoryId)
            .AsNoTracking();

            return repositories;
        }

        public IQueryable<Repository> PublishDtBrowseResult(DateTime publishDt, string cat, long subcat)
        {
            IQueryable<Repository> repositories = from r in _context.Repositories
                                                  join rc in _context.RefCollections on r.RefCollectionId equals rc.RefCollectionId
                                                  where r.PublishDate == publishDt && rc.CollCode == cat && r.CollectionDid == subcat
                                                  select r;

            repositories = repositories
                .Include(r => r.RepositoryDs)
                    .ThenInclude(p => p.Author)
                .Include(s => s.RepoStatistics)
                .OrderByDescending(o => o.RepositoryId)
                .AsNoTracking();

            return repositories;
        }

        public IQueryable<Repository> YearRangeBrowseResult(string yrange)
        {
            int ystart =  Convert.ToInt32(yrange[..4]);
            int yend = Convert.ToInt32(yrange.Substring(yrange.Length - 5, 5));

            DateTime dtStart = new(ystart, 1, 1);
            DateTime dtEnd = new(yend, 12, 31);

            IQueryable<Repository> repositories = _context.Repositories.Where(r => r.PublishDate >= dtStart && r.PublishDate <= dtEnd);

            return repositories
                .Include(d => d.RepositoryDs).ThenInclude(a => a.Author)
                .Include(s => s.RepoStatistics)
                .OrderByDescending(o => o.RepositoryId)
                .AsNoTracking();
        }

        public IQueryable<Repository> KeywordRepoResult(string keywordcode)
        {
            IQueryable<Repository> data = from r in _context.Repositories
                                          join k in _context.RepositoryKeywords on r.RepositoryId equals k.RepostioryId
                                          join rk in _context.RefKeywords on k.RefKeywordId equals rk.RefKeywordId
                                          where rk.KeywordCode == keywordcode
                                          orderby r.RepositoryId descending
                                          select r;

            data = data.Include(r => r.RepositoryDs)
                         .ThenInclude(a => a.Author)
                     .Include(s => s.RepoStatistics)
                     .OrderByDescending(o => o.RepositoryId)
                     .AsNoTracking();

            return data;
        }
    }
}
