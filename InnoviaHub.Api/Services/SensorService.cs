using System.Net;
using System.Net.Http.Json;
using InnoviaHub.Api.Services.Interfaces;
using InnoviaHub.Shared.DTOs.Sensor;

namespace InnoviaHub.Api.Services;

// Reads the tenant's devices from the Innovia IoT DeviceRegistry.Api
public class SensorService(IHttpClientFactory httpClientFactory, IConfiguration configuration) : ISensorService
{
    public async Task<SensorOverviewDto> GetOverviewAsync(CancellationToken cancellationToken)
    {
        var tenantSlug = configuration["Iot:TenantSlug"] ?? "innovia";
        var client = httpClientFactory.CreateClient("deviceRegistry");

        // DeviceRegistry answers 404 for an unknown slug; GetFromJsonAsync would turn that into a generic 500
        using var tenantResponse = await client.GetAsync($"api/tenants/by-slug/{tenantSlug}", cancellationToken);
        if (tenantResponse.StatusCode == HttpStatusCode.NotFound)
            throw new KeyNotFoundException("IOT_TENANT_NOT_FOUND");
        tenantResponse.EnsureSuccessStatusCode();

        var tenant = await tenantResponse.Content.ReadFromJsonAsync<RegistryTenant>(cancellationToken)
            ?? throw new KeyNotFoundException("IOT_TENANT_NOT_FOUND");

        var devices = await client.GetFromJsonAsync<List<RegistryDevice>>(
            $"api/tenants/{tenant.Id}/devices", cancellationToken) ?? [];

        return new SensorOverviewDto
        {
            TenantSlug = tenant.Slug,
            Devices =
            [
                .. devices
                    .OrderBy(d => d.Serial)
                    .Select(d => new SensorDeviceDto
                    {
                        Id = d.Id,
                        Serial = d.Serial,
                        Model = d.Model,
                        Status = d.Status
                    })
            ]
        };
    }

    private sealed record RegistryTenant(Guid Id, string Name, string Slug);

    private sealed record RegistryDevice(Guid Id, Guid TenantId, Guid? RoomId, string Model, string Serial, string Status);
}
