namespace InnoviaHub.Shared.DTOs.Sensor;

public class SensorOverviewDto
{
    // Tenant slug the client joins in Realtime.Hub to receive live measurements
    public string TenantSlug { get; set; } = string.Empty;
    public List<SensorDeviceDto> Devices { get; set; } = [];
}
