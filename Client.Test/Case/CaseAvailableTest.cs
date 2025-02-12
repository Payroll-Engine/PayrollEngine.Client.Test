using System.ComponentModel.DataAnnotations;
using PayrollEngine.Client.Model;

namespace PayrollEngine.Client.Test.Case;

/// <summary>Case available function test.
/// Test input: case change setup
/// Test output: bool indicates the case available state
/// </summary>
public class CaseAvailableTest : CaseScriptTest<CaseChangeSetup, bool>
{
    /// <summary>The case name</summary>
    [Required]
    public string CaseName { get; set; }
}