using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PayrollEngine.Client.Exchange;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Script;
using Task = System.Threading.Tasks.Task;

namespace PayrollEngine.Client.Test.Payrun;

/// <summary>Payrun employee test runner</summary>
public class PayrunEmployeeTestRunner : PayrunFileTestRunner
{
    /// <summary>The delay between creation and test</summary>
    public int DelayBetweenCreateAndTest { get; set; }

    /// <summary>The employee test mode</summary>
    public IScriptParser ScriptParser { get; }

    /// <summary>The employee test mode</summary>
    public EmployeeTestMode EmployeeMode { get; }

    /// <summary>Initializes a new instance of the <see cref="PayrunEmployeeTestRunner"/> class</summary>
    /// <param name="httpClient">The payroll engine http client</param>
    /// <param name="fileName">Name of the file</param>
    /// <param name="scriptParser">The script parser</param>
    /// <param name="owner">The test owner</param>
    /// <param name="testPrecision">The testing precision</param>
    /// <param name="employeeMode">The employee test mode</param>
    public PayrunEmployeeTestRunner(PayrollHttpClient httpClient, string fileName, IScriptParser scriptParser,
        TestPrecision testPrecision, string owner = null, EmployeeTestMode employeeMode = EmployeeTestMode.InsertEmployee) :
        base(httpClient, fileName, testPrecision, owner)
    {
        ScriptParser = scriptParser ?? throw new ArgumentNullException(nameof(scriptParser));
        EmployeeMode = employeeMode;
    }

    /// <summary>Start the test</summary>
    /// <param name="namespace">The tenant name</param>
    /// <returns>A list of payrun job results</returns>
    public override async Task<Dictionary<Tenant, List<PayrollTestResult>>> TestAllAsync(string @namespace = null)
    {
        // exchange
        var exchange = await LoadExchangeAsync(@namespace);
        // apply owner
        ApplyOwner(exchange, Owner);

        var results = new Dictionary<Tenant, List<PayrollTestResult>>();
        foreach (var tenant in exchange.Tenants)
        {
            // existing tenant
            var existingTenant = await GetTenantAsync(tenant.Identifier);
            if (existingTenant == null)
            {
                throw new PayrollException($"Missing tenant {tenant.Identifier}");
            }

            // duplicate test employee
            await DuplicateTestEmployee(existingTenant.Id, tenant);

            // create new tenant including all payrolls, payrun and payrun job
            var import = new ExchangeImport(HttpClient, exchange, ScriptParser);
            await import.ImportAsync();

            // all payrun jobs should be executed
            if (DelayBetweenCreateAndTest > 0)
            {
                Task.Delay(DelayBetweenCreateAndTest).Wait();
            }

            // test results
            var payrunJobResult = await TestPayrunJobAsync(tenant, JobResultMode.Multiple);
            results.Add(tenant, payrunJobResult.ToList());
        }

        return results;
    }

    /// <summary>Duplicates the test employee</summary>
    /// <param name="tenantId">The tenant id</param>
    /// <param name="tenant">The exchange tenant</param>
    protected virtual async Task DuplicateTestEmployee(int tenantId, ExchangeTenant tenant)
    {
        // collect employees from cases
        var employeeIdentifiers = new List<string>();
        foreach (var payroll in tenant.Payrolls)
        {
            var casesByEmployees = new HashSet<string>(payroll.Cases.Select(x => x.EmployeeIdentifier));
            employeeIdentifiers.AddRange(casesByEmployees);
        }

        if (!employeeIdentifiers.Any())
        {
            throw new PayrollException($"Missing test employees in tenant {tenant.Identifier}");
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
                    throw new PayrollException($"Missing test employee {employeeIdentifier}");
                }

                // find next available employee identifier
                string testEmployeeIdentifier;
                var i = 1;
                while (true)
                {
                    var nextIdentifier = $"{employeeIdentifier} Test {i}";
                    var nextEmployee = await GetEmployeeAsync(tenantId, nextIdentifier);
                    if (nextEmployee == null)
                    {
                        testEmployeeIdentifier = nextIdentifier;
                        break;
                    }
                    i++;
                }
                if (string.IsNullOrWhiteSpace(testEmployeeIdentifier))
                {
                    throw new PayrollException($"Invalid identifier on test employee {employeeIdentifier}");
                }

                // create test employee
                employee.Id = 0;
                employee.Identifier = testEmployeeIdentifier;
                if (!string.IsNullOrWhiteSpace(employee.LastName))
                {
                    employee.LastName = $"{employee.LastName} Test {i}";
                }

                // duplicate test employee
                tenant.Employees ??= new();
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
}