using PayrollEngine.Client.Model;

namespace PayrollEngine.Client.Test.Payrun;

/// <summary>Payrun test result</summary>
public class PayrunTestResult : TestResultBase<PayrunResult>
{
    /// <summary>Initializes a new instance of the <see cref="PayrunTestResult"/> class</summary>
    /// <param name="expectedResult">The expected result</param>
    /// <param name="actualResult">The actual result</param>
    public PayrunTestResult(PayrunResult expectedResult, PayrunResult actualResult = null) :
        base(expectedResult, actualResult)
    {
    }

    /// <inheritdoc />
    public override bool IsValidResult()
    {
        if (ExpectedResult == null || ActualResult == null)
        {
            return false;
        }

        var expectedValue = ValueConvert.ToValue(ExpectedResult.Value, ExpectedResult.ValueType);
        var actualValue = ValueConvert.ToValue(ActualResult.Value, ActualResult.ValueType);
        return Equals(expectedValue, actualValue);
    }
}