using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.TextToImage;
using SemanticAspire.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

builder.Services.Configure<OpenAIConfig>
    (builder.Configuration.GetSection(nameof(OpenAIConfig)));
var app = builder.Build();

app.MapDefaultEndpoints();

var openAiConfig = app.Services.GetRequiredService<IOptions<OpenAIConfig>>();

#pragma warning disable SKEXP0012 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
var kernel = Kernel.CreateBuilder()
    .AddOpenAITextToImage(openAiConfig.Value.ApiKey)
    .AddOpenAIChatCompletion(openAiConfig.Value.ChatModelId, openAiConfig.Value.ApiKey)
    .Build();
#pragma warning restore SKEXP0012 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

#pragma warning disable SKEXP0002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
app.MapGet("/example1/dallE", async (string text,HttpClient client, CancellationToken cancellationToken)=>
{
    var dallE = kernel.GetRequiredService<ITextToImageService>();

    var image = await dallE.GenerateImageAsync(text,256, 256, cancellationToken: cancellationToken);

    var result = await client.GetAsync(image, cancellationToken: cancellationToken);

    return Results.Ok(image);

})
    .WithName("example/dalleE")
    .WithDescription("Generates an image from text using the dallE model")
    .WithOpenApi();

#pragma warning restore SKEXP0002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
app.Run();