using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Plugins.Core;
using SemanticAspire.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<OpenAIConfig>
    (builder.Configuration.GetSection(nameof(OpenAIConfig)));
var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
var openAiConfig = app.Services.GetRequiredService<IOptions<OpenAIConfig>>();

ArgumentException.ThrowIfNullOrEmpty(openAiConfig.Value.ApiKey, nameof(openAiConfig.Value.ApiKey));
ArgumentException.ThrowIfNullOrEmpty(openAiConfig.Value.ChatModelId, nameof(openAiConfig.Value.ChatModelId));

var kernel = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion(
    modelId: openAiConfig.Value.ChatModelId,
    apiKey: openAiConfig.Value.ApiKey)
    .Build();

kernel.ImportPluginFromType<TimePlugin>("time");

const string FunctionDefinition = @"
Today is: {{time.Date}}
Current time is: {{time.Time}}

Answer to the following questions using JSON syntax, including the data used.
Is it morning, afternoon, evening, or night? (morning/afternoon/evening/night)?
Is it weekend time (weekend/not weekend)?

JSON should have properties : date, time, period, weekend";

app.MapGet("/renderedPrompt", async (CancellationToken cancellationToken) =>
{
    var promptTemplateFactory = new KernelPromptTemplateFactory();
    var promptTemplate = promptTemplateFactory.Create(new PromptTemplateConfig(FunctionDefinition));
    var renderedPrompt = await promptTemplate.RenderAsync(kernel, cancellationToken: cancellationToken);
    return renderedPrompt;
})
.WithDescription("Renders the prompt template")
.WithName("renderedprompt")
.WithOpenApi();

app.MapGet("/runPrompt", async (CancellationToken cancellationToken) =>
{
    var kindOfDay = kernel.CreateFunctionFromPrompt(FunctionDefinition, new OpenAIPromptExecutionSettings() { MaxTokens = 100 });

    var result = await kernel.InvokeAsync(kindOfDay, cancellationToken: cancellationToken);

    return result.GetValue<string>();
})
.WithDescription("Runs the prompt template")
.WithName("runprompt")
.WithOpenApi();

app.MapDefaultEndpoints();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();



app.Run();