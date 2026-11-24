using System.Text.RegularExpressions;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;

namespace Support;

public static class Models
{
    // --azure uses a real model (AZURE_OPENAI_ENDPOINT, optional AZURE_OPENAI_DEPLOYMENT, az login).
    // Without it, a stand-in model runs offline so every run is repeatable on camera.
    public static IChatClient Create(string[] args, IChatClient offline)
    {
        if (!args.Contains("--azure")) return offline;
        var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
            ?? throw new InvalidOperationException("Set AZURE_OPENAI_ENDPOINT to use --azure.");
        return new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
            .GetChatClient(Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT") ?? "gpt-4.1-mini")
            .AsIChatClient();
    }
}

// Plays back the tool calls a model made on the refund task, one per turn, so the loop can be shown offline.
public sealed class ReplayChatClient : IChatClient
{
    static readonly (string Tool, Dictionary<string, object?> Args)[] Script =
    [
        ("GetOrder", new() { ["orderId"] = "1042" }),
        ("CheckRefundEligibility", new() { ["orderId"] = "1042", ["reason"] = "arrived broken" }),
        ("IssueRefund", new() { ["orderId"] = "1042" }),
    ];

    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        await Task.Delay(350, cancellationToken);
        var history = messages.ToList();
        var results = history.SelectMany(m => m.Contents).OfType<FunctionResultContent>().ToList();
        static bool Failed(FunctionResultContent r) => r.Exception is not null || r.Result?.ToString()?.StartsWith("Error") == true;
        // A model that keeps retrying a failing tool, which is what stop conditions are for.
        var step = results.Count(r => !Failed(r));
        var message = step < Script.Length
            ? new ChatMessage(ChatRole.Assistant, [new FunctionCallContent($"call_{results.Count + 1}", Script[step].Tool, Script[step].Args)])
            : new ChatMessage(ChatRole.Assistant, "Your mug arrived broken, so you get a full refund of 24.90. It's on its way back to your card.");
        var input = history.Sum(m => m.Text.Length + 40) / 4 + 180;
        return new ChatResponse(message) { Usage = new() { InputTokenCount = input, OutputTokenCount = 28, TotalTokenCount = input + 28 } };
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var update in (await GetResponseAsync(messages, options, cancellationToken)).ToChatResponseUpdates()) yield return update;
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;
    public void Dispose() { }
}

// A deliberately literal stand-in model: it calls the tool whose description shares the most words with
// the user's message, and gives up when nothing matches. Tool descriptions really change its behavior.
public sealed partial class DescriptionRouterChatClient : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var ask = Words(messages.Last(m => m.Role == ChatRole.User).Text);
        var best = (options?.Tools ?? []).OfType<AIFunction>()
            .Select(tool => (tool, score: Words(tool.Description).Intersect(ask).Count()))
            .OrderByDescending(x => x.score)
            .FirstOrDefault();
        var message = best.score == 0
            ? new ChatMessage(ChatRole.Assistant, "Sorry, I can't help with that.")
            : new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("call_1", best.tool.Name, new Dictionary<string, object?>())]);
        return Task.FromResult(new ChatResponse(message));
    }

    static HashSet<string> Words(string text) => [.. WordRegex().Matches(text.ToLowerInvariant()).Select(m => m.Value)];

    [GeneratedRegex("[a-z]{4,}")]
    private static partial Regex WordRegex();

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public object? GetService(Type serviceType, object? serviceKey = null) => null;
    public void Dispose() { }
}
