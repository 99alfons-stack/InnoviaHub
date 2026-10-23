using InnoviaHub.Api.Services.Interfaces;
using InnoviaHub.Shared.DTOs.Sensor;
using InnoviaHub.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InnoviaHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = nameof(UserRoles.Admin))]
public class SensorsController(ISensorService sensorService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<SensorOverviewDto>> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var overview = await sensorService.GetOverviewAsync(cancellationToken);
            return Ok(overview);
        }
        catch (Exception ex) when (ex is HttpRequestException
            || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            // Registry down, refused or timed out
            return StatusCode(StatusCodes.Status502BadGateway,
                "DeviceRegistry svarar inte.");
        }
    }
}
