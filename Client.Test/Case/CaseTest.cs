using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using PayrollEngine.Client.Model;

namespace PayrollEngine.Client.Test.Case;

/// <summary>Case test</summary>
public class CaseTest
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

    /// <summary>The payroll name</summary>
    [Required]
    public string PayrollName { get; set; }

    /// <summary>The employee identifier, mandatory for employee case</summary>
    public string EmployeeIdentifier { get; set; }

    /// <summary>The evaluation date, default is now</summary>
    public DateTime? EvaluationDate { get; set; }

    /// <summary>The period date, default is now</summary>
    public DateTime? PeriodDate { get; set; }

    /// <summary>The regulation date, default is now</summary>
    public DateTime? RegulationDate { get; set; }

    /// <summary>The init cases</summary>
    public List<CaseChangeSetup> InitCases { get; set; }

    /// <summary>Custom test files</summary>
    public List<string> CustomTestFiles { get; set; }

    /// <summary>The case available tests</summary>
    public List<CaseAvailableTest> AvailableTests { get; set; }

    /// <summary>The case build tests</summary>
    public List<CaseBuildTest> BuildTests { get; set; }

    /// <summary>The case validate tests</summary>
    public List<CaseValidateTest> ValidateTests { get; set; }
}