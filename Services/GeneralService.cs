using fekon_repository_api;
using fekon_repository_datamodel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public IEnumerable<RefCategorySearch> GetCategorySearch()
        {
            return _context.RefCategorySearches;
        }
    }
}
