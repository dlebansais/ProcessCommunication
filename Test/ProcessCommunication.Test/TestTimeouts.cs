namespace ProcessCommunication.Test;

using NUnit.Framework;

[TestFixture]
public class TestTimeouts
{
    [Test]
    public void TestSuccess()
    {
        Assert.That(Timeouts.ProcessLaunchTimeout.TotalSeconds, Is.EqualTo(5.0));
        Assert.That(Timeouts.BusyTimeout.TotalSeconds, Is.EqualTo(5.0));
        Assert.That(Timeouts.AcknowledgeTimeout.TotalSeconds, Is.EqualTo(10.0));
        Assert.That(Timeouts.IdleTimeout.TotalSeconds, Is.EqualTo(60.0));
    }
}
