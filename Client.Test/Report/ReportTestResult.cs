using System.Collections.Generic;
using System.Linq;

namespace PayrollEngine.Client.Test.Report;

/// <summary>Result of report test</summary>
public class ReportTestResult
{
    /// <summary>The test results</summary>
    public List<ReportScriptTestResult> Results { get; } = new();

    /// <summary>Test if report test is failed</summary>
    public bool IsFailed() =>
        Results.Any(x => x.Failed);
}