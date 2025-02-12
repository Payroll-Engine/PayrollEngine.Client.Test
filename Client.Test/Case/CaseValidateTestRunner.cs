using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using PayrollEngine.Client.Model;

namespace PayrollEngine.Client.Test.Case;

/// <summary>Case validate function test runner
/// Compares the output case change values with the received case change values
/// </summary>
public class CaseValidateTestRunner : CaseScriptTestRunner
{
    /// <summary>new instance of <see cref="CaseValidateTestRunner"/>see</summary>
    /// <param name="httpClient">The payroll http client</param>
    /// <param name="context">The test context</param>
    public CaseValidateTestRunner(PayrollHttpClient httpClient, CaseTestContext context) :
        base(httpClient, context)
    {
    }

    /// <summary>Test the case validation</summary>
    /// <param name="test">The test name</param>
    /// <returns>The test results</returns>
    public virtual async Task<IList<CaseScriptTestResult>> Test(CaseValidateTest test)
    {
        if (test == null)
        {
            throw new ArgumentNullException(nameof(test));
        }
        var caseName = test.Input?.Case?.CaseName;
        if (string.IsNullOrWhiteSpace(caseName))
        {
            throw new ArgumentException("Validate test without case name.");
        }

        var results = new List<CaseScriptTestResult>();
        var expectedIssue = test.Output?.Issues?.FirstOrDefault();
        try
        {
            var caseSetup = await AddCaseAsync(test.Input);

            // compare expected case setup with received case setup
            var testResults = CompareCaseChange(test.TestName, test.Output, caseSetup);
            if (testResults.Any())
            {
                results.AddRange(testResults);
            }
            else if (expectedIssue != null)
            {
                // expected issue
                results.Add(NewFailedResult(CaseTestType.CaseValidate, test.TestName,
                    $"Case validate {caseName} should be failed", test.Output));
            }
            else
            {
                // valid
                results.Add(NewResult(CaseTestType.CaseValidate, test.TestName,
                    $"Case validate {caseName}", test.Output, caseSetup));
            }
        }
        catch (HttpRequestException exception)
        {
            if (expectedIssue != null)
            {
                var errorCode = exception.StatusCode.HasValue ? (int)exception.StatusCode : 0;
                if (expectedIssue.Number == errorCode)
                {
                    // expected issue
                    results.Add(NewResult(CaseTestType.CaseValidate, test.TestName,
                        $"Case validate {caseName} - issue {errorCode}", test.Output, errorCode));
                }
                else
                {
                    // expected issue failed
                    results.Add(NewFailedResult(CaseTestType.CaseValidate, test.TestName,
                        $"Case validate {caseName} - issue {errorCode}", test.Output, errorCode));
                }
            }
            else
            {
                // test failed with http error
                results.Add(NewResult(exception, test.TestName, test.Output));
            }
        }
        return results;
    }

    /// <summary>Compare expected case with received case</summary>
    /// <param name="testName">The test name</param>
    /// <param name="expected">The expected case</param>
    /// <param name="actual">The actual case</param>
    /// <returns>TupleThe value case field matching the name, null on missing case field</returns>
    protected virtual List<CaseScriptTestResult> CompareCaseChange(string testName, CaseChange expected, CaseChange actual)
    {
        var results = new List<CaseScriptTestResult>();

        // case values
        if (expected.Values != null && expected.Values.Any())
        {
            // case fields
            var actualValues = new List<CaseValue>();
            if (actual.Values != null)
            {
                actualValues.AddRange(actual.Values);
            }
            if (actual.IgnoredValues != null)
            {
                actualValues.AddRange(actual.IgnoredValues);
            }
            if (!actualValues.Any())
            {
                results.Add(NewFailedResult(CaseTestType.CaseValidate, testName, $"Missing values for case change {expected.Reason}"));
            }
            else
            {
                foreach (var caseValue in expected.Values)
                {
                    // case field
                    var actualCaseValue = string.IsNullOrWhiteSpace(caseValue.CaseSlot) ?
                        actualValues.FirstOrDefault(x => string.Equals(x.CaseFieldName, caseValue.CaseFieldName)) :
                        actualValues.FirstOrDefault(x => string.Equals(x.CaseFieldName, caseValue.CaseFieldName)
                                                      && string.Equals(x.CaseSlot, caseValue.CaseSlot));
                    if (actualCaseValue == null)
                    {
                        results.Add(NewFailedResult(CaseTestType.CaseValidate, testName, $"Missing value of case field {caseValue.CaseFieldName}"));
                        continue;
                    }

                    // case value attribute
                    var invalidAttribute = caseValue.Attributes?.FirstInvalidAttribute(actualCaseValue.Attributes);
                    if (invalidAttribute != null)
                    {
                        results.Add(NewFailedResult(CaseTestType.CaseBuild, testName,
                            $"Invalid case value attribute {caseValue.CaseFieldName}.{invalidAttribute.Item1} - Expected: {invalidAttribute.Item2 ?? "null"}, Actual: {invalidAttribute.Item3 ?? "null"}"));
                    }

                    // case value
                    if (!string.Equals(caseValue.Value, actualCaseValue.Value))
                    {
                        results.Add(NewFailedResult(CaseTestType.CaseValidate, testName,
                            $"Invalid case value {caseValue.CaseFieldName} - Expected: {caseValue.Value}, Actual: {actualCaseValue.Value}"));
                    }
                }
            }
        }

        return results;
    }
}