using PayrollEngine.Client.Model;

namespace PayrollEngine.Client.Test.Payrun;

/// <summary>Collector test custom result</summary>
public class CollectorTestCustomResult : TestResultBase<CollectorCustomResult>
{
    /// <summary>The testing precision</summary>
    public TestPrecision TestPrecision { get; }

    /// <summary>Initializes a new instance of the <see cref="CollectorTestCustomResult"/> class</summary>
    /// <param name="testPrecision">The testing precision</param>
    /// <param name="expectedResult">The expected result</param>
    /// <param name="actualResult">The actual result</param>
    public CollectorTestCustomResult(TestPrecision testPrecision, CollectorCustomResult expectedResult,
        CollectorCustomResult actualResult = null) :
        base(expectedResult, actualResult)
    {
        TestPrecision = testPrecision;
    }

    /// <summary>Test for invalid result</summary>
    /// <inheritdoc />
    public override bool ValidValue() =>
        !(ActualResult == null ||
        ActualResult != null &&
        !ActualResult.Value.AlmostEquals(ExpectedResult.Value, TestPrecision.GetDecimals()) ||
        !ValidAttributes());

    /// <inheritdoc />
    public override bool ValidCulture() =>
        string.IsNullOrWhiteSpace(ExpectedResult.Culture) ||
        string.Equals(ExpectedResult.Culture, ActualResult.Culture);
}
