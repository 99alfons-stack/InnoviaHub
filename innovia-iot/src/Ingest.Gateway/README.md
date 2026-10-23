# Ingest.Gateway – Dokumentation
# Ingest.Gateway – Documentation

Ingest.Gateway is the service responsible for receiving telemetry data from IoT devices and writing it to the database. It also integrates with **DeviceRegistry** to resolve tenant and device information, and publishes real-time updates to **Realtime.Hub**.

## Overview
- **Receives data** from devices via HTTP endpoints (later extensible to MQTT).
- **Validates payloads** and resolves tenant + device IDs via DeviceRegistry.
- **Persists measurements** to the `Measurements` table in the database.
- **Publishes in real-time** to Realtime.Hub (SignalR) for connected clients.

## Base URL
By default, the service runs at:
```
http://localhost:5102
```

Swagger is available at:
```
http://localhost:5102/swagger
```

---

## Endpoints

### Ingest measurements via HTTP
```http
POST /ingest/http/{tenantSlug}
Content-Type: application/json
```

**Body:**
```json
{
  "deviceId": "dev-101",
  "apiKey": "dev-101-key",
  "timestamp": "2025-10-02T12:30:00Z",
  "metrics": [
    { "type": "temperature", "value": 23.3, "unit": "C" },
    { "type": "co2", "value": 972, "unit": "ppm" }
  ]
}
```

- `tenantSlug`: The slug of the tenant (must exist in DeviceRegistry).
- `deviceId`: The **serial** of the device (must exist in DeviceRegistry).
- `apiKey`: For now, a placeholder or fixed key (can later be validated per device).
- `metrics`: Array of telemetry values.

**Response (202 Accepted):**
```json
{
  "status": "accepted"
}
```

---

### Debug endpoint (for development)
```http
GET /ingest/debug/device/{deviceId:guid}
```

Returns how many rows exist in the database for the given device, plus the 5 most recent measurements.

---

## Usage Flow

1. **Create a tenant** in DeviceRegistry.
2. **Add one or more devices** under that tenant, each with a unique `serial`.
3. Devices (or simulators) send measurements to:
   ```
   POST /ingest/http/{tenantSlug}
   ```
   using their serial in the payload.
4. Ingest resolves tenant+serial → GUIDs using DeviceRegistry.
5. Data is stored in the database (`Measurements`) and simultaneously published to Realtime.Hub.

---

## Example with curl

```bash
curl -i -X POST http://localhost:5102/ingest/http/innovia \
  -H "Content-Type: application/json" \
  -d '{
    "deviceId": "dev-101",
    "apiKey": "dev-101-key",
    "timestamp": "2025-10-02T12:30:00Z",
    "metrics": [
      { "type": "temperature", "value": 23.3, "unit": "C" },
      { "type": "co2", "value": 972, "unit": "ppm" }
    ]
  }'
```

---

## Notes
- Only devices and tenants that exist in **DeviceRegistry** can send data.
- For simplicity, `apiKey` is not enforced yet – this can be extended for real authentication.
- Data is written into the **TimescaleDB** database configured in `appsettings.json`.
- Real-time publishing uses SignalR → clients subscribed in Realtime.Hub with `JoinTenant(tenantSlug)` will receive `measurementReceived` events.