using System;
using System.Globalization;
using PayrollEngine.Client.Model;

namespace PayrollEngine.Client.Test.Payrun;

/// <summary>Payrun test result</summary>
public class PayrunTestResult : TestResultBase<PayrunResult>
{
    /// <summary>The test culture</summary>
    public CultureInfo Culture { get; set; }

    /// <summary>Initializes a new instance of the <see cref="PayrunTestResult"/> class</summary>
    /// <param name="culture">The culture</param>
    /// <param name="expectedResult">The expected result</param>
    /// <param name="actualResult">The actual result</param>
    public PayrunTestResult(CultureInfo culture, PayrunResult expectedResult, PayrunResult actualResult = null) :
        base(expectedResult, actualResult)
    {
        Culture = culture ?? throw new ArgumentNullException(nameof(culture));
    }

    /// <inheritdoc />
    public override bool IsValidResult()
    {
        if (ExpectedResult == null || ActualResult == null)
        {
            return false;
        }

        var expectedValue = ValueConvert.ToValue(ExpectedResult.Value, ExpectedResult.ValueType, Culture);
        var actualValue = ValueConvert.ToValue(ActualResult.Value, ActualResult.ValueType, Culture);
        return Equals(expectedValue, actualValue);
    }
}