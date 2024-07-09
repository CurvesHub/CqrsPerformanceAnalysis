using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Cqrs.Api.Common.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cqrs.Api.Common.Extensions;

/// <summary>
/// Contains extension methods for recursive queries via recursive common table expressions.
/// </summary>
public static class RecursiveQueryExtensions // Optional: Implement Benchmarks for performance comparison with other methods
{
    /// <summary>
    /// Returns an IQueryable with a recursive query on the given <paramref name="dbSet"/> via a recursive common table expression.
    /// Works by manually creating the query string for a recursive common table expression. Performance is dependent on the database provider.
    /// Scales well with large and complex data structures.
    /// </summary>
    /// <param name="dbSet">The DbSet to create the recursive query on.</param>
    /// <param name="initialFilter">The filter for the initial-query-part of the recursive query.</param>
    /// <param name="navigationProperty">The navigation property of the recursive relationship.</param>
    /// <typeparam name="TEntity">The type of the entity of the DbSet.</typeparam>
    /// <typeparam name="TNavigationProperty">The type of the navigation property of the recursive relationship. Can be <typeparamref name="TEntity"/>, a collection of <typeparamref name="TEntity"/>, or a collection of a join entity.</typeparam>
    /// <returns>An IQueryable with a recursive query on the given <paramref name="dbSet"/> via a recursive common table expression.</returns>
    /// <exception cref="ArgumentException">If the given navigation property does not belong to a valid recursive relationship.</exception>
    public static IQueryable<TEntity> RecursiveCteQuery<TEntity, TNavigationProperty>(
        this DbSet<TEntity> dbSet,
        Expression<Func<TEntity, bool>> initialFilter,
        Expression<Func<TEntity, TNavigationProperty?>> navigationProperty)
        where TEntity : BaseEntity
    {
        var entityType = dbSet.EntityType;

        if (entityType is null)
        {
            throw new ArgumentException($"'{typeof(TEntity).Name}' is not a database entity!", paramName: nameof(dbSet));
        }

        if (!navigationProperty.VerifyRecursiveNavigation(entityType, out var foreignKey, out var inverseForeignKey, out var navigation))
        {
            throw new ArgumentException($"{navigationProperty} does not point to a valid recursive navigation property!", paramName: nameof(navigationProperty));
        }

        var (baseQueryString, parameters) = dbSet.Where(initialFilter).GetQueryParametersAndQueryString();
        var quotes = ('"', '"');
        var quotedCte = "recursiveCte".Quote(quotes);
        var tableName = entityType.GetTableName()!.Quote(quotes);
        var cteColumnNames = entityType.GetProperties().GetColumnNames(quotes);
        var tableColumnNames = entityType.GetProperties().GetColumnNames(quotes, tableName);

        string keyComparison;
        string finalColumnNames;
        var primaryKeyColumns = new List<string>();
        var finalJoin = string.Empty;

        // The inverse foreign key only exists for many-to-many relationships
        if (inverseForeignKey is null)
        {
            keyComparison = foreignKey.GetNavigation(pointsToPrincipal: true) == navigation
                ? foreignKey.GetComparison(quotes, tableName, quotedCte, false)
                : foreignKey.GetComparison(quotes, quotedCte, tableName);
            finalColumnNames = cteColumnNames;
        }
        else
        {
            var joinTableName = inverseForeignKey.DeclaringEntityType.GetTableName()!.Quote(quotes);
            keyComparison = foreignKey.GetComparison(quotes, quotedCte, joinTableName);
            finalColumnNames = tableColumnNames;
            tableColumnNames = inverseForeignKey.Properties.GetColumnNames(quotes, joinTableName);

            var primaryKey = entityType.FindPrimaryKey()!;
            cteColumnNames = primaryKey.Properties.GetColumnNames(quotes);

            var keyColumnNames = primaryKey.Properties.Select(property => property.GetColumnName().Quote(quotes)).ToList();

            finalJoin = Compare(quotedCte, tableName, true, keyColumnNames, keyColumnNames);

            primaryKeyColumns = primaryKey.Properties.Select(property => property.GetColumnName().Quote(quotes)).ToList();
        }

        baseQueryString = ProcessInitialFilter(baseQueryString, primaryKeyColumns, parameters, out var orderedParameters);

        var queryString = $"SELECT * FROM (WITH RECURSIVE {quotedCte} ({cteColumnNames}) AS ({baseQueryString} UNION SELECT {tableColumnNames} FROM {quotedCte} {keyComparison}) SELECT {finalColumnNames} FROM {quotedCte} {finalJoin}) t";

        return dbSet.FromSqlRaw(queryString, orderedParameters);
    }

    /// <summary>
    /// Returns <paramref name="initialFilterString"/>, without parameter declarations.
    /// Also removes all non-primary-key columns from the SELECT part of the string, if <paramref name="primaryKeyColumns"/> is not empty.
    /// </summary>
    /// <param name="initialFilterString">The initial filter string.</param>
    /// <param name="primaryKeyColumns">The primary key columns.</param>
    /// <param name="parameters">The parameters.</param>
    /// <param name="orderedParameters">The ordered parameters.</param>
    /// <returns>The processed initial filter string.</returns>
    private static string ProcessInitialFilter(string initialFilterString, List<string> primaryKeyColumns, IReadOnlyDictionary<string, object> parameters, out object[] orderedParameters)
    {
        const string splitQueryComment = "This LINQ query is being executed in split-query mode, and the SQL shown is for the first query to be executed. Additional queries may also be executed depending on the results of the first query.";

        var statements = initialFilterString.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        var query = statements.SkipWhile(s => !s.TrimStart().StartsWith("SELECT", StringComparison.InvariantCulture));
        query = query.Where(part => !part.Contains(splitQueryComment, StringComparison.Ordinal));
        initialFilterString = string.Join(' ', query.Select(s => s.Trim()));
        orderedParameters = new object[parameters.Count];

        foreach (var ((name, value), index) in parameters.Select((pair, index) => (pair, index)))
        {
            var param = "{" + index + "}";
            initialFilterString = initialFilterString.Replace('@' + name, param, StringComparison.Ordinal).Replace(name, param, StringComparison.Ordinal);
            orderedParameters[index] = value;
        }

        if (primaryKeyColumns.Count != 0)
        {
            var split = initialFilterString.Split("FROM ", 2);
            var select = split[0];
            var fromWhere = split[1];

            var selectedColumns = select[6..].Split(", ");
            var correctColumns = selectedColumns.Join(
                primaryKeyColumns,
                s => '"' + s.Split("." + '"', 2)[^1],
                s => s,
                (s, _) => s.Trim(),
                StringComparer.OrdinalIgnoreCase);

            initialFilterString = "SELECT " + string.Join(", ", correctColumns) + " FROM " + fromWhere;
        }

        return initialFilterString.Trim();
    }

    /// <summary>
    /// Returns whether <paramref name="navigationProperty"/> points to a navigation property of a recursive relationship.
    /// </summary>
    /// <param name="navigationProperty">The expression to be verified.</param>
    /// <param name="entityType">The corresponding <see cref="IEntityType"/> for <typeparamref name="TEntity"/>.</param>
    /// <param name="foreignKey">The foreign key of the relationship represented by the navigation property.</param>
    /// <param name="inverseForeignKey">For m:n relationships, the foreign key from the join table back to the actual table.</param>
    /// <param name="navigation">The <see cref="INavigationBase"/> for the navigation property.</param>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <typeparam name="TProperty">The type of the navigation property.</typeparam>
    /// <returns>A boolean indicating whether or not <paramref name="navigationProperty"/> points to a navigation property of a recursive relationship.</returns>
    /// <exception cref="ArgumentException">If the <paramref name="navigationProperty"/> does not refer to a property.</exception>
    private static bool VerifyRecursiveNavigation<TEntity, TProperty>(
        this Expression<Func<TEntity, TProperty?>> navigationProperty,
        IEntityType entityType,
        [MaybeNullWhen(false)] out IForeignKey foreignKey,
        out IForeignKey? inverseForeignKey,
        [MaybeNullWhen(false)] out INavigationBase navigation)
        where TEntity : class
    {
        foreignKey = null;
        inverseForeignKey = null;

        if (navigationProperty.Body is not MemberExpression { Member: PropertyInfo propertyInfo })
        {
            throw new ArgumentException($"{navigationProperty} does not refer to a property", nameof(navigationProperty));
        }

        navigation = entityType.FindNavigation(propertyInfo.Name) is null
            ? entityType.FindSkipNavigation(propertyInfo.Name)
            : entityType.FindNavigation(propertyInfo.Name);

        if (navigation is null)
        {
            return false;
        }

        foreignKey = navigation switch
        {
            INavigation takeNavigation => takeNavigation.ForeignKey,
            ISkipNavigation skipNavigation => skipNavigation.ForeignKey,
            _ => throw new ArgumentException("Invalid navigation property!", paramName: nameof(navigation))
        };

        var principalEntityType = foreignKey.DeclaringEntityType == entityType ? foreignKey.PrincipalEntityType : foreignKey.DeclaringEntityType;

        if (principalEntityType == entityType)
        {
            return true;
        }

        // Out-parameters should not be captured in lambdas
        var localForeignKey = foreignKey;
        inverseForeignKey = principalEntityType.GetForeignKeys()
            .FirstOrDefault(key => key != localForeignKey && key.PrincipalEntityType == entityType);

        return inverseForeignKey is not null;
    }

    /// <summary>
    /// Returns the query string of the given <see cref="IQueryable"/> and the values of the constant parameters.
    /// </summary>
    /// <param name="queryable">The queryable to get the query string and the values of the constant parameters from.</param>
    /// <returns>The query string and the extracted parameters.</returns>
    private static (string Query, IReadOnlyDictionary<string, object> Parameters) GetQueryParametersAndQueryString(this IQueryable queryable)
    {
#pragma warning disable EF1001
        var queryCompiler = (queryable.Provider.GetType()
            .GetRuntimeFields()
            .First(field1 => string.Equals(field1.Name, "_queryCompiler", StringComparison.Ordinal))
            .GetValue(queryable.Provider) as QueryCompiler)!;

        // Preserve the actual query context factory
        var factoryField = queryCompiler.GetType().GetRuntimeFields().First(field =>
            string.Equals(field.Name, "_queryContextFactory", StringComparison.Ordinal));

        var relationalQueryContextFactory = (factoryField.GetValue(queryCompiler) as RelationalQueryContextFactory)!;

        // Create a parameter preserving query string factory based on the actual factory
        var relationalQueryContextDependencies = (relationalQueryContextFactory.GetType().GetRuntimeProperties()
            .First(property => string.Equals(property.Name, "RelationalDependencies", StringComparison.Ordinal))
            .GetValue(relationalQueryContextFactory) as RelationalQueryContextDependencies)!;

        var parameterPreservingQueryStringFactory = new ParameterPreservingQueryStringFactory(relationalQueryContextDependencies.RelationalQueryStringFactory);

        // Create a temporary query context factory containing the string factory
        var tempFactory = new RelationalQueryContextFactory(
            (relationalQueryContextFactory.GetType()
                .GetRuntimeProperties()
                .First(property => string.Equals(property.Name, "Dependencies", StringComparison.Ordinal))
                .GetValue(relationalQueryContextFactory) as QueryContextDependencies)!,
            new RelationalQueryContextDependencies(
                (relationalQueryContextDependencies.GetType()
                    .GetRuntimeProperties()
                    .First(property => string.Equals(property.Name, "RelationalConnection", StringComparison.Ordinal))
                    .GetValue(relationalQueryContextDependencies) as IRelationalConnection)!,
                parameterPreservingQueryStringFactory));

        // Set the temporary factory and create the query string
        factoryField.SetValue(queryCompiler, tempFactory);

        var queryString = queryable.ToQueryString();

        // Set the actual factory again
        factoryField.SetValue(queryCompiler, relationalQueryContextFactory);

        // The query parameters will be preserved by the query string factory
        return (queryString, parameterPreservingQueryStringFactory.Parameters!);
#pragma warning restore EF1001
    }

    /// <summary>
    /// Returns a comparison between all the columns of a foreign key and a primary key.
    /// </summary>
    private static string GetComparison(this IForeignKey foreignKey, (char startQuoteChar, char endQuoteChar) quotes, string primaryKeyTable, string foreignKeyTable, bool joinOnForeignKey = true)
    {
        var otherKeyColumnNames = foreignKey.PrincipalKey.Properties.Select(property => property.GetColumnName().Quote(quotes));
        var foreignKeyColumnNames = foreignKey.Properties.Select(property => property.GetColumnName().Quote(quotes));

        return Compare(primaryKeyTable, foreignKeyTable, joinOnForeignKey, otherKeyColumnNames, foreignKeyColumnNames);
    }

    /// <summary>
    /// Returns a comparison between the given columns.
    /// </summary>
    private static string Compare(string keyTable, string otherKeyTable, bool joinOnForeignKey, IEnumerable<string> keyColumnNames, IEnumerable<string> otherKeyColumnNames)
    {
        var foreignKeyComparison = string.Join(" AND ", keyColumnNames.Zip(otherKeyColumnNames)
            .Select(tuple => $"{keyTable}.{tuple.First} = {otherKeyTable}.{tuple.Second}"));

        return $"JOIN {(joinOnForeignKey ? otherKeyTable : keyTable)} ON {foreignKeyComparison}";
    }

    /// <summary>
    /// Returns the column names of the given <paramref name="properties"/>, comma separated, with <paramref name="tableName"/> as prefix, if it is not null.
    /// </summary>
    private static string GetColumnNames(this IEnumerable<IProperty> properties, (char startQuoteChar, char endQuoteChar) quotes, string? tableName = null)
    {
        var prefix = tableName is null ? string.Empty : tableName + ".";
        return string.Join(',', properties.Select(property => prefix + property.GetColumnName().Quote(quotes)));
    }

    /// <summary>
    /// Returns <paramref name="s"/>, enclosed in <paramref name="quotes"/>.
    /// </summary>
    private static string Quote(this string s, (char startQuoteChar, char endQuoteChar) quotes)
    {
        var (startQuoteChar, endQuoteChar) = quotes;
        return (startQuoteChar + s + endQuoteChar).Trim();
    }

    private sealed class ParameterPreservingQueryStringFactory(IRelationalQueryStringFactory baseFactory)
        : IRelationalQueryStringFactory
    {
        public Dictionary<string, object>? Parameters { get; private set; }

        public string Create(DbCommand command)
        {
            Parameters = command.Parameters.Cast<DbParameter>()
                .ToDictionary(parameter => parameter.ParameterName, parameter => parameter.Value!, StringComparer.OrdinalIgnoreCase);
            return baseFactory.Create(command);
        }
    }
}
