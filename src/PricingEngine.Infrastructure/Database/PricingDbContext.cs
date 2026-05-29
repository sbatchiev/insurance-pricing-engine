using Microsoft.EntityFrameworkCore;
using PricingEngine.Infrastructure.Database.Records;

namespace PricingEngine.Infrastructure.Database;

public class PricingDbContext(DbContextOptions<PricingDbContext> options) : DbContext(options)
{
    public DbSet<ProductDefinitionRecord> ProductDefinitions => Set<ProductDefinitionRecord>();
    public DbSet<QuoteRecord> Quotes => Set<QuoteRecord>();
    public DbSet<OutboxMessageRecord> OutboxMessages => Set<OutboxMessageRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductDefinitionRecord>(builder =>
        {
            builder.ToTable("product_definitions");
            builder.HasKey(product => product.Id);
            builder.HasIndex(product => new { product.Code, product.Version }).IsUnique();
            builder.Property(product => product.Code).HasMaxLength(64).IsRequired();
            builder.Property(product => product.Name).HasMaxLength(200).IsRequired();
            builder.Property(product => product.InputSchemaJson).HasColumnType("jsonb");
            builder.Property(product => product.PricingConfigJson).HasColumnType("jsonb");
        });

        modelBuilder.Entity<QuoteRecord>(builder =>
        {
            builder.ToTable("quotes");
            builder.HasKey(quote => quote.Id);
            builder.HasIndex(quote => new { quote.ProductCode, quote.CreatedAt });
            builder.Property(quote => quote.ProductCode).HasMaxLength(64).IsRequired();
            builder.Property(quote => quote.Channel).HasMaxLength(64).IsRequired();
            builder.Property(quote => quote.Currency).HasMaxLength(3).IsRequired();
            builder.Property(quote => quote.NetPremium).HasPrecision(18, 2);
            builder.Property(quote => quote.Taxes).HasPrecision(18, 2);
            builder.Property(quote => quote.Fees).HasPrecision(18, 2);
            builder.Property(quote => quote.Total).HasPrecision(18, 2);
            builder.Property(quote => quote.RequestJson).HasColumnType("jsonb");
            builder.Property(quote => quote.ResponseJson).HasColumnType("jsonb");
        });

        modelBuilder.Entity<OutboxMessageRecord>(builder =>
        {
            builder.ToTable("outbox_messages");
            builder.HasKey(message => message.Id);
            builder.HasIndex(message => new { message.Status, message.NextAttemptAt });
            builder.Property(message => message.Type).HasMaxLength(200).IsRequired();
            builder.Property(message => message.PayloadJson).HasColumnType("jsonb");
            builder.Property(message => message.Status).HasMaxLength(32).IsRequired();
            builder.Property(message => message.LastError).HasMaxLength(2000);
        });
    }
}
