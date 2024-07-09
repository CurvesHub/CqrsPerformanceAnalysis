using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Cqrs.Api.Common.BaseRequests;
using Cqrs.Api.UseCases.Articles.Errors;
using Cqrs.Api.UseCases.Articles.Persistence.Entities;
using Cqrs.Api.UseCases.Articles.Persistence.Repositories;
using Cqrs.Api.UseCases.Attributes.Common.Errors;
using Cqrs.Api.UseCases.Attributes.Common.Models;
using Cqrs.Api.UseCases.Attributes.Common.Persistence.Entities.AttributeValues;
using Cqrs.Api.UseCases.Attributes.Common.Persistence.Repositories;
using Cqrs.Api.UseCases.Categories.Common.Persistence.Repositories;
using ErrorOr;
using Attribute = Cqrs.Api.UseCases.Attributes.Common.Persistence.Entities.Attribute;

namespace Cqrs.Api.UseCases.Attributes.Commands.UpdateAttributeValues;

/// <summary>
/// Handles the attribute requests.
/// </summary>
[SuppressMessage("Design", "MA0042:Do not use blocking calls in an async method", Justification = "The task is awaited by Task.WhenAll().")]
public class UpdateAttributeValuesCommandHandler(
    ICategoryWriteRepository _categoryWriteRepository,
    NewAttributeValueValidationService _validationService,
    IArticleWriteRepository _articleWriteRepository,
    IAttributeWriteRepository _attributeWriteRepository,
    IServiceProvider _serviceProvider)
{
    private static readonly string[] TrueStringArray = ["true"];

    /// <summary>
    /// Handles the request to update the attribute values of an article.
    /// </summary>
    /// <param name="command">The request.</param>
    /// <returns>An <see cref="ErrorOr.Error"/> or a <see cref="Updated"/> result.</returns>
    public async Task<ErrorOr<Updated>> UpdateAttributeValuesAsync(
        UpdateAttributeValuesCommand command)
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
        var productTypeMpIdsWithNewTrueValues = await _attributeWriteRepository
            .GetProductTypeMpIdsByAttributeIds(attributeIdsForNewTrueValues)
            .ToListAsync();

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

        var attributes = await _attributeWriteRepository
            .GetAttributesWithSubAttributesByIdOrMpIdAndByRootCategoryId(
                productTypeMpIdsWithNewTrueValues.Single(),
                receivedAttributeIds,
                command.RootCategoryId)
            .ToListAsync();

        // 6. Validate the new attribute values and get the articles in parallel
        var validationTask = _validationService.ValidateAttributes(
            command.ArticleNumber,
            attributes,
            command.NewAttributeValues.ToList(),
            articleDtos);

        // We need to create a new scope to avoid the DbContext being shared between tasks (threads) since the article repository is also used indirectly by the validation service
        await using var scope = _serviceProvider.CreateAsyncScope();
        var secondArticleWriteRepository = scope.ServiceProvider.GetRequiredService<IArticleWriteRepository>();

        if (ReferenceEquals(secondArticleWriteRepository, _articleWriteRepository))
        {
            throw new InvalidOperationException("The article repository is not registered as a scoped/transient service. Therefore task parallelism is not possible.");
        }

        var articlesTask = secondArticleWriteRepository.GetByNumberWithAttributeValuesByRootCategoryId(
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
        await secondArticleWriteRepository.SaveChangesAsync();
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
        var articleDtos = await _articleWriteRepository.GetArticleDtos(query.ArticleNumber).ToListAsync();

        if (articleDtos.Count == 0)
        {
            return ArticleErrors.ArticleNotFound(query.ArticleNumber);
        }

        var mappedCategoryId = await _categoryWriteRepository.GetMappedCategoryIdByRootCategoryId(query.ArticleNumber, query.RootCategoryId);

        if (mappedCategoryId is null)
        {
            return ArticleErrors.MappedCategoriesForArticleNotFound(query.ArticleNumber, query.RootCategoryId);
        }

        return (articleDtos, mappedCategoryId.Value);
    }
}
