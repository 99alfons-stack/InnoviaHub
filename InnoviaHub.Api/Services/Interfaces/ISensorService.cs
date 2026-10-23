using InnoviaHub.Shared.DTOs.Sensor;

namespace InnoviaHub.Api.Services.Interfaces;

public interface ISensorService
{
    Task<SensorOverviewDto> GetOverviewAsync(CancellationToken cancellationToken);
}
