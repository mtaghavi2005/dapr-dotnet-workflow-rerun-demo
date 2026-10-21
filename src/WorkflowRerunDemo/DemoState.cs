using System.Collections.Concurrent;
using Dapr.Workflow;

namespace WorkflowRerunDemo;

// Same-process demonstration only: no database or file-based recovery infrastructure.
public sealed class DemoState
{
    public volatile bool FailPayment = true;
    public ConcurrentDictionary<string, ActivityCheckpoint> Payments { get; } = new();
    public ConcurrentQueue<(string InstanceId, string Activity)> Executions { get; } = new();

    public ActivityCheckpoint Capture(WorkflowActivityContext context, string activity, string result = "OK")
    {
        Executions.Enqueue((context.InstanceId, activity));

        Console.WriteLine($"{activity,-18} {context.ScheduledEventId,-16} {context.TaskExecutionKey,-32} {result}");

        return new ActivityCheckpoint(context.ScheduledEventId, context.TaskExecutionKey);
    }
}
