using PayrollEngine.Client.Model;

namespace PayrollEngine.Client.Test.Payrun;

/// <summary>The wage type test custom result</summary>
public class WageTypeTestCustomResult : TestResultBase<WageTypeCustomResult>
{

    /// <summary>The testing precision</summary>
    public TestPrecision TestPrecision { get; }

    /// <summary>Initializes a new instance of the <see cref="WageTypeTestCustomResult"/> class</summary>
    /// <param name="testPrecision">The testing precision</param>
    /// <param name="expectedResult">The expected result</param>
    /// <param name="actualResult">The actual result</param>
    public WageTypeTestCustomResult(TestPrecision testPrecision, WageTypeCustomResult expectedResult, WageTypeCustomResult actualResult = null) :
        base(expectedResult, actualResult)
    {
        TestPrecision = testPrecision;
    }

    /// <inheritdoc />
    public override bool ValidCulture() =>
        string.IsNullOrWhiteSpace(ExpectedResult.Culture) ||
        string.Equals(ExpectedResult.Culture, ActualResult.Culture);

    /// <inheritdoc />
    public override bool ValidValue() =>
        !(ActualResult == null ||
        ActualResult != null &&
        !ActualResult.Value.AlmostEquals(ExpectedResult.Value, TestPrecision.GetDecimals()) ||
        !ValidAttributes());
}