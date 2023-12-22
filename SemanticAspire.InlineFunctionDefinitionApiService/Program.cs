using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
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

var excuseFunction = kernel.CreateFunctionFromPrompt
    (GetPromptTemplate(),
    new OpenAIPromptExecutionSettings()
    {
        MaxTokens = 100,
        Temperature = 0.4,
        TopP = 1,
    });
var fixedFunction = kernel.CreateFunctionFromPrompt(@"Translate this date {{$input}} to French format", new OpenAIPromptExecutionSettings() { MaxTokens = 100 });

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapGet("/excuseFunction", async (string input,CancellationToken cancellationToken) =>
{
    var result = await kernel.InvokeAsync(excuseFunction, new() { ["input"] = input },cancellationToken: cancellationToken);
    return result.GetValue<string>();
})
.WithName("excusefunction")
.WithDescription("Generates a creative reason or excuse for the given event.")
.WithOpenApi();

app.MapGet("/fixedFunction", async (DateTime date, CancellationToken cancellationToken) =>
{
    var result = await kernel.InvokeAsync(fixedFunction, new() { ["input"] = $"{date:f}" }, cancellationToken: cancellationToken);
    return result.GetValue<string>();
})
.WithName("fixedfunction")
.WithDescription("generates date in french format")
.WithOpenApi();


app.Run();



// Function defined using few-shot design pattern
string GetPromptTemplate() => @"
Generate a creative reason or excuse for the given event.
Be creative and be funny. Let your imagination run wild.

Event: I am running late.
Excuse: I was being held ransom by giraffe gangsters.

Event: I haven't been to the gym for a year
Excuse: I've been too busy training my pet dragon.

Event: {{$input}}
";