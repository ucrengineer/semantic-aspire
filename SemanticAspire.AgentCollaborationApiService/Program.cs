using Microsoft.SemanticKernel.Experimental.Agents;
using Microsoft.SemanticKernel;
using SemanticAspire.Shared.Plugins;
using SemanticAspire.Shared;
using Pipelines.Sockets.Unofficial.Arenas;
using System.Text;
using Azure;

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
var copyWriterAgent = await new AgentBuilder()
             .WithOpenAIChatCompletion(
                 builder.Configuration.GetSection(nameof(OpenAIConfig))["ChatModelId"]!,
                 builder.Configuration.GetSection(nameof(OpenAIConfig))["ApiKey"]!)
                    .WithInstructions("You are a copywriter with ten years of experience and are known for brevity and a dry humor. You're laser focused on the goal at hand. Don't waste time with chit chat. The goal is to refine and decide on the single best copy as an expert in the field.  Consider suggestions when refining an idea.")
                    .WithName("Copywriter")
                    .WithDescription("Copywriter")
                    .WithPlugin(null)
             .BuildAsync();

var artDirectorAgent = await new AgentBuilder()
             .WithOpenAIChatCompletion(
                 builder.Configuration.GetSection(nameof(OpenAIConfig))["ChatModelId"]!,
                 builder.Configuration.GetSection(nameof(OpenAIConfig))["ApiKey"]!)
                    .WithInstructions("You are an art director who has opinions about copywriting born of a love for David Ogilvy. The goal is to determine is the given copy is acceptable to print, even if it isn't perfect.  If not, provide insight on how to refine suggested copy without example.  Always respond to the most recent message by evaluating and providing critique without example.  Always repeat the copy at the beginning.  If copy is acceptable and meets your criteria, say: PRINT IT.")
                    .WithName("Art Director")
                    .WithDescription("Art Director")
             .BuildAsync();

builder.Services.AddActivatedKeyedSingleton<IAgent>(nameof(copyWriterAgent), (x, y) => copyWriterAgent);

builder.Services.AddActivatedKeyedSingleton<IAgent>(nameof(artDirectorAgent), (x, y) => artDirectorAgent);

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();


app.UseHttpsRedirection();

app.MapGet("/runcollaboration", async (string concept, [FromKeyedServices(nameof(copyWriterAgent))] IAgent agent, ILogger<Program> _logger) =>
{
    var response = new StringBuilder();

    var thread = await copyWriterAgent.NewThreadAsync();

    var messageUser = await thread.AddUserMessageAsync(concept);

    response.AppendLine(DisplayMessage(messageUser, _logger));

    var isComplete = false;
    try
    {
        do
        {
            var agentMessages = await thread.InvokeAsync(copyWriterAgent).ToArrayAsync();
            response.AppendLine(DisplayMessages(agentMessages, _logger, copyWriterAgent));

            agentMessages = await thread.InvokeAsync(artDirectorAgent).ToArrayAsync();

            response.AppendLine(DisplayMessages(agentMessages, _logger, artDirectorAgent));


            if (agentMessages.Any(x => x.Content.Contains("PRINT IT", StringComparison.InvariantCultureIgnoreCase)))
            {
                isComplete = true;
            }
        }
        while (!isComplete);
    }

    finally
    {
    }

    return Results.Text(response.ToString());
})
    .WithName("agentcollaborationapiservice")
    .WithDescription("agents collaboration")
    .WithOpenApi();

app.Run();
 string DisplayMessages(IEnumerable<IChatMessage> messages,ILogger<Program> _logger, IAgent? agent = null)
{
    var response = new StringBuilder();
    foreach (var message in messages)
    {

        response.AppendLine(DisplayMessage(message, _logger, agent));
    }

    return response.ToString();
}

string DisplayMessage(IChatMessage message,ILogger<Program> _logger, IAgent? agent = null)
{
    _logger.LogInformation($"[{message.Id}]");
    if (agent != null)
    {
        _logger.LogInformation($"# {message.Role}: ({agent.Name}) {message.Content}");
        return $"# {message.Role}: ({agent.Name}) {message.Content}";
    }
    else
    {
        _logger.LogInformation($"# {message.Role}: {message.Content}");
        return $"# {message.Role}: {message.Content}";
    }
}