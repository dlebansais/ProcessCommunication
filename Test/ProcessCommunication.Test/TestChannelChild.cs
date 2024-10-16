namespace ProcessCommunication;

using System;

public class TestChannelChild(Guid guid, Mode mode) : Channel(guid, mode)
{
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }
}