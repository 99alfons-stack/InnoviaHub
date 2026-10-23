# Arkitekturöversikt

```
Edge Devices -> (MQTT/HTTP) -> Ingest.Gateway -> DB (measurements)
                                    |               ^
                                    v               |
                             Realtime.Hub (SignalR) |
                                    |               |
                             Portal.Adapter (REST/Webhook)
                                    ^
DeviceRegistry.Api (tenants/sites/rooms/devices/sensors/keys)
```

- **Multi-tenant**: Alla queries filtrerar på `tenant_id`.
- **Measurements** lagras i tabellen `measurements` (TimescaleDB-hypertable i produktion).
- **Realtime**: senaste värden pushas till SignalR-grupper per tenant/room/device.
