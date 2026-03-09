using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Service;
using PayrollEngine.Client.Service.Api;

namespace PayrollEngine.Client.Test.Payrun;

/// <summary>Payrun employee preview test runner</summary>
public class PayrunEmployeePreviewTestRunner : PayrunTestRunnerBase
{
    /// <summary>Initializes a new instance of the <see cref="PayrunEmployeePreviewTestRunner"/> class</summary>
    /// <param name="httpClient">The payroll engine http client</param>
    /// <param name="settings">The test settings</param>
    public PayrunEmployeePreviewTestRunner(PayrollHttpClient httpClient, PayrunTestSettings settings) :
        base(httpClient, settings)
    {
    }

    /// <summary>Start the test</summary>
    /// <param name="exchange">The exchange model</param>
    /// <returns>A dictionary of tenant to payroll test results</returns>
    public override async Task<Dictionary<Tenant, List<PayrollTestResult>>> TestAllAsync(Model.Exchange exchange)
    {
        // apply owner
        ApplyOwner(exchange, Settings.Owner);

        var results = new Dictionary<Tenant, List<PayrollTestResult>>();
        foreach (var tenant in exchange.Tenants)
        {
            // existing tenant
            var existingTenant = await GetTenantAsync(tenant.Identifier);
            if (existingTenant == null)
            {
                throw new PayrollException($"Missing tenant {tenant.Identifier}.");
            }

            // test results via preview
            var testResults = await TestPreviewAsync(tenant, existingTenant);
            results.Add(existingTenant, testResults);
        }

        return results;
    }

    /// <summary>Test payrun jobs via preview</summary>
    /// <param name="tenant">The exchange tenant</param>
    /// <param name="existingTenant">The existing tenant</param>
    /// <returns>List of payroll test results</returns>
    private async Task<List<PayrollTestResult>> TestPreviewAsync(ExchangeTenant tenant, Tenant existingTenant)
    {
        var testResults = new List<PayrollTestResult>();
        if (tenant.PayrollResults == null || !tenant.PayrollResults.Any())
        {
            return testResults;
        }
        if (tenant.PayrunJobInvocations == null || !tenant.PayrunJobInvocations.Any())
        {
            throw new PayrollException("Missing payrun job invocations for preview test.");
        }

        // culture
        var culture = CultureTool.GetTenantCulture(tenant);
        var payrunJobService = new PayrunJobService(HttpClient);
        var context = new TenantServiceContext(existingTenant.Id);

        foreach (var expectedResult in tenant.PayrollResults)
        {
            // employee
            if (string.IsNullOrWhiteSpace(expectedResult.EmployeeIdentifier))
            {
                throw new PayrollException("Missing employee identifier in payroll result.");
            }
            var employee = await GetEmployeeAsync(existingTenant.Id, expectedResult.EmployeeIdentifier);
            if (employee == null)
            {
                throw new PayrollException($"Missing employee {expectedResult.EmployeeIdentifier}.");
            }

            // find matching invocation
            var invocation = tenant.PayrunJobInvocations.FirstOrDefault(
                x => string.Equals(x.Name, expectedResult.PayrunJobName));
            if (invocation == null)
            {
                throw new PayrollException($"Missing payrun job invocation {expectedResult.PayrunJobName}.");
            }

            // call preview
            var startTime = DateTime.UtcNow;
            var previewResult = await payrunJobService.PreviewJobAsync<PayrollResultSet>(context, invocation);
            var endTime = DateTime.UtcNow;

            // placeholder payrun job for result display
            var payrunJob = CreatePlaceholderPayrunJob(invocation, startTime, endTime);

            if (previewResult == null)
            {
                // special case: no results expected
                if (!expectedResult.HasResults())
                {
                    testResults.Add(new PayrollTestResult(existingTenant, employee, payrunJob));
                    continue;
                }
                throw new PayrollException($"Missing preview results for {invocation.Name}.");
            }

            var testResult = new PayrollTestResult(existingTenant, employee, payrunJob);

            // wage type results
            if (expectedResult.WageTypeResults != null)
            {
                foreach (var expectedWageType in expectedResult.WageTypeResults)
                {
                    var actualWageType = expectedWageType.Tags == null ?
                        previewResult.WageTypeResults?.FirstOrDefault(
                            x => x.WageTypeNumber == expectedWageType.WageTypeNumber) :
                        previewResult.WageTypeResults?.FirstOrDefault(
                            x => x.WageTypeNumber == expectedWageType.WageTypeNumber &&
                                 CompareTool.EqualLists(x.Tags, expectedWageType.Tags));
                    var result = new WageTypeTestResult(TestPrecision, expectedWageType, actualWageType);
                    testResult.WageTypeResults.Add(result);

                    // wage type custom results
                    if (expectedWageType.CustomResults != null && actualWageType != null)
                    {
                        foreach (var expectedCustomResult in expectedWageType.CustomResults)
                        {
                            var actualCustomResult = expectedCustomResult.Tags == null ?
                                actualWageType.CustomResults?.FirstOrDefault(
                                    x => string.Equals(x.Source, expectedCustomResult.Source)) :
                                actualWageType.CustomResults?.FirstOrDefault(
                                    x => string.Equals(x.Source, expectedCustomResult.Source) &&
                                         CompareTool.EqualLists(x.Tags, expectedCustomResult.Tags));
                            var customResult = new WageTypeTestCustomResult(TestPrecision, expectedCustomResult, actualCustomResult);
                            result.CustomResults.Add(customResult);
                        }
                    }
                }
            }

            // collector results
            if (expectedResult.CollectorResults != null)
            {
                foreach (var expectedCollector in expectedResult.CollectorResults)
                {
                    var actualCollector = expectedCollector.Tags == null ?
                        previewResult.CollectorResults?.FirstOrDefault(
                            x => string.Equals(x.CollectorName, expectedCollector.CollectorName)) :
                        previewResult.CollectorResults?.FirstOrDefault(
                            x => string.Equals(x.CollectorName, expectedCollector.CollectorName) &&
                                 CompareTool.EqualLists(x.Tags, expectedCollector.Tags));
                    var result = new CollectorTestResult(TestPrecision, expectedCollector, actualCollector);
                    testResult.CollectorResults.Add(result);

                    // collector custom results
                    if (expectedCollector.CustomResults != null && actualCollector != null)
                    {
                        foreach (var expectedCustomResult in expectedCollector.CustomResults)
                        {
                            var actualCustomResult = expectedCustomResult.Tags == null ?
                                actualCollector.CustomResults?.FirstOrDefault(
                                    x => string.Equals(x.Source, expectedCustomResult.Source)) :
                                actualCollector.CustomResults?.FirstOrDefault(
                                    x => string.Equals(x.Source, expectedCustomResult.Source) &&
                                         CompareTool.EqualLists(x.Tags, expectedCustomResult.Tags));
                            var customResult = new CollectorTestCustomResult(TestPrecision, expectedCustomResult, actualCustomResult);
                            result.CustomResults.Add(customResult);
                        }
                    }
                }
            }

            // payrun results
            if (expectedResult.PayrunResults != null)
            {
                foreach (var expectedPayrunResult in expectedResult.PayrunResults)
                {
                    var actualPayrunResult = expectedPayrunResult.Tags == null ?
                        previewResult.PayrunResults?.FirstOrDefault(
                            x => string.Equals(x.Name, expectedPayrunResult.Name)) :
                        previewResult.PayrunResults?.FirstOrDefault(
                            x => string.Equals(x.Name, expectedPayrunResult.Name) &&
                                 CompareTool.EqualLists(x.Tags, expectedPayrunResult.Tags));
                    var result = new PayrunTestResult(culture, expectedPayrunResult, actualPayrunResult);
                    testResult.PayrunResults.Add(result);
                }
            }

            testResults.Add(testResult);
        }

        return testResults;
    }

    /// <summary>Create a placeholder payrun job for test result display</summary>
    /// <param name="invocation">The payrun job invocation</param>
    /// <param name="startTime">The preview start time</param>
    /// <param name="endTime">The preview end time</param>
    private static PayrunJob CreatePlaceholderPayrunJob(PayrunJobInvocation invocation,
        DateTime startTime, DateTime endTime) =>
        new()
        {
            Name = invocation.Name,
            PeriodStart = invocation.PeriodStart,
            JobStart = startTime,
            JobEnd = endTime,
            JobStatus = PayrunJobStatus.Complete,
            JobResult = PayrunJobResult.Full
        };
}
