using Dapr.Workflow;
using Dapr.Workflow.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WorkflowRerunDemo;
using WorkflowRerunDemo.Activities;

var automatic = args.Contains("--auto");
var builder = Host.CreateApplicationBuilder(args.Where(a => a != "--auto").ToArray());
builder.Logging.ClearProviders(); // Keep the demonstration output focused on the two identifiers.
builder.Services.AddSingleton<DemoState>();
builder.Services.AddDaprWorkflow(options =>
{
    options.RegisterWorkflow<OrderWorkflow>();
    options.RegisterActivity<ValidateOrder>();
    options.RegisterActivity<ProcessPayment>();
    options.RegisterActivity<SendConfirmation>();
});

using var host = builder.Build();
await host.StartAsync();
try
{
    var client = host.Services.GetRequiredService<DaprWorkflowClient>();
    var demo = host.Services.GetRequiredService<DemoState>();
    var sourceId = $"source-{Guid.NewGuid():N}";

    PrintRun("SOURCE", sourceId);

    await client.ScheduleNewWorkflowAsync(nameof(OrderWorkflow), sourceId, "order-42");

    var source = await WaitAsync(client, sourceId);

    if (source.RuntimeStatus != WorkflowRuntimeStatus.Failed)
        throw new InvalidOperationException($"Expected Failed, got {source.RuntimeStatus}.");

    // The activity captured these values BEFORE throwing, not from its return value.
    var checkpoint = demo.Payments[sourceId];

    var history = await client.GetInstanceHistoryAsync(sourceId);

    if (!history.Any(e => e.EventType == WorkflowHistoryEventType.TaskScheduled && e.EventId == checkpoint.EventId))
        throw new InvalidOperationException("Captured ID does not match a TaskScheduled history event.");

    Console.WriteLine("\nDiagrid: http://localhost:8080 — compare ProcessPayment's scheduling event.");
    Pause($"[Enter] Rerun from event {checkpoint.EventId}");

    demo.FailPayment = false;
    var rerunId = $"rerun-{Guid.NewGuid():N}";
    PrintRun("RERUN", rerunId);
    await client.RerunWorkflowFromEventAsync(
        sourceId,
        checked((uint)checkpoint.EventId),
        new RerunWorkflowFromEventOptions { NewInstanceId = rerunId });

    var rerun = await WaitAsync(client, rerunId);

    var executed = demo.Executions.Where(e => e.InstanceId == rerunId)
        .Select(e => e.Activity).Distinct().ToArray();

    if (rerun.RuntimeStatus != WorkflowRuntimeStatus.Completed ||
        !executed.SequenceEqual(new[] { nameof(ProcessPayment), nameof(SendConfirmation) }))

        throw new InvalidOperationException(
            $"Unexpected rerun: {rerun.RuntimeStatus}; activities: {string.Join(", ", executed)}.");

    Console.WriteLine("\nPASS: validation was replayed; payment and confirmation executed.");
    Pause("[Enter] Exit");
}
catch (Exception exception)
{
    Console.Error.WriteLine($"ERROR: {exception.Message}");
    Environment.ExitCode = 1;
}
finally
{
    await host.StopAsync();
}

void Pause(string message)
{
    if (automatic) return;
    Console.WriteLine(message);
    if (Console.ReadLine() is null)
        throw new InvalidOperationException("Run in an interactive terminal, or pass --auto to skip the pauses.");
}

static void PrintRun(string phase, string instanceId)
{
    Console.WriteLine($"\n{phase}: {instanceId}");

    Console.WriteLine($"{"Activity",-18} {"TaskId/EventId",-16} {"TaskExecutionKey",-32} Result");
}

static async Task<WorkflowState> WaitAsync(DaprWorkflowClient client, string instanceId)
{
    using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));

    return await client.WaitForWorkflowCompletionAsync(instanceId, cancellation: timeout.Token);
}
