namespace ProcessCommunication.Test;

using System;
using NUnit.Framework;

[TestFixture]
public class TestTimeouts
{
    [Test]
    public void TestSuccess()
    {
        Assert.That(Timeouts.ProcessLaunchTimeout.TotalSeconds, Is.EqualTo(10.0));
        Assert.That(Timeouts.BusyTimeout.TotalSeconds, Is.EqualTo(5.0));
        Assert.That(Timeouts.AcknowledgeTimeout.TotalSeconds, Is.EqualTo(10.0));
        Assert.That(Timeouts.IdleTimeout.TotalSeconds, Is.EqualTo(60.0));
    }

    [Test]
    public void TestChanges()
    {
        Timeouts.ProcessLaunchTimeout = TimeSpan.FromSeconds(60);
        Assert.That(Timeouts.ProcessLaunchTimeout.TotalSeconds, Is.EqualTo(60.0));

        Timeouts.BusyTimeout = TimeSpan.FromSeconds(61);
        Assert.That(Timeouts.BusyTimeout.TotalSeconds, Is.EqualTo(61.0));

        Timeouts.AcknowledgeTimeout = TimeSpan.FromSeconds(62);
        Assert.That(Timeouts.AcknowledgeTimeout.TotalSeconds, Is.EqualTo(62.0));

        Timeouts.AcknowledgeTimeout = TimeSpan.FromSeconds(63);
        Assert.That(Timeouts.AcknowledgeTimeout.TotalSeconds, Is.EqualTo(63.0));

        Timeouts.Reset();

        Assert.That(Timeouts.ProcessLaunchTimeout.TotalSeconds, Is.EqualTo(10.0));
        Assert.That(Timeouts.BusyTimeout.TotalSeconds, Is.EqualTo(5.0));
        Assert.That(Timeouts.AcknowledgeTimeout.TotalSeconds, Is.EqualTo(10.0));
        Assert.That(Timeouts.IdleTimeout.TotalSeconds, Is.EqualTo(60.0));
    }
}
