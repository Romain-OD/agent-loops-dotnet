# agent-loops-dotnet

Runnable .NET 11 code for the Dev Skills Unlock video on loop AI engineering: the three loops around an AI agent.

| Loop | Folder | What it shows |
|---|---|---|
| Inner: the agent loop | `InnerLoop/` | model, tool call, observe, repeat, written by hand over `IChatClient`, with a budget and a stop reason for every exit; then the same task through `FunctionInvokingChatClient` |
| Middle: the Ralph loop | `RalphLoop/` | a coding agent (GitHub Copilot CLI) run in a `while` loop against a spec and failing tests until `dotnet test` passes |
| Outer: the eval loop | `EvalLoop/` | a `Microsoft.Extensions.AI.Evaluation` evaluator scoring which tool the agent picks, used as a CI gate |

Requires the .NET 11 SDK.

## Inner loop

```powershell
dotnet run --project InnerLoop                       # hand-written loop, stops with Answered
dotnet run --project InnerLoop -- --broken           # refund service returns 503, stops with RepeatedErrors
dotnet run --project InnerLoop -- --builtin          # the same task through UseFunctionInvocation()
dotnet run --project InnerLoop -- --builtin --broken # FunctionInvokingChatClient rethrows after 3 consecutive errors
```

Every run of the hand-written loop saves its message history to `traces/run-*.json`.

## Eval loop

```powershell
cd EvalLoop
dotnet run    # exits 1 when the pass rate is under 90%
```

The repo starts in the "before" state: 3 of 5 cases pass. To replay the video:

1. In `Support/SupportTools.cs`, set the `CheckRefundEligibility` description to
   `Checks whether an order can get a refund or a return: an item that arrived broken or damaged, or a customer who wants their money back.` → 5/5.
2. Add the case that came from a production trace to `EvalLoop/evals.json`:
   `{ "prompt": "Is my package still in transit? #1043", "expectedTool": "TrackShipment" }` → 5/6, exit 1.
3. Set the `TrackShipment` description to `Tracks where a shipment, parcel or package is in transit.` → 6/6.

## Ralph loop

Run it on a throwaway branch or a copy: the agent edits files and commits on every iteration.

```powershell
cd RalphLoop
git switch -c ralph-run
dotnet run ralph.cs 10   # at most 10 iterations
```

Each iteration runs `dotnet test`; if anything is red it starts a fresh `copilot -p "Follow the instructions in PROMPT.md." --allow-all-tools`, then commits. `SPEC.md` is the requirement, the tests are the stop signal, and `progress.md` is the only memory carried between iterations. You need the [GitHub Copilot CLI](https://docs.github.com/copilot/how-tos/use-copilot-agents/use-copilot-cli) installed and logged in.

`RalphLoop` has its own `global.json`, which turns on the Microsoft.Testing.Platform mode of `dotnet test` that xunit.v3 needs on the .NET 10+ SDK.

## Models

Without flags, the inner and eval loops use offline stand-in models from `Support/Models.cs`, so every run is repeatable:

- `ReplayChatClient` plays back the tool calls of the refund task and keeps retrying a tool that fails.
- `DescriptionRouterChatClient` is deliberately literal: it calls the tool whose description shares the most words with the user's message, so editing a description really changes its behavior.

Add `--azure` to use a real model through Azure OpenAI (`az login`, then set `AZURE_OPENAI_ENDPOINT` and optionally `AZURE_OPENAI_DEPLOYMENT`, default `gpt-4.1-mini`). A real model will need different eval cases to fail, which is the point of having an eval loop.

## License

MIT
