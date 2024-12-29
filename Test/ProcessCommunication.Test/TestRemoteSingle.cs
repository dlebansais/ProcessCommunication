#pragma warning disable CA1849 // Call async methods when in an async method

namespace ProcessCommunication.Test;

using System;
using System.Diagnostics;
#if NET8_0_OR_GREATER
using System.Globalization;
#endif
using System.Threading.Tasks;
using NUnit.Framework;

[TestFixture]
internal class TestRemoteSingle
{
    [Test]
    [NonParallelizable]
    public async Task TestInvalidExe()
    {
        Remote.Reset();

        string PathToProccess = Remote.GetSiblingFullPath("Foo.exe");
        IChannel? Channel;

        Stopwatch TestStopwatch = Stopwatch.StartNew();

        Channel = Remote.LaunchAndOpenChannel(PathToProccess, TestChannel.TestGuid);
        Assert.That(Channel, Is.Null);

        await Task.Delay(Timeouts.ProcessLaunchTimeout - TimeSpan.FromSeconds(1) - TestStopwatch.Elapsed).ConfigureAwait(true);

        Channel = Remote.LaunchAndOpenChannel(PathToProccess, TestChannel.TestGuid);
        Assert.That(Channel, Is.Null);
    }

    [Test]
    [NonParallelizable]
    public async Task TestAsyncInvalidExe()
    {
        Remote.Reset();

        string PathToProccess = Remote.GetSiblingFullPath("Foo.exe");
        IChannel? Channel;

        Channel = await Remote.LaunchAndOpenChannelAsync(PathToProccess, TestChannel.TestGuid).ConfigureAwait(true);
        Assert.That(Channel, Is.Null);
    }

#if NET8_0_OR_GREATER
    [Test]
    [NonParallelizable]
    public async Task TestRemoteSuccess()
    {
        Remote.Reset();

        string PathToProccess = Remote.GetSiblingFullPath("TestProcess.exe");
        IChannel? Channel;

        Stopwatch TestStopwatch = Stopwatch.StartNew();
        TimeSpan ExitDelay = TimeSpan.FromSeconds(20);

        Channel = Remote.LaunchAndOpenChannel(PathToProccess, TestChannel.TestGuid, $"false 1 {ExitDelay.TotalSeconds.ToString(CultureInfo.InvariantCulture)}");
        Assert.That(Channel, Is.Null);

        await Task.Delay(Timeouts.ProcessLaunchTimeout - TimeSpan.FromSeconds(1) - TestStopwatch.Elapsed).ConfigureAwait(true);

        Channel = Remote.LaunchAndOpenChannel(PathToProccess, TestChannel.TestGuid, "false 1");
        Assert.That(Channel, Is.Not.Null);

        Channel.Dispose();

        await Task.Delay(ExitDelay + TimeSpan.FromSeconds(5)).ConfigureAwait(true);
    }

    [Test]
    [NonParallelizable]
    public async Task TestRetry()
    {
        Remote.Reset();

        string PathToProccess = Remote.GetSiblingFullPath("TestProcess.exe");
        IChannel? Channel;

        TimeSpan ExitDelay = TimeSpan.FromSeconds(20);

        Channel = Remote.LaunchAndOpenChannel(PathToProccess, TestChannel.TestGuid, $"false 1 {ExitDelay.TotalSeconds.ToString(CultureInfo.InvariantCulture)}");
        Assert.That(Channel, Is.Null);

        await Task.Delay(Timeouts.ProcessLaunchTimeout + TimeSpan.FromSeconds(1)).ConfigureAwait(true);

        Channel = Remote.LaunchAndOpenChannel(PathToProccess, TestChannel.TestGuid, "false 1");
        Assert.That(Channel, Is.Null);

        Remote.Reset();

        Channel = Remote.LaunchAndOpenChannel(PathToProccess, TestChannel.TestGuid, $"false 1 {ExitDelay.TotalSeconds.ToString(CultureInfo.InvariantCulture)}");
        Assert.That(Channel, Is.Not.Null);

        Channel.Dispose();

        await Task.Delay(ExitDelay + TimeSpan.FromSeconds(5)).ConfigureAwait(true);
    }

    [Test]
    [NonParallelizable]
    public async Task TestAsyncRemoteSuccess()
    {
        Remote.Reset();

        string PathToProccess = Remote.GetSiblingFullPath("TestProcess.exe");
        IChannel? Channel;

        TimeSpan ExitDelay = TimeSpan.FromSeconds(20);

        Channel = await Remote.LaunchAndOpenChannelAsync(PathToProccess, TestChannel.TestGuid, $"false 1 {ExitDelay.TotalSeconds.ToString(CultureInfo.InvariantCulture)}").ConfigureAwait(true);
        Assert.That(Channel, Is.Not.Null);

        Channel.Dispose();

        await Task.Delay(ExitDelay + TimeSpan.FromSeconds(5)).ConfigureAwait(true);
    }

    [Test]
    [NonParallelizable]
    public async Task TestAsyncInvalidType()
    {
        Remote.Reset();

        string PathToProccess = Remote.GetSiblingFullPath("TestProcess.exe");
        IChannel? Channel;

        TimeSpan ExitDelay = TimeSpan.FromSeconds(20);

        Channel = await Remote.LaunchAndOpenChannelAsync(PathToProccess, TestChannel.TestGuid, $"true 1 {ExitDelay.TotalSeconds.ToString(CultureInfo.InvariantCulture)}").ConfigureAwait(true);
        Assert.That(Channel, Is.Null);

        await Task.Delay(ExitDelay + TimeSpan.FromSeconds(5)).ConfigureAwait(true);
    }
#endif
}
