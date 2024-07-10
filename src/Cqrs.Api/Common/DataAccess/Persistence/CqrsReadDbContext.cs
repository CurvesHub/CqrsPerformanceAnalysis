using Cqrs.Api.UseCases.Articles.Persistence.Entities;
using Cqrs.Api.UseCases.Attributes.Common.Persistence.Entities;
using Cqrs.Api.UseCases.Attributes.Common.Persistence.Entities.AttributeValues;
using Cqrs.Api.UseCases.Categories.Common.Persistence.Entities;
using Cqrs.Api.UseCases.RootCategories.Common.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Attribute = Cqrs.Api.UseCases.Attributes.Common.Persistence.Entities.Attribute;

namespace Cqrs.Api.Common.DataAccess.Persistence;

/// <inheritdoc cref="Microsoft.EntityFrameworkCore.DbContext" />
public class CqrsReadDbContext(DbContextOptions<CqrsReadDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Gets a db set of <see cref="Article"/>s.
    /// </summary>
    public DbSet<Article> Articles => Set<Article>();

    /// <summary>
    /// Gets a db set of <see cref="Attribute"/>s.
    /// </summary>
    public DbSet<Attribute> Attributes => Set<Attribute>();

    /// <summary>
    /// Gets a db set of <see cref="AttributeMapping"/>s.
    /// </summary>
    public DbSet<AttributeMapping> AttributeMappings => Set<AttributeMapping>();

    /// <summary>
    /// Gets a db set of <see cref="Category"/>s.
    /// </summary>
    public DbSet<Category> Categories => Set<Category>();

    /// <summary>
    /// Gets a db set of <see cref="RootCategory"/>s.
    /// </summary>
    public DbSet<RootCategory> RootCategories => Set<RootCategory>();

    /// <summary>
    /// Gets a db set of <see cref="AttributeBooleanValue"/>s.
    /// </summary>
    public DbSet<AttributeBooleanValue> AttributeBooleanValues => Set<AttributeBooleanValue>();

    /// <summary>
    /// Gets a db set of <see cref="AttributeStringValue"/>s.
    /// </summary>
    public DbSet<AttributeStringValue> AttributeStringValues => Set<AttributeStringValue>();

    /// <summary>
    /// Gets a db set of <see cref="AttributeIntValue"/>s.
    /// </summary>
    public DbSet<AttributeIntValue> AttributeIntValues => Set<AttributeIntValue>();

    /// <summary>
    /// Gets a db set of <see cref="AttributeDecimalValue"/>s.
    /// </summary>
    public DbSet<AttributeDecimalValue> AttributeDecimalValues => Set<AttributeDecimalValue>();

    /// <inheritdoc />
    public override int SaveChanges()
    {
        ThrowOnSaveChanges();
        return 0;
    }

    /// <inheritdoc />
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ThrowOnSaveChanges();
        return 0;
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ThrowOnSaveChanges();
        return Task.FromResult(0);
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ThrowOnSaveChanges();
        return Task.FromResult(0);
    }

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSnakeCaseNamingConvention();
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureDecimalPrecisionAndEnumConversion(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CqrsReadDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    private static void ThrowOnSaveChanges()
    {
        throw new InvalidOperationException("SaveChanges cannot be called on read context.");
    }

    /// <summary>
    /// Configures the precision of decimal properties and converts enum properties to string.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    private static void ConfigureDecimalPrecisionAndEnumConversion(ModelBuilder modelBuilder)
    {
        modelBuilder.UseIdentityColumns();

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if ((property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
                    && property.FindAnnotation("Relational:ColumnType") == null)
                {
                    modelBuilder.Entity(entityType.Name).Property(property.Name).HasColumnType("numeric(9,3)");
                }

                if (property.ClrType.IsEnum)
                {
                    modelBuilder.Entity(entityType.Name).Property(property.Name).HasConversion<string>();
                }
            }
        }
    }
}
