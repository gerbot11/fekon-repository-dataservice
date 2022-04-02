using fekon_repository_api;
using fekon_repository_datamodel.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace fekon_repository_dataservice.Services
{
    public class CollectionService : BaseService, ICollectionService
    {
        public CollectionService(REPOSITORY_DEVContext context)
            : base(context)
        {
        }

        public async Task<IEnumerable<RefCollection>> GetRefCollectionsAsyncForAddRepo()
        {
            return await _context.RefCollections.ToListAsync();
        }

        public async Task<IEnumerable<CollectionD>> GetCollectionDsByRefCollIdAsync(long id)
        {
            return await _context.CollectionDs.Where(c => c.RefCollectionId.Equals(id)).ToListAsync();
        }

        public RefCollection FindRefCollectionByCode(string code)
        {
            return _context.RefCollections.Where(c => c.CollCode.Equals(code)).FirstOrDefault();
        }

        public CollectionD FindCollectionDById(long id)
        {
            return _context.CollectionDs.Find(id);
        }

        public async Task<IEnumerable<Community>> GetCommunitiesAsync()
        {
            return await _context.Communities.ToListAsync();
        }

        public async Task<IEnumerable<CollectionD>> GetCollectionDAsync()
        {
            return await _context.CollectionDs.ToListAsync();
        }

        public RefCollection FindRefCollectionByCollDId(long id)
        {
            return (from r in _context.RefCollections
                    join c in _context.CollectionDs on r.RefCollectionId equals c.RefCollectionId
                    where c.CollectionDid == id
                    select r).FirstOrDefault();
        }

        public async Task<IEnumerable<RefCollection>> GetRefCollectionsForSideMenu()
        {
            return await _context.RefCollections
                    .Include(c => c.CollectionDs)
                        .ThenInclude(r => r.Repositories)
                    .AsNoTracking()
                    .ToListAsync();
        }

        #region REF COLL ADMIN DASH
        public string AddNewRefCollection(RefCollection rc)
        {
            string res = string.Empty;
            bool collExist = _context.RefCollections.Where(r => r.CollCode == rc.CollCode).Any();

            if (collExist)
            {
                return "Type Code Already Exist, Please Input Another";
            }

            _context.Add(rc);
            _context.SaveChanges();
            return res;
        }

        public string EditRefCollection(RefCollection rc)
        {
            string res = string.Empty;
            try
            {
                long collExist = _context.RefCollections.Where(r => r.CollCode == rc.CollCode).Select(r => r.RefCollectionId).FirstOrDefault();
                if (collExist != rc.RefCollectionId && collExist is not 0)
                {
                    return "Type Code Already Exist, Please Input Another";
                }

                _context.Update(rc);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                res = ex.InnerException.Message is null ? ex.Message : ex.InnerException.Message;
            }
            return res;
        }

        public string DeleteRefCollection(long id)
        {
            string res = string.Empty;
            try
            {
                RefCollection rc = _context.RefCollections.Find(id);
                if (rc is not null)
                {
                    if (CheckIsRefCollUsed(id))
                    {
                        res = "Unable to Delete, This Collection is Currently Used";
                    }
                    else
                    {
                        _context.RefCollections.Remove(rc);
                        _context.SaveChanges();
                    }
                }
                else
                {
                    res = "Unable to Delete, This Collection is not Exist";
                }
            } 
            catch (Exception ex)
            {
                res = ex.InnerException.Message is null ? ex.Message : ex.InnerException.Message;
            }
            return res;
        }

        public IQueryable<RefCollection> GetRefCollectionsForPaging(string query)
        {
            IQueryable<RefCollection> result = _context.RefCollections;
            if (!string.IsNullOrWhiteSpace(query))
            {
                result.Where(c => c.CollName.Contains(query) || c.CollCode.Contains(query));
            }
            return result;
        }

        public RefCollection GetRefCollectionById(long id)
        {
            return _context.RefCollections.Find(id);
        }

        private bool CheckIsRefCollUsed(long id)
        {
            if (_context.Repositories.Where(r => r.RefCollectionId == id).Any())
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion

        #region SUB COLL ADMIN DASH
        public CollectionD GetSubCollectionById(long id)
        {
            return _context.CollectionDs.Find(id);
        }
        public IQueryable<CollectionD> GetSubCollForPaging(string query)
        {
            IQueryable<CollectionD> result = _context.CollectionDs;
            if (!string.IsNullOrWhiteSpace(query))
            {
                result.Where(c => c.CollectionDname.Contains(query));
            }
            return result.Include(r => r.RefCollection);
        }

        public string AddNewSubColl(CollectionD collectionD)
        {
            string res = string.Empty;
            try
            {
                if (CheckSubCollNameWithSameType(collectionD))
                {
                    res = "Unable To Add New Sub Collection With Same Name and Same Type";
                }
                else
                {
                    collectionD.CommunityId = _context.Communities.Select(c => c.CommunityId).FirstOrDefault();
                    _context.Add(collectionD);
                    _context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                res = ex.InnerException.Message is not null ? ex.InnerException.Message : ex.Message;
            }
            return res;
        }

        public string EditSubColl(CollectionD collectionD)
        {
            string res = string.Empty;
            try
            {
                if (CheckSubCollNameWithSameType(collectionD))
                {
                    res = "Unable To Add New Sub Collection With Same Name and Same Type";
                }
                else
                {
                    collectionD.CommunityId = _context.Communities.Select(c => c.CommunityId).FirstOrDefault();
                    _context.Update(collectionD);
                    _context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                res = ex.InnerException.Message is not null ? ex.InnerException.Message : ex.Message;
            }
            return res;
        }

        public string DeleteSubcoll(long id)
        {
            string res = string.Empty;
            try
            {
                CollectionD cd = _context.CollectionDs.Find(id);
                if(cd is not null)
                {
                    if (CheckSubCollInUsed(id))
                    {
                        res = "Unable to Delete, This Sub Collection in Used";
                    }
                    else
                    {
                        _context.Remove(cd);
                        _context.SaveChanges();
                    }
                }
                else
                {
                    res = "Unable to Delete, This Sub Collection is not Exist";
                }
            }
            catch (Exception ex)
            {
                res = ex.InnerException.Message is not null ? ex.InnerException.Message : ex.Message;
            }
            return res;
        }

        private bool CheckSubCollNameWithSameType(CollectionD collectionD)
        {
            bool cek = _context.CollectionDs.Where(c => c.RefCollectionId == collectionD.RefCollectionId && c.CollectionDname.Contains(collectionD.CollectionDname)).Any();
            if (cek)
                return true;
            else
                return false;
        }

        private bool CheckSubCollInUsed(long id)
        {
            if (_context.Repositories.Where(r => r.CollectionDid == id).Any())
                return true;
            else
                return false;
        }
        #endregion
    }
}
