using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PricingEngine.Application.Products;
using PricingEngine.Application.Quotes;
using PricingEngine.Domain.Quotes;
using PricingEngine.Infrastructure.Messaging;
using PricingEngine.Infrastructure.Outbox;
using PricingEngine.Infrastructure.Database;
using PricingEngine.Infrastructure.Products;

namespace PricingEngine.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPricingEngine(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PricingDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("PricingDatabase")));

        services.Configure<QuoteOptions>(configuration.GetSection("Quote"));
        services.Configure<OutboxOptions>(configuration.GetSection("Outbox"));
        services.Configure<RabbitMqOptions>(configuration.GetSection("RabbitMq"));
        services.AddScoped<IMessagePublisher, RabbitMqPublisher>();

        services.AddScoped<DatabaseInitializer>();
        services.AddScoped<IProductDefinitionRepository, ProductDefinitionRepository>();
        services.AddScoped<IQuoteRepository, QuoteRepository>();
        services.AddScoped<IProductPricingStrategy, InsuredSumTariffPricingStrategy>();
        services.AddScoped<ProductPricingStrategyResolver>();
        services.AddScoped<QuoteService>();
        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<QuoteOptions>>().Value;
            return new InstallmentOptionCalculator(options.InstallmentCounts);
        });
        services.AddHostedService<OutboxPublisherBackgroundService>();

        return services;
    }
}
