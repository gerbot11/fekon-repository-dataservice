using fekon_repository_api;
using fekon_repository_datamodel.MergeModels;
using fekon_repository_datamodel.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace fekon_repository_dataservice.Services
{
    public class AuthorService : BaseService, IAuthorService
    {
        public AuthorService(REPOSITORY_DEVContext context) 
            : base(context)
        {
        }

        public IQueryable<Author> GetAuthorsForIndexDash(string query, string isadv)
        {
            IQueryable<Author> resAuthor = _context.Authors;

            if(!string.IsNullOrEmpty(query) || !string.IsNullOrWhiteSpace(query))
                resAuthor = resAuthor.Where(a => a.FirstName.Contains(query) || a.LastName.Contains(query) || a.AuthorNo.Contains(query));

            if (!string.IsNullOrEmpty(isadv))
                resAuthor = resAuthor.Where(a => a.IsAdvisor == isadv);

            return resAuthor.OrderBy(a => a.FirstName);
        }

        public IQueryable<Author> GetAuthorForSelectionByName(string name)
        {
            IQueryable<Author> authors = _context.Authors.Where(a => a.IsAdvisor != "1");
            if (string.IsNullOrEmpty(name))
            {
                authors = authors.Take(10);
            }
            else
            {
                authors = authors.Where(a => a.FirstName.Contains(name) || a.LastName.Contains(name));
            }
            return authors;
        }

        public IQueryable<Author> GetAdvisiorForSelectionByName(string name)
        {
            IQueryable<Author> authors = _context.Authors.Where(a => a.IsAdvisor == "1");
            if (string.IsNullOrEmpty(name))
            {
                authors = authors.Take(10);
            }
            else
            {
                authors = authors.Where(a => a.FirstName.Contains(name) || a.LastName.Contains(name));
            }
            return authors;
        }

        public async Task<IEnumerable<Author>> GetListAuthorForAddRepos()
        {
            return await _context.Authors.Where(a => a.IsAdvisor != "1").ToListAsync();
        }

        public async Task<IEnumerable<Author>> GetAuthorsAdvisorAsync()
        {
            return await _context.Authors.Where(a => a.IsAdvisor == "1").ToListAsync();
        }

        public async Task<Author> GetAuthorByAuthorIdAsync(long id)
        {
            return await _context.Authors.FindAsync(id);
        }

        public async Task<IEnumerable<MergeAuthorGrouping>> GetListAuthorForSideMenu(string isAdvisior)
        {
            IQueryable<Author> dataauthor = from a in _context.Authors
                                            join rd in _context.RepositoryDs on a.AuthorId equals rd.AuthorId
                                            join r in _context.Repositories on rd.RepositoryId equals r.RepositoryId
                                            where a.IsAdvisor == isAdvisior
                                            orderby r.RepositoryId descending
                                            select a;

            IEnumerable<MergeAuthorGrouping> authorRepo = await (from a in dataauthor
                                                                 group a by new { a.AuthorId, a.FirstName, a.LastName } into grpRes
                                                                 orderby grpRes.Key.AuthorId descending
                                                                 select new MergeAuthorGrouping
                                                                 {
                                                                     Id = grpRes.Key.AuthorId,
                                                                     Name = string.IsNullOrEmpty(grpRes.Key.LastName) ? $"{ grpRes.Key.FirstName }" : $"{grpRes.Key.LastName}, {grpRes.Key.FirstName}",
                                                                     RepoCount = grpRes.Count()
                                                                 }).Take(10).ToListAsync();
            return authorRepo;
        }

        public async Task<IEnumerable<MergeAuthorGrouping>> GetListAdvisiorForSideMenu()
        {
            IEnumerable<MergeAuthorGrouping> authorRepo = await (from a in _context.Authors
                                                                 join rd in _context.RepositoryDs on a.AuthorId equals rd.AuthorId
                                                                 join r in _context.Repositories on rd.RepositoryId equals r.RepositoryId
                                                                 where a.IsAdvisor == "1"
                                                                 orderby r.UploadDate descending
                                                                 group a by new { a.AuthorId, a.FirstName, a.LastName } into grpRes
                                                                 select new MergeAuthorGrouping
                                                                 {
                                                                     Id = grpRes.Key.AuthorId,
                                                                     Name = string.IsNullOrEmpty(grpRes.Key.LastName) ? $"{ grpRes.Key.FirstName }" : $"{grpRes.Key.LastName}, {grpRes.Key.FirstName}",
                                                                     RepoCount = grpRes.Count()
                                                                 }).Take(10).ToListAsync();
            return authorRepo;
        }

        public IEnumerable<Author> GetListAuthorByReposId(long repoid)
        {
            return from a in _context.Authors
                   join rd in _context.RepositoryDs on a.AuthorId equals rd.AuthorId
                   where rd.RepositoryId == repoid
                   select a;
        }

        #region ADMIN DASH
        public List<string> GetAuthorNameByRepositoryId(long repoid)
        {
            var authorName = from a in _context.Authors
                             join r in _context.RepositoryDs on a.AuthorId equals r.AuthorId
                             where r.RepositoryId == repoid
                             select new
                             {
                                 AuthorName = $"{a.FirstName} {a.LastName}"
                             };

            List<string> data = new();
            foreach (var item in authorName)
            {
                data.Add(item.AuthorName);
            }

            return data;
        }

        public async Task<string> AddNewAuthorAsync(Author author)
        {
            string res = string.Empty;
            try
            {
                if (!string.IsNullOrWhiteSpace(author.AuthorNo))
                {
                    if (!CheckDuplicateAuthorNo(author.AuthorNo))
                    {
                        _context.Add(author);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        res = "Duplicate Author No, Please Input Another";
                    }
                }
                else
                {
                    _context.Add(author);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                res = ex.InnerException.Message is not null ? ex.InnerException.Message : ex.Message;
            }
            return res;
        }

        public async Task<string> EditAuthorAsync(Author author)
        {
            string res = string.Empty;
            try
            {
                if (!string.IsNullOrWhiteSpace(author.AuthorNo))
                {
                    string authorNoExist = _context.Authors.Where(a => a.AuthorId == author.AuthorId).Select(a => a.AuthorNo).FirstOrDefault();
                    if (author.AuthorNo != authorNoExist && CheckDuplicateAuthorNo(author.AuthorNo))
                    {
                        res = "Duplicate Author No, Please Input Another";
                    }
                    else
                    {
                        _context.Update(author);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    _context.Update(author);
                    await _context.SaveChangesAsync();
                }
            } 
            catch (Exception ex)
            {
                res = ex.InnerException.Message is not null ? ex.InnerException.Message : ex.Message;
            }

            return res;
        }

        public async Task<string> DeleteAuthorAsync(long id)
        {
            string res = string.Empty;
            Author author = _context.Authors.Find(id);

            if (CheckInUsedAuthor(id))
            {
                return "Unable to delete this Author, there a Repository with this Author";
            }

            _context.Remove(author);
            await _context.SaveChangesAsync();

            return res;
        }

        public int GetTotalAuthor()
        {
            return _context.Authors.Count();
        }

        private bool CheckInUsedAuthor(long authorid)
        {
            bool rdCount = _context.RepositoryDs.Where(r => r.AuthorId == authorid).Any();
            if (rdCount)
                return true;
            else
                return false;
        }

        private bool CheckDuplicateAuthorNo(string authorNo)
        {
            if (_context.Authors.Where(a => a.AuthorNo == authorNo).Any())
                return true;
            else
                return false;
        }
        #endregion
    }
}
