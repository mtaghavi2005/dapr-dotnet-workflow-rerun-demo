using Dapr.Workflow;

namespace WorkflowRerunDemo.Activities;

public sealed class SendConfirmation(DemoState demo) : WorkflowActivity<string, string>
{
    public override Task<string> RunAsync(WorkflowActivityContext context, string order)
    {
        demo.Capture(context, nameof(SendConfirmation));

        return Task.FromResult($"Confirmed {order}");
    }
}