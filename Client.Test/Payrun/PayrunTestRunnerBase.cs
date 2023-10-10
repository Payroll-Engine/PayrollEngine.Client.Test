using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Service.Api;

namespace PayrollEngine.Client.Test.Payrun;

/// <summary>Base class for file based payroll tests</summary>
public abstract class PayrunTestRunnerBase : TestRunnerBase
{
    /// <summary>The testing precision</summary>
    public TestPrecision TestPrecision { get; }

    /// <summary>The test owner</summary>
    public string Owner { get; }

    /// <summary>Initializes a new instance of the <see cref="PayrunTestRunnerBase"/> class</summary>
    /// <param name="httpClient">The payroll engine http client</param>
    /// <param name="testPrecision">The testing precision</param>
    /// <param name="owner">The test owner</param>
    protected PayrunTestRunnerBase(PayrollHttpClient httpClient, TestPrecision testPrecision, string owner = null) :
        base(httpClient)
    {
        TestPrecision = testPrecision;
        Owner = owner;
    }

    /// <summary>Start the test</summary>
    /// <param name="exchange">The test exchange</param>
    /// <returns>A list of payrun job results</returns>
    public abstract Task<Dictionary<Tenant, List<PayrollTestResult>>> TestAllAsync(Model.Exchange exchange);

    /// <summary>Tests the payrun job</summary>
    /// <param name="tenant">The tenant</param>
    /// <param name="jobResultMode">The job result mode</param>
    /// <returns>List of payrun job test results</returns>
    protected virtual async Task<IList<PayrollTestResult>> TestPayrunJobAsync(ExchangeTenant tenant, JobResultMode jobResultMode)
    {
        var testResults = new List<PayrollTestResult>();
        if (tenant.PayrollResults == null || !tenant.PayrollResults.Any())
        {
            return testResults;
        }

        // culture
        var culture = CultureTool.GetTenantCulture(tenant);

        // compare all payroll results values with the provided values
        foreach (var payrollResult in tenant.PayrollResults)
        {
            // no results to test
            if (payrollResult.WageTypeResults == null && payrollResult.CollectorResults == null)
            {
                Log.Warning("Empty payroll results");
                continue;
            }

            // employee
            if (string.IsNullOrWhiteSpace(payrollResult.EmployeeIdentifier))
            {
                throw new PayrollException("Missing payrun job employee in payroll result");
            }
            var employee = await GetEmployeeAsync(tenant.Id, payrollResult.EmployeeIdentifier);
            if (employee == null)
            {
                throw new PayrollException($"Missing payroll result employee with identifier {payrollResult.EmployeeIdentifier}");
            }

            // payrun job
            PayrunJob payrunJob;
            if (string.IsNullOrWhiteSpace(payrollResult.PayrunJobName))
            {
                throw new PayrollException("Missing payrun job name in payroll result");
            }
            if (jobResultMode == JobResultMode.Single)
            {
                var invocation = tenant.PayrunJobInvocations.FirstOrDefault(x => string.Equals(x.Name, payrollResult.PayrunJobName));
                if (invocation == null || !invocation.PayrunJobId.HasValue)
                {
                    throw new PayrollException($"Missing payrun job {payrollResult.PayrunJobName}");
                }

                var retroPeriodStart = payrollResult.RetroPeriodStart;
                if (retroPeriodStart != null)
                {
                    var payrunJobs = await GetPayrunJobsAsync(tenant.Id, payrollResult.PayrunJobName);
                    // retro job test: select the newest (last created) incremental (retro) job
                    // only incremental/retro jobs
                    payrunJob = payrunJobs.Where(x => x.JobResult == PayrunJobResult.Incremental &&
                                                      string.Equals(x.Name, payrollResult.PayrunJobName) &&
                                                      x.PeriodStart.Equals(retroPeriodStart.Value)).MaxBy(x => x.Created);
                    if (payrunJob == null)
                    {
                        throw new PayrollException($"Unknown retro payrun job on period {payrollResult.RetroPeriodStart.Value}");
                    }
                }
                else
                {
                    payrunJob = await new PayrunJobService(HttpClient).GetAsync<PayrunJob>(
                        new(tenant.Id), invocation.PayrunJobId.Value);
                }
            }
            else
            {
                var employePayrunJobs = await GetEmployeePayrunJobsAsync(tenant.Id, employee.Id);
                if (employePayrunJobs == null || !employePayrunJobs.Any())
                {
                    throw new PayrollException($"Missing payrun job {payrollResult.PayrunJobName}");
                }

                var retroPeriodStart = payrollResult.RetroPeriodStart;
                if (retroPeriodStart != null)
                {
                    // retro job test: select the newest (last created) incremental (retro) job
                    // only incremental/retro jobs
                    payrunJob = employePayrunJobs.Where(x => x.JobResult == PayrunJobResult.Incremental &&
                                                             string.Equals(x.Name, payrollResult.PayrunJobName) &&
                                                             x.PeriodStart.Equals(retroPeriodStart.Value)).MaxBy(x => x.Created);
                    if (payrunJob == null)
                    {
                        throw new PayrollException($"Unknown retro payrun job on period {payrollResult.RetroPeriodStart.Value}");
                    }
                }
                else
                {
                    // full job test: select the newest (last created) full (non-retro) job
                    var payrunJobs = employePayrunJobs.Where(x => x.JobResult == PayrunJobResult.Full &&
                                                                  string.Equals(x.Name, payrollResult.PayrunJobName)).ToList();
                    if (!payrunJobs.Any())
                    {
                        throw new PayrollException($"Missing payrun job {payrollResult.PayrunJobName}");
                    }
                    payrunJob = payrunJobs.OrderByDescending(x => x.Created).First();
                }
            }

            // aborted job
            if (payrunJob.JobStatus == PayrunJobStatus.Abort)
            {
                throw new PayrollException($"Job abort [{payrunJob.Name}]: {payrunJob.Message}");
            }

            // prepare result
            var testResult = new PayrollTestResult(tenant, employee, payrunJob);

            // existing result
            var actualPayrollResult = await GetPayrollResultAsync(tenant.Id, employee.Id, payrunJob.Id);
            // one result expected
            if (actualPayrollResult == null)
            {
                // special case: no results expected
                if (!payrollResult.HasResults())
                {
                    testResults.Add(testResult);
                    return testResults;
                }
                throw new PayrollException($"Missing results for payrun job {payrunJob.Name}");
            }

            // wage type results
            if (payrollResult.WageTypeResults != null)
            {
                var wageTypeResults = await GetWageTypeResultsAsync(tenant.Id, actualPayrollResult.Id);
                foreach (var wageTypeResult in payrollResult.WageTypeResults)
                {
                    var actualWageTypeResult = wageTypeResult.Tags == null ?
                        wageTypeResults?.FirstOrDefault(x => x.WageTypeNumber == wageTypeResult.WageTypeNumber) :
                        wageTypeResults?.FirstOrDefault(x => x.WageTypeNumber == wageTypeResult.WageTypeNumber &&
                                                             CompareTool.EqualLists(x.Tags, wageTypeResult.Tags));
                    var result = new WageTypeTestResult(TestPrecision, wageTypeResult, actualWageTypeResult);
                    testResult.WageTypeResults.Add(result);

                    // wage type custom results
                    if (wageTypeResult.CustomResults != null)
                    {
                        var wageTypeCustomResults = await GetWageTypeCustomResultsAsync(tenant.Id, actualPayrollResult.Id, actualWageTypeResult.Id);
                        foreach (var wageTypeCustomResult in wageTypeResult.CustomResults)
                        {
                            var actualWageTypeCustomResult = wageTypeCustomResult.Tags == null ?
                                wageTypeCustomResults?.FirstOrDefault(x => string.Equals(x.Source, wageTypeCustomResult.Source)) :
                                wageTypeCustomResults?.FirstOrDefault(x => string.Equals(x.Source, wageTypeCustomResult.Source) &&
                                                                           CompareTool.EqualLists(x.Tags, wageTypeCustomResult.Tags));
                            var customResult = new WageTypeTestCustomResult(TestPrecision, wageTypeCustomResult, actualWageTypeCustomResult);
                            result.CustomResults.Add(customResult);
                        }
                    }
                }
            }

            // collector results
            if (payrollResult.CollectorResults != null)
            {
                var collectorResults = await GetCollectorResultsAsync(tenant.Id, actualPayrollResult.Id);
                foreach (var collectorResult in payrollResult.CollectorResults)
                {
                    var actualCollectorResult = collectorResult.Tags == null ?
                        collectorResults?.FirstOrDefault(x => string.Equals(x.CollectorName, collectorResult.CollectorName)) :
                        collectorResults?.FirstOrDefault(x => string.Equals(x.CollectorName, collectorResult.CollectorName) &&
                                                              CompareTool.EqualLists(x.Tags, collectorResult.Tags));
                    var result = new CollectorTestResult(TestPrecision, collectorResult, actualCollectorResult);
                    testResult.CollectorResults.Add(result);

                    // collector custom results
                    if (collectorResult.CustomResults != null)
                    {
                        var collectorCustomResults = await GetCollectorCustomResultsAsync(tenant.Id, actualPayrollResult.Id, collectorResult.Id);
                        foreach (var collectorCustomResult in collectorResult.CustomResults)
                        {
                            var actualCollectorCustomResult = collectorCustomResult.Tags == null ?
                                collectorCustomResults?.FirstOrDefault(x => string.Equals(x.Source, collectorCustomResult.Source)) :
                                collectorCustomResults?.FirstOrDefault(x => string.Equals(x.Source, collectorCustomResult.Source) &&
                                                                            CompareTool.EqualLists(x.Tags, collectorCustomResult.Tags));
                            var customResult = new CollectorTestCustomResult(TestPrecision, collectorCustomResult, actualCollectorCustomResult);
                            result.CustomResults.Add(customResult);
                        }
                    }
                }
            }

            // payrun results
            if (payrollResult.PayrunResults != null)
            {
                var payrunResults = await GetPayrunResultsAsync(tenant.Id, actualPayrollResult.Id);
                foreach (var payrunResult in payrollResult.PayrunResults)
                {
                    var actualResult = payrunResult.Tags == null ?
                        payrunResults?.FirstOrDefault(x => string.Equals(x.Name, payrunResult.Name) &&
                                                           Date.SameSecond(x.Start, payrunResult.Start) &&
                                                           Date.SameSecond(x.End, payrunResult.End)) :
                        payrunResults?.FirstOrDefault(x => string.Equals(x.Name, payrunResult.Name) &&
                                                           Date.SameSecond(x.Start, payrunResult.Start) &&
                                                           Date.SameSecond(x.End, payrunResult.End) &&
                                                           CompareTool.EqualLists(x.Tags, payrunResult.Tags));
                    var result = new PayrunTestResult(culture, payrunResult, actualResult);
                    testResult.PayrunResults.Add(result);
                }
            }

            testResults.Add(testResult);
        }

        return testResults;
    }

    /// <summary>Apply the test owner</summary>
    /// <param name="exchange">The exchange</param>
    /// <param name="owner">The owner name</param>
    /// <returns>List of payrun job test results</returns>
    protected virtual void ApplyOwner(Model.Exchange exchange, string owner)
    {
        if (string.IsNullOrWhiteSpace(owner) || exchange.Tenants == null)
        {
            return;
        }

        foreach (var tenant in exchange.Tenants)
        {
            if (tenant.PayrunJobInvocations == null)
            {
                continue;
            }
            foreach (var job in tenant.PayrunJobInvocations)
            {
                job.Owner = owner;
            }
        }
    }

    #region Api

    /// <summary>Get payrun jobs</summary>
    /// <param name="tenantId">The tenant id</param>
    /// <param name="payrunName">The payrun name</param>
    protected async Task<List<PayrunJob>> GetPayrunJobsAsync(int tenantId, string payrunName)
    {
        var query = new Query
        {
            Filter = $"{nameof(Model.Payrun.Name)} eq '{payrunName}'"
        };
        return await new PayrunJobService(HttpClient).QueryAsync<PayrunJob>(new(tenantId), query);
    }


    /// <summary>Get employee payrun jobs</summary>
    /// <param name="tenantId">The tenant id</param>
    /// <param name="employeeId">The tenant identifier</param>
    protected async Task<List<PayrunJob>> GetEmployeePayrunJobsAsync(int tenantId, int employeeId) =>
        await new PayrunJobService(HttpClient).QueryEmployeePayrunJobsAsync<PayrunJob>(new(tenantId), employeeId);

    /// <summary>Get tenant</summary>
    /// <param name="tenantId">The tenant id</param>
    /// <param name="employeeId">The employee id</param>
    /// <param name="payrunJobId">The payrun job id</param>
    protected async Task<PayrollResult> GetPayrollResultAsync(int tenantId, int employeeId, int payrunJobId)
    {
        var query = new Query
        {
            Filter = $"{nameof(employeeId)} eq {employeeId} and {nameof(payrunJobId)} eq {payrunJobId}"
        };
        return (await new PayrollResultService(HttpClient).QueryAsync<PayrollResult>(new(tenantId), query))
            .FirstOrDefault();
    }

    /// <summary>Get wage type results</summary>
    /// <param name="tenantId">The tenant id</param>
    /// <param name="payrollResultId">The payroll result id</param>
    protected async Task<List<WageTypeResultSet>> GetWageTypeResultsAsync(int tenantId, int payrollResultId) =>
        (await new PayrollResultService(HttpClient).QueryWageTypeResultsAsync<WageTypeResultSet>(
            new(tenantId, payrollResultId))).ToList();

    /// <summary>Get wage type custom results</summary>
    /// <param name="tenantId">The tenant id</param>
    /// <param name="payrollResultId">The payroll result id</param>
    /// <param name="wageTypeResultId">The wage type result id</param>
    protected async Task<List<WageTypeCustomResult>> GetWageTypeCustomResultsAsync(int tenantId, int payrollResultId, int wageTypeResultId) =>
        (await new PayrollResultService(HttpClient).QueryWageTypeCustomResultsAsync<WageTypeCustomResult>(
            new(tenantId), payrollResultId, wageTypeResultId)).ToList();

    /// <summary>Get collector results</summary>
    /// <param name="tenantId">The tenant id</param>
    /// <param name="payrollResultId">The payroll result id</param>
    protected async Task<List<CollectorResultSet>> GetCollectorResultsAsync(int tenantId, int payrollResultId) =>
        (await new PayrollResultService(HttpClient).QueryCollectorResultsAsync<CollectorResultSet>(
            new(tenantId, payrollResultId))).ToList();

    /// <summary>Get collector custom results</summary>
    /// <param name="tenantId">The tenant id</param>
    /// <param name="payrollResultId">The payroll result id</param>
    /// <param name="collectorResultId">The collector result id</param>
    protected async Task<List<CollectorCustomResult>> GetCollectorCustomResultsAsync(int tenantId, int payrollResultId, int collectorResultId) =>
        (await new PayrollResultService(HttpClient).QueryCollectorCustomResultsAsync<CollectorCustomResult>(
            new(tenantId), payrollResultId, collectorResultId)).ToList();

    /// <summary>Get payrun results</summary>
    /// <param name="tenantId">The tenant id</param>
    /// <param name="payrollResultId">The payroll result id</param>
    protected async Task<List<PayrunResult>> GetPayrunResultsAsync(int tenantId, int payrollResultId) =>
        (await new PayrollResultService(HttpClient).QueryPayrunResultsAsync<PayrunResult>(
            new(tenantId, payrollResultId))).ToList();

    #endregion

}