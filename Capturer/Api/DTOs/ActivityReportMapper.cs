using Capturer.Models;

namespace Capturer.Api.DTOs;

/// <summary>
/// Mapper para convertir entre modelos internos y DTOs de API
/// </summary>
public static class ActivityReportMapper
{
    /// <summary>
    /// Convierte ActivityReport (modelo interno) a ActivityReportDto (API DTO)
    /// </summary>
    public static ActivityReportDto ToDto(ActivityReport report)
    {
        if (report == null) throw new ArgumentNullException(nameof(report));

        var dto = new ActivityReportDto
        {
            Id = Guid.TryParse(report.Id, out var guid) ? guid : Guid.NewGuid(),
            ComputerId = Environment.MachineName, // Hardware ID in production
            ComputerName = Environment.MachineName,
            ReportDate = report.ReportStartTime.Date,
            StartTime = report.ReportStartTime.TimeOfDay,
            EndTime = report.ReportEndTime.TimeOfDay,
            QuadrantActivities = report.Entries.Select(entry => new QuadrantActivityDto
            {
                QuadrantName = entry.QuadrantName,
                TotalComparisons = entry.TotalComparisons,
                ActivityDetectionCount = entry.ActivityDetectionCount,
                ActivityRate = entry.ActivityRate,
                AverageChangePercentage = entry.AverageChangePercentage,
                ActiveDuration = entry.ActiveDuration,
                Timeline = MapTimelineEntries(entry, report.ReportStartTime)
            }).ToList(),
            Metadata = new Dictionary<string, object>
            {
                ["SessionName"] = report.SessionName,
                ["QuadrantConfiguration"] = report.QuadrantConfigurationName,
                ["ReportType"] = report.ReportType,
                ["Comments"] = report.Comments,
                ["IsActive"] = report.IsActive,
                ["TotalQuadrants"] = report.Summary.TotalQuadrants,
                ["TotalComparisons"] = report.Summary.TotalComparisons,
                ["TotalActivities"] = report.Summary.TotalActivities,
                ["HighestActivityQuadrant"] = report.Summary.HighestActivityQuadrant
            },
            Version = "4.0.0"
        };

        return dto;
    }

    /// <summary>
    /// Convierte ActivityReportDto a ActivityReport para persistencia interna
    /// </summary>
    public static ActivityReport FromDto(ActivityReportDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        var report = new ActivityReport
        {
            Id = dto.Id.ToString(),
            CreatedAt = DateTime.Now,
            ReportStartTime = dto.ReportDate.Add(dto.StartTime),
            ReportEndTime = dto.ReportDate.Add(dto.EndTime),
            SessionName = dto.Metadata.GetValueOrDefault("SessionName", "").ToString() ?? "",
            QuadrantConfigurationName = dto.Metadata.GetValueOrDefault("QuadrantConfiguration", "").ToString() ?? "",
            ReportType = dto.Metadata.GetValueOrDefault("ReportType", "API").ToString() ?? "API",
            Comments = dto.Metadata.GetValueOrDefault("Comments", "").ToString() ?? "",
            IsActive = bool.TryParse(dto.Metadata.GetValueOrDefault("IsActive", false).ToString(), out var isActive) && isActive,
            Entries = dto.QuadrantActivities.Select(qa => new ActivityReportEntry
            {
                QuadrantName = qa.QuadrantName,
                TotalComparisons = (int)qa.TotalComparisons,
                ActivityDetectionCount = (int)qa.ActivityDetectionCount,
                ActivityRate = qa.ActivityRate,
                AverageChangePercentage = qa.AverageChangePercentage,
                ActiveDuration = qa.ActiveDuration
            }).ToList()
        };

        report.GenerateSummary();
        return report;
    }

    /// <summary>
    /// Mapea timeline entries para el DTO
    /// </summary>
    private static List<ActivityTimelineEntry> MapTimelineEntries(ActivityReportEntry entry, DateTime reportStart)
    {
        // Generate sample timeline based on activity data
        // In production, this would come from actual timeline data
        var timeline = new List<ActivityTimelineEntry>();
        
        var duration = entry.ActiveDuration;
        var intervals = Math.Min(60, (int)(duration.TotalMinutes / 5)); // Every 5 minutes, max 60 points
        
        for (int i = 0; i < intervals; i++)
        {
            var timestamp = reportStart.AddMinutes(i * 5);
            var activityLevel = GenerateActivityLevel(entry.ActivityRate, i, intervals);
            
            timeline.Add(new ActivityTimelineEntry
            {
                Timestamp = timestamp,
                ActivityLevel = activityLevel,
                QuadrantLevels = new Dictionary<string, double>
                {
                    [entry.QuadrantName] = activityLevel
                }
            });
        }

        return timeline;
    }

    /// <summary>
    /// Genera nivel de actividad simulado basado en datos reales
    /// </summary>
    private static double GenerateActivityLevel(double baseRate, int interval, int totalIntervals)
    {
        // Create realistic activity pattern
        var normalizedTime = (double)interval / totalIntervals;
        
        // Simulate activity patterns (more active in middle of session)
        var patternMultiplier = 0.5 + 0.5 * Math.Sin(normalizedTime * Math.PI);
        
        // Add some variation
        var random = new Random(interval * 42); // Deterministic for consistency
        var variation = 0.8 + 0.4 * random.NextDouble();
        
        return Math.Min(100.0, baseRate * patternMultiplier * variation);
    }

    /// <summary>
    /// Crea un ActivityReportDto mock con datos realistas para testing
    /// </summary>
    public static ActivityReportDto CreateMockReport(string computerName = null)
    {
        computerName = computerName ?? Environment.MachineName;
        
        return new ActivityReportDto
        {
            Id = Guid.NewGuid(),
            ComputerId = computerName,
            ComputerName = computerName,
            ReportDate = DateTime.Today,
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(17),
            QuadrantActivities = new List<QuadrantActivityDto>
            {
                new QuadrantActivityDto
                {
                    QuadrantName = "Trabajo",
                    TotalComparisons = 15420,
                    ActivityDetectionCount = 4326,
                    ActivityRate = 28.06,
                    AverageChangePercentage = 12.4,
                    ActiveDuration = TimeSpan.FromHours(6.2),
                    Timeline = CreateMockTimeline("Trabajo", TimeSpan.FromHours(9))
                },
                new QuadrantActivityDto
                {
                    QuadrantName = "Dashboard",
                    TotalComparisons = 8640,
                    ActivityDetectionCount = 1728,
                    ActivityRate = 20.0,
                    AverageChangePercentage = 8.7,
                    ActiveDuration = TimeSpan.FromHours(2.1),
                    Timeline = CreateMockTimeline("Dashboard", TimeSpan.FromHours(9))
                }
            },
            Metadata = new Dictionary<string, object>
            {
                ["SessionName"] = "Work Session - Main",
                ["QuadrantConfiguration"] = "Oficina Trabajo",
                ["ReportType"] = "Live",
                ["TotalQuadrants"] = 2,
                ["HighestActivityQuadrant"] = "Trabajo"
            },
            Version = "4.0.0"
        };
    }

    private static List<ActivityTimelineEntry> CreateMockTimeline(string quadrantName, TimeSpan startTime)
    {
        var timeline = new List<ActivityTimelineEntry>();
        var baseTime = DateTime.Today.Add(startTime);
        
        for (int i = 0; i < 48; i++) // Every 10 minutes for 8 hours
        {
            var timestamp = baseTime.AddMinutes(i * 10);
            var activity = 15 + 20 * Math.Sin(i * 0.3) + 5 * Math.Sin(i * 0.8); // Realistic pattern
            
            timeline.Add(new ActivityTimelineEntry
            {
                Timestamp = timestamp,
                ActivityLevel = Math.Max(0, activity),
                QuadrantLevels = new Dictionary<string, double>
                {
                    [quadrantName] = Math.Max(0, activity)
                }
            });
        }
        
        return timeline;
    }
}