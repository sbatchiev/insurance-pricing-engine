using PricingEngine.Api.Contracts;
using PricingEngine.Application.Quotes;

namespace PricingEngine.Api.Endpoints;

public static class QuoteEndpoints
{
    public static IEndpointRouteBuilder MapQuoteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/quotes").WithTags("Quotes");

        group.MapPost("/", async (
            CreateQuoteRequest request,
            QuoteService quoteService,
            CancellationToken cancellationToken) =>
        {
            var quote = await quoteService.CreateQuoteAsync(
                new QuoteCalculationRequest(
                    request.ProductCode,
                    request.Channel,
                    request.Currency,
                    request.Inputs,
                    DateTimeOffset.UtcNow),
                cancellationToken);

            return Results.Created($"/quotes/{quote.Id}", QuoteResponse.FromQuote(quote));
        })
        .WithName("CreateQuote")
        .WithSummary("Calculates a quote and stores it for audit.");

        group.MapGet("/{quoteId:guid}", async (
            Guid quoteId,
            QuoteService quoteService,
            CancellationToken cancellationToken) =>
        {
            var quote = await quoteService.GetQuoteAsync(quoteId, cancellationToken);

            return quote is null
                ? Results.NotFound()
                : Results.Ok(QuoteResponse.FromQuote(quote));
        })
        .WithName("GetQuote")
        .WithSummary("Returns a stored quote by id.");

        return app;
    }
}
