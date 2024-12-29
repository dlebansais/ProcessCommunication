namespace ProcessCommunication.Test;

using System;
using NUnit.Framework;

[TestFixture]
internal class TestMultiChannel
{
    internal static readonly Guid TestGuid = new("20E9C969-C990-4DEB-984F-979C824DCC18");

    [Test]
    public void TestSuccess()
    {
        using MultiChannel TestReceiver = new(TestGuid, ChannelMode.Receive, 1);
        using MultiChannel TestSender = new(TestGuid, ChannelMode.Send, 1);

        Assert.That(TestReceiver, Is.Not.Null);
        Assert.That(TestSender, Is.Not.Null);

        TestReceiver.Open();
        TestSender.Open();

        Assert.That(TestReceiver.IsOpen, Is.True);
        Assert.That(TestSender.IsOpen, Is.True);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender.LastError, Is.Empty);
        Assert.That(TestSender.IsConnected(0), Is.True);

        Assert.That(TestReceiver.GetFreeLength(0), Is.EqualTo(MultiChannel.Capacity - 1));
        Assert.That(TestReceiver.GetUsedLength(0), Is.EqualTo(0));
        Assert.That(TestSender.GetFreeLength(), Is.EqualTo(MultiChannel.Capacity - 1));
        Assert.That(TestSender.GetUsedLength(), Is.EqualTo(0));
        Assert.That(TestSender.GetFreeLength(0), Is.EqualTo(MultiChannel.Capacity - 1));
        Assert.That(TestSender.GetUsedLength(0), Is.EqualTo(0));

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
        using MultiChannel TestReceiver = new(TestGuid, ChannelMode.Receive, 1);
        using MultiChannel TestSender = new(TestGuid, ChannelMode.Send, 1);

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

        Assert.That(TestSender.GetFreeLength(), Is.EqualTo(MultiChannel.Capacity - 1 - DataSent.Length));
        Assert.That(TestSender.GetUsedLength(), Is.EqualTo(DataSent.Length));
        Assert.That(TestSender.GetFreeLength(0), Is.EqualTo(MultiChannel.Capacity - 1 - DataSent.Length));
        Assert.That(TestSender.GetUsedLength(0), Is.EqualTo(DataSent.Length));

        Assert.That(TestReceiver.GetFreeLength(0), Is.EqualTo(MultiChannel.Capacity - 1 - DataSent.Length));
        Assert.That(TestReceiver.GetUsedLength(0), Is.EqualTo(DataSent.Length));

        bool IsDataReceived = TestReceiver.TryRead(out byte[] DataReceived, out int Index);

        Assert.That(IsDataReceived, Is.True);
        Assert.That(DataReceived, Is.Not.Null);
        Assert.That(DataReceived, Is.EqualTo(DataSent));
        Assert.That(Index, Is.EqualTo(0));

        Assert.That(TestSender.GetFreeLength(), Is.EqualTo(MultiChannel.Capacity - 1));
        Assert.That(TestSender.GetUsedLength(), Is.EqualTo(0));
        Assert.That(TestSender.GetFreeLength(0), Is.EqualTo(MultiChannel.Capacity - 1));
        Assert.That(TestSender.GetUsedLength(0), Is.EqualTo(0));

        Assert.That(TestReceiver.GetFreeLength(0), Is.EqualTo(MultiChannel.Capacity - 1));
        Assert.That(TestReceiver.GetUsedLength(0), Is.EqualTo(0));

        string ReceiverStats = TestReceiver.GetStats(string.Empty, 0);
        Assert.That(ReceiverStats, Is.Not.Empty);

        string SenderStats;

        SenderStats = TestSender.GetStats(string.Empty);
        Assert.That(SenderStats, Is.Not.Empty);
        SenderStats = TestSender.GetStats(string.Empty, 0);
        Assert.That(SenderStats, Is.Not.Empty);

        TestReceiver.Close();
        TestSender.Close();

        Assert.That(TestReceiver.IsOpen, Is.False);
        Assert.That(TestSender.IsOpen, Is.False);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender.LastError, Is.Empty);
    }

    [Test]
    public void TestReadWriteSeveralChannels()
    {
        using MultiChannel TestReceiver = new(TestGuid, ChannelMode.Receive, 4);
        using MultiChannel TestSender1 = new(TestGuid, ChannelMode.Send, 4);
        using MultiChannel TestSender2 = new(TestGuid, ChannelMode.Send, 4);
        using MultiChannel TestSender3 = new(TestGuid, ChannelMode.Send, 4);

        Assert.That(TestReceiver, Is.Not.Null);
        Assert.That(TestSender1, Is.Not.Null);
        Assert.That(TestSender2, Is.Not.Null);
        Assert.That(TestSender3, Is.Not.Null);

        TestReceiver.Open();
        TestSender1.Open();
        TestSender2.Open();
        TestSender3.Open();

        Assert.That(TestReceiver.IsOpen, Is.True);
        Assert.That(TestSender1.IsOpen, Is.True);
        Assert.That(TestSender2.IsOpen, Is.True);
        Assert.That(TestSender3.IsOpen, Is.True);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender1.LastError, Is.Empty);
        Assert.That(TestSender2.LastError, Is.Empty);
        Assert.That(TestSender3.LastError, Is.Empty);

        byte[] DataSent = [0, 1, 2, 3, 4, 5];
        TestSender1.Write(DataSent);
        TestSender2.Write(DataSent);
        TestSender3.Write(DataSent);

        Assert.That(TestSender1.GetFreeLength(), Is.EqualTo(MultiChannel.Capacity - 1 - DataSent.Length));
        Assert.That(TestSender1.GetUsedLength(), Is.EqualTo(DataSent.Length));
        Assert.That(TestSender1.GetFreeLength(0), Is.EqualTo(MultiChannel.Capacity - 1 - DataSent.Length));
        Assert.That(TestSender1.GetUsedLength(0), Is.EqualTo(DataSent.Length));
        Assert.That(TestSender2.GetFreeLength(), Is.EqualTo(MultiChannel.Capacity - 1 - DataSent.Length));
        Assert.That(TestSender2.GetUsedLength(), Is.EqualTo(DataSent.Length));
        Assert.That(TestSender2.GetFreeLength(1), Is.EqualTo(MultiChannel.Capacity - 1 - DataSent.Length));
        Assert.That(TestSender2.GetUsedLength(1), Is.EqualTo(DataSent.Length));
        Assert.That(TestSender3.GetFreeLength(), Is.EqualTo(MultiChannel.Capacity - 1 - DataSent.Length));
        Assert.That(TestSender3.GetUsedLength(), Is.EqualTo(DataSent.Length));
        Assert.That(TestSender3.GetFreeLength(2), Is.EqualTo(MultiChannel.Capacity - 1 - DataSent.Length));
        Assert.That(TestSender3.GetUsedLength(2), Is.EqualTo(DataSent.Length));

        Assert.That(TestReceiver.GetFreeLength(0), Is.EqualTo(MultiChannel.Capacity - 1 - DataSent.Length));
        Assert.That(TestReceiver.GetUsedLength(0), Is.EqualTo(DataSent.Length));
        Assert.That(TestReceiver.GetFreeLength(1), Is.EqualTo(MultiChannel.Capacity - 1 - DataSent.Length));
        Assert.That(TestReceiver.GetUsedLength(1), Is.EqualTo(DataSent.Length));
        Assert.That(TestReceiver.GetFreeLength(2), Is.EqualTo(MultiChannel.Capacity - 1 - DataSent.Length));
        Assert.That(TestReceiver.GetUsedLength(2), Is.EqualTo(DataSent.Length));

        bool IsDataReceived;

        IsDataReceived = TestReceiver.TryRead(out byte[] DataReceived, out int Index);

        Assert.That(IsDataReceived, Is.True);
        Assert.That(DataReceived, Is.Not.Null);
        Assert.That(DataReceived, Is.EqualTo(DataSent));
        Assert.That(Index, Is.EqualTo(0));

        Assert.That(TestSender1.GetFreeLength(), Is.EqualTo(MultiChannel.Capacity - 1));
        Assert.That(TestSender1.GetUsedLength(), Is.EqualTo(0));
        Assert.That(TestSender1.GetFreeLength(0), Is.EqualTo(MultiChannel.Capacity - 1));
        Assert.That(TestSender1.GetUsedLength(0), Is.EqualTo(0));
        Assert.That(TestSender2.GetFreeLength(), Is.EqualTo(MultiChannel.Capacity - 1 - DataSent.Length));
        Assert.That(TestSender2.GetUsedLength(), Is.EqualTo(DataSent.Length));
        Assert.That(TestSender2.GetFreeLength(1), Is.EqualTo(MultiChannel.Capacity - 1 - DataSent.Length));
        Assert.That(TestSender2.GetUsedLength(1), Is.EqualTo(DataSent.Length));
        Assert.That(TestSender3.GetFreeLength(), Is.EqualTo(MultiChannel.Capacity - 1 - DataSent.Length));
        Assert.That(TestSender3.GetUsedLength(), Is.EqualTo(DataSent.Length));
        Assert.That(TestSender3.GetFreeLength(2), Is.EqualTo(MultiChannel.Capacity - 1 - DataSent.Length));
        Assert.That(TestSender3.GetUsedLength(2), Is.EqualTo(DataSent.Length));

        Assert.That(TestReceiver.GetFreeLength(0), Is.EqualTo(MultiChannel.Capacity - 1));
        Assert.That(TestReceiver.GetUsedLength(0), Is.EqualTo(0));
        Assert.That(TestReceiver.GetFreeLength(1), Is.EqualTo(MultiChannel.Capacity - 1 - DataSent.Length));
        Assert.That(TestReceiver.GetUsedLength(1), Is.EqualTo(DataSent.Length));
        Assert.That(TestReceiver.GetFreeLength(2), Is.EqualTo(MultiChannel.Capacity - 1 - DataSent.Length));
        Assert.That(TestReceiver.GetUsedLength(2), Is.EqualTo(DataSent.Length));

        IsDataReceived = TestReceiver.TryRead(out DataReceived, out Index);

        Assert.That(IsDataReceived, Is.True);
        Assert.That(DataReceived, Is.Not.Null);
        Assert.That(DataReceived, Is.EqualTo(DataSent));
        Assert.That(Index, Is.EqualTo(1));

        Assert.That(TestSender1.GetFreeLength(), Is.EqualTo(MultiChannel.Capacity - 1));
        Assert.That(TestSender1.GetUsedLength(), Is.EqualTo(0));
        Assert.That(TestSender1.GetFreeLength(0), Is.EqualTo(MultiChannel.Capacity - 1));
        Assert.That(TestSender1.GetUsedLength(0), Is.EqualTo(0));
        Assert.That(TestSender2.GetFreeLength(), Is.EqualTo(MultiChannel.Capacity - 1));
        Assert.That(TestSender2.GetUsedLength(), Is.EqualTo(0));
        Assert.That(TestSender2.GetFreeLength(1), Is.EqualTo(MultiChannel.Capacity - 1));
        Assert.That(TestSender2.GetUsedLength(1), Is.EqualTo(0));
        Assert.That(TestSender3.GetFreeLength(), Is.EqualTo(MultiChannel.Capacity - 1 - DataSent.Length));
        Assert.That(TestSender3.GetUsedLength(), Is.EqualTo(DataSent.Length));
        Assert.That(TestSender3.GetFreeLength(2), Is.EqualTo(MultiChannel.Capacity - 1 - DataSent.Length));
        Assert.That(TestSender3.GetUsedLength(2), Is.EqualTo(DataSent.Length));

        Assert.That(TestReceiver.GetFreeLength(0), Is.EqualTo(MultiChannel.Capacity - 1));
        Assert.That(TestReceiver.GetUsedLength(0), Is.EqualTo(0));
        Assert.That(TestReceiver.GetFreeLength(1), Is.EqualTo(MultiChannel.Capacity - 1));
        Assert.That(TestReceiver.GetUsedLength(1), Is.EqualTo(0));
        Assert.That(TestReceiver.GetFreeLength(2), Is.EqualTo(MultiChannel.Capacity - 1 - DataSent.Length));
        Assert.That(TestReceiver.GetUsedLength(2), Is.EqualTo(DataSent.Length));

        IsDataReceived = TestReceiver.TryRead(out DataReceived, out Index);

        Assert.That(IsDataReceived, Is.True);
        Assert.That(DataReceived, Is.Not.Null);
        Assert.That(DataReceived, Is.EqualTo(DataSent));
        Assert.That(Index, Is.EqualTo(2));

        Assert.That(TestSender1.GetFreeLength(), Is.EqualTo(MultiChannel.Capacity - 1));
        Assert.That(TestSender1.GetUsedLength(), Is.EqualTo(0));
        Assert.That(TestSender1.GetFreeLength(0), Is.EqualTo(MultiChannel.Capacity - 1));
        Assert.That(TestSender1.GetUsedLength(0), Is.EqualTo(0));
        Assert.That(TestSender2.GetFreeLength(), Is.EqualTo(MultiChannel.Capacity - 1));
        Assert.That(TestSender2.GetUsedLength(), Is.EqualTo(0));
        Assert.That(TestSender2.GetFreeLength(1), Is.EqualTo(MultiChannel.Capacity - 1));
        Assert.That(TestSender2.GetUsedLength(1), Is.EqualTo(0));
        Assert.That(TestSender3.GetFreeLength(), Is.EqualTo(MultiChannel.Capacity - 1));
        Assert.That(TestSender3.GetUsedLength(), Is.EqualTo(0));
        Assert.That(TestSender3.GetFreeLength(2), Is.EqualTo(MultiChannel.Capacity - 1));
        Assert.That(TestSender3.GetUsedLength(2), Is.EqualTo(0));

        Assert.That(TestReceiver.GetFreeLength(0), Is.EqualTo(MultiChannel.Capacity - 1));
        Assert.That(TestReceiver.GetUsedLength(0), Is.EqualTo(0));
        Assert.That(TestReceiver.GetFreeLength(1), Is.EqualTo(MultiChannel.Capacity - 1));
        Assert.That(TestReceiver.GetUsedLength(1), Is.EqualTo(0));
        Assert.That(TestReceiver.GetFreeLength(2), Is.EqualTo(MultiChannel.Capacity - 1));
        Assert.That(TestReceiver.GetUsedLength(2), Is.EqualTo(0));

        IsDataReceived = TestReceiver.TryRead(out _, out _);
        Assert.That(IsDataReceived, Is.False);
    }

    [Test]
    public void TestOpenError()
    {
        using MultiChannel TestReceiver = new(TestGuid, ChannelMode.Receive, 1);

        TestReceiver.Open();

        Assert.That(TestReceiver.IsOpen, Is.True);

        _ = Assert.Throws<InvalidOperationException>(TestReceiver.Open);

        TestReceiver.Close();

        Assert.That(TestReceiver.IsOpen, Is.False);
    }

    [Test]
    public void TestIsConnectedError()
    {
        using MultiChannel TestReceiver = new(TestGuid, ChannelMode.Receive, 1);
        using MultiChannel TestSender = new(TestGuid, ChannelMode.Send, 1);

        Assert.That(TestReceiver, Is.Not.Null);
        Assert.That(TestSender, Is.Not.Null);

        TestReceiver.Open();
        TestSender.Open();

        Assert.That(TestReceiver.IsOpen, Is.True);
        Assert.That(TestSender.IsOpen, Is.True);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender.LastError, Is.Empty);

        Assert.That(TestSender.IsConnected(0), Is.True);
        _ = Assert.Throws<ArgumentOutOfRangeException>(() => TestSender.IsConnected(-1));
        _ = Assert.Throws<ArgumentOutOfRangeException>(() => TestSender.IsConnected(1));

        Assert.That(TestReceiver.GetFreeLength(0), Is.EqualTo(MultiChannel.Capacity - 1));
        Assert.That(TestReceiver.GetUsedLength(0), Is.EqualTo(0));
        Assert.That(TestSender.GetFreeLength(), Is.EqualTo(MultiChannel.Capacity - 1));
        Assert.That(TestSender.GetUsedLength(), Is.EqualTo(0));
        Assert.That(TestSender.GetFreeLength(0), Is.EqualTo(MultiChannel.Capacity - 1));
        Assert.That(TestSender.GetUsedLength(0), Is.EqualTo(0));

        TestReceiver.Close();
        TestSender.Close();

        Assert.That(TestReceiver.IsOpen, Is.False);
        Assert.That(TestSender.IsOpen, Is.False);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender.LastError, Is.Empty);
    }

    [Test]
    public void TestMultipleOpen1()
    {
        using MultiChannel TestReceiver1 = new(TestGuid, ChannelMode.Receive, 1);
        using MultiChannel TestReceiver2 = new(TestGuid, ChannelMode.Receive, 1);

        TestReceiver1.Open();
        TestReceiver2.Open();

        Assert.That(TestReceiver1.IsOpen, Is.True);
        Assert.That(TestReceiver1.LastError, Is.Empty);

        Assert.That(TestReceiver2.IsOpen, Is.False);
        Assert.That(TestReceiver2.LastError, Is.Not.Empty);
    }

    [Test]
    public void TestMultipleOpen2()
    {
        using MultiChannel TestReceiver = new(TestGuid, ChannelMode.Receive, 1);
        using MultiChannel TestSender1 = new(TestGuid, ChannelMode.Send, 1);
        using MultiChannel TestSender2 = new(TestGuid, ChannelMode.Send, 1);

        TestReceiver.Open();
        TestSender1.Open();
        TestSender2.Open();

        Assert.That(TestReceiver.IsOpen, Is.True);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender1.IsOpen, Is.True);
        Assert.That(TestSender1.LastError, Is.Empty);
        Assert.That(TestSender2.IsOpen, Is.False);
        Assert.That(TestSender2.LastError, Is.Not.Empty);
    }

    [Test]
    public void TestMultipleClose()
    {
        using MultiChannel TestReceiver = new(TestGuid, ChannelMode.Receive, 1);

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
        using MultiChannel TestReceiver = new(TestGuid, ChannelMode.Receive, 1);
        using MultiChannel TestSender = new(TestGuid, ChannelMode.Send, 1);

        Assert.That(TestReceiver, Is.Not.Null);
        Assert.That(TestSender, Is.Not.Null);

        _ = Assert.Throws<InvalidOperationException>(() => TestReceiver.TryRead(out _, out _));
        _ = Assert.Throws<InvalidOperationException>(() => TestSender.TryRead(out _, out _));

        TestReceiver.Open();
        TestSender.Open();

        Assert.That(TestReceiver.IsOpen, Is.True);
        Assert.That(TestSender.IsOpen, Is.True);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender.LastError, Is.Empty);

        byte[] DataSent = [0, 1, 2, 3, 4, 5];
        TestSender.Write(DataSent);

        TestReceiver.Close();

        _ = Assert.Throws<InvalidOperationException>(() => TestReceiver.TryRead(out _, out _));

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

        using MultiChannel TestReceiver = new(TestGuid, ChannelMode.Receive, 1);
        using MultiChannel TestSender = new(TestGuid, ChannelMode.Send, 1);

        Assert.That(TestReceiver, Is.Not.Null);
        Assert.That(TestSender, Is.Not.Null);

        _ = Assert.Throws<ArgumentNullException>(() => TestSender.Write(NullData));

        TestReceiver.Open();
        TestSender.Open();

        Assert.That(TestReceiver.IsOpen, Is.True);
        Assert.That(TestSender.IsOpen, Is.True);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender.LastError, Is.Empty);

        _ = Assert.Throws<ArgumentNullException>(() => TestSender.Write(NullData));
        _ = Assert.Throws<InvalidOperationException>(() => TestReceiver.Write(DataSent));

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
        using MultiChannel TestReceiver = new(TestGuid, ChannelMode.Receive, 1);
        using MultiChannel TestSender = new(TestGuid, ChannelMode.Send, 1);

        Assert.That(TestReceiver, Is.Not.Null);
        Assert.That(TestSender, Is.Not.Null);

        _ = Assert.Throws<InvalidOperationException>(() => TestReceiver.GetFreeLength());
        _ = Assert.Throws<InvalidOperationException>(() => TestReceiver.GetFreeLength(0));
        _ = Assert.Throws<InvalidOperationException>(() => TestReceiver.GetUsedLength());
        _ = Assert.Throws<InvalidOperationException>(() => TestReceiver.GetUsedLength(0));
        _ = Assert.Throws<InvalidOperationException>(() => TestSender.GetFreeLength());
        _ = Assert.Throws<InvalidOperationException>(() => TestSender.GetFreeLength(0));
        _ = Assert.Throws<InvalidOperationException>(() => TestSender.GetUsedLength());
        _ = Assert.Throws<InvalidOperationException>(() => TestSender.GetUsedLength(0));

        TestReceiver.Open();
        TestSender.Open();

        Assert.That(TestReceiver.IsOpen, Is.True);
        Assert.That(TestSender.IsOpen, Is.True);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender.LastError, Is.Empty);

        _ = Assert.Throws<ArgumentOutOfRangeException>(() => TestReceiver.GetFreeLength(-1));
        _ = Assert.Throws<ArgumentOutOfRangeException>(() => TestReceiver.GetFreeLength(1));
        _ = Assert.Throws<ArgumentOutOfRangeException>(() => TestReceiver.GetUsedLength(-1));
        _ = Assert.Throws<ArgumentOutOfRangeException>(() => TestReceiver.GetUsedLength(1));

        TestSender.Close();
        TestReceiver.Close();

        Assert.That(TestReceiver.IsOpen, Is.False);
        Assert.That(TestSender.IsOpen, Is.False);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender.LastError, Is.Empty);
    }

    [Test]
    public void TestDispose()
    {
        using MultiChannel TestReceiver = new(TestGuid, ChannelMode.Receive, 1);
        using MultiChannel TestSender = new(TestGuid, ChannelMode.Send, 1);

        Assert.That(TestReceiver, Is.Not.Null);
        Assert.That(TestSender, Is.Not.Null);

        TestReceiver.Open();
        TestSender.Open();

        using TestMultiChannelChild TestObject = new(TestGuid, ChannelMode.Receive, 1);
    }

    [Test]
    public void TestReadWriteLarge()
    {
        using MultiChannel TestReceiver = new(TestGuid, ChannelMode.Receive, 1);
        using MultiChannel TestSender = new(TestGuid, ChannelMode.Send, 1);

        Assert.That(TestReceiver, Is.Not.Null);
        Assert.That(TestSender, Is.Not.Null);

        TestReceiver.Open();
        TestSender.Open();

        Assert.That(TestReceiver.IsOpen, Is.True);
        Assert.That(TestSender.IsOpen, Is.True);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender.LastError, Is.Empty);

        byte[] DataSent = new byte[MultiChannel.Capacity * 3 / 16];
        for (int i = 0; i < DataSent.Length; i++)
            DataSent[i] = (byte)i;

        for (int i = 0; i < 16; i++)
        {
            TestSender.Write(DataSent);

            Assert.That(TestSender.GetFreeLength(), Is.EqualTo(MultiChannel.Capacity - 1 - DataSent.Length));
            Assert.That(TestSender.GetUsedLength(), Is.EqualTo(DataSent.Length));
            Assert.That(TestSender.GetFreeLength(0), Is.EqualTo(MultiChannel.Capacity - 1 - DataSent.Length));
            Assert.That(TestSender.GetUsedLength(0), Is.EqualTo(DataSent.Length));

            Assert.That(TestReceiver.GetFreeLength(0), Is.EqualTo(MultiChannel.Capacity - 1 - DataSent.Length));
            Assert.That(TestReceiver.GetUsedLength(0), Is.EqualTo(DataSent.Length));

            bool IsDataReceived = TestReceiver.TryRead(out byte[] DataReceived, out int Index);

            Assert.That(IsDataReceived, Is.True);
            Assert.That(DataReceived, Is.Not.Null);
            Assert.That(DataReceived, Is.EqualTo(DataSent));
            Assert.That(Index, Is.EqualTo(0));

            Assert.That(TestSender.GetFreeLength(), Is.EqualTo(MultiChannel.Capacity - 1));
            Assert.That(TestSender.GetUsedLength(), Is.EqualTo(0));
            Assert.That(TestSender.GetFreeLength(0), Is.EqualTo(MultiChannel.Capacity - 1));
            Assert.That(TestSender.GetUsedLength(0), Is.EqualTo(0));

            Assert.That(TestReceiver.GetFreeLength(0), Is.EqualTo(MultiChannel.Capacity - 1));
            Assert.That(TestReceiver.GetUsedLength(0), Is.EqualTo(0));
        }

        bool IsLastDataReceived = TestReceiver.TryRead(out _, out _);

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
        using MultiChannel TestReceiver = new(TestGuid, ChannelMode.Receive, 1);
        using MultiChannel TestSender = new(TestGuid, ChannelMode.Send, 1);

        Assert.That(TestReceiver, Is.Not.Null);
        Assert.That(TestSender, Is.Not.Null);

        TestReceiver.Open();
        TestSender.Open();

        Assert.That(TestReceiver.IsOpen, Is.True);
        Assert.That(TestSender.IsOpen, Is.True);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender.LastError, Is.Empty);

        byte[] DataSent = new byte[MultiChannel.Capacity * 3 / 16];

        for (int i = 0; i < 4; i++)
            TestSender.Write(DataSent);

        for (int i = 0; i < 4; i++)
            _ = TestReceiver.TryRead(out _, out _);

        for (int i = 0; i < 4; i++)
            TestSender.Write(DataSent);

        for (int i = 0; i < 4; i++)
            _ = TestReceiver.TryRead(out _, out _);

        for (int i = 0; i < 4; i++)
            TestSender.Write(DataSent);

        for (int i = 0; i < 4; i++)
            _ = TestReceiver.TryRead(out _, out _);

        for (int i = 0; i < 4; i++)
            TestSender.Write(DataSent);

        for (int i = 0; i < 4; i++)
            _ = TestReceiver.TryRead(out _, out _);

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
        using MultiChannel TestReceiver = new(TestGuid, ChannelMode.Receive, 1);
        using MultiChannel TestSender = new(TestGuid, ChannelMode.Send, 1);

        Assert.That(TestReceiver, Is.Not.Null);
        Assert.That(TestSender, Is.Not.Null);

        TestReceiver.Open();
        TestSender.Open();

        Assert.That(TestReceiver.IsOpen, Is.True);
        Assert.That(TestSender.IsOpen, Is.True);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender.LastError, Is.Empty);

        byte[] DataSent = new byte[MultiChannel.Capacity * 3 / 16];

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
        using MultiChannel TestReceiver = new(TestGuid, ChannelMode.Receive, 1);
        using MultiChannel TestSender = new(TestGuid, ChannelMode.Send, 1);

        Assert.That(TestReceiver, Is.Not.Null);
        Assert.That(TestSender, Is.Not.Null);

        TestReceiver.Open();
        TestSender.Open();

        Assert.That(TestReceiver.IsOpen, Is.True);
        Assert.That(TestSender.IsOpen, Is.True);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender.LastError, Is.Empty);

        byte[] DataSent = new byte[MultiChannel.Capacity * 3 / 16];

        for (int i = 0; i < 3; i++)
        {
            TestSender.Write(DataSent);
            _ = TestReceiver.TryRead(out _, out _);
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
        using MultiChannel TestReceiver1 = new(TestGuid, ChannelMode.Receive, 1);
        using MultiChannel TestSender1 = new(TestGuid, ChannelMode.Send, 1);

        Assert.That(TestReceiver1, Is.Not.Null);
        Assert.That(TestSender1, Is.Not.Null);

        TestReceiver1.Open();
        TestSender1.Open();

        Assert.That(TestReceiver1.IsOpen, Is.True);
        Assert.That(TestSender1.IsOpen, Is.True);
        Assert.That(TestReceiver1.LastError, Is.Empty);
        Assert.That(TestSender1.LastError, Is.Empty);

        int OldCapacity = MultiChannel.Capacity;

        Assert.That(OldCapacity, Is.GreaterThan(0));

        MultiChannel.Capacity = -1;

        Assert.That(TestReceiver1.GetFreeLength(0), Is.EqualTo(OldCapacity - 1));
        Assert.That(TestReceiver1.GetUsedLength(0), Is.EqualTo(0));
        Assert.That(TestSender1.GetFreeLength(), Is.EqualTo(OldCapacity - 1));
        Assert.That(TestSender1.GetUsedLength(), Is.EqualTo(0));
        Assert.That(TestSender1.GetFreeLength(0), Is.EqualTo(OldCapacity - 1));
        Assert.That(TestSender1.GetUsedLength(0), Is.EqualTo(0));

        TestReceiver1.Close();
        TestSender1.Close();

        using MultiChannel TestReceiver2 = new(TestGuid, ChannelMode.Receive, 1);
        using MultiChannel TestSender2 = new(TestGuid, ChannelMode.Send, 1);

        TestReceiver2.Open();
        TestSender2.Open();

        Assert.That(TestReceiver2.IsOpen, Is.True);
        Assert.That(TestSender2.IsOpen, Is.True);
        Assert.That(TestReceiver2.LastError, Is.Empty);
        Assert.That(TestSender2.LastError, Is.Empty);

        Assert.That(TestReceiver2.GetFreeLength(0), Is.GreaterThan(0));
        Assert.That(TestReceiver2.GetFreeLength(0), Is.LessThan(OldCapacity));
        Assert.That(TestReceiver2.GetUsedLength(0), Is.EqualTo(0));
        Assert.That(TestReceiver2.GetUsedLength(0), Is.LessThan(OldCapacity));
        Assert.That(TestSender2.GetFreeLength(), Is.GreaterThan(0));
        Assert.That(TestSender2.GetFreeLength(), Is.LessThan(OldCapacity));
        Assert.That(TestSender2.GetUsedLength(), Is.EqualTo(0));
        Assert.That(TestSender2.GetFreeLength(0), Is.GreaterThan(0));
        Assert.That(TestSender2.GetFreeLength(0), Is.LessThan(OldCapacity));
        Assert.That(TestSender2.GetUsedLength(0), Is.EqualTo(0));

        TestReceiver2.Close();
        TestSender2.Close();

        Assert.That(TestReceiver2.IsOpen, Is.False);
        Assert.That(TestSender2.IsOpen, Is.False);
        Assert.That(TestReceiver2.LastError, Is.Empty);
        Assert.That(TestSender2.LastError, Is.Empty);

        MultiChannel.Capacity = OldCapacity;
    }

    [Test]
    public void TestSuccessWithInvalidChannelCount()
    {
        using MultiChannel TestReceiver = new(TestGuid, ChannelMode.Receive, 0);
        using MultiChannel TestSender = new(TestGuid, ChannelMode.Send, 0);

        Assert.That(TestReceiver, Is.Not.Null);
        Assert.That(TestSender, Is.Not.Null);

        TestReceiver.Open();
        TestSender.Open();

        Assert.That(TestReceiver.IsOpen, Is.True);
        Assert.That(TestSender.IsOpen, Is.True);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender.LastError, Is.Empty);

        Assert.That(TestReceiver.GetFreeLength(0), Is.EqualTo(MultiChannel.Capacity - 1));
        Assert.That(TestReceiver.GetUsedLength(0), Is.EqualTo(0));
        Assert.That(TestSender.GetFreeLength(), Is.EqualTo(MultiChannel.Capacity - 1));
        Assert.That(TestSender.GetUsedLength(), Is.EqualTo(0));
        Assert.That(TestSender.GetFreeLength(0), Is.EqualTo(MultiChannel.Capacity - 1));
        Assert.That(TestSender.GetUsedLength(0), Is.EqualTo(0));

        TestReceiver.Close();
        TestSender.Close();

        Assert.That(TestReceiver.IsOpen, Is.False);
        Assert.That(TestSender.IsOpen, Is.False);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender.LastError, Is.Empty);
    }

    [Test]
    public void TestGetStatsError()
    {
        const string NullName = null!;
        using MultiChannel TestReceiver = new(TestGuid, ChannelMode.Receive, 2);
        using MultiChannel TestSender = new(TestGuid, ChannelMode.Send, 2);

        Assert.That(TestReceiver, Is.Not.Null);
        Assert.That(TestSender, Is.Not.Null);

        _ = Assert.Throws<InvalidOperationException>(() => TestReceiver.GetStats(string.Empty));
        _ = Assert.Throws<InvalidOperationException>(() => TestSender.GetStats(string.Empty));

        TestReceiver.Open();
        TestSender.Open();

        Assert.That(TestReceiver.IsOpen, Is.True);
        Assert.That(TestSender.IsOpen, Is.True);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender.LastError, Is.Empty);

        _ = Assert.Throws<ArgumentNullException>(() => TestReceiver.GetStats(NullName));
        _ = Assert.Throws<ArgumentNullException>(() => TestSender.GetStats(NullName));
        _ = Assert.Throws<ArgumentNullException>(() => TestReceiver.GetStats(NullName, 0));
        _ = Assert.Throws<ArgumentNullException>(() => TestSender.GetStats(NullName, 0));
        _ = Assert.Throws<ArgumentOutOfRangeException>(() => TestReceiver.GetStats(string.Empty, -1));
        _ = Assert.Throws<ArgumentOutOfRangeException>(() => TestReceiver.GetStats(string.Empty, TestReceiver.ChannelCount));
        _ = Assert.Throws<InvalidOperationException>(() => TestSender.GetStats(string.Empty, 1));

        TestSender.Close();
        TestReceiver.Close();

        Assert.That(TestReceiver.IsOpen, Is.False);
        Assert.That(TestSender.IsOpen, Is.False);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender.LastError, Is.Empty);
    }

    [Test]
    public void TestSeveralChannels()
    {
        using MultiChannel TestReceiver = new(TestGuid, ChannelMode.Receive, 2);
        using MultiChannel TestSender1 = new(TestGuid, ChannelMode.Send, 2);
        using MultiChannel TestSender2 = new(TestGuid, ChannelMode.Send, 2);
        using MultiChannel TestSender3 = new(TestGuid, ChannelMode.Send, 2);

        Assert.That(TestReceiver, Is.Not.Null);
        Assert.That(TestSender1, Is.Not.Null);
        Assert.That(TestSender2, Is.Not.Null);
        Assert.That(TestSender3, Is.Not.Null);

        TestReceiver.Open();
        TestSender1.Open();
        TestSender2.Open();
        TestSender3.Open();

        Assert.That(TestReceiver.IsOpen, Is.True);
        Assert.That(TestSender1.IsOpen, Is.True);
        Assert.That(TestSender2.IsOpen, Is.True);
        Assert.That(TestSender3.IsOpen, Is.False);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender1.LastError, Is.Empty);
        Assert.That(TestSender2.LastError, Is.Empty);
        Assert.That(TestSender3.LastError, Is.Not.Empty);
        Assert.That(TestSender1.IsConnected(0), Is.True);
        Assert.That(TestSender1.IsConnected(1), Is.False);
        Assert.That(TestSender2.IsConnected(0), Is.False);
        Assert.That(TestSender2.IsConnected(1), Is.True);

        Assert.That(TestReceiver.GetFreeLength(0), Is.EqualTo(MultiChannel.Capacity - 1));
        Assert.That(TestReceiver.GetUsedLength(0), Is.EqualTo(0));
        Assert.That(TestSender1.GetFreeLength(), Is.EqualTo(MultiChannel.Capacity - 1));
        Assert.That(TestSender1.GetUsedLength(), Is.EqualTo(0));
        Assert.That(TestSender1.GetFreeLength(0), Is.EqualTo(MultiChannel.Capacity - 1));
        Assert.That(TestSender1.GetUsedLength(0), Is.EqualTo(0));
        Assert.That(TestSender2.GetFreeLength(), Is.EqualTo(MultiChannel.Capacity - 1));
        Assert.That(TestSender2.GetUsedLength(), Is.EqualTo(0));
        Assert.That(TestSender2.GetFreeLength(1), Is.EqualTo(MultiChannel.Capacity - 1));
        Assert.That(TestSender2.GetUsedLength(1), Is.EqualTo(0));

        TestReceiver.Close();
        TestSender1.Close();
        TestSender2.Close();

        Assert.That(TestReceiver.IsOpen, Is.False);
        Assert.That(TestSender1.IsOpen, Is.False);
        Assert.That(TestSender2.IsOpen, Is.False);
        Assert.That(TestReceiver.LastError, Is.Empty);
        Assert.That(TestSender1.LastError, Is.Empty);
        Assert.That(TestSender2.LastError, Is.Empty);
    }
}
