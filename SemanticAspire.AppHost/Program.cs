var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var db = builder.AddPostgres("postgres")
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

builder.AddProject<Projects.SemanticAspire_DallEApiService>("dalleapiservice");

builder.AddProject<Projects.SemanticAspire_DIContainerApiService>("dicontainerapiservice");

builder.AddProject<Projects.SemanticAspire_GptVisionApiService>("gptvisionapiservice");

builder.AddProject<Projects.SemanticAspire_AgentsApiService>("agentsapiservice");

builder.AddProject<Projects.SemanticAspire_AgentCollaborationApiService>("agentcollaborationapiservice");

builder.Build().Run();
