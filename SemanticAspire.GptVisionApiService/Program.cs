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
    var chatHistory = new ChatHistory();
    chatHistory.AddSystemMessage("You are a professional chartist.");
    var prompt = @$"
            Instructions: What is the stock pattern shown in this image?
            If you don't know the intent, don't guess; instead respond with Unknown.

            Choices: triangle, rectangle, head and shoulders, inverted head and shoulders, Unknown.
            Bonus: You'll get $20 if you get this right.

            ## Examples
            User Input: image-url
            Pattern: triangle

            User Input: image-url
            Pattern: rectangle
            ## End Examples";
    chatHistory.AddSystemMessage(prompt);

    chatHistory.AddUserMessage(new ChatMessageContentItemCollection
    {
        new ImageContent(new Uri(chartImageUrl))
    });

    return Results.Ok(await _chatCompletionService.GetChatMessageContentsAsync(chatHistory));
})
    .WithDescription("Uses the OpenAI API to describe an image")
    .WithName("gptvision")
    .WithOpenApi();

app.Run();
