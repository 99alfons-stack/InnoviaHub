namespace InnoviaHub.Shared.DTOs.Sensor;

public class SensorDeviceDto
{
    public Guid Id { get; set; }
    public string Serial { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
