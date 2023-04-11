using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Service.Api;
using PayrollEngine.Data;
using Task = System.Threading.Tasks.Task;

namespace PayrollEngine.Client.Test.Report;

/// <summary>Report execute test runner.</summary>
public class ReportExecuteTestRunner : ReportScriptTestRunner
{
    /// <summary>new instance of <see cref="ReportExecuteTestRunner"/>see</summary>
    /// <param name="httpClient">The payroll http client</param>
    /// <param name="context">The test context</param>
    public ReportExecuteTestRunner(PayrollHttpClient httpClient, ReportTestContext context) :
        base(httpClient, context)
    {
    }

    /// <summary>Test the report end</summary>
    /// <param name="test">The test</param>
    /// <returns>The test results</returns>
    public virtual async Task<IList<ReportScriptTestResult>> Test(ReportExecuteTest test)
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
                results.Add(NewResult(ReportTestType.ReportExecute, test.TestName, $"Missing Report {test.ReportName}"));
                return results;
            }

            // execute report
            var reportResponse = await ExecuteReportAsync(Context.Tenant.Id,
                Context.Regulation.Id, report.Id, test.Input);

            // compare expected report with received report
            var testResults = CompareReport(test.TestName, test.Output, reportResponse.Result);
            if (testResults.Any())
            {
                results.AddRange(testResults);
            }
            else
            {
                results.Add(NewResult(ReportTestType.ReportExecute, test.TestName,
                    $"Report execute {report.Name}", test.Output, reportResponse));
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
        DataSet expected, DataSet actual)
    {
        var results = new List<ReportScriptTestResult>();

        // tables
        if (expected.Tables != null && expected.Tables.Any())
        {
            if (actual.Tables == null || !actual.Tables.Any())
            {
                results.Add(NewFailedResult(ReportTestType.ReportExecute, testName,
                    "Missing data set tables"));
            }
            else
            {
                // table
                foreach (var table in expected.Tables)
                {
                    var actualTable = actual.Tables?.FirstOrDefault(
                        x => string.Equals(x.Name, table.Name));
                    if (actualTable == null)
                    {
                        results.Add(NewFailedResult(ReportTestType.ReportExecute, testName,
                            $"Missing data set table {table.Name}"));
                        continue;
                    }

                    // columns
                    if (table.Columns != null && table.Columns.Any())
                    {
                        if (actualTable.Columns == null || !actualTable.Columns.Any())
                        {
                            results.Add(NewFailedResult(ReportTestType.ReportExecute, testName,
                                $"Missing data set columns in table {actualTable.Name}"));
                        }
                        else
                        {
                            // column
                            foreach (var column in table.Columns)
                            {
                                var actualColumn = actualTable.Columns?.FirstOrDefault(
                                    x => string.Equals(x.Name, column.Name));
                                if (actualColumn == null)
                                {
                                    results.Add(NewFailedResult(ReportTestType.ReportExecute, testName,
                                        $"Missing data set column {column.Name} in table {actualTable.Name}"));
                                }
                                else if (!actualColumn.Equals(column))
                                {
                                    results.Add(NewFailedResult(ReportTestType.ReportExecute, testName,
                                        $"Invalid data set column {column.Name} in table {table.Name} - " +
                                        $"Expected: {column}, Actual: {actualColumn}"));
                                }
                            }
                        }
                    }

                    // rows
                    if (table.Rows != null && table.Rows.Any())
                    {
                        if (actualTable.Rows == null || !actualTable.Rows.Any())
                        {
                            results.Add(NewFailedResult(ReportTestType.ReportExecute, testName,
                                $"Missing data set rows in table {actualTable.Name}"));
                        }
                        else if (actualTable.Rows.Count != table.Rows.Count)
                        {
                            results.Add(NewFailedResult(ReportTestType.ReportExecute, testName,
                                $"Mismatching count of data set rows in table {actualTable.Name} - " +
                                $"Expected: {table.Rows.Count}, Actual: {actualTable.Rows.Count}"));
                        }
                        else
                        {
                            // row
                            for (var row = 0; row < table.Rows.Count; row++)
                            {
                                var expectedRow = table.Rows[row];
                                var actualRow = actualTable.Rows[row];
                                // value
                                for (var column = 0; column < expectedRow.Values.Count; column++)
                                {
                                    // expected value
                                    var expectedValue = expectedRow.Values[column];
                                    if ("*".Equals(expectedValue))
                                    {
                                        // all values accepted, ignore value test
                                        continue;
                                    }
                                    if ("**".Equals(expectedValue))
                                    {
                                        // all-selector replacement
                                        expectedValue = "*";
                                    }
                                    // expect null
                                    if ("null".Equals(expectedValue, StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        expectedValue = null;
                                    }

                                    // actual value
                                    var actualValue = actualRow.Values[column];

                                    // cleanup escaping
                                    if (expectedValue != null)
                                    {
                                        expectedValue = Regex.Unescape(expectedValue);
                                    }
                                    if (actualValue != null)
                                    {
                                        actualValue = Regex.Unescape(actualValue);
                                    }

                                    // compare values
                                    if (!Equals(expectedValue, actualValue))
                                    {
                                        var columnName = $"Index{column}";
                                        if (table.Columns != null)
                                        {
                                            columnName = table.Columns[column].Name;
                                        }
                                        else if (actualTable.Columns != null)
                                        {
                                            columnName = actualTable.Columns[column].Name;
                                        }
                                        results.Add(NewFailedResult(ReportTestType.ReportExecute, testName,
                                            $"Invalid value in table {table.Name}, row #{row}, column {columnName} - " +
                                            $"Expected: {expectedValue}, Actual: {actualValue}"));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // relations
        if (expected.Relations != null && expected.Relations.Any())
        {
            if (actual.Relations == null || !actual.Relations.Any())
            {
                results.Add(NewFailedResult(ReportTestType.ReportExecute, testName,
                    "Missing data set relations"));
            }
            else
            {
                foreach (var relation in expected.Relations)
                {
                    var actualRelation = actual.Relations?.FirstOrDefault(
                        x => string.Equals(x.Name, relation.Name));
                    if (actualRelation == null)
                    {
                        results.Add(NewFailedResult(ReportTestType.ReportExecute, testName,
                            $"Missing data set relation {relation.Name}"));
                    }
                    else if (!actualRelation.Equals(relation))
                    {
                        results.Add(NewFailedResult(ReportTestType.ReportExecute, testName,
                            $"Invalid data set relation {relation.Name} - " +
                            $"Expected: {relation}, Actual: {actualRelation}"));
                    }
                }
            }
        }

        return results;
    }

    private async Task<ReportResponse> ExecuteReportAsync(int tenantId,
        int regulationId, int reportId, ReportRequest reportRequest)
    {
        return await new ReportService(HttpClient).ExecuteReportAsync(
            new(tenantId, regulationId), reportId, reportRequest);
    }

}