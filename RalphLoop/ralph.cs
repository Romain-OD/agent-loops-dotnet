using System.Diagnostics;

// The Ralph loop: run a coding agent again and again until the tests say it's done.
// Run it on a throwaway branch: dotnet run ralph.cs -- 10
var maxIterations = args.Length > 0 ? int.Parse(args[0]) : 10;

for (var i = 1; i <= maxIterations; i++)
{
    if (Run("dotnet", "test") == 0)
    {
        Console.WriteLine($"green after {i - 1} iterations");
        return 0;
    }

    Console.WriteLine($"--- iteration {i} ---");
    Run("copilot", "-p", "Follow the instructions in PROMPT.md.", "--allow-all-tools");
    Run("git", "add", "-A");
    Run("git", "commit", "-q", "-m", $"ralph: iteration {i}");
}

Console.WriteLine($"stopped after {maxIterations} iterations, tests still red");
return 1;

static int Run(string file, params string[] args)
{
    // copilot and dotnet are .cmd/.exe shims on Windows; cmd /c resolves both.
    var start = OperatingSystem.IsWindows() ? new ProcessStartInfo("cmd", ["/c", file, .. args]) : new ProcessStartInfo(file, args);
    using var process = Process.Start(start)!;
    process.WaitForExit();
    return process.ExitCode;
}
