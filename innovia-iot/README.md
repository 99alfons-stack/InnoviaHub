# Innovia Hub – IoT API (Monorepo, .NET 8)

Innovia Hub is a comprehensive IoT platform for smart buildings and office hubs, enabling organizations to seamlessly connect, manage, and monitor IoT devices and sensors at scale. The system provides robust APIs and real-time data integration for efficient facility management and automation.

This monorepo contains all core services: Device Registry, Ingest Gateway, Realtime Hub, Portal Adapter, and Rules Engine, as well as an Edge Simulator for device and data testing.

## Architecture
- **DeviceRegistry.Api** – Web API for tenants/sites/rooms/devices/sensors and key management (EF Core, PostgreSQL).
- **Ingest.Gateway** – HTTP-ingest + MQTT-consumer that normalizes and stores measurement data (EF Core).
- **Realtime.Hub** – SignalR hub that pushes the latest values per tenant/room/device.
- **Portal.Adapter** – Simplified endpoints for external portals + (placeholder for) webhooks.
- **Rules.Engine** – Worker Service that runs rules and generates alerts.
- **Edge.Simulator** – Console app that publishes MQTT measurements.

Database: **PostgreSQL (TimescaleDB compatible)**. Cache/queue: **Redis**. MQTT: **Eclipse Mosquitto**.

## Quickstart
1. Install **Docker**, **Docker Compose**, and **.NET 8 SDK**.
2. Run the setup script to create the solution and add all projects:
   ```powershell
   ./scripts/setup.ps1
   ```
   (This creates `Innovia.IoT.sln` and adds all projects.)
   - Alternatively, install the PowerShell extension in VS Code and run the script directly.
3. `docker compose -f deploy/docker-compose.yml up -d` (starts db/redis/mosquitto)
4. In a new terminal window, run the services locally (example):
   - `dotnet restore`
   - `dotnet build`
   - Start **DeviceRegistry.Api**, **Ingest.Gateway**, **Realtime.Hub**, **Portal.Adapter** (each in its own terminal)
5. Run **Edge.Simulator** to send data via MQTT.
6. Connect your own portal to **Realtime.Hub** (SignalR) and **Portal.Adapter** (REST).

See `docs/architecture.md` and `docs/api-specs.md` for more details.

## Guides: Running and Using the System

### 1. Start the system
- Ensure that **Docker Desktop** is running.
- Run `docker compose -f deploy/docker-compose.yml up -d` to start the database (Postgres), Redis, and Mosquitto.
- Verify containers: `docker ps`.

### 2. Start the services
- Open separate terminals for each service:
  ```bash
  cd src/DeviceRegistry.Api && dotnet run
  cd src/Ingest.Gateway && dotnet run
  cd src/Realtime.Hub && dotnet run
  cd src/Portal.Adapter && dotnet run
  ```
- Swagger is available on each service port, e.g. http://localhost:5101/swagger.

### 3. Create a tenant and a device
- Create a tenant via DeviceRegistry:
  ```bash
  curl -X POST http://localhost:5101/api/tenants \
    -H "Content-Type: application/json" \
    -d '{ "name": "Innovia Hub", "slug": "innovia" }'
  ```
- Create a device under a tenant:
  ```bash
  curl -X POST http://localhost:5101/api/tenants/<TENANT_ID>/devices \
    -H "Content-Type: application/json" \
    -d '{ "model":"Acme CO2-Temp", "serial":"dev-101", "status":"active" }'
  ```

### 4. Send measurement data
- Via Ingest HTTP:
  ```bash
  curl -X POST http://localhost:5102/ingest/http/innovia \
    -H "Content-Type: application/json" \
    -d '{
      "deviceId": "dev-101",
      "apiKey": "dev-101-key",
      "timestamp": "2025-10-01T12:00:00Z",
      "metrics": [
        { "type": "temperature", "value": 22.5, "unit": "C" },
        { "type": "co2", "value": 1000, "unit": "ppm" }
      ]
    }'
  ```

### 5. Read data via Portal.Adapter
- Fetch measurement series:
  ```bash
  curl "http://localhost:5104/portal/innovia/devices/<DEVICE_ID>/series?type=co2&from=2025-09-01T00:00:00Z&to=2025-10-01T23:59:59Z"
  ```
- Returns JSON with timestamped datapoints.

### 6. Real-time data
- Connect to the SignalR hub (Realtime.Hub) at `http://localhost:5103/hub/telemetry`.
- Example in JavaScript:
  ```js
  const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5103/hub/telemetry")
    .build();
  await connection.start();
  await connection.invoke("JoinTenant", "innovia");
  connection.on("measurementReceived", data => console.log("Realtime:", data));
  ```

### 7. Tips
- Use Swagger to test endpoints.
- Inspect the database with:
  ```bash
  docker exec -it <db-container> psql -U postgres -d innovia -c "\dt"
  ```
- Combine DeviceRegistry + Ingest + Portal.Adapter for an end-to-end flow.

## Ports (suggested)
- DeviceRegistry.Api: http://localhost:5101
- Ingest.Gateway:    http://localhost:5102
- Realtime.Hub:      http://localhost:5103
- Portal.Adapter:    http://localhost:5104
- Mosquitto (MQTT):  localhost:1883
- PostgreSQL:        localhost:5432
- Redis:             localhost:6379

## EF Core migrations
Create & run migrations in **DeviceRegistry.Api** and **Ingest.Gateway**. Example:
```bash
cd src/DeviceRegistry.Api
dotnet ef migrations add Init --project DeviceRegistry.Api.csproj
dotnet ef database update
```
(Ensure you have the tools: `dotnet tool install --global dotnet-ef`)
