using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PayrollEngine.Client.Test.Report;

/// <summary>Report script base test</summary>
/// <typeparam name="TIn">The case test input type</typeparam>
/// <typeparam name="TOut">The case test output type</typeparam>
public abstract class ReportScriptTest<TIn, TOut> : TestBase<TIn, TOut>
{
    /// <summary>The report name</summary>
    [Required]
    public string ReportName { get; set; }

    /// <summary>The report parameters</summary>
    public Dictionary<string, string> Parameters { get; set; }
}