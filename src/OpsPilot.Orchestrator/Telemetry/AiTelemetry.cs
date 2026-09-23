using System.Diagnostics.Metrics;

namespace OpsPilot.Orchestrator.Telemetry;

public static class AiTelemetry
{
    public const string SourceName = "OpsPilot.AI";
    private static readonly Meter Meter = new(SourceName);

    public static readonly Counter<long> ToolCalls = Meter.CreateCounter<long>("opspilot.tool.calls", description: "AI tool invocations by tool and outcome");
    public static readonly Counter<long> SuspectedInjections  = Meter.CreateCounter<long>("opspilot.guard.suspected_injections", description: "User messages matching prompt-injection markers");
    public static readonly Counter<long> UnknownCitations = Meter.CreateCounter<long>("opspilot.grounding.unknown_citations", description: "Citation ids in answers that retrieval never returned");
}