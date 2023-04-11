using System;
using System.Runtime.Serialization;

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

    /// <inheritdoc/>
    protected AssertFailedException(SerializationInfo info, StreamingContext context) :
        base(info, context)
    {
    }
}