using Microsoft.Extensions.AI;
using Support;

// dotnet run                 the hand-written loop, offline replay
// dotnet run -- --broken     same task while the refund service is down
// dotnet run -- --builtin    the same task through FunctionInvokingChatClient
// add --azure to any of them for a real model
SupportTools.RefundServiceDown = args.Contains("--broken");
IChatClient model = Models.Create(args, offline: new ReplayChatClient());
var options = new ChatOptions { Tools = SupportTools.All() };
List<ChatMessage> messages =
[
    new(ChatRole.System, SupportTools.Instructions),
    new(ChatRole.User, "Order 1042 arrived broken. Can I get my money back?"),
];

if (args.Contains("--builtin"))
{
    IChatClient agent = model.AsBuilder()
        .UseFunctionInvocation(configure: f =>
        {
            f.MaximumIterationsPerRequest = 8;
            f.MaximumConsecutiveErrorsPerRequest = 3;
        })
        .Build();
    try
    {
        var response = await agent.GetResponseAsync(messages, options);
        Console.WriteLine($"{response.Messages.SelectMany(m => m.Contents).OfType<FunctionCallContent>().Count()} tool calls, handled for you");
        Console.WriteLine(response.Text);
    }
    catch (Exception ex)
    {
        // Past MaximumConsecutiveErrorsPerRequest, FunctionInvokingChatClient rethrows the tool's exception.
        Console.WriteLine($"stopped: {ex.Message}");
    }
    return;
}

var budget = new Budget(MaxSteps: 8, MaxTokens: 20_000, MaxConsecutiveErrors: 3, Timeout: TimeSpan.FromSeconds(60));
var result = await AgentLoop.RunAsync(model, messages, options, budget);

Console.WriteLine();
Console.WriteLine($"stop: {result.Reason} after {result.Steps} steps, {result.Tokens} tokens");
if (result.Answer is not null) Console.WriteLine(result.Answer);
Trace.Save(messages);
