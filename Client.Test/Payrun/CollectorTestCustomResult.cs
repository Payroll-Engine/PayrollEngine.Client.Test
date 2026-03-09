using PayrollEngine.Client.Model;

namespace PayrollEngine.Client.Test.Payrun;

/// <summary>Collector test custom result</summary>
public class CollectorTestCustomResult : NumericTestResultBase<CollectorCustomResult>
{

    /// <summary>Initializes a new instance of the <see cref="CollectorTestCustomResult"/> class</summary>
    /// <param name="testPrecision">The testing precision</param>
    /// <param name="expectedResult">The expected result</param>
    /// <param name="actualResult">The actual result</param>
    public CollectorTestCustomResult(TestPrecision testPrecision, CollectorCustomResult expectedResult,
        CollectorCustomResult actualResult = null) :
        base(testPrecision, expectedResult, actualResult)
    {
    }

    /// <inheritdoc />
    public override bool ValidCulture() =>
        string.IsNullOrWhiteSpace(ExpectedResult.Culture) ||
        string.Equals(ExpectedResult.Culture, ActualResult.Culture);
}
