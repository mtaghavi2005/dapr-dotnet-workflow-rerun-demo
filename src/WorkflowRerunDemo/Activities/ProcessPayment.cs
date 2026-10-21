using Dapr.Workflow;

namespace WorkflowRerunDemo.Activities;

public sealed class ProcessPayment(DemoState demo) : WorkflowActivity<string, string>
{
    public override Task<string> RunAsync(WorkflowActivityContext context, string order)
    {
        var fail = demo.FailPayment;

        demo.Payments[context.InstanceId] =
            demo.Capture(context, nameof(ProcessPayment), fail ? "FAILED (intentional)" : "OK");

        if (fail) throw new InvalidOperationException("Intentional payment failure.");

        return Task.FromResult(order);
    }
}