using InstagramBot.Core.Entities;
using InstagramBot.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramBot.Application.Services
{
    public interface IReportingService
    {
        Task<AnalyticsReportDto> GenerateAccountReportAsync(int userId, int accountId, DateTime fromDate, DateTime toDate);
        Task<List<PostAnalytics>> GetTopPerformingPostsAsync(int userId, int accountId, DateTime fromDate, DateTime toDate, int count = 10);
        Task<Dictionary<string, object>> GetEngagementTrendsAsync(int userId, int accountId, DateTime fromDate, DateTime toDate);
        Task<Dictionary<string, object>> GetAudienceInsightsAsync(int userId, int accountId, DateTime fromDate, DateTime toDate);
        Task<Dictionary<string, object>> GetBestPostingTimesAsync(int userId, int accountId);
        Task<Dictionary<string, object>> GetHashtagPerformanceAsync(int userId, int accountId, DateTime fromDate, DateTime toDate);
        Task<Dictionary<string, object>> GetCompetitorAnalysisAsync(int userId, int accountId, List<string> competitorUsernames);
        Task<byte[]> ExportReportToPdfAsync(AnalyticsReportDto report);
        Task<byte[]> ExportReportToExcelAsync(AnalyticsReportDto report);
    }
}
