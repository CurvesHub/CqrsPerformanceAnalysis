namespace Cqrs.Tests.TestCommon.BaseTest;

/// <summary>
/// The shared test collection.
/// </summary>
[CollectionDefinition(nameof(SharedTraditionalApiFactoryTestCollection))]
public class SharedTraditionalApiFactoryTestCollection : ICollectionFixture<TraditionalApiFactory>;
