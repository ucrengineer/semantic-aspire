var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedisContainer("cache");

var db = builder.AddPostgresContainer("postgres")
        .WithAnnotation(new ContainerImageAnnotation
        {
            Image = "ankane/pgvector",
            Tag = "latest"
        })
        .AddDatabase("vectordb");

var apiservice = builder.AddProject<Projects.SemanticAspire_ApiService>("apiservice");

builder.AddProject<Projects.SemanticAspire_Web>("webfrontend")
    .WithReference(cache)
    .WithReference(apiservice);

builder.AddProject<Projects.SemanticAspire_MethodFunctionsApiService>("methodfunctionsapiservice");

builder.AddProject<Projects.SemanticAspire_ArgumentsApiService>("argumentsapiservice");

builder.AddProject<Projects.SemanticAspire_InlineFunctionDefinitionApiService>("inlinefunctiondefinitionapiservice");

builder.AddProject<Projects.SemanticAspire_TemplateLanguageApiService>("templatelanguageapiservice");

builder.AddProject<Projects.SemanticAspire_BingAndGooglePluginsApiService>("bingandgooglepluginsapiservice");

builder.AddProject<Projects.SemanticAspire_ConversationSummaryPluginApiService>("conversationsummarypluginapiservice");

builder.AddProject<Projects.SemanticAspire_SemanticMemoryApiService>("semanticmemoryapiservice")
    .WithReference(db);

builder.Build().Run();
