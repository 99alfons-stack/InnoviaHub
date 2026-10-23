# API-specifikationer (översikt)

## DeviceRegistry.Api
- POST /api/tenants
- POST /api/tenants/{tenantId}/sites
- POST /api/tenants/{tenantId}/devices
- GET  /api/tenants/{tenantId}/devices/{deviceId}

## Ingest.Gateway
- POST /ingest/http/{tenantSlug}
- MQTT topic: tenants/{tenantSlug}/devices/{deviceId}/measurements

## Realtime.Hub (SignalR)
- Hub: /hub/telemetry
- Metod: JoinTenant(tenantSlug)

## Portal.Adapter
- GET /portal/{tenantSlug}/rooms/{roomId}/latest
- GET /portal/{tenantSlug}/devices/{deviceId}/series?type=co2&from=...&to=...
