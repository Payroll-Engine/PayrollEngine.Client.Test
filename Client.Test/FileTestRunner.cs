using System;

namespace PayrollEngine.Client.Test;

/// <summary>Base class for file based payroll tests</summary>
public abstract class FileTestRunner : TestRunnerBase
{
    /// <summary>The test file name</summary>
    public string FileName { get; }

    /// <summary>Initializes a new instance of the <see cref="FileTestRunner"/> class</summary>
    /// <param name="httpClient">The payroll engine http client</param>
    /// <param name="fileName">Name of the file</param>
    protected FileTestRunner(PayrollHttpClient httpClient, string fileName) :
        base(httpClient)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException(nameof(fileName));
        }
        FileName = fileName;
    }
}