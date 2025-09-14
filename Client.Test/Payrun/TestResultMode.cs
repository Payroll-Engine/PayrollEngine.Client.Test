
namespace PayrollEngine.Client.Test.Payrun;

/// <summary>The test result mode</summary>
public enum TestResultMode
{
    /// <summary>Cleanup test results</summary>
    CleanTest,

    /// <summary>Keep test results of failed test (manual cleanup)</summary>
    KeepFailedTest,

    /// <summary>Keep test results (manual cleanup)</summary>
    KeepTest
}