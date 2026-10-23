namespace Innovia.Shared.Models;
public class Measurement
{
    public DateTimeOffset Time { get; set; }
    public Guid TenantId { get; set; }
    public Guid DeviceId { get; set; }
    public Guid SensorId { get; set; }
    public int TypeId { get; set; }
    public double Value { get; set; }
}
