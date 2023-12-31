using Google.Protobuf;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Experimental.Agents;
using SemanticAspire.Shared;
using SemanticAspire.Shared.Plugins;
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

var menuAgent = await new AgentBuilder()
             .WithOpenAIChatCompletion(
                 builder.Configuration.GetSection(nameof(OpenAIConfig))["ChatModelId"]!,
                 builder.Configuration.GetSection(nameof(OpenAIConfig))["ApiKey"]!)
             .FromTemplate(File.ReadAllText(Path.Combine("..", "SemanticAspire.Shared", "Agents", "ToolAgent.yaml")))
             .WithPlugin(KernelPluginFactory.CreateFromType<MenuPlugin>())
             .BuildAsync();

var function = KernelFunctionFactory
                    .CreateFromPrompt(
    "Correct any misspellings or gramatical errors provided in the input: {{$input}}",
    functionName: "spellChecker",
    description: "Correct the spelling for the user input");

var plugin = KernelPluginFactory.CreateFromFunctions(
    "spelling",
    "Spelling functions",
    new[] { function });

var spellcheckerAgent = await new AgentBuilder()
             .WithOpenAIChatCompletion(
                 builder.Configuration.GetSection(nameof(OpenAIConfig))["ChatModelId"]!,
                 builder.Configuration.GetSection(nameof(OpenAIConfig))["ApiKey"]!)
             .FromTemplate(File.ReadAllText(Path.Combine("..", "SemanticAspire.Shared", "Agents", "ToolAgent.yaml")))
             .WithPlugin(plugin)
             .BuildAsync();

builder.Services.AddActivatedKeyedSingleton<IAgent>(nameof(parrotAgent), (x, y) => parrotAgent);

builder.Services.AddActivatedKeyedSingleton<IAgent>(nameof(menuAgent), (x, y) => menuAgent);

builder.Services.AddActivatedKeyedSingleton<IAgent>(nameof(spellcheckerAgent), (x, y) => spellcheckerAgent);

#pragma warning restore SKEXP0101 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
// Configure the HTTP request pipeline.
var app = builder.Build();


app.MapDefaultEndpoints();


app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

#pragma warning disable SKEXP0101 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
app.MapPost("/parrotagent", async (string[] messages, ILogger<Program> _logger, [FromKeyedServices(nameof(parrotAgent))] IAgent agent) =>
{
    return await ChatAsync(messages, agent, _logger);
})
    .WithName("parrotAgent/simplechat")
    .WithDescription("Chats with the parrot agent")
    .WithOpenApi();

app.MapPost("/menuagent", async (string[] messages, ILogger<Program> _logger, [FromKeyedServices(nameof(menuAgent))] IAgent agent) =>
{
    return await ChatAsync(messages, agent, _logger);

})
    .WithName("toolagent/simplechat")
    .WithDescription("Chats with the tool agent")
    .WithOpenApi();

app.MapPost("/spellcheckeragent", async (string[] messages, ILogger<Program> _logger, [FromKeyedServices(nameof(spellcheckerAgent))] IAgent agent) =>
{
    return await ChatAsync(messages, agent, _logger);
})
    .WithName("spellcheckeragent")
    .WithDescription("checks spelling")
    .WithOpenApi();
#pragma warning restore SKEXP0101 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
app.Run();

#pragma warning disable SKEXP0101 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
async Task<string> ChatAsync(string[] messages , IAgent agent, ILogger<Program> _logger)
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
}
#pragma warning restore SKEXP0101 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.