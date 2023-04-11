using System.Collections.Generic;
using PayrollEngine.Client.Model;

namespace PayrollEngine.Client.Test.Case;

/// <summary>Case script base test</summary>
/// <typeparam name="TIn">The case test input type</typeparam>
/// <typeparam name="TOut">The case test output type</typeparam>
public abstract class CaseScriptTest<TIn, TOut> : TestBase<TIn, TOut>
{
    /// <summary>The init cases</summary>
    public List<CaseChangeSetup> InitCases { get; set; }
}