namespace ProcessCommunication.Test;

using System;
using NUnit.Framework;

[TestFixture]
internal class TestChannel
{
    internal static readonly Guid TestGuid = new("20E9C969-C990-4DEB-984F-979C824DCC18");

    [Test]
    public void TestSuccess()
    {
        using Channel TestReceiver = new(TestGuid, ChannelMode.Receive);
        using Channel TestSender = new(TestGuid, ChannelMode.Send);

        Assert.That(TestReceiver, Is.Not.Null);
        Assert.That(TestSender, Is.Not.Null);

        TestReceiver.Open();
        TestSender.Open();

        Assert.That(TestReceiver.IsOpen, Is.True);
        Assert.That(TestSender.IsOpen, Is.True);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender.LastError, Is.Empty);

        Assert.That(TestReceiver.GetFreeLength(), Is.EqualTo(Channel.Capacity - 1));
        Assert.That(TestReceiver.GetUsedLength(), Is.Zero);
        Assert.That(TestSender.GetFreeLength(), Is.EqualTo(Channel.Capacity - 1));
        Assert.That(TestSender.GetUsedLength(), Is.Zero);

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
        using Channel TestReceiver = new(TestGuid, ChannelMode.Receive);
        using Channel TestSender = new(TestGuid, ChannelMode.Send);

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

        bool IsDataReceived = TestReceiver.TryRead(out byte[] DataReceived);

        Assert.That(IsDataReceived, Is.True);
        Assert.That(DataReceived, Is.Not.Null);
        Assert.That(DataReceived, Is.EqualTo(DataSent));

        Assert.That(TestSender.GetFreeLength(), Is.EqualTo(Channel.Capacity - 1));
        Assert.That(TestSender.GetUsedLength(), Is.Zero);

        Assert.That(TestReceiver.GetFreeLength(), Is.EqualTo(Channel.Capacity - 1));
        Assert.That(TestReceiver.GetUsedLength(), Is.Zero);

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
        using Channel TestReceiver = new(TestGuid, ChannelMode.Receive);

        TestReceiver.Open();

        Assert.That(TestReceiver.IsOpen, Is.True);

        _ = Assert.Throws<InvalidOperationException>(TestReceiver.Open);

        TestReceiver.Close();

        Assert.That(TestReceiver.IsOpen, Is.False);
    }

    [Test]
    public void TestMultipleOpenReceiver()
    {
        using Channel TestReceiver1 = new(TestGuid, ChannelMode.Receive);
        using Channel TestReceiver2 = new(TestGuid, ChannelMode.Receive);

        TestReceiver1.Open();
        TestReceiver2.Open();

        Assert.That(TestReceiver1.IsOpen, Is.True);
        Assert.That(TestReceiver1.LastError, Is.Empty);

        Assert.That(TestReceiver2.IsOpen, Is.False);
        Assert.That(TestReceiver2.LastError, Is.Not.Empty);
    }

    [Test]
    public void TestMultipleOpenSender()
    {
        using Channel TestReceiver = new(TestGuid, ChannelMode.Receive);
        using Channel Sender1 = new(TestGuid, ChannelMode.Send);
        using Channel Sender2 = new(TestGuid, ChannelMode.Send);

        TestReceiver.Open();
        Sender1.Open();
        Sender2.Open();

        Assert.That(TestReceiver.IsOpen, Is.True);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(Sender1.IsOpen, Is.True);
        Assert.That(Sender1.LastError, Is.Empty);
        Assert.That(Sender2.IsOpen, Is.False);
        Assert.That(Sender2.LastError, Is.Not.Empty);
    }

    [Test]
    public void TestMultipleClose()
    {
        using Channel TestReceiver = new(TestGuid, ChannelMode.Receive);

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
        using Channel TestReceiver = new(TestGuid, ChannelMode.Receive);
        using Channel TestSender = new(TestGuid, ChannelMode.Send);

        Assert.That(TestReceiver, Is.Not.Null);
        Assert.That(TestSender, Is.Not.Null);

        _ = Assert.Throws<InvalidOperationException>(() => TestReceiver.TryRead(out _));

        TestReceiver.Open();
        TestSender.Open();

        Assert.That(TestReceiver.IsOpen, Is.True);
        Assert.That(TestSender.IsOpen, Is.True);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender.LastError, Is.Empty);

        byte[] DataSent = [0, 1, 2, 3, 4, 5];
        TestSender.Write(DataSent);

        TestReceiver.Close();

        _ = Assert.Throws<InvalidOperationException>(() => TestReceiver.TryRead(out _));
        _ = Assert.Throws<InvalidOperationException>(() => TestSender.TryRead(out _));

        TestSender.Close();

        Assert.That(TestReceiver.IsOpen, Is.False);
        Assert.That(TestSender.IsOpen, Is.False);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender.LastError, Is.Empty);
    }

    [Test]
    public void TestWriteError()
    {
        const byte[] NullData = null!;
        byte[] DataSent = [0, 1, 2, 3, 4, 5];

        using Channel TestReceiver = new(TestGuid, ChannelMode.Receive);
        using Channel TestSender = new(TestGuid, ChannelMode.Send);

        Assert.That(TestReceiver, Is.Not.Null);
        Assert.That(TestSender, Is.Not.Null);

        _ = Assert.Throws<ArgumentNullException>(() => TestSender.Write(NullData));

        TestReceiver.Open();
        TestSender.Open();

        Assert.That(TestReceiver.IsOpen, Is.True);
        Assert.That(TestSender.IsOpen, Is.True);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender.LastError, Is.Empty);

        _ = Assert.Throws<InvalidOperationException>(() => TestReceiver.Write(DataSent));
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
        const string NullName = null!;

        using Channel TestReceiver = new(TestGuid, ChannelMode.Receive);
        using Channel TestSender = new(TestGuid, ChannelMode.Send);

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

        _ = Assert.Throws<ArgumentNullException>(() => TestReceiver.GetStats(NullName));
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
        using Channel TestReceiver = new(TestGuid, ChannelMode.Receive);
        using Channel TestSender = new(TestGuid, ChannelMode.Send);

        Assert.That(TestReceiver, Is.Not.Null);
        Assert.That(TestSender, Is.Not.Null);

        TestReceiver.Open();
        TestSender.Open();

        using TestChannelChild TestObject = new(TestGuid, ChannelMode.Receive);
    }

    [Test]
    public void TestReadWriteLarge()
    {
        using Channel TestReceiver = new(TestGuid, ChannelMode.Receive);
        using Channel TestSender = new(TestGuid, ChannelMode.Send);

        Assert.That(TestReceiver, Is.Not.Null);
        Assert.That(TestSender, Is.Not.Null);

        TestReceiver.Open();
        TestSender.Open();

        Assert.That(TestReceiver.IsOpen, Is.True);
        Assert.That(TestSender.IsOpen, Is.True);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender.LastError, Is.Empty);

        byte[] DataSent = new byte[Channel.Capacity * 3 / 16];
        for (int i = 0; i < DataSent.Length; i++)
            DataSent[i] = (byte)i;

        for (int i = 0; i < 16; i++)
        {
            TestSender.Write(DataSent);

            Assert.That(TestSender.GetFreeLength(), Is.EqualTo(Channel.Capacity - 1 - DataSent.Length));
            Assert.That(TestSender.GetUsedLength(), Is.EqualTo(DataSent.Length));

            Assert.That(TestReceiver.GetFreeLength(), Is.EqualTo(Channel.Capacity - 1 - DataSent.Length));
            Assert.That(TestReceiver.GetUsedLength(), Is.EqualTo(DataSent.Length));

            bool IsDataReceived = TestReceiver.TryRead(out byte[] DataReceived);

            Assert.That(IsDataReceived, Is.True);
            Assert.That(DataReceived, Is.Not.Null);
            Assert.That(DataReceived, Is.EqualTo(DataSent));

            Assert.That(TestSender.GetFreeLength(), Is.EqualTo(Channel.Capacity - 1));
            Assert.That(TestSender.GetUsedLength(), Is.Zero);

            Assert.That(TestReceiver.GetFreeLength(), Is.EqualTo(Channel.Capacity - 1));
            Assert.That(TestReceiver.GetUsedLength(), Is.Zero);
        }

        bool IsLastDataReceived = TestReceiver.TryRead(out _);

        Assert.That(IsLastDataReceived, Is.False);

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
        using Channel TestReceiver = new(TestGuid, ChannelMode.Receive);
        using Channel TestSender = new(TestGuid, ChannelMode.Send);

        Assert.That(TestReceiver, Is.Not.Null);
        Assert.That(TestSender, Is.Not.Null);

        TestReceiver.Open();
        TestSender.Open();

        Assert.That(TestReceiver.IsOpen, Is.True);
        Assert.That(TestSender.IsOpen, Is.True);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender.LastError, Is.Empty);

        byte[] DataSent = new byte[Channel.Capacity * 3 / 16];

        for (int i = 0; i < 4; i++)
            TestSender.Write(DataSent);

        for (int i = 0; i < 4; i++)
            _ = TestReceiver.TryRead(out _);

        for (int i = 0; i < 4; i++)
            TestSender.Write(DataSent);

        for (int i = 0; i < 4; i++)
            _ = TestReceiver.TryRead(out _);

        for (int i = 0; i < 4; i++)
            TestSender.Write(DataSent);

        for (int i = 0; i < 4; i++)
            _ = TestReceiver.TryRead(out _);

        for (int i = 0; i < 4; i++)
            TestSender.Write(DataSent);

        for (int i = 0; i < 4; i++)
            _ = TestReceiver.TryRead(out _);

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
        using Channel TestReceiver = new(TestGuid, ChannelMode.Receive);
        using Channel TestSender = new(TestGuid, ChannelMode.Send);

        Assert.That(TestReceiver, Is.Not.Null);
        Assert.That(TestSender, Is.Not.Null);

        TestReceiver.Open();
        TestSender.Open();

        Assert.That(TestReceiver.IsOpen, Is.True);
        Assert.That(TestSender.IsOpen, Is.True);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender.LastError, Is.Empty);

        byte[] DataSent = new byte[Channel.Capacity * 3 / 16];

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
        using Channel TestReceiver = new(TestGuid, ChannelMode.Receive);
        using Channel TestSender = new(TestGuid, ChannelMode.Send);

        Assert.That(TestReceiver, Is.Not.Null);
        Assert.That(TestSender, Is.Not.Null);

        TestReceiver.Open();
        TestSender.Open();

        Assert.That(TestReceiver.IsOpen, Is.True);
        Assert.That(TestSender.IsOpen, Is.True);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender.LastError, Is.Empty);

        byte[] DataSent = new byte[Channel.Capacity * 3 / 16];

        for (int i = 0; i < 3; i++)
        {
            TestSender.Write(DataSent);
            _ = TestReceiver.TryRead(out _);
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

    [Test]
    public void TestCapacity()
    {
        using Channel TestReceiver1 = new(TestGuid, ChannelMode.Receive);
        using Channel TestSender1 = new(TestGuid, ChannelMode.Send);

        Assert.That(TestReceiver1, Is.Not.Null);
        Assert.That(TestSender1, Is.Not.Null);

        TestReceiver1.Open();
        TestSender1.Open();

        Assert.That(TestReceiver1.IsOpen, Is.True);
        Assert.That(TestSender1.IsOpen, Is.True);
        Assert.That(TestReceiver1.LastError, Is.Empty);
        Assert.That(TestSender1.LastError, Is.Empty);

        int OldCapacity = Channel.Capacity;

        Assert.That(OldCapacity, Is.GreaterThan(0));

        Channel.Capacity = -1;

        Assert.That(TestReceiver1.GetFreeLength(), Is.EqualTo(OldCapacity - 1));
        Assert.That(TestReceiver1.GetUsedLength(), Is.Zero);
        Assert.That(TestSender1.GetFreeLength(), Is.EqualTo(OldCapacity - 1));
        Assert.That(TestSender1.GetUsedLength(), Is.Zero);

        TestReceiver1.Close();
        TestSender1.Close();

        Assert.That(TestReceiver1.IsOpen, Is.False);
        Assert.That(TestSender1.IsOpen, Is.False);
        Assert.That(TestReceiver1.LastError, Is.Empty);
        Assert.That(TestSender1.LastError, Is.Empty);

        using Channel TestReceiver2 = new(TestGuid, ChannelMode.Receive);
        using Channel TestSender2 = new(TestGuid, ChannelMode.Send);

        Assert.That(TestReceiver2, Is.Not.Null);
        Assert.That(TestSender2, Is.Not.Null);

        TestReceiver2.Open();
        TestSender2.Open();

        Assert.That(TestReceiver2.IsOpen, Is.True);
        Assert.That(TestSender2.IsOpen, Is.True);
        Assert.That(TestReceiver2.LastError, Is.Empty);
        Assert.That(TestSender2.LastError, Is.Empty);

        Assert.That(TestReceiver2.GetFreeLength(), Is.GreaterThan(0));
        Assert.That(TestReceiver2.GetFreeLength(), Is.LessThan(OldCapacity));
        Assert.That(TestReceiver2.GetUsedLength(), Is.Zero);
        Assert.That(TestSender2.GetFreeLength(), Is.GreaterThan(0));
        Assert.That(TestSender2.GetFreeLength(), Is.LessThan(OldCapacity));
        Assert.That(TestSender2.GetUsedLength(), Is.Zero);

        Channel.Capacity = OldCapacity;
    }
}
