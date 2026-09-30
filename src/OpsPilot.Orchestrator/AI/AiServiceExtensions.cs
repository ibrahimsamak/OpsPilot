using System.ClientModel;
using System.ClientModel.Primitives;
using Azure.Identity;
using Microsoft.Extensions.AI;
using OpenAI;
using OpsPilot.Orchestrator.Telemetry;

namespace OpsPilot.Orchestrator.AI;
public static class AiServiceExtensions
{
    public static WebApplicationBuilder AddAI(this WebApplicationBuilder builder)
    {
        var options = builder.Configuration.GetSection(AiOptions.Section).Get<AiOptions>() ?? new AiOptions();
        if (string.IsNullOrWhiteSpace(options.Endpoint))
            throw new InvalidOperationException(
                "AzureOpenAI:Endpoint is missing. Add it to user secrets (Day 1, Part 1F).");

        builder.Services.AddSingleton(options);

        var clientOptions  = new OpenAIClientOptions
        {
            Endpoint = new Uri($"{options.Endpoint.TrimEnd('/')}/openai/v1/")
        };

        // The AuthenticationPolicy constructor (keyless Entra ID auth) is marked experimental in OpenAI 2.x.
        #pragma warning disable OPENAI001
        OpenAIClient openAi = string.IsNullOrWhiteSpace(options.ApiKey)
            ? new OpenAIClient(
                new BearerTokenPolicy(new DefaultAzureCredential(), "https://cognitiveservices.azure.com/.default"),
                clientOptions)
            : new OpenAIClient(new ApiKeyCredential(options.ApiKey), clientOptions);
        #pragma warning restore OPENAI001

        var captureContent = builder.Environment.IsDevelopment();
        builder.Services
            .AddChatClient(openAi.GetChatClient(options.ChatDeployment).AsIChatClient())
            .UseFunctionInvocation(configure: f => f.MaximumIterationsPerRequest = 6)
            .UseOpenTelemetry(sourceName: AiTelemetry.SourceName, configure: o => o.EnableSensitiveData = captureContent)
            .UseLogging();

        builder.Services
            .AddEmbeddingGenerator(openAi.GetEmbeddingClient(options.EmbeddingDeployment).AsIEmbeddingGenerator())
            .UseOpenTelemetry(sourceName: AiTelemetry.SourceName, configure: o => o.EnableSensitiveData = captureContent)
            .UseLogging();


        return builder;
    }
}