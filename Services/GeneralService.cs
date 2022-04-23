using fekon_repository_api;
using fekon_repository_datamodel.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace fekon_repository_dataservice.Services
{
    public class GeneralService : IGeneralService
    {
        private readonly REPOSITORY_DEVContext _context;
        public GeneralService(REPOSITORY_DEVContext context)
        {
            _context = context;
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
    }
}
