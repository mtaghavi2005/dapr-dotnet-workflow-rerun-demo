using Dapr.Workflow;
using WorkflowRerunDemo.Activities;

namespace WorkflowRerunDemo;

public sealed class OrderWorkflow : Workflow<string, string>
{
    public override async Task<string> RunAsync(WorkflowContext context, string order)
    {
        await context.CallActivityAsync<string>(nameof(ValidateOrder), order);
        await context.CallActivityAsync<string>(nameof(ProcessPayment), order);
        return await context.CallActivityAsync<string>(nameof(SendConfirmation), order);
    }
}