namespace ProcessCommunication;

using System;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using Contracts;

/// <summary>
/// Provides methods to manage circular buffers.
/// </summary>
internal static class CircularBufferHelper
{
    /// <summary>
    /// Reads data from a circular buffer.
    /// </summary>
    /// <param name="accessor">The buffer accessor.</param>
    /// <param name="capacity">The buffer capacity.</param>
    /// <param name="data">The data upon return if successful.</param>
    public static bool Read(MemoryMappedViewAccessor accessor, int capacity, out byte[] data)
    {
        int EndOfBuffer = capacity;
        accessor.Read(EndOfBuffer, out int Head);
        accessor.Read(EndOfBuffer + sizeof(int), out int Tail);

        if (Head > Tail)
        {
            int Length = Head - Tail;
            data = new byte[Length];
            int Read = accessor.ReadArray(Tail, data, 0, Length);
            Debug.Assert(Read == Length);
        }
        else if (Head < Tail)
        {
            int Length = EndOfBuffer - Tail + Head;
            data = new byte[Length];
            int Read = accessor.ReadArray(Tail, data, 0, EndOfBuffer - Tail);
            Read += accessor.ReadArray(0, data, EndOfBuffer - Tail, Head);
            Debug.Assert(Read == Length);
        }
        else
        {
            Contract.Unused(out data);
            return false;
        }

        // Copy head to tail.
        accessor.Write(EndOfBuffer + sizeof(int), Head);

        return true;
    }

    /// <summary>
    /// Writes data to a circular buffer. There must be enough room to write the data.
    /// </summary>
    /// <param name="accessor">The buffer accessor.</param>
    /// <param name="capacity">The buffer capacity.</param>
    /// <param name="data">The data to write.</param>
    /// <exception cref="InvalidOperationException">There no room to write the data.</exception>
    public static void Write(MemoryMappedViewAccessor accessor, int capacity, byte[] data)
    {
        int EndOfBuffer = capacity;
        accessor.Read(EndOfBuffer, out int Head);
        accessor.Read(EndOfBuffer + sizeof(int), out int Tail);

        int Length = data.Length;

        if (Tail > Head)
        {
            if (Length > Tail - Head - 1)
                throw new InvalidOperationException();

            accessor.WriteArray(Head, data, 0, data.Length);
        }
        else
        {
            if (Length > EndOfBuffer - Head + Tail - 1)
                throw new InvalidOperationException();

            int FirstCopyLength = Math.Min(EndOfBuffer - Head, Length);
            int SecondCopyLength = Length - FirstCopyLength;

            accessor.WriteArray(Head, data, 0, FirstCopyLength);
            accessor.WriteArray(0, data, FirstCopyLength, SecondCopyLength);
        }

        Head += Length;
        if (Head >= EndOfBuffer)
            Head -= EndOfBuffer;

        // Copy the new head.
        accessor.Write(EndOfBuffer, Head);
    }

    /// <summary>
    /// Gets the number of free bytes in the circular buffer.
    /// </summary>
    /// <param name="accessor">The buffer accessor.</param>
    /// <param name="capacity">The buffer capacity.</param>
    public static int GetFreeLength(MemoryMappedViewAccessor accessor, int capacity)
    {
        int EndOfBuffer = capacity;
        accessor.Read(EndOfBuffer, out int Head);
        accessor.Read(EndOfBuffer + sizeof(int), out int Tail);

        return GetFreeLength(Head, Tail, capacity);
    }

    /// <summary>
    /// Gets the number of free bytes in the circular buffer.
    /// </summary>
    /// <param name="head">The head position.</param>
    /// <param name="tail">The tail position.</param>
    /// <param name="capacity">The buffer capacity.</param>
    public static int GetFreeLength(int head, int tail, int capacity)
    {
        return tail > head
            ? tail - head - 1
            : capacity - head + tail - 1;
    }

    /// <summary>
    /// Gets the number of free bytes in the circular buffer.
    /// </summary>
    /// <param name="accessor">The buffer accessor.</param>
    /// <param name="capacity">The buffer capacity.</param>
    public static int GetUsedLength(MemoryMappedViewAccessor accessor, int capacity)
    {
        int EndOfBuffer = capacity;
        accessor.Read(EndOfBuffer, out int Head);
        accessor.Read(EndOfBuffer + sizeof(int), out int Tail);

        return GetUsedLength(Head, Tail, capacity);
    }

    /// <summary>
    /// Gets the number of used bytes in the circular buffer.
    /// </summary>
    /// <param name="head">The head position.</param>
    /// <param name="tail">The tail position.</param>
    /// <param name="capacity">The buffer capacity.</param>
    public static int GetUsedLength(int head, int tail, int capacity)
    {
        return head >= tail
            ? head - tail
            : capacity - tail + head;
    }
}
