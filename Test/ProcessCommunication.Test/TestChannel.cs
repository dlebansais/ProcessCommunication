#pragma warning disable CA1848 // Use the LoggerMessage delegates

namespace ProcessCommunication.Test;

using System;
using NUnit.Framework;

[TestFixture]
public class TestChannel
{
    private static readonly Guid TestGuid = new("20E9C969-C990-4DEB-984F-979C824DCC18");

    [Test]
    public void TestSuccess()
    {
        using Channel TestReceiver = new(TestGuid, Mode.Receive);
        using Channel TestSender = new(TestGuid, Mode.Send);

        Assert.That(TestReceiver, Is.Not.Null);
        Assert.That(TestSender, Is.Not.Null);

        TestReceiver.Open();
        TestSender.Open();

        Assert.That(TestReceiver.IsOpen, Is.True);
        Assert.That(TestSender.IsOpen, Is.True);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender.LastError, Is.Empty);

        Assert.That(TestReceiver.GetFreeLength(), Is.EqualTo(Channel.Capacity - 1));
        Assert.That(TestReceiver.GetUsedLength(), Is.EqualTo(0));
        Assert.That(TestSender.GetFreeLength(), Is.EqualTo(Channel.Capacity - 1));
        Assert.That(TestSender.GetUsedLength(), Is.EqualTo(0));

        TestReceiver.Close();
        TestSender.Close();

        Assert.That(TestReceiver.IsOpen, Is.False);
        Assert.That(TestSender.IsOpen, Is.False);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender.LastError, Is.Empty);
    }

    [Test]
    public void TestReadWrite()
    {
        using Channel TestReceiver = new(TestGuid, Mode.Receive);
        using Channel TestSender = new(TestGuid, Mode.Send);

        Assert.That(TestReceiver, Is.Not.Null);
        Assert.That(TestSender, Is.Not.Null);

        TestReceiver.Open();
        TestSender.Open();

        Assert.That(TestReceiver.IsOpen, Is.True);
        Assert.That(TestSender.IsOpen, Is.True);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender.LastError, Is.Empty);

        byte[] DataSent = [0, 1, 2, 3, 4, 5];
        TestSender.Write(DataSent);

        Assert.That(TestSender.GetFreeLength(), Is.EqualTo(Channel.Capacity - 1 - DataSent.Length));
        Assert.That(TestSender.GetUsedLength(), Is.EqualTo(DataSent.Length));

        Assert.That(TestReceiver.GetFreeLength(), Is.EqualTo(Channel.Capacity - 1 - DataSent.Length));
        Assert.That(TestReceiver.GetUsedLength(), Is.EqualTo(DataSent.Length));

        byte[]? DataReceived = TestReceiver.Read();

        Assert.That(DataReceived, Is.Not.Null);
        Assert.That(DataReceived, Is.EqualTo(DataSent));

        Assert.That(TestSender.GetFreeLength(), Is.EqualTo(Channel.Capacity - 1));
        Assert.That(TestSender.GetUsedLength(), Is.EqualTo(0));

        Assert.That(TestReceiver.GetFreeLength(), Is.EqualTo(Channel.Capacity - 1));
        Assert.That(TestReceiver.GetUsedLength(), Is.EqualTo(0));

        string ReceiverStats = TestReceiver.GetStats(string.Empty);
        string SenderStats = TestSender.GetStats(string.Empty);

        Assert.That(ReceiverStats, Is.Not.Empty);
        Assert.That(SenderStats, Is.Not.Empty);

        TestReceiver.Close();
        TestSender.Close();

        Assert.That(TestReceiver.IsOpen, Is.False);
        Assert.That(TestSender.IsOpen, Is.False);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender.LastError, Is.Empty);
    }

    [Test]
    public void TestOpenError()
    {
        using Channel TestReceiver = new(TestGuid, Mode.Receive);

        TestReceiver.Open();

        Assert.That(TestReceiver.IsOpen, Is.True);

        _ = Assert.Throws<InvalidOperationException>(() => TestReceiver.Open());

        TestReceiver.Close();

        Assert.That(TestReceiver.IsOpen, Is.False);
    }

    [Test]
    public void TestMultipleOpen()
    {
        using Channel TestReceiver1 = new(TestGuid, Mode.Receive);
        using Channel TestReceiver2 = new(TestGuid, Mode.Receive);

        TestReceiver1.Open();
        TestReceiver2.Open();

        Assert.That(TestReceiver1.IsOpen, Is.True);
        Assert.That(TestReceiver1.LastError, Is.Empty);

        Assert.That(TestReceiver2.IsOpen, Is.False);
        Assert.That(TestReceiver2.LastError, Is.Not.Empty);
    }

    [Test]
    public void TestMultipleClose()
    {
        using Channel TestReceiver = new(TestGuid, Mode.Receive);

        TestReceiver.Open();

        Assert.That(TestReceiver.IsOpen, Is.True);

        TestReceiver.Close();

        Assert.That(TestReceiver.IsOpen, Is.False);

        TestReceiver.Close();

        Assert.That(TestReceiver.IsOpen, Is.False);
    }

    [Test]
    public void TestReadError()
    {
        using Channel TestReceiver = new(TestGuid, Mode.Receive);
        using Channel TestSender = new(TestGuid, Mode.Send);

        Assert.That(TestReceiver, Is.Not.Null);
        Assert.That(TestSender, Is.Not.Null);

        TestReceiver.Open();
        TestSender.Open();

        Assert.That(TestReceiver.IsOpen, Is.True);
        Assert.That(TestSender.IsOpen, Is.True);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender.LastError, Is.Empty);

        byte[] DataSent = [0, 1, 2, 3, 4, 5];
        TestSender.Write(DataSent);

        TestReceiver.Close();

        _ = Assert.Throws<InvalidOperationException>(() => TestReceiver.Read());

        TestSender.Close();

        Assert.That(TestReceiver.IsOpen, Is.False);
        Assert.That(TestSender.IsOpen, Is.False);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender.LastError, Is.Empty);
    }

    [Test]
    public void TestWriteError()
    {
        using Channel TestReceiver = new(TestGuid, Mode.Receive);
        using Channel TestSender = new(TestGuid, Mode.Send);

        Assert.That(TestReceiver, Is.Not.Null);
        Assert.That(TestSender, Is.Not.Null);

        TestReceiver.Open();
        TestSender.Open();

        Assert.That(TestReceiver.IsOpen, Is.True);
        Assert.That(TestSender.IsOpen, Is.True);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender.LastError, Is.Empty);

        const byte[] NullData = null!;
        byte[] DataSent = [0, 1, 2, 3, 4, 5];

        _ = Assert.Throws<ArgumentNullException>(() => TestSender.Write(NullData));

        TestSender.Close();

        _ = Assert.Throws<InvalidOperationException>(() => TestSender.Write(DataSent));

        TestReceiver.Close();

        Assert.That(TestReceiver.IsOpen, Is.False);
        Assert.That(TestSender.IsOpen, Is.False);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender.LastError, Is.Empty);
    }

    [Test]
    public void TestGetLengthError()
    {
        using Channel TestReceiver = new(TestGuid, Mode.Receive);
        using Channel TestSender = new(TestGuid, Mode.Send);

        Assert.That(TestReceiver, Is.Not.Null);
        Assert.That(TestSender, Is.Not.Null);

        TestReceiver.Open();
        TestSender.Open();

        Assert.That(TestReceiver.IsOpen, Is.True);
        Assert.That(TestSender.IsOpen, Is.True);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender.LastError, Is.Empty);

        TestSender.Close();
        TestReceiver.Close();

        Assert.That(TestReceiver.IsOpen, Is.False);
        Assert.That(TestSender.IsOpen, Is.False);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender.LastError, Is.Empty);

        _ = Assert.Throws<InvalidOperationException>(() => TestReceiver.GetFreeLength());
        _ = Assert.Throws<InvalidOperationException>(() => TestReceiver.GetUsedLength());
        _ = Assert.Throws<InvalidOperationException>(() => TestReceiver.GetStats(string.Empty));

        _ = Assert.Throws<InvalidOperationException>(() => TestSender.GetFreeLength());
        _ = Assert.Throws<InvalidOperationException>(() => TestSender.GetUsedLength());
        _ = Assert.Throws<InvalidOperationException>(() => TestSender.GetStats(string.Empty));
    }

    [Test]
    public void TestDispose()
    {
        using Channel TestReceiver = new(TestGuid, Mode.Receive);
        using Channel TestSender = new(TestGuid, Mode.Send);

        Assert.That(TestReceiver, Is.Not.Null);
        Assert.That(TestSender, Is.Not.Null);

        TestReceiver.Open();
        TestSender.Open();

        using TestChannelChild TestObject = new(TestGuid, Mode.Receive);
    }

    [Test]
    public void TestReadWriteLarge()
    {
        using Channel TestReceiver = new(TestGuid, Mode.Receive);
        using Channel TestSender = new(TestGuid, Mode.Send);

        Assert.That(TestReceiver, Is.Not.Null);
        Assert.That(TestSender, Is.Not.Null);

        TestReceiver.Open();
        TestSender.Open();

        Assert.That(TestReceiver.IsOpen, Is.True);
        Assert.That(TestSender.IsOpen, Is.True);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender.LastError, Is.Empty);

        byte[] DataSent = new byte[(Channel.Capacity * 3) / 16];
        for (int i = 0; i < DataSent.Length; i++)
            DataSent[i] = (byte)i;

        for (int i = 0; i < 16; i++)
        {
            TestSender.Write(DataSent);

            Assert.That(TestSender.GetFreeLength(), Is.EqualTo(Channel.Capacity - 1 - DataSent.Length));
            Assert.That(TestSender.GetUsedLength(), Is.EqualTo(DataSent.Length));

            Assert.That(TestReceiver.GetFreeLength(), Is.EqualTo(Channel.Capacity - 1 - DataSent.Length));
            Assert.That(TestReceiver.GetUsedLength(), Is.EqualTo(DataSent.Length));

            byte[]? DataReceived = TestReceiver.Read();

            Assert.That(DataReceived, Is.Not.Null);
            Assert.That(DataReceived, Is.EqualTo(DataSent));

            Assert.That(TestSender.GetFreeLength(), Is.EqualTo(Channel.Capacity - 1));
            Assert.That(TestSender.GetUsedLength(), Is.EqualTo(0));

            Assert.That(TestReceiver.GetFreeLength(), Is.EqualTo(Channel.Capacity - 1));
            Assert.That(TestReceiver.GetUsedLength(), Is.EqualTo(0));
        }

        byte[]? LastDataReceived = TestReceiver.Read();

        Assert.That(LastDataReceived, Is.Null);

        TestReceiver.Close();
        TestSender.Close();

        Assert.That(TestReceiver.IsOpen, Is.False);
        Assert.That(TestSender.IsOpen, Is.False);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender.LastError, Is.Empty);
    }

    [Test]
    public void TestReadHalfFill()
    {
        using Channel TestReceiver = new(TestGuid, Mode.Receive);
        using Channel TestSender = new(TestGuid, Mode.Send);

        Assert.That(TestReceiver, Is.Not.Null);
        Assert.That(TestSender, Is.Not.Null);

        TestReceiver.Open();
        TestSender.Open();

        Assert.That(TestReceiver.IsOpen, Is.True);
        Assert.That(TestSender.IsOpen, Is.True);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender.LastError, Is.Empty);

        byte[] DataSent = new byte[(Channel.Capacity * 3) / 16];

        for (int i = 0; i < 4; i++)
            TestSender.Write(DataSent);

        for (int i = 0; i < 4; i++)
            _ = TestReceiver.Read();

        for (int i = 0; i < 4; i++)
            TestSender.Write(DataSent);

        for (int i = 0; i < 4; i++)
            _ = TestReceiver.Read();

        for (int i = 0; i < 4; i++)
            TestSender.Write(DataSent);

        for (int i = 0; i < 4; i++)
            _ = TestReceiver.Read();

        for (int i = 0; i < 4; i++)
            TestSender.Write(DataSent);

        for (int i = 0; i < 4; i++)
            _ = TestReceiver.Read();

        TestReceiver.Close();
        TestSender.Close();

        Assert.That(TestReceiver.IsOpen, Is.False);
        Assert.That(TestSender.IsOpen, Is.False);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender.LastError, Is.Empty);
    }

    [Test]
    public void TestWriteSaturate1()
    {
        using Channel TestReceiver = new(TestGuid, Mode.Receive);
        using Channel TestSender = new(TestGuid, Mode.Send);

        Assert.That(TestReceiver, Is.Not.Null);
        Assert.That(TestSender, Is.Not.Null);

        TestReceiver.Open();
        TestSender.Open();

        Assert.That(TestReceiver.IsOpen, Is.True);
        Assert.That(TestSender.IsOpen, Is.True);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender.LastError, Is.Empty);

        byte[] DataSent = new byte[(Channel.Capacity * 3) / 16];

        for (int i = 0; i < 5; i++)
            TestSender.Write(DataSent);

        int FreeLength = TestSender.GetFreeLength();
        Assert.That(FreeLength, Is.LessThan(DataSent.Length));
        _ = Assert.Throws<InvalidOperationException>(() => TestSender.Write(DataSent));

        TestReceiver.Close();
        TestSender.Close();

        Assert.That(TestReceiver.IsOpen, Is.False);
        Assert.That(TestSender.IsOpen, Is.False);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender.LastError, Is.Empty);
    }

    [Test]
    public void TestWriteSaturate2()
    {
        using Channel TestReceiver = new(TestGuid, Mode.Receive);
        using Channel TestSender = new(TestGuid, Mode.Send);

        Assert.That(TestReceiver, Is.Not.Null);
        Assert.That(TestSender, Is.Not.Null);

        TestReceiver.Open();
        TestSender.Open();

        Assert.That(TestReceiver.IsOpen, Is.True);
        Assert.That(TestSender.IsOpen, Is.True);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender.LastError, Is.Empty);

        byte[] DataSent = new byte[(Channel.Capacity * 3) / 16];

        for (int i = 0; i < 3; i++)
        {
            TestSender.Write(DataSent);
            _ = TestReceiver.Read();
        }

        for (int i = 0; i < 5; i++)
            TestSender.Write(DataSent);

        int FreeLength = TestSender.GetFreeLength();
        Assert.That(FreeLength, Is.LessThan(DataSent.Length));
        _ = Assert.Throws<InvalidOperationException>(() => TestSender.Write(DataSent));

        TestReceiver.Close();
        TestSender.Close();

        Assert.That(TestReceiver.IsOpen, Is.False);
        Assert.That(TestSender.IsOpen, Is.False);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender.LastError, Is.Empty);
    }
}
