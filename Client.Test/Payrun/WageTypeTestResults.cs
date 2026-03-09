using System.Collections.Generic;
using PayrollEngine.Client.Model;

namespace PayrollEngine.Client.Test.Payrun;

/// <summary>Wage type test result</summary>
public class WageTypeTestResult : NumericTestResultBase<WageTypeResultSet>
{
    /// <summary>The wage type custom results</summary>
    public IList<WageTypeTestCustomResult> CustomResults { get; } = new List<WageTypeTestCustomResult>();

    /// <summary>Initializes a new instance of the <see cref="WageTypeTestResult"/> class</summary>
    /// <param name="testPrecision">The testing precision</param>
    /// <param name="expectedResult">The expected result</param>
    /// <param name="actualResult">The actual result</param>
    public WageTypeTestResult(TestPrecision testPrecision, WageTypeResultSet expectedResult, WageTypeResultSet actualResult = null) :
        base(testPrecision, expectedResult, actualResult)
    {
    }

    /// <inheritdoc />
    public override bool ValidCulture() =>
        string.IsNullOrWhiteSpace(ExpectedResult.Culture) ||
        string.Equals(ExpectedResult.Culture, ActualResult.Culture);
}