using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Cqrs.Api.Common.BaseRequests;
using Cqrs.Api.Common.DataAccess.Persistence;
using Cqrs.Api.Common.DataAccess.Repositories;
using Cqrs.Api.UseCases.Articles.Errors;
using Cqrs.Api.UseCases.Articles.Persistence.Entities;
using Cqrs.Api.UseCases.Attributes.Common.Errors;
using Cqrs.Api.UseCases.Attributes.Common.Models;
using Cqrs.Api.UseCases.Attributes.Common.Persistence.Entities;
using Cqrs.Api.UseCases.Attributes.Common.Persistence.Entities.AttributeValues;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Attribute = Cqrs.Api.UseCases.Attributes.Common.Persistence.Entities.Attribute;

namespace Cqrs.Api.UseCases.Attributes.Commands.UpdateAttributeValues;

/// <summary>
/// Handles the attribute requests.
/// </summary>
[SuppressMessage("Design", "MA0042:Do not use blocking calls in an async method", Justification = "The task is awaited by Task.WhenAll().")]
public class UpdateAttributeValuesCommandHandler(
    CqrsWriteDbContext _dbContext,
    ICachedReadRepository<AttributeMapping> _attributeMappingReadRepository) : IRequestHandler<UpdateAttributeValuesCommand, ErrorOr<Updated>>
{
    private static readonly string[] TrueStringArray = ["true"];

    /// <summary>
    /// Handles the request to update the attribute values of an article.
    /// </summary>
    /// <param name="command">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An <see cref="ErrorOr.Error"/> or a <see cref="Updated"/> result.</returns>
    public async Task<ErrorOr<Updated>> Handle(UpdateAttributeValuesCommand command, CancellationToken cancellationToken)
    {
        // 1. Fetch the article DTOs
        var dtoOrError = await GetArticleDtosAndMappedCategoryIdAsync(command);

        if (dtoOrError.IsError)
        {
            return dtoOrError.Errors;
        }

        var (articleDtos, _) = dtoOrError.Value;

        // 3. Get the attribute ids for the new true boolean values
        var attributeIdsForNewTrueValues = command.NewAttributeValues
            .Where(value => value.InnerValues.TrueForAll(innerValue =>
                innerValue.Values.SequenceEqual(TrueStringArray, StringComparer.OrdinalIgnoreCase)))
            .Select(value => value.AttributeId)
            .ToList();

        // 4. Get the marketplace attribute ids for the root attributes with new true boolean values
        var productTypeMpIdsWithNewTrueValues = await GetProductTypeMpIdsByAttributeIds(attributeIdsForNewTrueValues).ToListAsync();

        // If there are no or more than one marketplace attribute ids for the root attributes with new true boolean values, return an error
        if (productTypeMpIdsWithNewTrueValues.Count is not 1)
        {
            return productTypeMpIdsWithNewTrueValues.Count is 0
                ? AttributeErrors.NotEnoughValues(
                    command.NewAttributeValues[0].AttributeId,
                    productTypeMpIdsWithNewTrueValues.Count,
                    1)
                : AttributeErrors.TooManyValues(
                command.NewAttributeValues[0].AttributeId,
                productTypeMpIdsWithNewTrueValues.Count,
                1);
        }

        // 5. Get the attributes for the new attribute values
        var receivedAttributeIds = command.NewAttributeValues.Select(value => value.AttributeId).ToList();

        var attributes = await GetAttributesWithSubAttributesByIdOrMpIdAndByRootCategoryId(
                productTypeMpIdsWithNewTrueValues.Single(),
                receivedAttributeIds,
                command.RootCategoryId)
            .ToListAsync();

        // 6. Validate the new attribute values and get the articles in parallel
        var attributeMappings = await _attributeMappingReadRepository.GetAllAsync();

        var validationTask = NewAttributeValueValidationService.ValidateAttributes(
            command.ArticleNumber,
            attributes,
            command.NewAttributeValues.ToList(),
            articleDtos,
            attributeMappings);

        var articlesTask = GetByNumberWithAttributeValuesByRootCategoryId(
            command.ArticleNumber,
            command.RootCategoryId)
            .ToListAsync()
            .AsTask();

        var allTask = Task.WhenAll(validationTask, articlesTask);
        await allTask;

        if (!allTask.IsCompletedSuccessfully)
        {
            throw new InvalidOperationException("Either the validation or the article task failed.");
        }

        var articles = articlesTask.Result;
        var validationErrors = validationTask.Result.ErrorsOrEmptyList;

        if (validationErrors.Count is not 0)
        {
            return validationErrors;
        }

        // 7. Remove the old attribute values and add the new attribute values to the articles
        RemoveAttributeValuesFromArticle(articles);
        AddNewAttributeValuesToArticles(command.NewAttributeValues, articles, attributes);

        // 8. Save the changes and return the updated result
        await _dbContext.SaveChangesAsync();
        return Result.Updated;
    }

    private static void RemoveAttributeValuesFromArticle(List<Article> articles)
    {
        foreach (var article in articles)
        {
            article.AttributeBooleanValues!.Clear();

            article.AttributeDecimalValues!.Clear();

            article.AttributeIntValues!.Clear();

            article.AttributeStringValues!.Clear();
        }
    }

    private static void AddNewAttributeValuesToArticles(
        NewAttributeValue[] newAttributeValues,
        List<Article> articles,
        IReadOnlyCollection<Attribute> attributes)
    {
        foreach (var newAttributeValue in newAttributeValues)
        {
            var attribute = attributes.First(attribute => attribute.Id == newAttributeValue.AttributeId);

            if (attribute.SubAttributes!.Count != 0 && attribute.ParentAttribute != null)
            {
                continue;
            }

            AddNewInnerAttributeValuesToArticles(articles, newAttributeValue, attribute);
        }
    }

    private static void AddNewInnerAttributeValuesToArticles(List<Article> articles, NewAttributeValue newAttributeValue, Attribute attribute)
    {
        foreach (var newInnerValue in newAttributeValue.InnerValues)
        {
            var article = articles.Find(a => a.CharacteristicId == newInnerValue.CharacteristicId)!;

            switch (attribute.ValueType)
            {
                case AttributeValueType.Boolean:
                    article.AttributeBooleanValues!.AddRange(newInnerValue.Values.Select(value =>
                        new AttributeBooleanValue(bool.Parse(value)) { Attribute = attribute }));
                    break;
                case AttributeValueType.Decimal:
                    article.AttributeDecimalValues!.AddRange(newInnerValue.Values.Select(value =>
                        new AttributeDecimalValue(decimal.Parse(value, CultureInfo.InvariantCulture)) { Attribute = attribute }));
                    break;
                case AttributeValueType.Int:
                    article.AttributeIntValues!.AddRange(newInnerValue.Values.Select(value =>
                        new AttributeIntValue(int.Parse(value, CultureInfo.InvariantCulture)) { Attribute = attribute }));
                    break;
                case AttributeValueType.String:
                    article.AttributeStringValues!.AddRange(newInnerValue.Values.Select(value =>
                        new AttributeStringValue(value) { Attribute = attribute }));
                    break;
                default:
                    throw new NotSupportedException();
            }
        }
    }

    /// <summary>
    /// Get the article DTOs and the mapped category id for the requested article number.
    /// </summary>
    /// <param name="query">The request.</param>
    /// <returns>A <see cref="ErrorOr.Error"/> or a tuple of the article DTOs and the mapped category id.</returns>
    private async Task<ErrorOr<(List<ArticleDto>, int CategoryId)>> GetArticleDtosAndMappedCategoryIdAsync(BaseQuery query)
    {
        var articleDtos = await _dbContext.Articles
            .Where(a => a.ArticleNumber == query.ArticleNumber)
            .Select(article => new ArticleDto(article.Id, article.CharacteristicId))
            .ToListAsync();

        if (articleDtos.Count == 0)
        {
            return ArticleErrors.ArticleNotFound(query.ArticleNumber);
        }

        var mappedCategoryId = await _dbContext.Categories
            .Where(category => category.RootCategoryId == query.RootCategoryId && category.Articles!.Any(article => article.ArticleNumber == query.ArticleNumber))
            .Select(category => (int?)category.Id)
            .SingleOrDefaultAsync();

        if (mappedCategoryId is null)
        {
            return ArticleErrors.MappedCategoriesForArticleNotFound(query.ArticleNumber, query.RootCategoryId);
        }

        return (articleDtos, mappedCategoryId.Value);
    }

    /// <summary>
    /// Gets the articles by the article number with all attribute values.
    /// </summary>
    /// <param name="articleNumber">The article number to search for.</param>
    /// <param name="rootCategoryId">The root category id to search for.</param>
    /// <returns>An <see cref="IAsyncEnumerable{Article}"/> of <see cref="Article"/>s.</returns>
    private IAsyncEnumerable<Article> GetByNumberWithAttributeValuesByRootCategoryId(string articleNumber, int rootCategoryId)
    {
        return _dbContext.Articles
            .Where(a => a.ArticleNumber == articleNumber)
            .Include(article => article.AttributeBooleanValues!.Where(value => value.Attribute!.RootCategoryId == rootCategoryId))
            .Include(article => article.AttributeDecimalValues!.Where(value => value.Attribute!.RootCategoryId == rootCategoryId))
            .Include(article => article.AttributeIntValues!.Where(value => value.Attribute!.RootCategoryId == rootCategoryId))
            .Include(article => article.AttributeStringValues!.Where(value => value.Attribute!.RootCategoryId == rootCategoryId))
            .ToAsyncEnumerable();
    }

    /// <summary>
    /// Gets the attributes with sub-attributes by the given <paramref name="productTypeMpId"/>, <paramref name="attributeIds"/> and <paramref name="rootCategoryId"/>.
    /// </summary>
    /// <param name="productTypeMpId">The product type marketplace ids to get the attributes for.</param>
    /// <param name="attributeIds">The attribute ids to get the attributes for.</param>
    /// <param name="rootCategoryId">The root category id to get the attributes for.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> of <see cref="Attribute"/>s.</returns>
    private IAsyncEnumerable<Attribute> GetAttributesWithSubAttributesByIdOrMpIdAndByRootCategoryId(string productTypeMpId, IEnumerable<int> attributeIds, int rootCategoryId)
    {
        return _dbContext.Attributes
            .Where(attribute =>
                attribute.RootCategoryId == rootCategoryId
                && (attributeIds.Contains(attribute.Id)
                    || attribute.ProductType == productTypeMpId))
            .Include(a => a.SubAttributes)
            .ToAsyncEnumerable();
    }

    /// <summary>
    /// Gets the product type marketplace ids by the given <paramref name="attributeIds"/>.
    /// </summary>
    /// <param name="attributeIds">The attribute ids to get the product type marketplace ids for.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> of product type marketplace ids.</returns>
    private IAsyncEnumerable<string> GetProductTypeMpIdsByAttributeIds(IEnumerable<int> attributeIds)
    {
        return _dbContext.Attributes
            .Where(attribute => attributeIds.Contains(attribute.Id) && attribute.ParentAttributeId == null)
            .Select(attribute => attribute.MarketplaceAttributeIds)
            .ToAsyncEnumerable();
    }
}
