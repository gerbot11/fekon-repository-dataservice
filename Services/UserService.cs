using fekon_repository_api;
using fekon_repository_datamodel.MergeModels;
using fekon_repository_datamodel.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fekon_repository_dataservice.Services
{
    public class UserService : BaseService, IUserService
    {
        private const string VISITOR = "VISITOR";
        private const string ADMIN = "ADMIN";
        public UserService(REPOSITORY_DEVContext context)
            : base(context)
        {
        }

        #region USERS
        public IQueryable<AspNetUser> GetUsersForPaging(string query)
        {
            IQueryable<AspNetUser> result = from u in _context.AspNetUsers
                                            join r in _context.AspNetUserRoles on u.Id equals r.UserId
                                            join rl in _context.AspNetRoles on r.RoleId equals rl.Id
                                            where rl.NormalizedName == VISITOR
                                            select u;

            if (!string.IsNullOrWhiteSpace(query))
            {
                result = result.Where(u => u.Email.Contains(query) || u.UserName.Contains(query) || u.PhoneNumber.Contains(query));
            }
            return result;
        }
        #endregion

        #region ADIMN
        public IQueryable<AspNetUser> GetAdminForPaging(string query)
        {
            IQueryable<AspNetUser> result = from u in _context.AspNetUsers
                                            join r in _context.AspNetUserRoles on u.Id equals r.UserId
                                            join rl in _context.AspNetRoles on r.RoleId equals rl.Id
                                            where rl.NormalizedName == ADMIN
                                            select u;

            if (!string.IsNullOrWhiteSpace(query))
            {
                result = result.Where(u => u.Email.Contains(query) || u.UserName.Contains(query) || u.PhoneNumber.Contains(query));
            }
            return result;
        }

        public MergeAdminInfo GetAdminInfoByIdAsync(string id, int pagenumber, ref bool canloadmore)
        {
            AspNetUser user = _context.AspNetUsers.Find(id);
            if (user is null)
            {
                return null;
            }
            RefEmployee refEmployee = _context.RefEmployees.Where(r => r.UserId == user.Id).FirstOrDefault();

            int defItemToShow = 5;
            int takeitem = pagenumber * defItemToShow;
            if (takeitem == 0)
            {
                takeitem = defItemToShow;
            }

            IEnumerable<DateTime> dateAct = _context.UserActivityHists
                .Where(u => u.UserId == user.Id).Select(u => u.ActivityTime.Date).Distinct().Take(takeitem).OrderByDescending(o => o.Date);
            dateAct = dateAct.OrderByDescending(o => o.Date);

            int dataSize = _context.UserActivityHists.Where(u => u.UserId == id).Select(u => u.ActivityTime.Date).Distinct().Count();
            int totalPage = (int)Math.Ceiling(dataSize / (double)defItemToShow);
            canloadmore = pagenumber < totalPage;

            int usrRepoSubmisionCnt = _context.Repositories.Where(r => r.UsrCreate == id).Count();

            List<UserActivityHist> actHist = new();
            List<ActivityDetail> listdetail = new();
            foreach (DateTime item in dateAct)
            {
                List<UserActivityHist> uah = _context.UserActivityHists.Where(a => a.UserId == id && a.ActivityTime.Date == item).OrderByDescending(o => o.ActivityTime).ToList();
                ActivityDetail ad = new()
                {
                    DateActivity = item,
                    UserActivityHists = uah
                };
                listdetail.Add(ad);
            }

            MergeAdminInfo mergeAdminInfo = new()
            {
                AspNetUser = user,
                UserActivityDetail = listdetail,
                RefEmployee = refEmployee,
                TotalRepositorySubmit = usrRepoSubmisionCnt
            };

            return mergeAdminInfo;
        }

        public RefEmployee GetRefEmployeeObjByUserId(string userid)
        {
            return _context.RefEmployees.Where(r => r.UserId == userid).FirstOrDefault();
        }

        public async Task EditRefEmpAsync(RefEmployee re, string fileLoc, string username, IFormFile file)
        {
            if (file is not null)
            {
                string filePath = Path.Combine(fileLoc, file.FileName);
                if (filePath != re.ProfilePicLoc)
                {
                    string location = await UploadUsrPic(fileLoc, username, file);
                    re.ProfilePicLoc = location;
                }
            }

            _context.Update(re);
            await _context.SaveChangesAsync();
        }
        #endregion

        public async Task AddUserActHist(string userid, string actionDesc, string action)
        {
            UserActivityHist userActivity = new()
            {
                UserId = userid,
                ActivityDesc = actionDesc,
                ActivityTime = DateTime.Now,
                ActivityAction = action
            };

            await _context.AddAsync(userActivity);
            await _context.SaveChangesAsync();
        }

        private static async Task<string> UploadUsrPic(string fileLoc, string username, IFormFile file)
        {
            string dir = Path.Combine(fileLoc, username);

            if (!Directory.Exists(dir))
            {
                DirectoryInfo di = Directory.CreateDirectory(dir);
                di.Create();
            }
            else
            {
                string[] files = Directory.GetFiles(dir);
                if (files.Length > 0)
                {
                    foreach (var item in files)
                    {
                        File.Delete(item);
                    }
                }
            }

            string fullPath = Path.Combine(dir, file.FileName);

            using FileStream stream = new(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return Path.Combine("\\user_img", username, file.FileName);
        }
    }
}
