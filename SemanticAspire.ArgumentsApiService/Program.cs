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

var arguments = new KernelArguments()
{
    ["input"] = "Today is: ",
    ["day"] = DateTimeOffset.Now.ToString("dddd", CultureInfo.CurrentCulture)
};

app.MapGet("/executefunctionmethod1", async () =>
{
    return await kernel.InvokeAsync<string>(textPlugin["AppendDay"], arguments);
})
.WithName("executefunctionmethod1")
.WithOpenApi();


app.MapGet("/executefunctionmethod2", async () =>
{
    var results = await kernel.InvokeAsync(textPlugin["AppendDay"], arguments);

    return results.Function;
})
.WithName("executefunctionmethod2")
.WithOpenApi();

app.Run();
