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
    public class LangService : BaseService, ILangService
    {
        public LangService(REPOSITORY_DEVContext context)
            : base(context)
        {
        }

        public async Task<IEnumerable<RefLanguage>> GetRefLanguagesAsyncForAddRepos()
        {
            return await _context.RefLanguages.ToListAsync();
        } 
    }
}
