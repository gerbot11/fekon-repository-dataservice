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
    public class PublisherService : BaseService, IPublisherService
    {
        public PublisherService(REPOSITORY_DEVContext context)
            : base(context)
        {
        }

        public async Task<IEnumerable<Publisher>> GetListPublishersAsync()
        {
            return await _context.Publishers.ToListAsync();
        }
    }
}
