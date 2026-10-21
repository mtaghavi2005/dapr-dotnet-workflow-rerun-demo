using Dapr.Workflow;

namespace WorkflowRerunDemo.Activities;

public sealed class ValidateOrder(DemoState demo) : WorkflowActivity<string, string>
{
    public override Task<string> RunAsync(WorkflowActivityContext context, string order)
    {
        demo.Capture(context, nameof(ValidateOrder));

        return Task.FromResult(order);
    }
}