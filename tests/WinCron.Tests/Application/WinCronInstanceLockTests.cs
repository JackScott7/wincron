using WinCron.Application;

namespace WinCron.Tests.Application;

public sealed class WinCronInstanceLockTests
{
    [Fact]
    public void AcquireRejectsSecondOwnerForSameConfiguration()
    {
        var configurationPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.wc");
        using var firstLock = WinCronInstanceLock.Acquire(configurationPath);

        var exception = Assert.Throws<InvalidOperationException>(
            () => WinCronInstanceLock.Acquire(configurationPath));

        Assert.Contains(Path.GetFullPath(configurationPath), exception.Message);
    }

    [Fact]
    public void DisposeAllowsConfigurationToBeAcquiredAgain()
    {
        var configurationPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.wc");
        WinCronInstanceLock.Acquire(configurationPath).Dispose();

        using var replacementLock = WinCronInstanceLock.Acquire(configurationPath);

        Assert.Equal(WinCronInstanceLock.CreateName(configurationPath), replacementLock.Name);
    }

    [Fact]
    public void DifferentConfigurationsUseDifferentLocks()
    {
        var firstPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.wc");
        var secondPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.wc");

        using var firstLock = WinCronInstanceLock.Acquire(firstPath);
        using var secondLock = WinCronInstanceLock.Acquire(secondPath);

        Assert.NotEqual(firstLock.Name, secondLock.Name);
    }
}
