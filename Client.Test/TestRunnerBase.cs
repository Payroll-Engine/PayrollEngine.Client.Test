using System;
using System.Threading.Tasks;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Service.Api;
using Task = System.Threading.Tasks.Task;

namespace PayrollEngine.Client.Test;

/// <summary>Base class for payroll tests</summary>
public abstract class TestRunnerBase
{
    /// <summary>The payroll http client</summary>
    public PayrollHttpClient HttpClient { get; }

    /// <summary>Initializes a new instance of the <see cref="TestRunnerBase"/> class</summary>
    /// <param name="httpClient">The payroll engine http client</param>
    protected TestRunnerBase(PayrollHttpClient httpClient)
    {
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <summary>Get tenant</summary>
    /// <param name="identifier">The tenant identifier</param>
    protected async Task<Tenant> GetTenantAsync(string identifier) =>
        await new TenantService(HttpClient).GetAsync<Tenant>(new(), identifier);

    /// <summary>Delete tenant</summary>
    /// <param name="tenantId">The tenant id</param>
    protected async Task DeleteTenantAsync(int tenantId) =>
        await new TenantService(HttpClient).DeleteAsync(new(), tenantId);

    /// <summary>Get user</summary>
    /// <param name="tenantId">The tenant id</param>
    /// <param name="identifier">The tenant identifier</param>
    protected async Task<User> GetUserAsync(int tenantId, string identifier) =>
        await new UserService(HttpClient).GetAsync<User>(new(tenantId), identifier);

    /// <summary>Get tenant</summary>
    /// <param name="tenantId">The tenant id</param>
    /// <param name="identifier">The tenant identifier</param>
    protected async Task<Employee> GetEmployeeAsync(int tenantId, string identifier) =>
        await new EmployeeService(HttpClient).GetAsync<Employee>(new(tenantId), identifier);
}