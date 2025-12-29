using InstagramBot.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramBot.Application.Services
{
    public interface IReportingService
    {
        Task<AnalyticsReportDto> GenerateAccountReportAsync(int accountId, DateTime fromDate, DateTime toDate);
        Task<List<PostAnalyticsDto>> GetTopPerformingPostsAsync(int accountId, DateTime fromDate, DateTime toDate, int count = 10);
        Task<Dictionary<string, object>> GetEngagementTrendsAsync(int accountId, DateTime fromDate, DateTime toDate);
        Task<Dictionary<string, object>> GetAudienceInsightsAsync(int accountId, DateTime fromDate, DateTime toDate);
        Task<Dictionary<string, object>> GetBestPostingTimesAsync(int accountId);
        Task<Dictionary<string, object>> GetHashtagPerformanceAsync(int accountId, DateTime fromDate, DateTime toDate);
        Task<Dictionary<string, object>> GetCompetitorAnalysisAsync(int accountId, List<string> competitorUsernames);
        Task<byte[]> ExportReportToPdfAsync(AnalyticsReportDto report);
        Task<byte[]> ExportReportToExcelAsync(AnalyticsReportDto report);
    }
}
