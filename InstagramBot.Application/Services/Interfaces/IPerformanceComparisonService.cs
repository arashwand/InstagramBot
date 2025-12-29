namespace InstagramBot.Application.Services.Interfaces
{
    public interface IPerformanceComparisonService
    {
        Task<Dictionary<string, object>> ComparePeriodsAsync(int accountId, DateTime period1Start, DateTime period1End, DateTime period2Start, DateTime period2End);
        Task<Dictionary<string, object>> CompareBenchmarkAsync(int accountId, string industry);
        Task<Dictionary<string, object>> GetPerformanceScoreAsync(int accountId, DateTime fromDate, DateTime toDate);
        Task<List<Dictionary<string, object>>> GetCompetitorComparisonAsync(int accountId, List<string> competitorUsernames);
    }
}