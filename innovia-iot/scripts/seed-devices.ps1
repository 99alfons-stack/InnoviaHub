# Registrerar tenanten "innovia" och de 10 enheter som Edge.Simulator skickar data för.
# Kan köras flera gånger: tenant och enheter som redan finns hoppas över.
# Kräver att DeviceRegistry.Api är igång.
#
#   .\scripts\seed-devices.ps1
#   .\scripts\seed-devices.ps1 -RegistryUrl http://localhost:5101

param(
    [string]$RegistryUrl = "http://localhost:5101",
    [string]$TenantName = "Innovia Hub",
    [string]$TenantSlug = "innovia"
)

$ErrorActionPreference = "Stop"

# Samma uppsättning som i src/Edge.Simulator/Program.cs
$devices = @(
    @{ Serial = "dev-101"; Model = "Multisensor T/CO2" },
    @{ Serial = "dev-102"; Model = "Multisensor T/CO2" },
    @{ Serial = "dev-103"; Model = "Multisensor T/CO2" },
    @{ Serial = "dev-104"; Model = "Multisensor T/CO2" },
    @{ Serial = "dev-105"; Model = "CO2-sensor" },
    @{ Serial = "dev-106"; Model = "CO2-sensor" },
    @{ Serial = "dev-107"; Model = "CO2-sensor" },
    @{ Serial = "dev-108"; Model = "Temperatursensor" },
    @{ Serial = "dev-109"; Model = "Temperatursensor" },
    @{ Serial = "dev-110"; Model = "Temperatursensor" }
)

# Returnerar $null vid 404 i stället för att kasta
function Get-OrNull([string]$url) {
    try { return Invoke-RestMethod -Uri $url -Method Get }
    catch {
        if ($_.Exception.Response -and [int]$_.Exception.Response.StatusCode -eq 404) { return $null }
        throw
    }
}

function Post-Json([string]$url, $body) {
    Invoke-RestMethod -Uri $url -Method Post -ContentType "application/json" -Body ($body | ConvertTo-Json)
}

$tenant = Get-OrNull "$RegistryUrl/api/tenants/by-slug/$TenantSlug"
if ($tenant) {
    Write-Host "Tenant '$TenantSlug' finns redan ($($tenant.id))"
} else {
    $tenant = Post-Json "$RegistryUrl/api/tenants" @{ name = $TenantName; slug = $TenantSlug }
    Write-Host "Skapade tenant '$TenantSlug' ($($tenant.id))" -ForegroundColor Green
}

foreach ($device in $devices) {
    $existing = Get-OrNull "$RegistryUrl/api/tenants/$($tenant.id)/devices/by-serial/$($device.Serial)"
    if ($existing) {
        Write-Host "  $($device.Serial) finns redan"
        continue
    }
    Post-Json "$RegistryUrl/api/tenants/$($tenant.id)/devices" @{ model = $device.Model; serial = $device.Serial; status = "active" } | Out-Null
    Write-Host "  Skapade $($device.Serial) ($($device.Model))" -ForegroundColor Green
}

$count = (Invoke-RestMethod "$RegistryUrl/api/tenants/$($tenant.id)/devices").Count
Write-Host "Klart: $count enheter i tenant '$TenantSlug'"
