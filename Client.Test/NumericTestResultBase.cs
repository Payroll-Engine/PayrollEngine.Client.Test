using PayrollEngine.Client.Model;

namespace PayrollEngine.Client.Test;

/// <summary>Result of test</summary>
public abstract class NumericTestResultBase<T> : TestResultBase<T>
    where T : IAttributeObject, INumericValueResult
{
    /// <summary>The testing precision</summary>
    public TestPrecision TestPrecision { get; }

    /// <summary>Initializes a new instance of the <see cref="TestResultBase{T}"/> class</summary>
    /// <param name="testPrecision">The testing precision</param>
    /// <param name="expectedResult">The expected result</param>
    /// <param name="actualResult">The actual result</param>
    protected NumericTestResultBase(TestPrecision testPrecision, T expectedResult,
        T actualResult = default) :
        base(expectedResult, actualResult)
    {
        TestPrecision = testPrecision;
    }

    /// <inheritdoc />
    public override bool ValidValue()
    {
        // actual result is null but expected value is non-zero → invalid
        var nullMismatch = ActualResult == null && ExpectedResult.Value != null;

        // actual result exists but doesn't match expected value within precision → invalid
        var valueMismatch = ActualResult != null &&
                            !ActualResult.AlmostEqualValue(ExpectedResult.Value, TestPrecision.GetDecimals());

        // attributes are invalid
        var attributesInvalid = !ValidAttributes();

        if (nullMismatch || valueMismatch || attributesInvalid)
        {
            return false;
        }
        return true;
    }
}