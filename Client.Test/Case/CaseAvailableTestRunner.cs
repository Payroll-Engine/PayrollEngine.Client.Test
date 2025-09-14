using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PayrollEngine.Client.Test.Case;

/// <summary>Case available function test runner.
/// Compare expected output with the case available state
/// </summary>
public class CaseAvailableTestRunner : CaseScriptTestRunner
{
    /// <summary>new instance of <see cref="CaseValidateTestRunner"/>see</summary>
    /// <param name="httpClient">The payroll http client</param>
    /// <param name="context">The test context</param>
    public CaseAvailableTestRunner(PayrollHttpClient httpClient, CaseTestContext context) :
        base(httpClient, context)
    {
    }

    /// <summary>Test the case available</summary>
    /// <param name="test">The test</param>
    /// <returns>The test results</returns>
    public virtual async Task<IList<CaseScriptTestResult>> Test(CaseAvailableTest test)
    {
        if (test == null)
        {
            throw new ArgumentNullException(nameof(test));
        }
        if (string.IsNullOrWhiteSpace(test.CaseName))
        {
            throw new ArgumentException("Available test without case name.");
        }

        var results = new List<CaseScriptTestResult>();
        try
        {
            var caseSet = await GetAvailableCaseAsync(test.CaseName);

            // expecting the case is not available
            if (!test.Output)
            {
                results.Add(NewResult(caseSet != null, CaseTestType.CaseAvailable, test.TestName,
                    $"Case {test.CaseName}: expected case is not available, received case with id {caseSet?.Id}", test.Output, caseSet));
            }
            else
            {
                // expecting the case is not available
                results.Add(NewResult(caseSet == null, CaseTestType.CaseAvailable, test.TestName,
                    $"Case {test.CaseName}: expected case is available but is not", test.Output, caseSet));
            }
        }
        catch (HttpRequestException exception)
        {
            // test failed with http error
            results.Add(NewResult(exception, test.TestName, test.Output));
        }
        return results;
    }
}