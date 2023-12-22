var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedisContainer("cache");

var apiservice = builder.AddProject<Projects.SemanticAspire_ApiService>("apiservice");

builder.AddProject<Projects.SemanticAspire_Web>("webfrontend")
    .WithReference(cache)
    .WithReference(apiservice);

builder.AddProject<Projects.SemanticAspire_MethodFunctionsApiService>("methodfunctionsapiservice");

builder.AddProject<Projects.SemanticAspire_ArgumentsApiService>("argumentsapiservice");

builder.AddProject<Projects.SemanticAspire_InlineFunctionDefinitionApiService>("inlinefunctiondefinitionapiservice");

builder.AddProject<Projects.SemanticAspire_TemplateLanguageApiService>("templatelanguageapiservice");

builder.AddProject<Projects.SemanticAspire_BingAndGooglePluginsApiService>("bingandgooglepluginsapiservice");

builder.Build().Run();
