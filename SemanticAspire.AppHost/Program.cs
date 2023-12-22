var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedisContainer("cache");

var apiservice = builder.AddProject<Projects.SemanticAspire_ApiService>("apiservice");

builder.AddProject<Projects.SemanticAspire_Web>("webfrontend")
    .WithReference(cache)
    .WithReference(apiservice);

builder.AddProject<Projects.SemanticAspire_MethodFunctionsApiService>("methodfunctionsapiservice");

builder.AddProject<Projects.SemanticAspire_ArgumentsApiService>("semanticaspire.argumentsapiservice");

builder.Build().Run();
