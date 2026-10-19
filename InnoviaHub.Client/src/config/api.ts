export const BASE_API_URL = import.meta.env.VITE_API_URL;

// SignalR hub in Innovia IoT (Realtime.Hub) that pushes live sensor measurements
export const REALTIME_HUB_URL = import.meta.env.VITE_REALTIME_HUB_URL ?? "http://localhost:5103/hub/telemetry";