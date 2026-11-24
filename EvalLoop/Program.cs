using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Support;

// dotnet run              evals against the offline stand-in model
// dotnet run -- --azure   the same evals against a real model
const double PassBar = 0.9;

var cases = JsonSerializer.Deserialize<List<EvalCase>>(File.ReadAllText("evals.json"), JsonSerializerOptions.Web)!;
IChatClient model = Models.Create(args, offline: new DescriptionRouterChatClient());
var options = new ChatOptions { Tools = SupportTools.All() };
var evaluator = new ToolChoiceEvaluator();

var passed = 0;
foreach (var c in cases)
{
    List<ChatMessage> messages = [new(ChatRole.System, SupportTools.Instructions), new(ChatRole.User, c.Prompt)];
    var response = await model.GetResponseAsync(messages, options);
    var result = await evaluator.EvaluateAsync(messages, response, additionalContext: [new ExpectedTool(c.ExpectedTool)]);
    var metric = result.Get<BooleanMetric>(ToolChoiceEvaluator.MetricName);

    if (metric.Value == true) passed++;
    Console.ForegroundColor = metric.Value == true ? ConsoleColor.Green : ConsoleColor.Red;
    Console.Write(metric.Value == true ? "PASS " : "FAIL ");
    Console.ResetColor();
    Console.WriteLine($"{c.Prompt,-58} expected {c.ExpectedTool}, {metric.Reason}");
}

var score = (double)passed / cases.Count;
Console.WriteLine();
Console.WriteLine($"{passed}/{cases.Count} passed ({score:P0}), bar {PassBar:P0}");
return score >= PassBar ? 0 : 1;

record EvalCase(string Prompt, string ExpectedTool);
