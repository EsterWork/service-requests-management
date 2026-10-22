using System.Text.Json;
using System.Text.Json.Serialization;

namespace ServiceRequests.IntegrationTests.Infrastructure;

[Collection(IntegrationTestCollection.Name)]
public abstract class IntegrationTestBase(ApiFactory factory) : IAsyncLifetime
{
    protected const string RequestsUrl = "/api/requests";

    /// <summary>Same JSON conventions as the API (camelCase, enums as strings).</summary>
    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    protected ApiFactory Factory { get; } = factory;
    protected HttpClient Client { get; } = factory.CreateClient();

    // Every test starts from an empty database and an empty cache.
    public Task InitializeAsync() => Factory.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;
}
