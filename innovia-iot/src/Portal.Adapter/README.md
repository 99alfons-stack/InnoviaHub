# Portal.Adapter dokumentation
# Portal.Adapter – Documentation

Portal.Adapter is the service responsible for exposing data to external portals and clients. It provides APIs to query devices, measurement data, and series in a tenant-safe way.

## Overview
- **Serves tenant data** for portals and dashboards.
- **Queries measurements** stored by Ingest.Gateway and DeviceRegistry.
- **Provides time series data** for visualization and reporting.
- **Acts as a secure adapter** between backend services and user-facing portals.

## Base URL
By default, the service runs at:
```
http://localhost:5104
```

Swagger is available at:
```
http://localhost:5104/swagger
```

---

## Endpoints

### List devices for a tenant
```http
GET /portal/{tenantId:guid}/devices
```

Returns all devices registered under a specific tenant.

---

### Get device by serial
```http
GET /portal/{tenantId:guid}/devices/by-serial/{serial}
```

---

### Get time series data for a device
```http
GET /portal/{tenantId:guid}/devices/{deviceId:guid}/series
```

Query string parameters:
- `from` (optional, ISO8601 datetime)
- `to` (optional, ISO8601 datetime)
- `type` (optional, e.g., `temperature`, `co2`)

---

### Get all measurements for a device
```http
GET /portal/{tenantId:guid}/devices/{deviceId:guid}/measurements
```

Returns all measurements for a given device (optionally filtered by `from/to`).

---

### Get all series types for a device
```http
GET /portal/{tenantId:guid}/devices/{deviceId:guid}/series/all
```

Returns available measurement types (e.g., `temperature`, `co2`) with their time ranges.

---

## Usage Flow

1. **Tenant + devices** must first be created in DeviceRegistry.
2. **Measurements** must be ingested through Ingest.Gateway.
3. **Portal.Adapter** can then be queried to:
   - List devices per tenant
   - Retrieve measurement history
   - Fetch time series data for visualization
   - Inspect available metric types

---

## Example with curl

```bash
curl "http://localhost:5104/portal/44a8ce94-888e-478c-8007-3b28544fdf51/devices/027cd86a-1459-4d15-94aa-91007deee95f/measurements"
```

**Response:**
```json
{
  "deviceId": "027cd86a-1459-4d15-94aa-91007deee95f",
  "count": 10,
  "from": "2025-10-01T12:00:00Z",
  "to": "2025-10-02T12:00:00Z",
  "type": "temperature",
  "measurements": [
    { "time": "2025-10-02T11:50:00Z", "value": 23.5 },
    { "time": "2025-10-02T12:00:00Z", "value": 23.6 }
  ]
}
```

---

## Notes
- Tenant and device IDs must be GUIDs.
- Slug + serial are resolved via DeviceRegistry, but Portal endpoints expect GUIDs for secure access.
- Data returned by Portal.Adapter is **read-only** – devices and tenants must be created via DeviceRegistry and data ingested via Ingest.Gateway.