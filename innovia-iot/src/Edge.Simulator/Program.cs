using MQTTnet;
using MQTTnet.Client;
using System.Text.Json;
using System.Text;

var factory = new MqttFactory();
var client = factory.CreateMqttClient();

var options = new MqttClientOptionsBuilder()
    .WithTcpServer("localhost", 1883)
    .Build();

Console.WriteLine("Edge.Simulator starting… connecting to MQTT at localhost:1883");
try
{
    await client.ConnectAsync(options);
    Console.WriteLine("✅ Connected to MQTT broker.");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Failed to connect to MQTT broker: {ex.Message}");
    throw;
}

const string tenantSlug = "innovia";

// The 10 devices registered in DeviceRegistry, and which metrics each one reports
var devices = new (string Serial, bool Temperature, bool Co2)[]
{
    ("dev-101", true, true),
    ("dev-102", true, true),
    ("dev-103", true, true),
    ("dev-104", true, true),
    ("dev-105", false, true),
    ("dev-106", false, true),
    ("dev-107", false, true),
    ("dev-108", true, false),
    ("dev-109", true, false),
    ("dev-110", true, false),
};

// Every device publishes once per tick. Requirement is a reading at least every 10 s,
// so tick at 5 s to keep a safe margin against timer jitter and slow publishes.
var interval = TimeSpan.FromSeconds(5);
Console.WriteLine($"Simulating {devices.Length} devices in tenant '{tenantSlug}', one reading per device every {interval.TotalSeconds:0} s");

var rand = new Random();
using var timer = new PeriodicTimer(interval);
do
{
    foreach (var device in devices)
    {
        var metrics = new List<object>();
        if (device.Temperature)
            metrics.Add(new { type = "temperature", value = Math.Round(21.5 + rand.NextDouble(), 2), unit = "C" });
        if (device.Co2)
            metrics.Add(new { type = "co2", value = 900 + rand.Next(0, 700), unit = "ppm" });

        var payload = new
        {
            deviceId = device.Serial,
            apiKey = $"{device.Serial}-key",
            timestamp = DateTimeOffset.UtcNow,
            metrics
        };

        var topic = $"tenants/{tenantSlug}/devices/{device.Serial}/measurements";
        var json = JsonSerializer.Serialize(payload);

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(Encoding.UTF8.GetBytes(json))
            .Build();

        await client.PublishAsync(message);
        Console.WriteLine($"[{DateTimeOffset.UtcNow:o}] Published to '{topic}': {json}");
    }
}
while (await timer.WaitForNextTickAsync());
