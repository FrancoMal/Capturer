using Capturer.Models;

namespace Capturer.Services;

public interface IReportPeriodService
{
    Task<List<string>> GetFilteredScreenshotsAsync(ReportPeriod period, string screenshotFolder);
    ReportPeriod GetCurrentReportPeriod(ScheduleSettings scheduleSettings);
    DateTime GetNextReportTime(ScheduleSettings scheduleSettings);
}

public class ReportPeriodService : IReportPeriodService
{
    public Task<List<string>> GetFilteredScreenshotsAsync(ReportPeriod period, string screenshotFolder)
    {
        return Task.Run(() =>
        {
            if (!Directory.Exists(screenshotFolder))
                return new List<string>();

            var allFiles = Directory.GetFiles(screenshotFolder, "*.png")
                .Concat(Directory.GetFiles(screenshotFolder, "*.jpg"))
                .Concat(Directory.GetFiles(screenshotFolder, "*.jpeg"))
                .ToList();

            var filteredFiles = new List<string>();

            foreach (var filePath in allFiles)
            {
                var fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists) continue;

                var fileDate = fileInfo.CreationTime;

                // Verificar que esté en el rango de fechas
                if (fileDate < period.StartDate || fileDate > period.EndDate)
                    continue;

                // Verificar día de la semana si está configurado
                if (period.ActiveWeekDays.Any() && !period.ActiveWeekDays.Contains(fileDate.DayOfWeek))
                    continue;

                // Verificar filtros de horario si están configurados
                if (period.StartTime.HasValue && period.EndTime.HasValue)
                {
                    var fileTime = fileDate.TimeOfDay;
                    
                    // Manejar casos donde EndTime < StartTime (ej: 23:00 - 08:00)
                    if (period.EndTime.Value < period.StartTime.Value)
                    {
                        // Horario nocturno que cruza medianoche
                        if (!(fileTime >= period.StartTime.Value || fileTime <= period.EndTime.Value))
                            continue;
                    }
                    else
                    {
                        // Horario normal dentro del mismo día
                        if (fileTime < period.StartTime.Value || fileTime > period.EndTime.Value)
                            continue;
                    }
                }

                filteredFiles.Add(filePath);
            }

            return filteredFiles.OrderBy(f => new FileInfo(f).CreationTime).ToList();
        });
    }

    public ReportPeriod GetCurrentReportPeriod(ScheduleSettings scheduleSettings)
    {
        return scheduleSettings.Frequency switch
        {
            ReportFrequency.Daily => ReportPeriod.GetDailyPeriod(scheduleSettings),
            ReportFrequency.Weekly => ReportPeriod.GetWeeklyPeriod(scheduleSettings),
            ReportFrequency.Monthly => ReportPeriod.GetMonthlyPeriod(scheduleSettings),
            ReportFrequency.Custom => ReportPeriod.GetCustomPeriod(scheduleSettings),
            _ => ReportPeriod.GetWeeklyPeriod(scheduleSettings)
        };
    }

    public DateTime GetNextReportTime(ScheduleSettings scheduleSettings)
    {
        var now = DateTime.Now;
        
        return scheduleSettings.Frequency switch
        {
            ReportFrequency.Daily => GetNextDailyReportTime(now, scheduleSettings.ReportTime),
            ReportFrequency.Weekly => GetNextWeeklyReportTime(now, scheduleSettings.WeeklyReportDay, scheduleSettings.ReportTime),
            ReportFrequency.Monthly => GetNextMonthlyReportTime(now, scheduleSettings.ReportTime),
            ReportFrequency.Custom => GetNextCustomReportTime(now, scheduleSettings.CustomDays, scheduleSettings.ReportTime),
            _ => GetNextWeeklyReportTime(now, scheduleSettings.WeeklyReportDay, scheduleSettings.ReportTime)
        };
    }

    private DateTime GetNextDailyReportTime(DateTime now, TimeSpan reportTime)
    {
        var today = now.Date.Add(reportTime);
        
        // Si ya pasó la hora de hoy, programar para mañana
        if (now > today)
        {
            return today.AddDays(1);
        }
        
        return today;
    }

    private DateTime GetNextWeeklyReportTime(DateTime now, DayOfWeek targetDay, TimeSpan reportTime)
    {
        var today = now.DayOfWeek;
        var daysUntilTarget = ((int)targetDay - (int)today + 7) % 7;
        
        var targetDate = now.Date.AddDays(daysUntilTarget).Add(reportTime);
        
        // Si es el día objetivo pero ya pasó la hora, programar para la próxima semana
        if (daysUntilTarget == 0 && now.TimeOfDay > reportTime)
        {
            targetDate = targetDate.AddDays(7);
        }
        
        return targetDate;
    }

    private DateTime GetNextMonthlyReportTime(DateTime now, TimeSpan reportTime)
    {
        // Primer día del próximo mes
        var firstDayNextMonth = new DateTime(now.Year, now.Month, 1).AddMonths(1);
        return firstDayNextMonth.Add(reportTime);
    }

    private DateTime GetNextCustomReportTime(DateTime now, int customDays, TimeSpan reportTime)
    {
        // Para frecuencia personalizada, calcular desde la última ejecución
        var nextExecution = now.Date.AddDays(customDays).Add(reportTime);
        
        // Si ya pasó la hora de hoy y customDays es 1 (diario personalizado), usar mañana
        if (customDays == 1 && now.TimeOfDay > reportTime)
        {
            nextExecution = now.Date.AddDays(1).Add(reportTime);
        }
        
        return nextExecution;
    }
}