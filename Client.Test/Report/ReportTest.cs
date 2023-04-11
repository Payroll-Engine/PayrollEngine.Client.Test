using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PayrollEngine.Client.Test.Report;

/// <summary>Report test</summary>
public class ReportTest
{
    /// <summary>The test name</summary>
    [Required]
    public string TestName { get; set; }

    /// <summary>The test description</summary>
    public string TestDescription { get; set; }

    /// <summary>The test category</summary>
    public string TestCategory { get; set; }

    /// <summary>The tenant identifier</summary>
    [Required]
    public string TenantIdentifier { get; set; }

    /// <summary>The user identifier</summary>
    [Required]
    public string UserIdentifier { get; set; }

    /// <summary>The regulation name</summary>
    [Required]
    public string RegulationName { get; set; }

    /// <summary>Custom test files</summary>
    public List<string> CustomTestFiles { get; set; }

    /// <summary>The report build tests</summary>
    public List<ReportBuildTest> BuildTests { get; set; }

    /// <summary>The report execute tests</summary>
    public List<ReportExecuteTest> ExecuteTests { get; set; }
}