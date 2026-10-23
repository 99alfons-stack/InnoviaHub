# Rules.Engine dokumentation
# Rules.Engine (Work in Progress)

The **Rules.Engine** service is part of the Innovia IoT platform.  
Its purpose is to evaluate incoming IoT measurements against user-defined rules and trigger alerts when conditions are met.

⚠️ **Important:** This service is currently a **work in progress**. It provides a basic foundation but is not yet production-ready. Expect breaking changes and incomplete features.

---

## Purpose

Rules.Engine allows tenants to define simple conditions (rules) for their devices.  
For example:

- *"If temperature > 28°C, raise an alert."*
- *"If CO₂ level exceeds 1200 ppm, notify in real-time."*

When a rule matches, the engine stores an **alert** in the database and (optionally) pushes it to the **Realtime.Hub** via SignalR.

---

## Current State

- Project scaffold with configuration for two databases:
  - `innovia_rules` (rules and alerts, created on startup)
  - `innovia_ingest` (read-only access to measurements written by **Ingest.Gateway**)
- Draft EF Core models for rules and alerts (incomplete).
- Background worker that runs every 10 seconds – **rule evaluation is not implemented**.
- SignalR client that connects to **Realtime.Hub** on startup (no alerts are published yet).
- No rule management endpoints yet (see TODOs in `Program.cs`).

---

## Usage

1. Start **Rules.Engine** (requires PostgreSQL from `deploy/docker-compose.yml`).
   ```bash
   cd src/Rules.Engine
   dotnet run
   ```

2. Verify that the service responds:
   ```bash
   curl http://localhost:<port>/
   ```

---

## Roadmap / TODOs

- [ ] Endpoints for creating and listing rules (`POST /rules`, `GET /rules`).
- [ ] Rule evaluation in the background worker (latest value vs threshold).
- [ ] Store alerts and expose them (`GET /alerts`).
- [ ] Publish alerts to **Realtime.Hub** (dedicated `alertRaised` event to the tenant group).
- [ ] Improve rule evaluation (support aggregations, time windows).
- [ ] Add authentication and multi-tenant isolation.
- [ ] Full admin API for rule management.
- [ ] Richer alert payloads (severity levels, metadata).
- [ ] Replace `EnsureCreated()` with EF migrations for production.
- [ ] Unit and integration testing.

---

## Status

This service should be seen as an **MVP / proof-of-concept**.  
It is included in the repository to give developers a starting point for experimentation and extension.

Contributions and improvements are expected in future iterations.