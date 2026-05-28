namespace Monthoya.Data;

internal static class DbContextOperationGate
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static readonly AsyncLocal<int> Depth = new();

    public static async ValueTask<Scope> EnterAsync(CancellationToken cancellationToken)
    {
        if (Depth.Value > 0)
        {
            Depth.Value++;
            return new Scope(releaseGate: false);
        }

        await Gate.WaitAsync(cancellationToken);
        Depth.Value = 1;
        return new Scope(releaseGate: true);
    }

    public sealed class Scope(bool releaseGate) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            Depth.Value = Math.Max(0, Depth.Value - 1);
            if (releaseGate)
            {
                Depth.Value = 0;
                Gate.Release();
            }

            return ValueTask.CompletedTask;
        }
    }
}
