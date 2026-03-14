using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PayrollEngine.Client.Exchange;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Script;
using PayrollEngine.Client.Service.Api;
using Task = System.Threading.Tasks.Task;

namespace PayrollEngine.Client.Test.Payrun;

/// <summary>Payrun test runner</summary>
public class PayrunTestRunner : PayrunTestRunnerBase
{
    /// <summary>The employee test mode</summary>
    public IScriptParser ScriptParser { get; }

    /// <summary>The data import mode</summary>
    public DataImportMode ImportMode { get; }

    /// <summary>The test running mode</summary>
    public TestRunMode RunMode { get; }

    /// <summary>Initializes a new instance of the <see cref="PayrunTestRunner"/> class</summary>
    /// <param name="httpClient">The payroll engine http client</param>
    /// <param name="scriptParser">The script parser</param>
    /// <param name="settings">The test settings</param>
    /// <param name="importMode">The data import mode (default: single)</param>
    /// <param name="runMode">The employee test mode</param>
    public PayrunTestRunner(PayrollHttpClient httpClient,
        IScriptParser scriptParser,
        PayrunTestSettings settings,
        DataImportMode importMode = DataImportMode.Single,
        TestRunMode runMode = TestRunMode.RunTests) :
        base(httpClient, settings)
    {
        ArgumentNullException.ThrowIfNull(scriptParser);
        ScriptParser = scriptParser;
        ImportMode = importMode;
        RunMode = runMode;
    }

    /// <summary>Import exchange data without running tests</summary>
    /// <param name="exchange">The test exchange</param>
    public async Task ImportAsync(Model.Exchange exchange)
    {
        // apply owner
        ApplyOwner(exchange, Settings.Owner);

        // validate tenants
        foreach (var tenant in exchange.Tenants)
        {
            ValidateTenant(tenant);
        }

        // cleanup existing tenants before import
        var emptyResults = new Dictionary<Tenant, List<PayrollTestResult>>();
        await CleanupTenants(exchange, emptyResults);

        // create new tenant including all payrolls, payrun and payrun job
        var import = new ExchangeImport(HttpClient, exchange, ScriptParser, importMode: ImportMode);
        await import.ImportAsync();
    }

    /// <summary>Run payrun tests on already-imported exchange data</summary>
    /// <param name="exchange">The test exchange (must have been imported via <see cref="ImportAsync"/>)</param>
    /// <returns>A list of payrun job results</returns>
    public async Task<Dictionary<Tenant, List<PayrollTestResult>>> TestAsync(Model.Exchange exchange)
    {
        var results = new Dictionary<Tenant, List<PayrollTestResult>>();
        try
        {
            // not test skip
            if (RunMode == TestRunMode.RunTests)
            {
                foreach (var tenant in exchange.Tenants)
                {
                    if (tenant.PayrollResults == null || !tenant.PayrollResults.Any())
                    {
                        continue;
                    }
                    var payrunJobResult = await TestPayrunJobAsync(tenant, JobResultMode.Single);
                    results.Add(tenant, payrunJobResult.ToList());
                }
            }
        }
        finally
        {
            await CleanupTenants(exchange, results);
        }
        return results;
    }

    /// <summary>Start the test (import + test in one step)</summary>
    /// <returns>A list of payrun job results</returns>
    public override async Task<Dictionary<Tenant, List<PayrollTestResult>>> TestAllAsync(Model.Exchange exchange)
    {
        await ImportAsync(exchange);
        return await TestAsync(exchange);
    }

    /// <summary>Validate the tenant</summary>
    /// <param name="tenant">The tenant</param>
    protected virtual void ValidateTenant(ExchangeTenant tenant)
    {
        if (tenant.Employees == null && tenant.PayrunJobInvocations == null && tenant.PayrollResults == null)
        {
            return;
        }

        // employees
        if (tenant.Employees == null || tenant.Employees.Count == 0)
        {
            throw new PayrollException($"Tenant {tenant.Identifier} without employees.");
        }

        // payrun job invocations
        if (tenant.PayrunJobInvocations == null || tenant.PayrunJobInvocations.Count == 0)
        {
            throw new PayrollException($"Tenant {tenant.Identifier} without payrun job invocations.");
        }

        // payroll results
        if (tenant.PayrollResults == null || !tenant.PayrollResults.Any() || !tenant.PayrollResults[0].HasResults())
        {
            throw new PayrollException($"Tenant {tenant.Identifier} without payroll result.");
        }
    }

    /// <summary>Cleanup test tenants</summary>
    /// <param name="exchange">The exchange</param>
    /// <param name="results">Results</param>
    protected virtual async Task CleanupTenants(Model.Exchange exchange, Dictionary<Tenant, List<PayrollTestResult>> results)
    {
        if (Settings.ResultMode == TestResultMode.KeepTest)
        {
            return;
        }

        // cleanup in reverse order
        var exchangeTenants = new List<Tenant>(exchange.Tenants);
        exchangeTenants.Reverse();
        foreach (var exchangeTenant in exchangeTenants)
        {
            var keep = false;

            // keep tenant on failed test
            if (Settings.ResultMode == TestResultMode.KeepFailedTest &&
                results.TryGetValue(exchangeTenant, out var result))
            {
                keep = result.Any(x => x.Failed);
            }

            if (!keep)
            {
                await CleanupTenant(exchangeTenant);
            }
        }
    }

    /// <summary>Cleanup test tenant</summary>
    /// <param name="exchangeTenant">The exchange tenant</param>
    protected virtual async Task CleanupTenant(Tenant exchangeTenant)
    {
        if (exchangeTenant.Id == 0 && string.IsNullOrWhiteSpace(exchangeTenant.Identifier))
        {
            var tenant = await new TenantService(HttpClient).GetAsync<Tenant>(
                new(), exchangeTenant.Identifier);
            if (tenant != null)
            {
                exchangeTenant.Id = tenant.Id;
            }
        }
        try
        {
            if (exchangeTenant.Id != 0)
            {
                await DeleteTenantAsync(exchangeTenant.Id);
            }
        }
        catch (Exception exception)
        {
            Log.Error(exception, exception.GetBaseMessage());
        }
    }
}
