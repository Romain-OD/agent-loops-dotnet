using System.Text.Json;
using Microsoft.Extensions.AI;

public static class Trace
{
    public static void Step(int step, FunctionCallContent call, object? result, long tokens)
    {
        var args = string.Join(", ", (call.Arguments ?? new Dictionary<string, object?>()).Select(a => $"{a.Key}: {a.Value}"));
        var text = result is string s ? s : JsonSerializer.Serialize(result);
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"step {step}  ");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write($"{call.Name}({args})");
        Console.ForegroundColor = text.StartsWith("Error") ? ConsoleColor.Red : ConsoleColor.Gray;
        Console.WriteLine($"  -> {text}");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"        tokens so far: {tokens}");
        Console.ResetColor();
    }

    // Every run is saved, so a bad one can become an eval case later.
    public static void Save(IEnumerable<ChatMessage> messages)
    {
        Directory.CreateDirectory("traces");
        var path = Path.Combine("traces", $"run-{DateTime.Now:yyyyMMdd-HHmmss}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(messages, AIJsonUtilities.DefaultOptions));
        Console.WriteLine($"trace: {path}");
    }
}
