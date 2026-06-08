namespace Monthoya.Data;

internal static class DbContextOperationGate
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public static async ValueTask<Scope> EnterAsync(CancellationToken cancellationToken)
    {
        await Gate.WaitAsync(cancellationToken);
        return new Scope();
    }

    public sealed class Scope : IAsyncDisposable
    {
        private bool _disposed;

        public ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _disposed = true;
                Gate.Release();
            }

            return ValueTask.CompletedTask;
        }
    }
}