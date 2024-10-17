namespace ProcessCommunication.Test;

using System;
using NUnit.Framework;

[TestFixture]
public class TestConverter
{
    [Test]
    public void TestSuccess()
    {
        const string TestString = "test";

        byte[] Encoded = Converter.EncodeString(TestString);

        int Offset = 0;
        bool Success = Converter.TryDecodeString(Encoded, ref Offset, out string DecodedString);

        Assert.That(Success, Is.True);
        Assert.That(DecodedString, Is.EqualTo(TestString));
    }

    [Test]
    public void TestDecodeError()
    {
        const byte[] NullData = null!;

        int Offset = 0;
        _ = Assert.Throws<ArgumentNullException>(() => Converter.TryDecodeString(NullData, ref Offset, out _));
    }

    [Test]
    public void TestDecodeInvalidOffset()
    {
        const string TestString = "test";

        byte[] Encoded = Converter.EncodeString(TestString);
        int Offset;
        bool Success;

        Offset = 1;
        Success = Converter.TryDecodeString(Encoded, ref Offset, out _);

        Assert.That(Success, Is.False);

        Offset = int.MaxValue / 2;
        Success = Converter.TryDecodeString(Encoded, ref Offset, out _);

        Assert.That(Success, Is.False);
    }

    [Test]
    public void TestDecodeInvalidData()
    {
        const string TestString = "test";

        byte[] Encoded = Converter.EncodeString(TestString);

        for (int i = 0; i < Encoded.Length; i++)
            Encoded[i] = 0;

        int Offset = 0;
        bool Success = Converter.TryDecodeString(Encoded, ref Offset, out _);

        Assert.That(Success, Is.False);
    }
}
