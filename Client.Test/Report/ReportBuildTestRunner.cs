using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using PayrollEngine.Client.Model;
using Task = System.Threading.Tasks.Task;

namespace PayrollEngine.Client.Test.Report;

/// <summary>Report build test runner.</summary>
public class ReportBuildTestRunner : ReportScriptTestRunner
{
    /// <summary>new instance of <see cref="ReportBuildTestRunner"/>see</summary>
    /// <param name="httpClient">The payroll http client</param>
    /// <param name="context">The test context</param>
    public ReportBuildTestRunner(PayrollHttpClient httpClient, ReportTestContext context) :
        base(httpClient, context)
    {
    }

    /// <summary>Test the report build</summary>
    /// <param name="test">The test</param>
    /// <returns>The test results</returns>
    public virtual async Task<IList<ReportScriptTestResult>> Test(ReportBuildTest test)
    {
        if (test == null)
        {
            throw new ArgumentNullException(nameof(test));
        }

        var results = new List<ReportScriptTestResult>();
        try
        {
            // user
            if (test.Input.UserId == default && !string.IsNullOrWhiteSpace(test.Input.UserIdentifier))
            {
                var user = await GetUserAsync(Context.Tenant.Id, test.Input.UserIdentifier);
                test.Input.UserId = user.Id;
            }

            // report
            var report = await GetReportAsync(Context.Tenant.Id, Context.Regulation.Id, test.ReportName);
            if (report == null)
            {
                results.Add(NewResult(ReportTestType.ReportBuild, test.TestName, $"Missing Report {test.ReportName}"));
                return results;
            }

            // build report
            var buildReport = await BuildReportAsync(Context.Tenant.Id, Context.Regulation.Id,
                report.Id, test.Input);

            // compare expected report with received report
            var testResults = CompareReport(test.TestName, test.Output, buildReport.Parameters);
            if (testResults.Any())
            {
                results.AddRange(testResults);
            }
            else
            {
                results.Add(NewResult(ReportTestType.ReportBuild, test.TestName,
                    $"Report build {report.Name}", test.Output, report));
            }
        }
        catch (HttpRequestException exception)
        {
            // test failed with http error
            results.Add(NewResult(exception, test.TestName, test.Output));
        }
        return await Task.FromResult(results);
    }

    /// <summary>Compare expected report with received report</summary>
    /// <param name="testName">The test name</param>
    /// <param name="expected">The expected report</param>
    /// <param name="actual">The actual report</param>
    /// <returns>TupleThe value case field matching the name, null on missing case field</returns>
    protected virtual List<ReportScriptTestResult> CompareReport(string testName,
        List<ReportParameter> expected, List<ReportParameter> actual)
    {
        var results = new List<ReportScriptTestResult>();

        // report parameters
        if (expected != null && expected.Any())
        {
            if (actual == null || !actual.Any())
            {
                results.Add(NewFailedResult(ReportTestType.ReportBuild, testName, "Missing report parameters"));
            }
            else
            {
                foreach (var parameter in expected)
                {
                    var actualParameter = actual.FirstOrDefault(x => string.Equals(x.Name, parameter.Name));
                    if (actualParameter == null)
                    {
                        results.Add(NewFailedResult(ReportTestType.ReportBuild, testName, $"Missing report parameter {parameter.Name}"));
                    }
                    else
                    {
                        // value
                        if (!string.Equals(actualParameter.Value, parameter.Value))
                        {
                            results.Add(NewFailedResult(ReportTestType.ReportBuild, testName,
                                $"Invalid value for report parameter {parameter.Name} - Expected: {parameter.Value}, Actual: {actualParameter.Value}"));
                        }
                        // mandatory
                        if (actualParameter.Mandatory != parameter.Mandatory)
                        {
                            results.Add(NewFailedResult(ReportTestType.ReportBuild, testName,
                                $"Invalid value for report parameter mandatory {parameter.Name} - Expected: {parameter.Mandatory}, Actual: {actualParameter.Mandatory}"));
                        }
                        // value type
                        if (actualParameter.ValueType != parameter.ValueType)
                        {
                            results.Add(NewFailedResult(ReportTestType.ReportBuild, testName,
                                $"Invalid value for report parameter value type {parameter.Name} - Expected: {parameter.ValueType}, Actual: {actualParameter.ValueType}"));
                        }
                        // parameter type
                        if (actualParameter.ParameterType != parameter.ParameterType)
                        {
                            results.Add(NewFailedResult(ReportTestType.ReportBuild, testName,
                                $"Invalid value for report parameter type {parameter.Name} - Expected: {parameter.ParameterType}, Actual: {actualParameter.ParameterType}"));
                        }
                        // attributes
                        if (parameter.Attributes != null &&
                            !CompareTool.EqualDictionaries<string, object>(actualParameter.Attributes, parameter.Attributes))
                        {
                            results.Add(NewFailedResult(ReportTestType.ReportBuild, testName,
                                $"Invalid value for report parameter attributes {parameter.Name} - Expected: {parameter.Attributes}, Actual: {actualParameter.Attributes}"));
                        }
                    }
                }
            }
        }

        return results;
    }
}