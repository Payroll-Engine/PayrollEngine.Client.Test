using System;
using System.Collections.Generic;
using System.Linq;
using PayrollEngine.Client.Model;

namespace PayrollEngine.Client.Test.Payrun;

/// <summary>Result of payroll test</summary>
public class PayrollTestResult
{
    /// <summary>The wage type results</summary>
    public IList<WageTypeTestResult> WageTypeResults { get; } = new List<WageTypeTestResult>();

    /// <summary>The collector results</summary>
    public IList<CollectorTestResult> CollectorResults { get; } = new List<CollectorTestResult>();

    /// <summary>The payrun results</summary>
    public IList<PayrunTestResult> PayrunResults { get; } = new List<PayrunTestResult>();

    /// <summary>The tenant</summary>
    public Tenant Tenant { get; }

    /// <summary>The employee</summary>
    public Employee Employee { get; }

    /// <summary>The payrun job</summary>
    public PayrunJob PayrunJob { get; }

    /// <summary>Total result count</summary>
    public int TotalResultCount =>
        WageTypeResults.Count + CollectorResults.Count + PayrunResults.Count;

    /// <summary>Initializes a new instance of the <see cref="PayrollTestResult"/> class</summary>
    /// <param name="tenant">The tenant</param>
    /// <param name="payrunJob">The payrun job</param>
    /// <param name="employee">The employee</param>
    public PayrollTestResult(Tenant tenant, Employee employee, PayrunJob payrunJob)
    {
        Tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        Employee = employee ?? throw new ArgumentNullException(nameof(employee));
        PayrunJob = payrunJob ?? throw new ArgumentNullException(nameof(payrunJob));
    }

    /// <summary>Test if payrun test is failed</summary>
    public bool IsFailed() =>
        WageTypeResults.Any(x => x.IsFailed()) ||
        CollectorResults.Any(x => x.IsFailed()) ||
        PayrunResults.Any(x => x.IsFailed());
}