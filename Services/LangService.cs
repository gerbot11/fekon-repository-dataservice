using fekon_repository_api;
using fekon_repository_datamodel.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
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

        public string MergeRepositoryLang(List<string> langCode)
        {
            string lang = string.Empty;
            for (int i = 0; i < langCode.Count; i++)
            {
                lang = $"{lang}{langCode[i]};";
            }
            lang = lang.Remove(lang.Length - 1, 1);

            return lang;
        }
    }
}
