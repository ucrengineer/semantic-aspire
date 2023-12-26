using Microsoft.SemanticKernel.Connectors.Postgres;
using Microsoft.SemanticKernel.Memory;
using Npgsql;
using Microsoft.Extensions.Configuration;

namespace SemanticAspire.Shared;
public static class MemoryStoresExtensions
{
    //private static async Task<IMemoryStore> CreateSampleRedisMemoryStoreAsync(string connectionstring)
    //{
    //    string configuration = TestConfiguration.Redis.Configuration;
    //    ConnectionMultiplexer connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(configuration);
    //    IDatabase database = connectionMultiplexer.GetDatabase();
    //    IMemoryStore store = new RedisMemoryStore(database, vectorSize: 1536);
    //    return store;
    //}

#pragma warning disable SKEXP0003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    public static IMemoryStore CreateSamplePostgresMemoryStore(IConfiguration config)
#pragma warning restore SKEXP0003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    {
        NpgsqlDataSourceBuilder dataSourceBuilder = new(config.GetConnectionString("vectordb"));
        dataSourceBuilder.UseVector();
        NpgsqlDataSource dataSource = dataSourceBuilder.Build();
#pragma warning disable SKEXP0003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0032 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        IMemoryStore store = new PostgresMemoryStore(dataSource, vectorSize: 1536, schema: "public");
#pragma warning restore SKEXP0032 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning restore SKEXP0003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        return store;
    }
}
