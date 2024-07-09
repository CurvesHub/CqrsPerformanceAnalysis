namespace Cqrs.Tests.TestCommon.BaseTest;

/// <summary>
/// The shared test collection.
/// </summary>
[CollectionDefinition(nameof(SharedCqrsApiFactoryTestCollection))]
public class SharedCqrsApiFactoryTestCollection : ICollectionFixture<CqrsApiFactory>;
