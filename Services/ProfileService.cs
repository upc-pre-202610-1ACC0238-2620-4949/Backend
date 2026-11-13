using Microsoft.EntityFrameworkCore;
using SmartGas.Api.Data;
using SmartGas.Api.DTOs;

namespace SmartGas.Api.Services;

public class ProfileService
{
    private readonly AppDbContext _context;

    public ProfileService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ProfileResponse?> GetByAccountAsync(int accountId)
    {
        var profile = await _context.Profiles
            .Include(profile => profile.Account)
            .FirstOrDefaultAsync(profile => profile.AccountId == accountId);

        if (profile == null)
        {
            return null;
        }

        return new ProfileResponse
        {
            Id = profile.Id,
            AccountId = profile.AccountId,
            FullName = profile.FullName,
            Email = profile.Account.Email,
            Role = profile.Account.Role,
            AccountType = GetAccountType(profile.Account.Role),
            BusinessName = profile.BusinessName,
            Phone = profile.Phone,
            District = profile.District,
            Address = profile.Address,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt
        };
    }

    public async Task<ProfileResponse?> UpdateAsync(int accountId, UpdateProfileRequest request)
    {
        var profile = await _context.Profiles
            .Include(profile => profile.Account)
            .FirstOrDefaultAsync(profile => profile.AccountId == accountId);

        if (profile == null)
        {
            return null;
        }

        profile.FullName = request.FullName;
        profile.BusinessName = request.BusinessName;
        profile.Phone = request.Phone;
        profile.District = request.District;
        profile.Address = request.Address;
        profile.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new ProfileResponse
        {
            Id = profile.Id,
            AccountId = profile.AccountId,
            FullName = profile.FullName,
            Email = profile.Account.Email,
            Role = profile.Account.Role,
            AccountType = GetAccountType(profile.Account.Role),
            BusinessName = profile.BusinessName,
            Phone = profile.Phone,
            District = profile.District,
            Address = profile.Address,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt
        };
    }

    private static string GetAccountType(string role) =>
        role.Contains("restaurant", StringComparison.OrdinalIgnoreCase)
        || role.Contains("commercial", StringComparison.OrdinalIgnoreCase)
        || role.Contains("administrator", StringComparison.OrdinalIgnoreCase)
            ? "commercial" : "domestic";
}
