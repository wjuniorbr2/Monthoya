using System.Diagnostics;

namespace Monthoya.Desktop.Diagnostics;

internal static class PerformanceTrace
{
    public static IDisposable Measure(string operationName)
    {
        return new Measurement(operationName);
    }

    private sealed class Measurement : IDisposable
    {
        private readonly string _operationName;
        private readonly Stopwatch _stopwatch;
        private bool _disposed;

        public Measurement(string operationName)
        {
            _operationName = string.IsNullOrWhiteSpace(operationName) ? "Unnamed operation" : operationName;
            _stopwatch = Stopwatch.StartNew();
            Trace.WriteLine($"[PERF] START {_operationName}");
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _stopwatch.Stop();
            Trace.WriteLine($"[PERF] END {_operationName}: {_stopwatch.ElapsedMilliseconds} ms");
        }
    }
}
