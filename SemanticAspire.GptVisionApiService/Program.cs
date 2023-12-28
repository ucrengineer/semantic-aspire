using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SemanticAspire.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<OpenAIConfig>
    (builder.Configuration.GetSection(nameof(OpenAIConfig)));

builder.Services.AddOpenAIChatCompletion(
    builder.Configuration["OpenAIConfig:ChatModelId"] ?? throw new ArgumentNullException("ChatModelId"),
    builder.Configuration["OpenAIConfig:ApiKey"] ?? throw new ArgumentNullException("apikey"));

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapGet("/example/gptvision", async (string chartImageUrl, IChatCompletionService _chatCompletionService) =>
{
    var chatHistory = new ChatHistory("You are a chartist.");

    chatHistory.AddUserMessage(new ChatMessageContentItemCollection
    {
        new TextContent("What stock pattern is being shown in this image?"),
        new ImageContent(new Uri(chartImageUrl))
    });

    return Results.Ok(await _chatCompletionService.GetChatMessageContentsAsync(chatHistory));
})
    .WithDescription("Uses the OpenAI API to describe an image")
    .WithName("gptvision")
    .WithOpenApi();

app.Run();
