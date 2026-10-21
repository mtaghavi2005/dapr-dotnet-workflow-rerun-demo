namespace WorkflowRerunDemo;

public sealed record ActivityCheckpoint(int EventId, string ExecutionKey);
