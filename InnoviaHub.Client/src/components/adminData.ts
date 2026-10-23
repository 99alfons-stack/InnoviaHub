export type AdminTab = "dashboard" | "sensors" | "resources" | "members";

export const adminTabs: { id: AdminTab; label: string; icon: string }[] = [
  { id: "dashboard", label: "Översikt", icon: "◈" },
  { id: "sensors", label: "Sensorer", icon: "⚙" },
  { id: "resources", label: "Resurser", icon: "▪" },
  { id: "members", label: "Medlemmar", icon: "◯" },
];
