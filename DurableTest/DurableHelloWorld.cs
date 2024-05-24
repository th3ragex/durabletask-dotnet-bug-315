using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.DurableTask.Entities;
using Microsoft.Extensions.Logging;

namespace DurableTest;

/// <summary>
/// <see cref="https://learn.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-isolated-create-first-csharp?pivots=code-editor-vscode"/>
/// </summary>
static class DurableHelloWorld
{
    [Function(nameof(HelloCities))]
    public static async Task<string> HelloCities([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        // Works with Microsoft.Azure.Functions.Worker.Extensions.DurableTask v1.1.2, forever stuck with v1.1.3
        var entityLock = new EntityInstanceId("HelloWorldLock", "helloworldlock");
        await using (await context.Entities.LockEntitiesAsync(entityLock))
        {
            string result = "";
            result += await context.CallActivityAsync<string>(nameof(SayHello), "Tokyo") + " ";
            result += await context.CallActivityAsync<string>(nameof(SayHello), "London") + " ";
            result += await context.CallActivityAsync<string>(nameof(SayHello), "Seattle");
            return result;
        }
    }

    [Function(nameof(SayHello))]
    public static string SayHello([ActivityTrigger] string cityName, FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger(nameof(SayHello));
        logger.LogInformation("Saying hello to {name}", cityName);
        return $"Hello, {cityName}!";
    }

    [Function(nameof(StartHelloCities))]
    public static async Task<string> StartHelloCities(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger(nameof(StartHelloCities));

        string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(nameof(HelloCities));
        logger.LogInformation("Created new orchestration with instance ID = {instanceId}", instanceId);

        var durableTaskVersion = typeof(Microsoft.Azure.Functions.Worker.Extensions.DurableTask.Http.DurableHttpRequest).Assembly
            .GetName().Version.ToString();

        return $"Microsoft.Azure.Functions.Worker.Extensions.DurableTask: {durableTaskVersion}";
    }
}