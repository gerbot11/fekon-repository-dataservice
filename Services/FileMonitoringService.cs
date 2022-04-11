using fekon_repository_api;
using fekon_repository_datamodel.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fekon_repository_dataservice.Services
{
    public class FileMonitoringService : BaseService, IFileMonitoringService
    {
        public FileMonitoringService(REPOSITORY_DEVContext context)
            : base(context)
        {
        }

        public IQueryable<FileMonitoringHist> GetMonitoringHistsForPaging(DateTime? dtfrom, DateTime? dtto)
        {
            IQueryable<FileMonitoringHist> fileMonitorings = _context.FileMonitoringHists;
            if(dtfrom is not null && dtto is null)
                fileMonitorings = fileMonitorings.Where(f => f.RunningDate >= dtfrom);

            if (dtfrom is not null && dtto is not null)
                fileMonitorings = fileMonitorings.Where(f => f.RunningDate >= dtfrom && f.RunningDate <= dtto);

            return fileMonitorings;
        }

        public IQueryable<FileMonitoringResult> GetFileMonitoringResults(long fileMonitoringId)
        {
            IQueryable<FileMonitoringResult> results = _context.FileMonitoringResults.Where(f => f.FileMonitoringHistId == fileMonitoringId);
            return results
                .Include(f => f.FileDetail)
                .Include(m => m.FileMonitoringHist)
                .AsNoTracking();
        }

        public IEnumerable<FileDetail> GetFileDetailsForMonitoring()
        {
            return _context.FileDetails;
        }

        public FileMonitoringHist GetFileMonitoringHistObjById(long id)
        {
            return _context.FileMonitoringHists.Find(id);
        }

        public bool ValidateLastRun(DateTime dt, int nextRunMax)
        {
            FileMonitoringHist fmh = _context.FileMonitoringHists.OrderByDescending(f => f.RunningDate).FirstOrDefault();
            if (fmh is null)
                return false;

            TimeSpan ts = dt - fmh.RunningDate;
            double timeSpan = ts.TotalMinutes;

            if (timeSpan >= nextRunMax)
                return false;
            else
                return true;
        }

        public FileMonitoringHist CreateFileMonitoringH(int totalFile, int totalsize)
        {
            FileMonitoringHist hist = new()
            {
                RunningDate = DateTime.Now,
                TotalFile = totalFile,
                TotalSize = totalsize
            };

            _context.Add(hist);
            _context.SaveChanges();
            return hist;
        }

        public async Task UpdateFileMonitoringHist(FileMonitoringHist monitoringHist)
        {
            _context.Update(monitoringHist);
            await _context.SaveChangesAsync();
        }

        public List<FileMonitoringResult> GetMonitoringResultByFileDetailId(long fileDetailId)
        {
            return _context.FileMonitoringResults.Where(f => f.FileDetailId == fileDetailId).ToList();
        }

        public FileMonitoringResult RunMonitoring(FileDetail file, FileMonitoringHist mh)
        {
            FileMonitoringResult result = new();
            try
            {
                result = FileCheckPerFile(file);
            }
            catch (Exception ex)
            {
                _ = ex.Message;
            }

            return result;
        }

        private static List<FileMonitoringResult> FileChecking(IEnumerable<FileDetail> listFile)
        {
            List<FileMonitoringResult> listResult = new();
            foreach (FileDetail file in listFile)
            {
                if (!Directory.Exists(file.FilePath))
                {
                    FileMonitoringResult fileRes = new()
                    {
                        FileDetailId = file.FileDetailId,
                        StatusFile = $"Directory ({file.FilePath}) Not Exists"
                    };
                    listResult.Add(fileRes);
                }

                string[] files = Directory.GetFiles(file.FilePath);
                foreach (string fileitem in files)
                {
                    if (!File.Exists(fileitem))
                    {
                        FileMonitoringResult fileRes = new()
                        {
                            FileDetailId = file.FileDetailId,
                            StatusFile = $"{fileitem} Not Exists | Directory Location : {file.FilePath}"
                        };
                        listResult.Add(fileRes);
                    }
                }
            }

            return listResult;
        }

        private static FileMonitoringResult FileCheckPerFile(FileDetail file)
        {
            FileMonitoringResult result = new();
            long id = file.FileDetailId;
            if (!Directory.Exists(file.FilePath))
            {
                result.FileDetailId = id;
                result.StatusFile = $"Directory ({file.FilePath}) Not Exists";
            }

            string[] files = Directory.GetFiles(file.FilePath);
            if (files.Length > 0)
            {
                foreach (string fileitem in files)
                {
                    if (!File.Exists(fileitem))
                    {
                        result.FileDetailId = id;
                        result.StatusFile = $"{fileitem} Not Exists | Directory Location : {file.FilePath}";
                    }
                }
            }
            else
            {
                result.FileDetailId = id;
                result.StatusFile = $"{file.FileName} Not Exists | Directory Location : {file.FilePath}";
            }
            
            return result.FileDetailId is null ? null : result;
        }
    }
}
