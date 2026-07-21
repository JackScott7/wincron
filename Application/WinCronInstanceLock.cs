using System.Security.Cryptography;
using System.Text;

namespace WinCron.Application;

public sealed class WinCronInstanceLock : IDisposable
{
    private readonly Semaphore semaphore;
    private bool isDisposed;

    private WinCronInstanceLock(Semaphore semaphore, string name)
    {
        this.semaphore = semaphore;
        Name = name;
    }

    public string Name { get; }

    public static WinCronInstanceLock Acquire(string configurationPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationPath);

        var name = CreateName(configurationPath);
        var semaphore = new Semaphore(1, 1, name);
        if (!semaphore.WaitOne(TimeSpan.Zero))
        {
            semaphore.Dispose();
            throw new InvalidOperationException(
                $"Another WinCron instance is already using configuration '{Path.GetFullPath(configurationPath)}'.");
        }

        return new WinCronInstanceLock(semaphore, name);
    }

    public static string CreateName(string configurationPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationPath);

        var normalizedPath = Path.GetFullPath(configurationPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .ToUpperInvariant();
        var pathHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedPath)));
        return $"Local\\WinCron-{pathHash}";
    }

    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        semaphore.Release();
        semaphore.Dispose();
        isDisposed = true;
    }
}
