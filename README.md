# Dapr .NET activity event ID demo

Run a Dapr workflow locally, inspect its history in the Diagrid dashboard, and rerun it from a failed activity using the activity’s scheduling event ID.

The activity displays two separate values before it fails:

```csharp
context.ScheduledEventId // Runtime ActivityRequest.TaskId: the TaskScheduled history event ID
context.TaskExecutionKey // TaskExecutionId when supplied by the runtime
```

## Run

Start Docker Desktop. If Dapr has not been initialized, run `dapr init` once.

Set up the [Diagrid local dashboard](https://www.diagrid.io/blog/improving-the-local-dapr-workflow-experience-diagrid-dashboard) against the same local workflow state store. Start its container:

```bash
docker start <dashboard-container-name>
```

Then run the demo:

```bash
./scripts/run-local.sh
```

This builds the app and starts local Dapr. On Apple Silicon, the build automatically uses an x64 Docker build container. The console shows a compact table with the activity, scheduling event ID, execution key, and result. Routine build and Dapr logs stay in the ignored `.local/` folder; startup errors are shown when needed.

## Demonstrate

The workflow is:

```text
ValidateOrder -> ProcessPayment -> SendConfirmation
                    fails
```

1. The console prints the source instance ID, scheduling event ID, and execution key.
2. Open [Diagrid at localhost:8080](http://localhost:8080). Find the source instance under app `workflow-rerun-demo`.
3. In its history, find **ProcessPayment's TaskScheduled event**. Its `EventId` must equal the console's integer. Compare the task execution ID with the execution key too.
4. Back in the terminal, press **Enter**. The demo disables its intentional failure and calls:

   ```csharp
   await client.RerunWorkflowFromEventAsync(sourceId, (uint)checkpoint.EventId, options);
   ```

5. The new instance completes. The console verifies that only payment and confirmation execute again; validation uses retained history.
6. Inspect both instances in Diagrid. Press **Enter** again to stop.

Use the scheduling event ID, not a UI row number or the failure event's own ID. A workflow execution ID and an activity execution ID are also different identifiers.

The sample activities simulate validation, payment, and confirmation. The payment fails intentionally on the first run. Its scheduling event ID stays in memory for the rerun within the same process.

## SDK change

The SDK implementation lives in [the SDK fork](https://github.com/mtaghavi2005/dotnet-sdk/tree/feat/expose-activity-scheduled-event-id). `scripts/bootstrap-sdk.sh` pins its exact commit. Bootstrap clones that commit into the ignored `.sdk` build directory.

Application code is in [src/WorkflowRerunDemo](src/WorkflowRerunDemo), with separate files for the workflow, activities, and demo state. The key change is using the runtime-provided request `TaskId`, rather than assuming the orchestration's local action sequence number equals a history event ID.

## Run without pauses

Run the same demonstration without pauses:

```bash
./scripts/run-local.sh --auto
```

Success ends with `PASS: validation was replayed; payment and confirmation executed.`

Build and run scripts:

- `run-local.sh`: build and run the interactive demo.
- `build.sh`: native or Docker build.
- `bootstrap-sdk.sh`: fetch the pinned SDK fork.

Build only: `./scripts/build.sh`. Other hosts build natively; `DEMO_BUILD_MODE=native|container` overrides the choice. Docker uses separate `obj/container/` intermediate folders so its NuGet restore files do not overwrite Rider’s. The container build uses Linux x64 for compatibility with the SDK’s protobuf tooling.

The local runtime uses HTTP port `3580` and gRPC port `50801`. The Diagrid dashboard uses port `8080`.
