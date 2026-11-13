using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using SmartGas.Api.DTOs;
using SmartGas.Api.Services;

namespace SmartGas.Api.Controllers;

[ApiController]
[Route("api/v1/zones")]
public class ZonesController : ControllerBase
{
    private readonly ZoneService _zoneService;
    private readonly IStringLocalizer<ZonesController> _localizer;

    public ZonesController(ZoneService zoneService, IStringLocalizer<ZonesController> localizer)
    {
        _zoneService = zoneService;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> GetByAccount([FromQuery] int accountId)
    {
        var zones = await _zoneService.GetByAccountAsync(accountId);
        return Ok(zones);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var zone = await _zoneService.GetByIdAsync(id);

        if (zone == null)
        {
            return NotFound(new
            {
                message = _localizer["ZoneNotFound"].Value
            });
        }

        return Ok(zone);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateZoneRequest request)
    {
        try
        {
            var result = await _zoneService.CreateAsync(request);

            if (result == null)
            {
                return NotFound(new
                {
                    message = _localizer["AccountNotFound"].Value
                });
            }

            return Created($"/api/v1/zones/{result.Id}", result);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new
            {
                message = LocalizeException(exception)
            });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateZoneRequest request)
    {
        try
        {
            var result = await _zoneService.UpdateAsync(id, request);

            if (result == null)
            {
                return NotFound(new
                {
                    message = _localizer["ZoneNotFound"].Value
                });
            }

            return Ok(result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = LocalizeException(exception)
            });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var deleted = await _zoneService.DeleteAsync(id);

            if (!deleted)
            {
                return NotFound(new
                {
                    message = _localizer["ZoneNotFound"].Value
                });
            }

            return NoContent();
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new
            {
                message = LocalizeException(exception)
            });
        }
    }

    private string LocalizeException(Exception exception)
    {
        var message = exception.Message;

        if (message.StartsWith("Plan limit reached. Your current plan allows up to") && message.EndsWith("zones."))
        {
            var maxZones = message
                .Replace("Plan limit reached. Your current plan allows up to", string.Empty)
                .Replace("zones.", string.Empty)
                .Trim();

            return _localizer["ZonePlanLimitReached", maxZones].Value;
        }

        return message switch
        {
            "The account does not have an active subscription." => _localizer["NoActiveSubscription"].Value,
            "A zone name is required." => _localizer["ZoneNameRequired"].Value,
            "The zone still has sensors." => _localizer["ZoneHasSensors"].Value,
            _ => message
        };
    }
}
