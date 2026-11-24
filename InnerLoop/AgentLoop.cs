using Microsoft.Extensions.AI;

public record Budget(int MaxSteps, long MaxTokens, int MaxConsecutiveErrors, TimeSpan Timeout);

public enum StopReason { Answered, MaxSteps, TokenBudget, RepeatedErrors, Timeout }

public record LoopResult(StopReason Reason, int Steps, long Tokens, string? Answer);

public static class AgentLoop
{
    public static async Task<LoopResult> RunAsync(IChatClient model, List<ChatMessage> messages, ChatOptions options, Budget budget)
    {
        var tools = options.Tools!.OfType<AIFunction>().ToDictionary(t => t.Name);
        using var timeout = new CancellationTokenSource(budget.Timeout);
        long tokens = 0;
        int errors = 0, step = 0;
        try
        {
            while (step < budget.MaxSteps)
            {
                step++;
                ChatResponse response = await model.GetResponseAsync(messages, options, timeout.Token);
                tokens += response.Usage?.TotalTokenCount ?? 0;
                messages.AddMessages(response);

                var calls = response.Messages.SelectMany(m => m.Contents).OfType<FunctionCallContent>().ToList();
                if (calls.Count == 0) return new(StopReason.Answered, step, tokens, response.Text);
                if (tokens > budget.MaxTokens) return new(StopReason.TokenBudget, step, tokens, null);

                foreach (var call in calls)
                {
                    object? result;
                    try
                    {
                        result = await tools[call.Name].InvokeAsync(new(call.Arguments), timeout.Token);
                        errors = 0;
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        result = $"Error: {ex.Message}";
                        errors++;
                    }
                    Trace.Step(step, call, result, tokens);
                    messages.Add(new(ChatRole.Tool, [new FunctionResultContent(call.CallId, result)]));
                }
                if (errors >= budget.MaxConsecutiveErrors) return new(StopReason.RepeatedErrors, step, tokens, null);
            }
            return new(StopReason.MaxSteps, step, tokens, null);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            return new(StopReason.Timeout, step, tokens, null);
        }
    }
}
