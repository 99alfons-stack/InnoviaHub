using System.Globalization;
using System.Text;
using InnoviaHub.Api.Services.Interfaces;
using InnoviaHub.DataAccess.Repositories.Interfaces;

namespace InnoviaHub.Api.Services;

// Bygger en textsammanfattning av resurser och bokningar som skickas med till OpenAI,
// så att chatten kan svara på frågor om vad som är ledigt.
public class AvailabilityContextService(
    IResourceRepository resourceRepository,
    IBookingRepository bookingRepository) : IAvailabilityContextService
{
    private const int DaysAhead = 14;

    private static readonly TimeZoneInfo SwedishTime =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm");

    private static readonly CultureInfo Swedish = new("sv-SE");

    public async Task<string> BuildContextAsync()
    {
        var nowUtc = DateTime.UtcNow;
        var toUtc = nowUtc.AddDays(DaysAhead);

        var resources = (await resourceRepository.GetAllAsync())
            .Where(r => r.IsActive)
            .OrderBy(r => r.ResourceType.Name)
            .ThenBy(r => r.Name)
            .ToList();

        var bookingsByResource = (await bookingRepository.GetActiveInRangeAsync(nowUtc, toUtc))
            .ToLookup(b => b.ResourceId);

        var sb = new StringBuilder();
        sb.AppendLine($"Aktuell tid: {Format(nowUtc)} (svensk tid).");
        sb.AppendLine($"Bokningar visas för de kommande {DaysAhead} dagarna. "
            + "All tid som inte är bokad räknas som ledig.");
        sb.AppendLine();

        if (resources.Count == 0)
        {
            sb.AppendLine("Det finns inga aktiva resurser.");
            return sb.ToString();
        }

        foreach (var resource in resources)
        {
            var zone = resource.Zone is null ? "" : $", zon {resource.Zone}";
            sb.AppendLine($"Resurs: {resource.Name} (typ: {resource.ResourceType.Name}"
                + $"{zone}, kapacitet: {resource.Capacity})");

            var bookings = bookingsByResource[resource.Id].ToList();

            if (bookings.Count == 0)
            {
                sb.AppendLine("  Inga bokningar – helt ledig.");
                continue;
            }

            foreach (var booking in bookings)
                sb.AppendLine($"  Bokad {Format(booking.StartTime)} – {Format(booking.EndTime)}");
        }

        return sb.ToString();
    }

    private static string Format(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), SwedishTime)
            .ToString("dddd yyyy-MM-dd HH:mm", Swedish);
}
