using System.Text;

namespace WinCron.Execution;

internal static class BoundedProcessOutputReader
{
    private const int BufferSize = 4096;

    public static async Task<CapturedProcessOutput> ReadAsync(
        TextReader reader,
        int maximumCharacters,
        string jobId,
        JobOutputChannel channel,
        IJobOutputObserver observer)
    {
        var capturedOutput = new StringBuilder(Math.Min(maximumCharacters, BufferSize));
        var buffer = new char[BufferSize];
        var wasTruncated = false;

        while (true)
        {
            var charactersRead = await reader.ReadAsync(buffer, CancellationToken.None);
            if (charactersRead == 0)
            {
                break;
            }

            await observer.WriteAsync(
                jobId,
                channel,
                buffer.AsMemory(0, charactersRead),
                CancellationToken.None);

            var availableCapacity = maximumCharacters - capturedOutput.Length;
            if (availableCapacity > 0)
            {
                capturedOutput.Append(buffer, 0, Math.Min(availableCapacity, charactersRead));
            }

            wasTruncated |= charactersRead > availableCapacity;
        }

        return new CapturedProcessOutput(capturedOutput.ToString(), wasTruncated);
    }

    internal sealed record CapturedProcessOutput(string Text, bool WasTruncated);
}
