﻿namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Services;

public class ErrorThrowingStream : MemoryStream
{
    public ErrorThrowingStream(byte[] buffer)
        : base(buffer)
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new IOException("Simulated read error");
    }
}