namespace ProcessCommunication.Test;

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;

[TestFixture]
public class TestRemote
{
    [Test]
    [NonParallelizable]
    public async Task TestRemoteSuccess()
    {
        Remote.Reset();

        string PathToProccess = Remote.GetSiblingFullPath("TestProcess.exe");
        Channel? Channel;

        Stopwatch TestStopwatch = Stopwatch.StartNew();

        Channel = Remote.LaunchAndOpenChannel(PathToProccess, TestChannel.TestGuid);
        Assert.That(Channel, Is.Null);

        await Task.Delay(TimeSpan.FromSeconds(4.0 - TestStopwatch.Elapsed.TotalSeconds)).ConfigureAwait(true);

        Channel = Remote.LaunchAndOpenChannel(PathToProccess, TestChannel.TestGuid);
        Assert.That(Channel, Is.Not.Null);

        Channel.Dispose();

        await Task.Delay(TimeSpan.FromSeconds(15)).ConfigureAwait(true);
    }

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

        await Task.Delay(TimeSpan.FromSeconds(4.0 - TestStopwatch.Elapsed.TotalSeconds)).ConfigureAwait(true);

        Channel = Remote.LaunchAndOpenChannel(PathToProccess, TestChannel.TestGuid);
        Assert.That(Channel, Is.Null);

        await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(true);
    }

/*
    [Test]
    [NonParallelizable]
    public async Task TestRetry()
    {
        Remote.Reset();

        string PathToProccess = Remote.GetSiblingFullPath("TestProcess.exe");
        Channel? Channel;

        Stopwatch TestStopwatch = Stopwatch.StartNew();

        Channel = Remote.LaunchAndOpenChannel(PathToProccess, TestChannel.TestGuid);
        Assert.That(Channel, Is.Null);

        await Task.Delay(TimeSpan.FromSeconds(6.0 - TestStopwatch.Elapsed.TotalSeconds)).ConfigureAwait(true);

        Channel = Remote.LaunchAndOpenChannel(PathToProccess, TestChannel.TestGuid);
        Assert.That(Channel, Is.Null);

        Remote.Reset();

        Channel = Remote.LaunchAndOpenChannel(PathToProccess, TestChannel.TestGuid);
        Assert.That(Channel, Is.Not.Null);

        Channel.Dispose();

        await Task.Delay(TimeSpan.FromSeconds(15)).ConfigureAwait(true);
    }
*/
}
