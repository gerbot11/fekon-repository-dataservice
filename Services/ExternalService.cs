using fekon_repository_datamodel.Models;
using fekon_repository_api;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using fekon_repository_datamodel.MobileModel;

namespace fekon_repository_dataservice.Services
{
    public class ExternalService : IExternalService
    {
        private readonly REPOSITORY_DEVContext _context;
        public ExternalService(REPOSITORY_DEVContext context)
        {
            _context = context;
        }

        public List<MobileIndex> GetRepositoryIndex()
        {
            IEnumerable<Repository> repo = _context.Repositories
                .Include(r => r.RepositoryDs).ThenInclude(a => a.Author)
                .Include(s => s.RepoStatistics)
                .Take(20).AsNoTracking();

            List<MobileIndex> index = new();
            foreach (Repository item in repo)
            {
                MobileIndex newobj = new()
                {
                    RepositoryId = item.RepositoryId,
                    AuthorName = item.RepositoryDs.Select(r => $"{r.Author.LastName}, {r.Author.FirstName}"),
                    Stat = item.RepoStatistics.Select(x => (int)x.LinkHitCount),
                    Title = item.Title
                };
                index.Add(newobj);
            }

            return index;
        }
    }
}
