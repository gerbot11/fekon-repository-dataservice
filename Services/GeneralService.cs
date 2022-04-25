using fekon_repository_api;
using fekon_repository_datamodel.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace fekon_repository_dataservice.Services
{
    public class GeneralService : BaseService, IGeneralService
    {
        
        public GeneralService(REPOSITORY_DEVContext context) : base(context)
        {
        }

        public async Task<IEnumerable<RefRepositoryFileType>> GetRefRepositoryFileTypes()
        {
            return await _context.RefRepositoryFileTypes.ToListAsync();
        }

        public RefRepositoryFileType GetRefRepositoryFileTypeByCode(string code)
        {
            return _context.RefRepositoryFileTypes.Where(t => t.RepositoryFileTypeCode == code).FirstOrDefault();
        }

        public IQueryable<RefRepositoryFileType> GetRefRepositoryFileTypesPaging()
        {
            return _context.RefRepositoryFileTypes;
        }

        public async Task CreateNewRefRepositoryFileType(RefRepositoryFileType refRepositoryFileType)
        {
            await _context.RefRepositoryFileTypes.AddAsync(refRepositoryFileType);
            await _context.SaveChangesAsync();
        }

        public async Task EditRefRepositoryFileType(RefRepositoryFileType refRepositoryFileType)
        {
            _context.RefRepositoryFileTypes.Update(refRepositoryFileType);
            await _context.SaveChangesAsync();
        }

        public bool CheckDuplicateCode(string code, long? id)
        {
            bool isdup = false;
            if (id is not null)
            {
                long existId = _context.RefRepositoryFileTypes.Where(r => r.RepositoryFileTypeCode == code).Select(r => r.RefRepositoryFileTypeId).FirstOrDefault();
                if (existId != id)
                {
                    isdup = _context.RefRepositoryFileTypes.Where(r => r.RepositoryFileTypeCode == code).Any();
                }
            }
            else
            {
                isdup = _context.RefRepositoryFileTypes.Where(r => r.RepositoryFileTypeCode == code).Any();
            }
            
            return isdup;
        }

        #region KEYWORDS
        public IQueryable<RefKeyword> GetRefKeywords(string q)
        {
            IQueryable<RefKeyword> result = _context.RefKeywords;
            if (string.IsNullOrEmpty(q))
            {
                result = result.Take(20);
            }
            else
            {
                result = result.Where(r => r.KeywordName.Contains(q));
            }

            return result;
        }

        public RefKeyword GetRefKeywordObjById(long id)
        {
            return _context.RefKeywords.Find(id);
        }
        public RefKeyword GetRefKeywordObjByCode(string keycode)
        {
            keycode = MakeKeywordCode(keycode);
            return _context.RefKeywords.Where(k => k.KeywordCode == keycode).FirstOrDefault();
        }

        public async Task<IEnumerable<RefKeyword>> GetRepositoryKeywordByRepoId(long repoid)
        {
            IEnumerable<RefKeyword> repoKeywords = await (from r in _context.RepositoryKeywords
                                                          join k in _context.RefKeywords on r.RefKeywordId equals k.RefKeywordId
                                                          where r.RepostioryId == repoid
                                                          select k).ToListAsync();
            return repoKeywords;
        }

        public RefKeyword CreateNewKeyword(string keyword)
        {
            RefKeyword refKeyword = new()
            {
                KeywordName = keyword,
                KeywordCode = MakeKeywordCode(keyword)
            };

            return refKeyword;
        }

        private static string MakeKeywordCode(string keyname)
        {
            string keycode = keyname.Replace(' ', '-');
            keycode = keycode.ToLower();
            return keycode;
        }
        #endregion
    }
}
