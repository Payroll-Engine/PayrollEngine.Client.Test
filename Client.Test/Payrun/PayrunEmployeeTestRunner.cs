using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Script;
using PayrollEngine.Client.Exchange;
using PayrollEngine.Client.QueryExpression;
using PayrollEngine.Client.Service.Api;
using Task = System.Threading.Tasks.Task;

namespace PayrollEngine.Client.Test.Payrun;

/// <summary>Payrun employee test runner</summary>
public class PayrunEmployeeTestRunner : PayrunTestRunnerBase
{
    /// <summary>The script parser</summary>
    public IScriptParser ScriptParser { get; }

    /// <summary>The data import mode</summary>
    public DataImportMode ImportMode { get; }

    /// <summary>The employee test mode</summary>
    public EmployeeTestMode EmployeeMode { get; }

    /// <summary>The test running mode</summary>
    public TestRunMode RunMode { get; }

    /// <summary>Initializes a new instance of the <see cref="PayrunEmployeeTestRunner"/> class</summary>
    /// <param name="httpClient">The payroll engine http client</param>
    /// <param name="scriptParser">The script parser</param>
    /// <param name="settings">The test settings</param>
    /// <param name="importMode">The data import mode (default: single)</param>
    /// <param name="employeeMode">The test running mode</param>
    /// <param name="runMode">The employee test mode</param>
    public PayrunEmployeeTestRunner(PayrollHttpClient httpClient,
        IScriptParser scriptParser,
        PayrunTestSettings settings,
        DataImportMode importMode = DataImportMode.Single,
        EmployeeTestMode employeeMode = EmployeeTestMode.InsertEmployee,
        TestRunMode runMode = TestRunMode.RunTests) :
        base(httpClient, settings)
    {
        ArgumentNullException.ThrowIfNull(scriptParser);
        ScriptParser = scriptParser;
        ImportMode = importMode;
        EmployeeMode = employeeMode;
        RunMode = runMode;
    }

    /// <summary>Start the test</summary>
    /// <param name="exchange">The exchange model</param>
    /// <returns>A list of payrun job results</returns>
    public override async Task<Dictionary<Tenant, List<PayrollTestResult>>> TestAllAsync(Model.Exchange exchange)
    {
        // apply owner
        ApplyOwner(exchange, Settings.Owner);

        var results = new Dictionary<Tenant, List<PayrollTestResult>>();
        try
        {
            foreach (var tenant in exchange.Tenants)
            {
                // existing tenant
                var existingTenant = await GetTenantAsync(tenant.Identifier);
                if (existingTenant == null)
                {
                    throw new PayrollException($"Missing tenant {tenant.Identifier}.");
                }

                // duplicate test employee
                await DuplicateTestEmployees(existingTenant.Id, tenant);

                // create new tenant including all payrolls, payrun and payrun job
                var import = new ExchangeImport(HttpClient, exchange, ScriptParser, importMode: ImportMode);
                await import.ImportAsync();

                // test skip
                if (RunMode != TestRunMode.RunTests)
                {
                    continue;
                }

                // test results
                var payrunJobResult = await TestPayrunJobAsync(tenant, JobResultMode.Multiple);
                results.Add(tenant, payrunJobResult.ToList());
            }
        }
        finally
        {
            await CleanupEmployees(results);
        }

        return results;
    }

    /// <summary>Duplicates the test employees</summary>
    /// <param name="tenantId">The tenant id</param>
    /// <param name="tenant">The exchange tenant</param>
    protected virtual async Task DuplicateTestEmployees(int tenantId, ExchangeTenant tenant)
    {
        // collect employees from cases
        var employeeIdentifiers = new HashSet<string>();
        foreach (var payroll in tenant.Payrolls)
        {
            foreach (var employeeIdentifier in payroll.Cases.Select(x => x.EmployeeIdentifier))
            {
                employeeIdentifiers.Add(employeeIdentifier);
            }
        }

        if (!employeeIdentifiers.Any())
        {
            throw new PayrollException($"Missing test employees in tenant {tenant.Identifier}.");
        }

        // insert new test employees
        if (EmployeeMode == EmployeeTestMode.InsertEmployee)
        {
            foreach (var employeeIdentifier in employeeIdentifiers)
            {
                // employee template
                var employee = await GetEmployeeAsync(tenantId, employeeIdentifier);
                if (employee == null)
                {
                    throw new PayrollException($"Missing test employee {employeeIdentifier}.");
                }

                // find next available employee identifier: count existing test employees in one query
                var testPrefix = $"{employeeIdentifier} Test ";
                var countQuery = new DivisionQuery
                {
                    Filter = new StartsWith(nameof(Employee.Identifier), testPrefix).Expression
                };
                var existingCount = await new EmployeeService(HttpClient).QueryCountAsync(new(tenantId), countQuery);
                var i = (int)existingCount + 1;
                var testEmployeeIdentifier = $"{employeeIdentifier} Test {i}";
                if (string.IsNullOrWhiteSpace(testEmployeeIdentifier))
                {
                    throw new PayrollException($"Invalid identifier on test employee {employeeIdentifier}.");
                }

                // create test employee
                employee.Id = 0;
                employee.Identifier = testEmployeeIdentifier;
                if (!string.IsNullOrWhiteSpace(employee.LastName))
                {
                    employee.LastName = $"{employee.LastName} Test {i}";
                }

                // duplicate test employee
                tenant.Employees ??= [];
                tenant.Employees.Add(new(employee));

                // adjust case employee
                foreach (var payroll in tenant.Payrolls)
                {
                    foreach (var @case in payroll.Cases)
                    {
                        if (string.Equals(@case.EmployeeIdentifier, employeeIdentifier))
                        {
                            @case.EmployeeIdentifier = testEmployeeIdentifier;
                        }
                    }
                }

                // adjust payrun job invocation test employee
                foreach (var jobInvocation in tenant.PayrunJobInvocations)
                {
                    if (jobInvocation.EmployeeIdentifiers != null &&
                        jobInvocation.EmployeeIdentifiers.Contains(employeeIdentifier))
                    {
                        jobInvocation.EmployeeIdentifiers.Remove(employeeIdentifier);
                        jobInvocation.EmployeeIdentifiers.Add(testEmployeeIdentifier);
                    }
                }

                // adjust payroll result test employee
                foreach (var payrollResultSet in tenant.PayrollResults)
                {
                    if (string.Equals(payrollResultSet.EmployeeIdentifier, employeeIdentifier))
                    {
                        payrollResultSet.EmployeeIdentifier = testEmployeeIdentifier;
                    }
                }
            }
        }
    }

    /// <summary>Cleanup test employees</summary>
    /// <param name="tenantResults">Results</param>
    protected virtual async Task CleanupEmployees(Dictionary<Tenant, List<PayrollTestResult>> tenantResults)
    {
        if (Settings.ResultMode == TestResultMode.KeepTest)
        {
            return;
        }

        var deleteEmployeeIds = new HashSet<int>();
        foreach (var tenantResult in tenantResults)
        {
            var tenantId = tenantResult.Key.Id;
            foreach (var result in tenantResult.Value)
            {
                // keep failed
                if (Settings.ResultMode == TestResultMode.KeepFailedTest && result.Failed)
                {
                    continue;
                }

                var employeeId = result.Employee.Id;

                // check delete history
                if (deleteEmployeeIds.Contains(employeeId))
                {
                    continue;
                }

                try
                {
                    await DeleteEmployeeAsync(tenantId, employeeId);
                    // update delete history
                    deleteEmployeeIds.Add(employeeId);
                }
                catch (Exception exception)
                {
                    Log.Error(exception, exception.GetBaseMessage());
                }
            }
        }
    }
}
