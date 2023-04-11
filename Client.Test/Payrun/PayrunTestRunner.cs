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
public class PayrunTestRunner : PayrunFileTestRunner
{
    /// <summary>The employee test mode</summary>
    public IScriptParser ScriptParser { get; }

    /// <summary>The data import mode</summary>
    public DataImportMode ImportMode { get; }

    /// <summary>The test result mode</summary>
    public TestResultMode ResultMode { get; }

    /// <summary>The delay between creation and test</summary>
    public int DelayBetweenCreateAndTest { get; set; }

    /// <summary>Initializes a new instance of the <see cref="PayrunTestRunner"/> class</summary>
    /// <param name="httpClient">The payroll engine http client</param>
    /// <param name="fileName">Name of the file</param>
    /// <param name="scriptParser">The script parser</param>
    /// <param name="testPrecision">The testing precision</param>
    /// <param name="owner">The test owner</param>
    /// <param name="importMode">The data import mode (default: single)</param>
    /// <param name="resultMode">The test result mode (default: clean)</param>
    public PayrunTestRunner(PayrollHttpClient httpClient, string fileName, IScriptParser scriptParser,
        TestPrecision testPrecision, string owner = null, DataImportMode importMode = DataImportMode.Single,
        TestResultMode resultMode = TestResultMode.CleanTest) :
        base(httpClient, fileName, testPrecision, owner)
    {
        ScriptParser = scriptParser ?? throw new ArgumentNullException(nameof(scriptParser));
        ImportMode = importMode;
        ResultMode = resultMode;
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
        try
        {
            // start cleanup
            await CleanupTenants(exchange);

            // validate tenants
            foreach (var tenant in exchange.Tenants)
            {
                // tenant validation: missing results
                ValidateTenant(tenant);
            }

            // validate tenants and regulation permissions
            // create new tenant including all payrolls, payrun and payrun job
            var import = new ExchangeImport(HttpClient, exchange, ScriptParser, UpdateMode.NoUpdate,
                ImportMode);
            await import.ImportAsync();

            // all payrun jobs should be executed
            if (DelayBetweenCreateAndTest > 0)
            {
                Task.Delay(DelayBetweenCreateAndTest).Wait();
            }

            // test tenants
            foreach (var tenant in exchange.Tenants)
            {
                // test results
                var payrunJobResult = await TestPayrunJobAsync(tenant, JobResultMode.Single);
                results.Add(tenant, payrunJobResult.ToList());
            }
        }
        finally
        {
            // end cleanup
            if (ResultMode == TestResultMode.CleanTest)
            {
                await CleanupTenants(exchange);
            }
        }

        return results;
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
            throw new PayrollException($"Tenant {tenant.Identifier} without employees");
        }

        // payrun job invocations
        if (tenant.PayrunJobInvocations == null || tenant.PayrunJobInvocations.Count == 0)
        {
            throw new PayrollException($"Tenant {tenant.Identifier} without payrun job invocations");
        }

        // payroll results
        if (tenant.PayrollResults == null || !tenant.PayrollResults.Any() || !tenant.PayrollResults[0].HasResults())
        {
            throw new PayrollException($"Tenant {tenant.Identifier} without payroll result");
        }
    }

    /// <summary>Cleanup exchange tenants</summary>
    /// <param name="exchange">The exchange</param>
    protected virtual async Task CleanupTenants(Model.Exchange exchange)
    {
        // cleanup in reverse order
        var exchangeTenants = new List<Tenant>(exchange.Tenants);
        exchangeTenants.Reverse();
        foreach (var exchangeTenant in exchangeTenants)
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
                // continue deleting remaining test tenants
            }
        }
    }
}