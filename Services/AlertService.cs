using Microsoft.EntityFrameworkCore;
using SmartGas.Api.Data;
using SmartGas.Api.DTOs;

namespace SmartGas.Api.Services;

public class AlertService
{
    private readonly AppDbContext _context;

    public AlertService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<AlertResponse>> GetByAccountAsync(int accountId)
    {
        return await _context.Alerts
            .Where(alert => alert.AccountId == accountId)
            .OrderByDescending(alert => alert.CreatedAt)
            .Select(alert => new AlertResponse
            {
                Id = alert.Id,
                AccountId = alert.AccountId,
                IncidentId = alert.IncidentId,
                ZoneName = alert.Incident.Zone.Name,
                IsResolved = alert.ResolvedAt != null || alert.Status == "Resolved" || alert.Status == "FalseAlarm",
                Title = alert.Title,
                Message = alert.Message,
                Severity = alert.Severity,
                Status = alert.Status,
                CreatedAt = alert.CreatedAt,
                ResolvedAt = alert.ResolvedAt
            })
            .ToListAsync();
    }
}
