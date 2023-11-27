using System;

namespace PayrollEngine.Client.Test;

/// <summary>Assert failed exception</summary>
public class AssertFailedException : PayrollException
{
    /// <inheritdoc/>
    public AssertFailedException()
    {
    }

    /// <inheritdoc/>
    public AssertFailedException(string message) :
        base(message)
    {
    }

    /// <inheritdoc/>
    public AssertFailedException(string message, Exception innerException) :
        base(message, innerException)
    {
    }
}