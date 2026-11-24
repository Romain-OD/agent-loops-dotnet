using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;

public sealed class ExpectedTool(string name) : EvaluationContext("Expected tool", name)
{
    public string ToolName { get; } = name;
}

// Deterministic: no judge model, so the score only moves when the agent's behavior does.
public sealed class ToolChoiceEvaluator : IEvaluator
{
    public const string MetricName = "Correct tool";

    public IReadOnlyCollection<string> EvaluationMetricNames => [MetricName];

    public ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        var expected = additionalContext!.OfType<ExpectedTool>().Single().ToolName;
        var called = modelResponse.Messages.SelectMany(m => m.Contents).OfType<FunctionCallContent>().FirstOrDefault()?.Name;
        var metric = new BooleanMetric(MetricName, called == expected, reason: $"called {called ?? "nothing"}");
        metric.Interpretation = new EvaluationMetricInterpretation(
            called == expected ? EvaluationRating.Good : EvaluationRating.Unacceptable,
            failed: called != expected);
        return new(new EvaluationResult(metric));
    }
}
