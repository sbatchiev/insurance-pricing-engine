using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PricingEngine.Infrastructure.Database.Records;

namespace PricingEngine.Infrastructure.Database;

public class DatabaseInitializer(
    PricingDbContext dbContext,
    IHostEnvironment hostEnvironment,
    ILogger<DatabaseInitializer> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        var productDefinitionPath = Path.Combine(hostEnvironment.ContentRootPath, "ProductDefinitions");
        if (!Directory.Exists(productDefinitionPath))
        {
            logger.LogWarning("Product definition directory {ProductDefinitionPath} does not exist.", productDefinitionPath);
            return;
        }

        var files = Directory.GetFiles(productDefinitionPath, "*.json");
        foreach (var file in files)
        {
            var definition = await LoadProductDefinitionFileAsync(file, cancellationToken);
            var now = DateTimeOffset.UtcNow;
            var existing = await dbContext.ProductDefinitions
                .FirstOrDefaultAsync(product =>
                    product.Code == definition.Code &&
                    product.Version == definition.Version,
                    cancellationToken);

            if (existing is null)
            {
                dbContext.ProductDefinitions.Add(new ProductDefinitionRecord
                {
                    Id = definition.Id,
                    Code = definition.Code,
                    Name = definition.Name,
                    Version = definition.Version,
                    IsActive = definition.IsActive,
                    CreatedAt = now,
                    UpdatedAt = now,
                    InputSchemaJson = definition.InputSchema.GetRawText(),
                    PricingConfigJson = definition.PricingConfig.GetRawText()
                });
            }
            else
            {
                existing.Name = definition.Name;
                existing.IsActive = definition.IsActive;
                existing.InputSchemaJson = definition.InputSchema.GetRawText();
                existing.PricingConfigJson = definition.PricingConfig.GetRawText();
                existing.UpdatedAt = now;
            }

            logger.LogInformation("Loaded product definition {ProductCode} v{ProductVersion} from {FileName}.",
                definition.Code,
                definition.Version,
                Path.GetFileName(file));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task<ProductDefinitionFile> LoadProductDefinitionFileAsync(
        string file,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(file);
        return await JsonSerializer.DeserializeAsync<ProductDefinitionFile>(stream, JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException($"Product definition file '{file}' is empty.");
    }
}

public record ProductDefinitionFile(
    Guid Id,
    string Code,
    string Name,
    int Version,
    bool IsActive,
    JsonElement InputSchema,
    JsonElement PricingConfig);
