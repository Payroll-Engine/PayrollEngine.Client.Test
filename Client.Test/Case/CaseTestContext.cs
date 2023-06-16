using PayrollEngine.Client.Model;
using System;

namespace PayrollEngine.Client.Test.Case;

/// <summary>The case test context</summary>
public class CaseTestContext : TestContextBase
{
    /// <summary>The payroll</summary>
    public Payroll Payroll { get; set; }

    /// <summary>The division</summary>
    public Division Division { get; set; }

    /// <summary>The employee, mandatory for employee case</summary>
    public Employee Employee { get; set; }
    
    /// <summary>The evaluation period</summary>
    public DatePeriod EvaluationPeriod { get; set; }

    /// <summary>The evaluation date, default is now</summary>
    public DateTime EvaluationDate { get; set; }

    /// <summary>The regulation date, default is the evaluation date</summary>
    public DateTime RegulationDate { get; set; }

}