using Skylab.Shared.Application.Contracts;
using Skylab.Shared.Domain.Enums;
using Skylab.Forms.Domain.Enums;
using Skylab.Forms.Application.Contracts.Metrics;
using Skylab.Forms.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace Skylab.Forms.Application.Services;

public class FormMetricService : IFormMetricService
{
    private readonly FormsDbContext _context;

    public FormMetricService(FormsDbContext context)
    {
        _context = context;
    }

    public async Task<ServiceResult<FormMetricsContract>> GetFormMetricsAsync(Guid formId, Guid userId, CancellationToken cancellationToken = default)
    {
        var formExists = await _context.Forms.AnyAsync(f => f.Id == formId, cancellationToken);
        if (!formExists)
            return new ServiceResult<FormMetricsContract>(ServiceStatus.NotFound, Message: "Form bulunamadı.");

        var isAuthorized = await _context.Collaborators.AnyAsync(c => c.FormId == formId && c.UserId == userId && (c.Role != CollaboratorRole.None), cancellationToken);
        if (!isAuthorized)
            return new ServiceResult<FormMetricsContract>(ServiceStatus.NotAuthorized, Message: "Bu formun yanıtlarını görüntüleme yetkiniz yok.");

        var query = _context.Responses.AsNoTracking().Where(r => r.FormId == formId);

        var basicStats = await query.GroupBy(r => 1).Select(g => new
        {
            Total = g.Count(),
            Pending = g.Count(r => r.Status == FormResponseStatus.Pending),
            Approved = g.Count(r => r.Status == FormResponseStatus.Approved),
            Rejected = g.Count(r => r.Status == FormResponseStatus.Declined),
            AvgTime = g.Average(r => r.TimeSpent),
            Registered = g.Count(r => r.UserId != null),
            Anonymous = g.Count(r => r.UserId == null)
        }).FirstOrDefaultAsync(cancellationToken);

        var emptyDailyTrend = Enumerable.Range(0, 7).Select(offset =>
        {
            var targetDate = DateTime.UtcNow.AddDays(-6 + offset).Date;
            return new TrendItemContract($"d-{offset}", targetDate.ToString("ddd"), 0);
        }).ToList();

        if (basicStats == null)
        {
            var emptyMetrics = new FormMetricsContract(
                TotalResponses: 0,
                PendingCount: 0,
                ApprovedCount: 0,
                RejectedCount: 0,
                AverageCompletionTime: null,
                DailyTrendPercentage: 0,
                HourlyTrendPercentage: 0,
                SourceBreakdown: new SourceBreakdownContract(0, 0),
                DailyTrend: emptyDailyTrend,
                HourlyTrend: new List<TrendItemContract>()
            );
            return new ServiceResult<FormMetricsContract>(ServiceStatus.Success, Data: emptyMetrics);
        }

        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7).Date;
        var dailyData = await query.Where(r => r.SubmittedAt >= sevenDaysAgo)
            .GroupBy(r => r.SubmittedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var dailyTrend = Enumerable.Range(0, 7).Select(offset =>
        {
            var targetDate = DateTime.UtcNow.AddDays(-6 + offset).Date;
            var data = dailyData.FirstOrDefault(d => d.Date == targetDate);
            return new TrendItemContract($"d-{offset}", targetDate.ToString("ddd"), data?.Count ?? 0);
        }).ToList();

        var hourlyData = await query.GroupBy(r => r.SubmittedAt.Hour).Select(g => new { Hour = g.Key, Count = g.Count() }).ToListAsync(cancellationToken);
        var currentHour = DateTime.UtcNow.Hour;
        var hourlyTrend = Enumerable.Range(0, 24).Select(offset => {
            var targetHour = (currentHour + 1 + offset) % 24;
            var data = hourlyData.FirstOrDefault(d => d.Hour == targetHour);
            return new TrendItemContract($"h-{targetHour}", targetHour.ToString("00"), data?.Count ?? 0);
        }).ToList();

        var result = new FormMetricsContract(
            basicStats.Total,
            basicStats.Pending,
            basicStats.Approved,
            basicStats.Rejected,
            basicStats.AvgTime,
            CalculateTrendPercentageChange(dailyTrend),
            CalculateTrendPercentageChange(hourlyTrend),
            new SourceBreakdownContract(basicStats.Registered, basicStats.Anonymous),
            dailyTrend,
            hourlyTrend
        );

        return new ServiceResult<FormMetricsContract>(ServiceStatus.Success, Data: result);
    }

    public async Task<ServiceResult<ServiceMetricsContract>> GetServiceMetricsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var totalForms = await _context.Forms.CountAsync(cancellationToken);
        var totalResponses = await _context.Responses.CountAsync(cancellationToken);
        var pendingResponses = await _context.Responses.CountAsync(r => r.Status == FormResponseStatus.Pending && !r.IsArchived, cancellationToken);

        var today = DateTime.UtcNow.Date;
        var diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
        var currentWeekStart = today.AddDays(-1 * diff).Date;

        var weeksToFetch = 8;
        var startDate = currentWeekStart.AddDays(-(weeksToFetch - 1) * 7);

        var formDates = await _context.Forms.AsNoTracking().Where(f => f.CreatedAt >= startDate).Select(f => f.CreatedAt).ToListAsync(cancellationToken);
        var responseDates = await _context.Responses.AsNoTracking().Where(r => r.SubmittedAt >= startDate).Select(r => r.SubmittedAt).ToListAsync(cancellationToken);

        var formsWeeklyTrend = new List<TrendItemContract>();
        var responsesWeeklyTrend = new List<TrendItemContract>();

        for (int i = weeksToFetch - 1; i >= 0; i--)
        {
            var weekStart = currentWeekStart.AddDays(-i * 7);
            var weekEnd = weekStart.AddDays(7);

            var weekLabel = $"{weekStart:dd MMM}";

            var formCount = formDates.Count(d => d >= weekStart && d < weekEnd);
            var responseCount = responseDates.Count(d => d >= weekStart && d < weekEnd);

            formsWeeklyTrend.Add(new TrendItemContract($"fw-{i}", weekLabel, formCount));
            responsesWeeklyTrend.Add(new TrendItemContract($"rw-{i}", weekLabel, responseCount));
        }

        var result = new ServiceMetricsContract(totalForms, totalResponses, pendingResponses, CalculateTrendPercentageChange(formsWeeklyTrend), CalculateTrendPercentageChange(responsesWeeklyTrend), formsWeeklyTrend, responsesWeeklyTrend);

        return new ServiceResult<ServiceMetricsContract>(ServiceStatus.Success, Data: result);
    }

    private double CalculateTrendPercentageChange(List<TrendItemContract> trend)
    {
        if (trend == null || trend.Count < 2) return 0;

        var previousItems = trend.Take(trend.Count - 1).Select(t => t.Count).ToList();
        var previousAverage = previousItems.Average();

        var currentCount = trend.Last().Count;

        if (previousAverage == 0)
            return currentCount > 0 ? 100.0 : 0.0;

        var percentageChange = ((currentCount - previousAverage) / previousAverage) * 100;

        return Math.Round(percentageChange, 2);
    }
}