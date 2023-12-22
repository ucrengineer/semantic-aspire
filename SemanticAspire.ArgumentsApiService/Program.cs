using Microsoft.SemanticKernel;
using SemanticAspire.Shared.Plugins;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

var kernel = new Kernel();
var textPlugin = kernel.ImportPluginFromType<StaticTextPlugin>();

app.MapGet("/executefunctionmethod1", async (string textToAppendTo, DateTime valueOfTheDayToAppend) =>
{
    return await kernel.InvokeAsync<string>(textPlugin["AppendDay"], new KernelArguments()
    {
        ["input"] = textToAppendTo,
        ["day"] = valueOfTheDayToAppend.ToString("dddd", CultureInfo.CurrentCulture)
    });
})
.WithName("executefunctionmethod1")
.WithOpenApi();


app.MapGet("/executefunctionmethod2", async (string textToAppendTo, DateTime valueOfTheDayToAppend) =>
{
    // throws a error. system.type cannot be deserialized
    var results = await kernel.InvokeAsync(textPlugin["AppendDay"], new KernelArguments()
    {
        ["input"] = textToAppendTo,
        ["day"] = valueOfTheDayToAppend.ToString("dddd", CultureInfo.CurrentCulture)
    });

    return results.Function;
})
.WithName("executefunctionmethod2")
.WithDescription("executes function that return meta data")
.WithOpenApi();

app.Run();
