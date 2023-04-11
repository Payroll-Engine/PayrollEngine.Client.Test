using System;
using PayrollEngine.Client.Model;

namespace PayrollEngine.Client.Test;

/// <summary>Base class for custom tests</summary>
public abstract class CustomTestBase<TContext>
    where TContext : TestContextBase
{
    /// <summary>The Payroll http client</summary>
    public PayrollHttpClient HttpClient { get; }

    /// <summary>The test context</summary>
    public TContext Context { get; }

    /// <summary>The Tenant</summary>
    public Tenant Tenant => Context.Tenant;

    /// <summary>The user</summary>
    public User User => Context.User;

    /// <summary>New instance of <see cref="CustomTestBase{T}"/></summary>
    /// <param name="httpClient">The payroll http client</param>
    /// <param name="context">The test context</param>
    protected CustomTestBase(PayrollHttpClient httpClient, TContext context)
    {
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }
}