using Microsoft.EntityFrameworkCore;
using SmartGas.Api.Data;
using SmartGas.Api.DTOs;
using SmartGas.Api.Models;

namespace SmartGas.Api.Services;

public class ZoneService
{
    private readonly AppDbContext _context;
    private readonly PlanLimitService _planLimitService;

    public ZoneService(AppDbContext context, PlanLimitService planLimitService)
    {
        _context = context;
        _planLimitService = planLimitService;
    }

    public async Task<List<ZoneResponse>> GetByAccountAsync(int accountId)
    {
        var zones = _context.Zones
            .Where(zone => zone.AccountId == accountId)
            .OrderByDescending(zone => zone.CreatedAt);

        return await ProjectZones(zones).ToListAsync();
    }

    public async Task<ZoneResponse?> GetByIdAsync(int id)
    {
        var zones = _context.Zones.Where(zone => zone.Id == id);

        return await ProjectZones(zones).FirstOrDefaultAsync();
    }

    public async Task<ZoneResponse?> CreateAsync(CreateZoneRequest request)
    {
        var accountExists = await _context.Accounts.AnyAsync(account => account.Id == request.AccountId);

        if (!accountExists)
        {
            return null;
        }

        var planLimitValidation = await _planLimitService.CanCreateZoneAsync(request.AccountId);

        if (!planLimitValidation.IsAllowed)
        {
            throw new InvalidOperationException(planLimitValidation.ErrorMessage);
        }

        var zone = new Zone
        {
            AccountId = request.AccountId,
            Name = request.Name,
            Description = request.Description,
            Sensitivity = request.Sensitivity,
            Status = "Safe"
        };

        _context.Zones.Add(zone);
        await _context.SaveChangesAsync();

        return new ZoneResponse
        {
            Id = zone.Id,
            AccountId = zone.AccountId,
            Name = zone.Name,
            Description = zone.Description,
            Status = zone.Status,
            Sensitivity = zone.Sensitivity,
            LastUpdated = zone.UpdatedAt,
            CreatedAt = zone.CreatedAt,
            UpdatedAt = zone.UpdatedAt
        };
    }

    public async Task<ZoneResponse?> UpdateAsync(int id, UpdateZoneRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("A zone name is required.");
        }

        var zone = await _context.Zones.FirstOrDefaultAsync(item => item.Id == id);

        if (zone == null)
        {
            return null;
        }

        zone.Name = request.Name.Trim();
        zone.Description = request.Description?.Trim();
        zone.Sensitivity = string.IsNullOrWhiteSpace(request.Sensitivity) ? zone.Sensitivity : request.Sensitivity.Trim();
        zone.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(zone.Id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var zone = await _context.Zones.FirstOrDefaultAsync(item => item.Id == id);

        if (zone == null)
        {
            return false;
        }

        var hasSensors = await _context.Sensors.AnyAsync(sensor => sensor.ZoneId == id);

        if (hasSensors)
        {
            throw new InvalidOperationException("The zone still has sensors.");
        }

        _context.Zones.Remove(zone);
        await _context.SaveChangesAsync();

        return true;
    }

    private IQueryable<ZoneResponse> ProjectZones(IQueryable<Zone> zones)
    {
        return zones.Select(zone => new ZoneResponse
        {
            Id = zone.Id,
            AccountId = zone.AccountId,
            Name = zone.Name,
            Description = zone.Description,
            Status = zone.Status,
            Sensitivity = zone.Sensitivity,
            SensorCount = _context.Sensors.Count(sensor => sensor.ZoneId == zone.Id),
            GasLevel = _context.SensorReadings
                .Where(reading => reading.ZoneId == zone.Id && reading.GasLevel != null)
                .OrderByDescending(reading => reading.CreatedAt).ThenByDescending(reading => reading.Id)
                .Select(reading => reading.GasLevel).FirstOrDefault(),
            Temperature = _context.SensorReadings
                .Where(reading => reading.ZoneId == zone.Id && reading.Temperature != null)
                .OrderByDescending(reading => reading.CreatedAt).ThenByDescending(reading => reading.Id)
                .Select(reading => reading.Temperature).FirstOrDefault(),
            RiskLevel = zone.Status == "Critical" ? "Critical" : zone.Status == "Warning" ? "High" : "Low",
            LastUpdated = _context.SensorReadings
                .Where(reading => reading.ZoneId == zone.Id)
                .Max(reading => (DateTime?)reading.CreatedAt) ?? zone.UpdatedAt,
            CreatedAt = zone.CreatedAt,
            UpdatedAt = zone.UpdatedAt
        });
    }
}
