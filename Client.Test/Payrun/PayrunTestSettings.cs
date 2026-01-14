namespace PayrollEngine.Client.Test.Payrun;

/// <summary>
/// Payrun test settings
/// </summary>
public class PayrunTestSettings
{
    /// <summary>
    /// Test precision (default: 2 digits)
    /// </summary>
    public TestPrecision TestPrecision { get; set; } = TestPrecision.TestPrecision2;

    /// <summary>
    /// Result mode (default: clean test)
    /// </summary>
    public TestResultMode ResultMode { get; set; } = TestResultMode.CleanTest;

    /// <summary>
    /// Owner
    /// </summary>
    public string Owner { get; set; }

    /// <summary>
    /// Result retry delay in milliseconds (default: 1000 ms)
    /// </summary>
    public int ResultRetryDelay { get; set; } = 1000;

    /// <summary>
    /// Result retry count (default: 50)
    /// </summary>
    public int ResultRetryCount { get; set; } = 50;
}