using System.Net.Http.Json;
using System.Text.Json;
using InnoviaHub.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InnoviaHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAvailabilityContextService _availabilityContextService;

    public ChatController(
        IHttpClientFactory httpClientFactory,
        IAvailabilityContextService availabilityContextService)
    {
        _httpClientFactory = httpClientFactory;
        _availabilityContextService = availabilityContextService;
    }

    public sealed record ChatRequest(string Question);

    public sealed record ChatResponse(string Answer);

    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Chat(
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest("Frågan får inte vara tom.");

        var availability = await _availabilityContextService.BuildContextAsync();

        var client = _httpClientFactory.CreateClient("openai");
        var openAiRequest = new
        {
            model = "gpt-4o-mini",
            instructions = "Du är supportassistent för InnoviaHub, en app för att boka "
                + "resurser som rum och utrustning. Svara kort och vänligt på svenska.\n"
                + "Använd endast tillgänglighetsdatan nedan när du svarar på frågor om vad "
                + "som är ledigt eller bokat. Hitta inte på resurser eller tider. Om frågan "
                + "gäller tid utanför datan, säg att du bara ser de kommande två veckorna. "
                + "Du kan inte själv boka – hänvisa användaren till bokningssidan.\n\n"
                + "TILLGÄNGLIGHET:\n" + availability,
            input = request.Question.Trim()
        };

        using var response = await client.PostAsJsonAsync(
            "responses",
            openAiRequest,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
            return StatusCode(StatusCodes.Status502BadGateway,
                "OpenAI kunde inte behandla frågan.");

        await using var responseStream = await response.Content
            .ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(
            responseStream,
            cancellationToken: cancellationToken);

        var answer = ExtractAnswer(document.RootElement);

        if (string.IsNullOrWhiteSpace(answer))
            return StatusCode(StatusCodes.Status502BadGateway,
                "OpenAI returnerade inget svar.");

        return Ok(new ChatResponse(answer));
    }

    private static string? ExtractAnswer(JsonElement response)
    {
        if (!response.TryGetProperty("output", out var output) ||
            output.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var outputItem in output.EnumerateArray())
        {
            if (!outputItem.TryGetProperty("content", out var content) ||
                content.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var contentItem in content.EnumerateArray())
            {
                if (!contentItem.TryGetProperty("type", out var type) ||
                    type.GetString() != "output_text" ||
                    !contentItem.TryGetProperty("text", out var text))
                    continue;

                return text.GetString();
            }
        }

        return null;
    }
}


