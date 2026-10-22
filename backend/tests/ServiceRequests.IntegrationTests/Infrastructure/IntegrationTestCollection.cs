namespace ServiceRequests.IntegrationTests.Infrastructure;

/// <summary>
/// All integration tests share one API host and one database, so they run sequentially in this collection.
/// </summary>
[CollectionDefinition(Name)]
public class IntegrationTestCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "Integration";
}
