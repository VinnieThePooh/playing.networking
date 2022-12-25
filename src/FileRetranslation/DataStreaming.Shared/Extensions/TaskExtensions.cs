namespace DataStreaming.Common.Extensions;

public static class TaskExtensions
{
    public static async Task WhenAll(this IEnumerable<Task> tasks)
    {
        var allTasks = Task.WhenAll(tasks);

        try
        {
            await allTasks;
        }
        catch
        {
            throw allTasks.Exception!;
        }
    }
}