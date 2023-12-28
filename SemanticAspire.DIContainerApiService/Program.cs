using Microsoft.SemanticKernel;
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

builder.Services.AddSingleton((x) =>
{
    var filePath = Path.Combine("..", "SemanticAspire.Shared", "Plugins", "TemplatesAndSettings", "SummarizePlugin");

    var kernel = new Kernel(services : x);
    kernel.ImportPluginFromPromptDirectory(filePath);

    return kernel;
});

builder.Services.AddTransient<KernelClient>();

var app = builder.Build();


app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

app.MapPost("/usingDi/summarize", async (string question, KernelClient kernelClient) =>
{
    return Results.Ok(await kernelClient.SummarizeAsync(question));
})
.WithDescription("Summarizes a question using the OpenAI API")
.WithName("summarize")
.WithOpenApi();

app.Run();


public sealed class KernelClient
{
    private readonly ILogger<KernelClient> _logger;
    private readonly Kernel _kernel;

    public KernelClient(ILogger<KernelClient> logger, Kernel kernel)
    {
        _logger = logger;
        _kernel = kernel;
    }

    public async Task<string> SummarizeAsync(string question)
    {
        var summarizePlugin = _kernel.Plugins["SummarizePlugin"];

        var result = await _kernel.InvokeAsync(summarizePlugin["Summarize"], new KernelArguments()
        {
            ["input"] = question
        });

        return result?.GetValue<string>() ?? string.Empty;

    }

}