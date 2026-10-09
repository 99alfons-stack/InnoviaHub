namespace InnoviaHub.Api.Services.Interfaces;

public interface IAvailabilityContextService
{
    Task<string> BuildContextAsync();
}
