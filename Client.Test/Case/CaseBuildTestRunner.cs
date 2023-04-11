using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using PayrollEngine.Client.Model;

namespace PayrollEngine.Client.Test.Case;

/// <summary>Case build function test runner.
/// Compares the output case slots/fields and related cases with the received case
/// </summary>
public class CaseBuildTestRunner : CaseScriptTestRunner
{
    /// <summary>new instance of <see cref="CaseValidateTestRunner"/>see</summary>
    /// <param name="httpClient">The payroll http client</param>
    /// <param name="context">The test context</param>
    public CaseBuildTestRunner(PayrollHttpClient httpClient, CaseTestContext context) :
        base(httpClient, context)
    {
    }

    /// <summary>Test the case validation</summary>
    /// <param name="test">The test name</param>
    /// <returns>The test results</returns>
    public virtual async Task<IList<CaseScriptTestResult>> Test(CaseBuildTest test)
    {
        if (test == null)
        {
            throw new ArgumentNullException(nameof(test));
        }
        var caseName = test.Input?.Case?.CaseName;
        if (string.IsNullOrWhiteSpace(caseName))
        {
            throw new ArgumentException("Build test without input case");
        }

        var results = new List<CaseScriptTestResult>();
        try
        {
            var caseSet = await GetCaseAsync(caseName, test.Input);

            // compare expected case with received case
            var testResults = CompareCase(test.TestName, test.Output, caseSet);
            if (testResults.Any())
            {
                results.AddRange(testResults);
            }
            else
            {
                results.Add(NewResult(CaseTestType.CaseBuild, test.TestName,
                    $"Case build {caseSet.Name}", test.Output, caseSet));
            }
        }
        catch (HttpRequestException exception)
        {
            // test failed with http error
            results.Add(NewResult(exception, test.TestName, test.Output));
        }
        return results;
    }

    /// <summary>Compare expected case with received case</summary>
    /// <param name="testName">The test name</param>
    /// <param name="expected">The expected case</param>
    /// <param name="actual">The actual case</param>
    /// <returns>TupleThe value case field matching the name, null on missing case field</returns>
    protected virtual List<CaseScriptTestResult> CompareCase(string testName, CaseSet expected, CaseSet actual)
    {
        var results = new List<CaseScriptTestResult>();

        // case slots
        if (expected.Slots != null && expected.Slots.Any())
        {
            // case slots
            if (actual.Slots == null || !actual.Slots.Any())
            {
                results.Add(NewFailedResult(CaseTestType.CaseBuild, testName, "Missing case build slots"));
            }
            else
            {
                foreach (var caseSlot in expected.Slots)
                {
                    // case slot
                    if (!actual.Slots.Any(x => string.Equals(x.Name, caseSlot.Name)))
                    {
                        results.Add(NewFailedResult(CaseTestType.CaseBuild, testName, $"Missing case slot {caseSlot.Name}"));
                    }
                }
            }
        }

        // case fields
        if (expected.Fields != null && expected.Fields.Any())
        {
            // case fields
            if (actual.Fields == null || !actual.Fields.Any())
            {
                results.Add(NewFailedResult(CaseTestType.CaseBuild, testName, "Missing case build fields"));
            }
            else
            {
                foreach (var caseField in expected.Fields)
                {
                    // case field
                    var actualCaseField = actual.Fields.FirstOrDefault(x => string.Equals(x.Name, caseField.Name));
                    if (actualCaseField == null)
                    {
                        results.Add(NewFailedResult(CaseTestType.CaseBuild, testName, $"Missing case field {caseField.Name}"));
                        continue;
                    }

                    // case field start date
                    if (caseField.Start != null && !Equals(caseField.Start, actualCaseField.Start))
                    {
                        results.Add(NewFailedResult(CaseTestType.CaseBuild, testName,
                            $"Invalid case value start {caseField.Name} - Expected: {caseField.Start}, Actual: {actualCaseField.Start}"));
                    }

                    // case field end date
                    if (caseField.End != null && !Equals(caseField.End, actualCaseField.End))
                    {
                        results.Add(NewFailedResult(CaseTestType.CaseBuild, testName,
                            $"Invalid case value end {caseField.Name} - Expected: {caseField.End}, Actual: {actualCaseField.End}"));
                    }

                    // case field attribute
                    var invalidAttribute = caseField.Attributes?.FirstInvalidAttribute(actualCaseField.Attributes);
                    if (invalidAttribute != null)
                    {
                        results.Add(NewFailedResult(CaseTestType.CaseBuild, testName,
                            $"Invalid case field attribute {caseField.Name}.{invalidAttribute.Item1} - Expected: {invalidAttribute.Item2 ?? "null"}, Actual: {invalidAttribute.Item3 ?? "null"}"));
                    }

                    // case value attribute
                    invalidAttribute = caseField.ValueAttributes?.FirstInvalidAttribute(actualCaseField.ValueAttributes);
                    if (invalidAttribute != null)
                    {
                        results.Add(NewFailedResult(CaseTestType.CaseBuild, testName,
                            $"Invalid case value attribute {caseField.Name}.{invalidAttribute.Item1} - Expected: {invalidAttribute.Item2 ?? "null"}, Actual: {invalidAttribute.Item3 ?? "null"}"));
                    }

                    // adapt value type
                    caseField.ValueType = actualCaseField.ValueType;

                    // case value
                    var expectedValue = caseField.GetValue();
                    var actualValue = actualCaseField.GetValue();
                    if (!Equals(expectedValue, actualValue))
                    {
                        results.Add(NewFailedResult(CaseTestType.CaseBuild, testName,
                            $"Invalid case value {caseField.Name} - Expected: {expectedValue ?? "null"}, Actual: {actualValue ?? "null"}"));
                    }
                }
            }
        }

        // related cases
        if (expected.RelatedCases != null && expected.RelatedCases.Any())
        {
            // related cases
            if (actual.RelatedCases == null || !actual.RelatedCases.Any())
            {
                results.Add(NewFailedResult(CaseTestType.CaseBuild, testName, "Missing related cases"));
            }
            else
            {
                foreach (var relatedCase in expected.RelatedCases)
                {
                    var actualRelatedCase = actual.RelatedCases.FirstOrDefault(x => string.Equals(x.Name, relatedCase.Name));
                    if (actualRelatedCase == null)
                    {
                        results.Add(NewFailedResult(CaseTestType.CaseBuild, testName, $"Missing related case {relatedCase.Name}"));
                        return results;
                    }
                    var relatedResults = CompareCase(testName, relatedCase, actualRelatedCase);
                    results.AddRange(relatedResults);
                }
            }
        }

        return results;
    }
}