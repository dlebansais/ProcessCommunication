﻿namespace ProcessCommunication.Test;

using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using NUnit.Framework;

[TestFixture]
public class TestRemote
{
    [Test]
    [NonParallelizable]
    public async Task TestInvalidExe()
    {
        Remote.Reset();

        string PathToProccess = Remote.GetSiblingFullPath("Foo.exe");
        Channel? Channel;

        Stopwatch TestStopwatch = Stopwatch.StartNew();

        Channel = Remote.LaunchAndOpenChannel(PathToProccess, TestChannel.TestGuid);
        Assert.That(Channel, Is.Null);

        await Task.Delay(Timeouts.ProcessLaunchTimeout - TimeSpan.FromSeconds(1) - TestStopwatch.Elapsed).ConfigureAwait(true);

        Channel = Remote.LaunchAndOpenChannel(PathToProccess, TestChannel.TestGuid);
        Assert.That(Channel, Is.Null);
    }

#if NET8_0_OR_GREATER
    [Test]
    [NonParallelizable]
    public async Task TestRemoteSuccess()
    {
        Remote.Reset();

        string PathToProccess = Remote.GetSiblingFullPath("TestProcess.exe");
        Channel? Channel;

        Stopwatch TestStopwatch = Stopwatch.StartNew();
        TimeSpan ExitDelay = TimeSpan.FromSeconds(20);

        Channel = Remote.LaunchAndOpenChannel(PathToProccess, TestChannel.TestGuid, ExitDelay.TotalSeconds.ToString(CultureInfo.InvariantCulture));
        Assert.That(Channel, Is.Null);

        await Task.Delay(Timeouts.ProcessLaunchTimeout - TimeSpan.FromSeconds(1) - TestStopwatch.Elapsed).ConfigureAwait(true);

        Channel = Remote.LaunchAndOpenChannel(PathToProccess, TestChannel.TestGuid);
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
        Channel? Channel;

        TimeSpan ExitDelay = TimeSpan.FromSeconds(20);

        Channel = Remote.LaunchAndOpenChannel(PathToProccess, TestChannel.TestGuid, ExitDelay.TotalSeconds.ToString(CultureInfo.InvariantCulture));
        Assert.That(Channel, Is.Null);

        await Task.Delay(Timeouts.ProcessLaunchTimeout + TimeSpan.FromSeconds(1)).ConfigureAwait(true);

        Channel = Remote.LaunchAndOpenChannel(PathToProccess, TestChannel.TestGuid);
        Assert.That(Channel, Is.Null);

        Remote.Reset();

        Channel = Remote.LaunchAndOpenChannel(PathToProccess, TestChannel.TestGuid, ExitDelay.TotalSeconds.ToString(CultureInfo.InvariantCulture));
        Assert.That(Channel, Is.Not.Null);

        Channel.Dispose();

        await Task.Delay(ExitDelay + TimeSpan.FromSeconds(5)).ConfigureAwait(true);
    }
#endif
}
