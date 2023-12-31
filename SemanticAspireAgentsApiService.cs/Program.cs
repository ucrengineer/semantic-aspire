using Microsoft.SemanticKernel.Experimental.Agents;
using SemanticAspire.Shared;
using System.Reflection.Emit;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<OpenAIConfig>
    (builder.Configuration.GetSection(nameof(OpenAIConfig)));
builder.Services.AddLogging();

#pragma warning disable SKEXP0101 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
var parrotAgent = await new AgentBuilder()
             .WithOpenAIChatCompletion(
                 builder.Configuration.GetSection(nameof(OpenAIConfig))["ChatModelId"]!,
                 builder.Configuration.GetSection(nameof(OpenAIConfig))["ApiKey"]!)
             .FromTemplate(File.ReadAllText(Path.Combine("..", "SemanticAspire.Shared", "Agents", "ParrotAgent.yaml")))
             .WithPlugin(null)
             .BuildAsync();

builder.Services.AddSingleton<IAgent>(parrotAgent);

builder.Services.AddActivatedKeyedSingleton<IAgent>("parrotAgent", (x, y) => parrotAgent);


#pragma warning restore SKEXP0101 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
// Configure the HTTP request pipeline.
var app = builder.Build();


app.MapDefaultEndpoints();


app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

#pragma warning disable SKEXP0101 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
app.MapGet("/parrotAgent/simplechat", async (string[] messages, ILogger<Program> _logger, [FromKeyedServices("parrotAgent")] IAgent agent) =>
{
    var agentResponse = new StringBuilder();
    var thread = await agent.NewThreadAsync();
    try
    {
        _logger.LogInformation("Starting conversation");
        _logger.LogInformation($"[{agent.Id}]");

        foreach (var response in messages.Select(m => thread.InvokeAsync(agent, m)))
        {
            await foreach (var message in response)
            {
                agentResponse.AppendLine($"[{message.Id}]# {message.Role}: {message.Content}");
                _logger.LogInformation($"[{message.Id}]");
                _logger.LogInformation($"# {message.Role}: {message.Content}");
            }
        }
    }
    finally
    {
        // Clean-up (storage costs $)
        // not deleting agent. it uses a in memory db.
        await Task.WhenAll(
            thread?.DeleteAsync() ?? Task.CompletedTask);
    }

    return agentResponse.ToString();
})
    .WithName("parrotAgent/simplechat")
    .WithDescription("Chats with the parrot agent")
    .WithOpenApi();
#pragma warning restore SKEXP0101 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
app.Run();