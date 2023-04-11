using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace PayrollEngine.Client.Test;

/// <summary>Case test compile exception</summary>
public class TestCompileException : PayrollException
{
    /// <summary>Initializes a new instance of the <see cref="TestCompileException"></see> class.</summary>
    /// <param name="message">The exception message</param>
    /// <param name="innerException">The inner exception</param>
    public TestCompileException(string message, Exception innerException) :
        base(message, innerException)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="TestCompileException"></see> class.</summary>
    /// <param name="info">The serialization info</param>
    /// <param name="context">The streaming context</param>
    protected TestCompileException(SerializationInfo info, StreamingContext context) :
        base(info, context)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="TestCompileException"></see> class.</summary>
    /// <param name="failures">The diagnostic results</param>
    internal TestCompileException(IList<string> failures) :
        base(GetMessage(failures))
    {
    }

    private static string GetMessage(IList<string> failures)
    {
        if (failures.Count == 1)
        {
            return failures[0];
        }

        var buffer = new StringBuilder();
        buffer.AppendLine($"{failures.Count} compile errors:");
        foreach (var failure in failures)
        {
            buffer.AppendLine(failure);
        }
        return buffer.ToString();
    }
}