using System.Collections.Generic;
using System.Linq;

namespace PayrollEngine.Client.Test.Case;

/// <summary>Result of case test</summary>
public class CaseTestResult
{
    /// <summary>The test results</summary>
    public List<CaseScriptTestResult> Results { get; } = new();

    /// <summary>Test if case test is failed</summary>
    public bool IsFailed() =>
        Results.Any(x => x.Failed);
}