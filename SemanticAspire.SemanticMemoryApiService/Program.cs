using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SemanticAspire.Shared;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.AddNpgsqlDbContext<ApplicationDbContext>("vectordb", configureDbContextOptions: dbContextOptionsBuilder =>
{
    dbContextOptionsBuilder.UseNpgsql(builder =>
    {
        builder.UseVector();
    });
});

builder.Services.Configure<OpenAIConfig>
    (builder.Configuration.GetSection(nameof(OpenAIConfig)));
var app = builder.Build();

var db = app.Services.CreateScope().ServiceProvider.GetRequiredService<ApplicationDbContext>();
db.Database.EnsureCreated();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
var openAiConfig = app.Services.GetRequiredService<IOptions<OpenAIConfig>>();

ArgumentException.ThrowIfNullOrEmpty(openAiConfig.Value.ApiKey, nameof(openAiConfig.Value.ApiKey));
ArgumentException.ThrowIfNullOrEmpty(openAiConfig.Value.ChatModelId, nameof(openAiConfig.Value.ChatModelId));

#pragma warning disable SKEXP0011 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
var memoryWithCustomDb = new MemoryBuilder()
    .WithOpenAITextEmbeddingGeneration(
        modelId: openAiConfig.Value.ChatModelId,
        apiKey: openAiConfig.Value.ApiKey)
    .WithMemoryStore(MemoryStoresExtensions.CreateSamplePostgresMemoryStore(app.Configuration))
    .Build();

#pragma warning restore SKEXP0003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning restore SKEXP0011 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

app.MapDefaultEndpoints();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapPost("/storememory", async (ILogger<Program> _logger, CancellationToken cancellationToken) =>
{
    _logger.LogInformation("adding some github file urls and their descriptions to the semantic memory.");
    var githubFiles = GetSampleData();
    var i = 0;
    foreach (var entiry in githubFiles)
    {
        await memoryWithCustomDb
        .SaveReferenceAsync(
            collection: "SKGitHub",
            externalSourceName: "GitHub",
            externalId: entiry.Key,
            description: entiry.Value,
            text: entiry.Value);

        _logger.LogInformation("{number} saved.", ++i);
    }

    return Results.Ok("memory saved.");
})
    .WithDisplayName("Store Memory")
    .WithDescription("stores some sample data in the semantic memory.")
    .WithOpenApi();

app.MapGet("/searchMemory", async (string query, ILogger<Program> _logger, CancellationToken cancellationToken) =>
{
    _logger.LogInformation("query: {query}.", query);
    var memoryResults = memoryWithCustomDb
    .SearchAsync(collection:"SKGitHub", query, limit: 2, minRelevanceScore: 0.5, cancellationToken: cancellationToken);

    var results = new List<dynamic>();
    await foreach(var result in memoryResults)
    {
        results.Add(new { result.Metadata.Id, result.Metadata.Description, result.Relevance });
    }

    return Results.Ok(results);
})
    .WithDisplayName("Search Memory")
    .WithDescription("searches the semantic memory for the word 'installation'.")
    .WithOpenApi();
app.Run();

Dictionary<string, string> GetSampleData()
{
    return new Dictionary<string, string>
    {
        ["https://github.com/microsoft/semantic-kernel/blob/main/README.md"]
            = "README: Installation, getting started, and how to contribute",
        ["https://github.com/microsoft/semantic-kernel/blob/main/dotnet/notebooks/02-running-prompts-from-file.ipynb"]
            = "Jupyter notebook describing how to pass prompts from a file to a semantic plugin or function",
        ["https://github.com/microsoft/semantic-kernel/blob/main/dotnet/notebooks//00-getting-started.ipynb"]
            = "Jupyter notebook describing how to get started with the Semantic Kernel",
        ["https://github.com/microsoft/semantic-kernel/tree/main/samples/plugins/ChatPlugin/ChatGPT"]
            = "Sample demonstrating how to create a chat plugin interfacing with ChatGPT",
        ["https://github.com/microsoft/semantic-kernel/blob/main/dotnet/src/SemanticKernel/Memory/VolatileMemoryStore.cs"]
            = "C# class that defines a volatile embedding store",
    };
}

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasPostgresExtension("vector");
    }
}