using System.Diagnostics.Metrics;

namespace MooSharp.Infrastructure;

/// <summary>
/// Encapsulates all MooSharp metrics instruments for OpenTelemetry.
/// Register as a singleton and inject where metrics need to be recorded.
/// </summary>
public sealed class MooSharpMetrics : IDisposable
{
    public const string MeterName = "MooSharp";

    private readonly Meter _meter;

    // Gauges for current state
    private readonly UpDownCounter<int> _playersOnline;
    private readonly UpDownCounter<int> _playersLinkdead;

    // Counters for events
    private readonly Counter<long> _logins;
    private readonly Counter<long> _logouts;
    private readonly Counter<long> _verbsExecuted;
    private readonly Counter<long> _llmCalls;

    // Histograms for timing
    private readonly Histogram<double> _llmCallDuration;
    private readonly Histogram<double> _verbExecutionDuration;

    public MooSharpMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        // Current state metrics
        _playersOnline = _meter.CreateUpDownCounter<int>(
            "moosharp.players.online",
            unit: "{players}",
            description: "Number of players currently online");

        _playersLinkdead = _meter.CreateUpDownCounter<int>(
            "moosharp.players.linkdead",
            unit: "{players}",
            description: "Number of players currently in linkdead state");

        // Event counters
        _logins = _meter.CreateCounter<long>(
            "moosharp.players.logins",
            unit: "{logins}",
            description: "Total number of player logins");

        _logouts = _meter.CreateCounter<long>(
            "moosharp.players.logouts",
            unit: "{logouts}",
            description: "Total number of player logouts");

        _verbsExecuted = _meter.CreateCounter<long>(
            "moosharp.scripting.verbs_executed",
            unit: "{verbs}",
            description: "Total number of verb scripts executed");

        _llmCalls = _meter.CreateCounter<long>(
            "moosharp.agents.llm_calls",
            unit: "{calls}",
            description: "Total number of LLM API calls made");

        // Timing histograms
        _llmCallDuration = _meter.CreateHistogram<double>(
            "moosharp.agents.llm_call_duration",
            unit: "ms",
            description: "Duration of LLM API calls in milliseconds");

        _verbExecutionDuration = _meter.CreateHistogram<double>(
            "moosharp.scripting.verb_execution_duration",
            unit: "ms",
            description: "Duration of verb script execution in milliseconds");
    }

    /// <summary>
    /// Record a player coming online (spawned into the world).
    /// </summary>
    public void RecordPlayerOnline() => _playersOnline.Add(1);

    /// <summary>
    /// Record a player going offline (despawned from the world).
    /// </summary>
    public void RecordPlayerOffline() => _playersOnline.Add(-1);

    /// <summary>
    /// Record a player entering linkdead state.
    /// </summary>
    public void RecordPlayerLinkdead() => _playersLinkdead.Add(1);

    /// <summary>
    /// Record a player leaving linkdead state (reconnected or fully disconnected).
    /// </summary>
    public void RecordPlayerLinkdeadRecovered() => _playersLinkdead.Add(-1);

    /// <summary>
    /// Record a player login event.
    /// </summary>
    public void RecordLogin() => _logins.Add(1);

    /// <summary>
    /// Record a player logout event.
    /// </summary>
    public void RecordLogout() => _logouts.Add(1);

    /// <summary>
    /// Record a verb script execution.
    /// </summary>
    /// <param name="verbName">Name of the verb executed.</param>
    /// <param name="durationMs">Duration of execution in milliseconds.</param>
    /// <param name="success">Whether the execution succeeded.</param>
    public void RecordVerbExecution(string verbName, double durationMs, bool success)
    {
        var tags = new TagList
        {
            { "verb_name", verbName },
            { "success", success }
        };

        _verbsExecuted.Add(1, tags);
        _verbExecutionDuration.Record(durationMs, tags);
    }

    /// <summary>
    /// Record an LLM API call.
    /// </summary>
    /// <param name="agentName">Name of the agent making the call.</param>
    /// <param name="source">The LLM provider (OpenAI, Anthropic, etc.).</param>
    /// <param name="durationMs">Duration of the call in milliseconds.</param>
    public void RecordLlmCall(string agentName, string source, double durationMs)
    {
        var tags = new TagList
        {
            { "agent_name", agentName },
            { "source", source }
        };

        _llmCalls.Add(1, tags);
        _llmCallDuration.Record(durationMs, tags);
    }

    public void Dispose()
    {
        _meter.Dispose();
    }
}
