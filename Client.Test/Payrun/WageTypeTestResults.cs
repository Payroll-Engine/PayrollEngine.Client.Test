using System.Collections.Generic;
using PayrollEngine.Client.Model;

namespace PayrollEngine.Client.Test.Payrun;

/// <summary>Wage type test result</summary>
public class WageTypeTestResult : TestResultBase<WageTypeResultSet>
{
    /// <summary>The wage type custom results</summary>
    public IList<WageTypeTestCustomResult> CustomResults { get; } = new List<WageTypeTestCustomResult>();

    /// <summary>The testing precision</summary>
    public TestPrecision TestPrecision { get; }

    /// <summary>Initializes a new instance of the <see cref="WageTypeTestResult"/> class</summary>
    /// <param name="testPrecision">The testing precision</param>
    /// <param name="expectedResult">The expected result</param>
    /// <param name="actualResult">The actual result</param>
    public WageTypeTestResult(TestPrecision testPrecision, WageTypeResultSet expectedResult, WageTypeResultSet actualResult = null) :
        base(expectedResult, actualResult)
    {
        TestPrecision = testPrecision;
    }

    /// <summary>Test for invalid result</summary>
    public bool IsInvalidResult() =>
        ActualResult == null && ExpectedResult.Value != default ||
        ActualResult != null && !ActualResult.AlmostEqualValue(ExpectedResult.Value, TestPrecision.GetDecimals()) ||
        !ValidAttributes();

    /// <inheritdoc />
    public override bool IsValidResult() => !IsInvalidResult();
}