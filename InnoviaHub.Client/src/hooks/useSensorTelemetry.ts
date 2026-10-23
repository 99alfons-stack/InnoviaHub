import { useEffect, useState } from "react";
import { HubConnectionBuilder } from "@microsoft/signalr";
import { REALTIME_HUB_URL } from "../config/api.ts";
import { getSensorOverview, type RealtimeMeasurement, type SensorDevice } from "../services/sensorService";

export type SensorReading = { value: number; time: string };

// Latest reading per measurement type ("temperature", "co2", ...) for one device
export type DeviceReadings = Record<string, SensorReading>;

export type TelemetryConnectionState = "connecting" | "connected" | "reconnecting" | "disconnected";

// A device that has not reported for this long is shown as offline
export const STALE_AFTER_MS = 15_000;

export function useSensorTelemetry() {
  const [devices, setDevices] = useState<SensorDevice[]>([]);
  const [readings, setReadings] = useState<Record<string, DeviceReadings>>({});
  const [connectionState, setConnectionState] = useState<TelemetryConnectionState>("connecting");
  const [error, setError] = useState<string | null>(null);
  const [now, setNow] = useState(() => Date.now());

  useEffect(() => {
    let cancelled = false;
    let tenantSlug = "";

    const connection = new HubConnectionBuilder()
      .withUrl(REALTIME_HUB_URL)
      .withAutomaticReconnect()
      .build();

    connection.on("measurementReceived", (m: RealtimeMeasurement) => {
      setReadings((current) => ({
        ...current,
        [m.deviceId]: { ...current[m.deviceId], [m.type]: { value: m.value, time: m.time } },
      }));
    });

    connection.onreconnecting(() => setConnectionState("reconnecting"));
    connection.onreconnected(async () => {
      // Group membership is lost on reconnect, so join the tenant again
      await connection.invoke("JoinTenant", tenantSlug);
      setConnectionState("connected");
    });
    connection.onclose(() => {
      if (!cancelled) setConnectionState("disconnected");
    });

    async function start() {
      try {
        const overview = await getSensorOverview();
        if (cancelled) return;
        setDevices(overview.devices);
        tenantSlug = overview.tenantSlug;

        await connection.start();
        await connection.invoke("JoinTenant", tenantSlug);
        if (!cancelled) setConnectionState("connected");
      } catch (err) {
        // Ignore errors caused by our own cleanup (e.g. React StrictMode unmounting during start)
        if (cancelled) return;
        setError(err instanceof Error ? err.message : "Kunde inte ansluta till sensorerna");
        setConnectionState("disconnected");
      }
    }

    start();
    const ticker = setInterval(() => setNow(Date.now()), 1000);

    return () => {
      cancelled = true;
      clearInterval(ticker);
      connection.stop();
    };
  }, []);

  function lastSeen(deviceId: string): number | null {
    const times = Object.values(readings[deviceId] ?? {}).map((r) => new Date(r.time).getTime());
    return times.length ? Math.max(...times) : null;
  }

  function deviceStatus(deviceId: string): "ok" | "waiting" | "offline" {
    const seen = lastSeen(deviceId);
    if (seen === null) return "waiting";
    return now - seen > STALE_AFTER_MS ? "offline" : "ok";
  }

  const offlineCount = devices.filter((device) => deviceStatus(device.id) === "offline").length;

  return { devices, readings, connectionState, error, now, lastSeen, deviceStatus, offlineCount };
}

export type SensorTelemetry = ReturnType<typeof useSensorTelemetry>;
