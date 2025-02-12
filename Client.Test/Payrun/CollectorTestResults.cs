using System.Collections.Generic;
using PayrollEngine.Client.Model;

namespace PayrollEngine.Client.Test.Payrun;

/// <summary>Collector test result</summary>
public class CollectorTestResult : TestResultBase<CollectorResultSet>
{
    /// <summary>The collector custom results</summary>
    public IList<CollectorTestCustomResult> CustomResults { get; } = new List<CollectorTestCustomResult>();

    /// <summary>The testing precision</summary>
    public TestPrecision TestPrecision { get; }

    /// <summary>Initializes a new instance of the <see cref="CollectorTestResult"/> class</summary>
    /// <param name="testPrecision">The testing precision</param>
    /// <param name="expectedResult">The expected result</param>
    /// <param name="actualResult">The actual result</param>
    public CollectorTestResult(TestPrecision testPrecision, CollectorResultSet expectedResult, CollectorResultSet actualResult = null) :
        base(expectedResult, actualResult)
    {
        TestPrecision = testPrecision;
    }

    /// <summary>Test for invalid result</summary>
    public bool IsInvalidResult() =>
        ActualResult == null && ExpectedResult.Value != 0 ||
        ActualResult != null && !ActualResult.AlmostEqualValue(ExpectedResult.Value, TestPrecision.GetDecimals()) ||
        !ValidAttributes();

    /// <inheritdoc />
    public override bool IsValidResult() => !IsInvalidResult();
}