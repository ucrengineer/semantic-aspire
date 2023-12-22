using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Plugins.Web;
using Microsoft.SemanticKernel.Plugins.Web.Bing;
using Microsoft.SemanticKernel.Plugins.Web.Google;
using SemanticAspire.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<OpenAIConfig>
    (builder.Configuration.GetSection(nameof(OpenAIConfig)));

builder.Services.Configure<GoogleConfig>
    (builder.Configuration.GetSection(nameof(GoogleConfig)));

builder.Services.Configure<BingConfig>
    (builder.Configuration.GetSection(nameof(BingConfig)));

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
var openAiConfig = app.Services.GetRequiredService<IOptions<OpenAIConfig>>();

var bingConfig = app.Services.GetRequiredService<IOptions<BingConfig>>();

var googleConfig = app.Services.GetRequiredService<IOptions<GoogleConfig>>();

ArgumentException.ThrowIfNullOrEmpty(openAiConfig.Value.ApiKey, nameof(openAiConfig.Value.ApiKey));
ArgumentException.ThrowIfNullOrEmpty(openAiConfig.Value.ChatModelId, nameof(openAiConfig.Value.ChatModelId));
ArgumentException.ThrowIfNullOrEmpty(bingConfig.Value.ApiKey, nameof(bingConfig.Value.ApiKey));
ArgumentException.ThrowIfNullOrEmpty(googleConfig.Value.ApiKey, nameof(googleConfig.Value.ApiKey));
ArgumentException.ThrowIfNullOrEmpty(googleConfig.Value.SearchEngineId, nameof(googleConfig.Value.SearchEngineId));


var kernel = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion(
    modelId: openAiConfig.Value.ChatModelId,
    apiKey: openAiConfig.Value.ApiKey)
    .Build();

#pragma warning disable SKEXP0054 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
var bingConnector = new BingConnector(bingConfig.Value.ApiKey);
var bing = new WebSearchEnginePlugin(bingConnector);
kernel.ImportPluginFromObject(bing, "bing");
#pragma warning restore SKEXP0054 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

#pragma warning disable SKEXP0054 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
using var googleConnector = new GoogleConnector(googleConfig.Value.ApiKey, googleConfig.Value.SearchEngineId);
var google = new WebSearchEnginePlugin(googleConnector);
kernel.ImportPluginFromObject(google, "google");
#pragma warning restore SKEXP0054 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapGet("/search/bing/example1", async (string question, CancellationToken cancellationToken) =>
{
    var function = kernel.Plugins["bing"]["search"];

    var result = await kernel.InvokeAsync(function, new() { ["query"] = question }, cancellationToken: cancellationToken);

    return result.GetValue<string>();
})
    .WithDescription("Searches bing for the given question")
    .WithName("bing search")
    .WithOpenApi();

app.MapGet("/search/google/example1", async (string question, CancellationToken cancellationToken) =>
{
    var function= kernel.Plugins["google"]["search"];
    var result = await kernel.InvokeAsync(function, new() { ["query"] = question }, cancellationToken: cancellationToken);
    return result.GetValue<string>();
}).WithDescription("searches google for the given question")
    .WithName("google search")
    .WithOpenApi();

app.MapGet("/search/bing/example2", async (string question, ILogger<Program> _logger, CancellationToken cancellationToken) =>
{
    var oracle = kernel.CreateFunctionFromPrompt(
        GetSemanticFunction(),
        new OpenAIPromptExecutionSettings()
        {
            MaxTokens = 150,
            Temperature = 0,
            TopP = 1
        });

    var answer = await kernel.InvokeAsync(
        oracle,
        new()
        {
            ["question"] = question,
            ["externalInformation"] = string.Empty
        }, cancellationToken: cancellationToken);

    var result = answer.GetValue<string>()!;

    if (result.Contains("bing.search", StringComparison.OrdinalIgnoreCase))
    {
        var promptTemplateFactory = new KernelPromptTemplateFactory();
        var promptTemplate = promptTemplateFactory.Create(new PromptTemplateConfig(result));

        _logger.LogInformation("-- fetching information from bing --");

        var information = await promptTemplate.RenderAsync(kernel, cancellationToken: cancellationToken);

        _logger.LogInformation("information found: {information}", information);

        answer = await kernel.InvokeAsync(
            oracle,
            new()
            {
                ["question"] = question,
                ["externalInformation"] = information
            }, cancellationToken: cancellationToken);
    }

    return answer.GetValue<string>();
}).
WithDescription("Searches bing for the given question and uses the result to answer the question if needed.")
.WithName("bing search with external information")
.WithName("bing search with external information");

app.Run();


string GetSemanticFunction() => @"Answer questions only when you know the facts or the information is provided.
When you don't have sufficient information you reply with a list of commands to find the information needed.
When answering multiple questions, use a bullet point list.
Note: make sure single and double quotes are escaped using a backslash char.

[COMMANDS AVAILABLE]
- bing.search

[INFORMATION PROVIDED]
{{ $externalInformation }}

[EXAMPLE 1]
Question: what's the biggest lake in Italy?
Answer: Lake Garda, also known as Lago di Garda.

[EXAMPLE 2]
Question: what's the biggest lake in Italy? What's the smallest positive number?
Answer:
* Lake Garda, also known as Lago di Garda.
* The smallest positive number is 1.

[EXAMPLE 3]
Question: what's Ferrari stock price? Who is the current number one female tennis player in the world?
Answer:
{{ '{{' }} bing.search ""what\\'s Ferrari stock price?"" {{ '}}' }}.
{{ '{{' }} bing.search ""Who is the current number one female tennis player in the world?"" {{ '}}' }}.

[END OF EXAMPLES]

[TASK]
Question: {{ $question }}.
Answer: ";