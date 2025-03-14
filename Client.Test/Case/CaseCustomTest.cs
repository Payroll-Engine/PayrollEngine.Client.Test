﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Service.Api;

namespace PayrollEngine.Client.Test.Case;

/// <summary>Case custom test class</summary>
public abstract class CaseCustomTest : CustomTestBase<CaseTestContext>
{
    private CultureInfo Culture { get; }

    /// <summary>The payroll</summary>
    public Payroll Payroll => Context.Payroll;

    /// <summary>The employee</summary>
    public Employee Employee => Context.Employee;

    /// <summary>The evaluation date, default is now</summary>
    public DateTime? EvaluationDate => Context.EvaluationDate;

    /// <summary>The regulation date, default is now</summary>
    public DateTime? RegulationDate => Context.RegulationDate;

    /// <summary>New instance of <see cref="CaseCustomTest"/></summary>
    /// <param name="httpClient">The payroll http client</param>
    /// <param name="context">The test context</param>
    protected CaseCustomTest(PayrollHttpClient httpClient, CaseTestContext context) :
        base(httpClient, context)
    {
        Culture = CultureTool.GetTenantCulture(Tenant);
    }

    #region Cases

    /// <summary>Get cases by type</summary>
    /// <param name="caseType">The case type</param>
    /// <param name="caseSlot">The case slot</param>
    /// <param name="clusterSetName">The cluster set name</param>
    /// <param name="culture">The culture</param>
    /// <returns>The cases by type</returns>
    protected List<Model.Case> GetAvailableCases(CaseType caseType, string caseSlot = null,
        string clusterSetName = null, string culture = null) =>
        new PayrollService(HttpClient).GetAvailableCasesAsync<Model.Case>(
            new(Tenant.Id, Payroll.Id),
            userId: User.Id,
            caseType: caseType,
            employeeId: Employee?.Id,
            caseSlot: caseSlot,
            clusterSetName: clusterSetName,
            culture: culture,
            regulationDate: RegulationDate,
            evaluationDate: EvaluationDate).Result;


    /// <summary>Get a case</summary>
    /// <param name="caseName">The case name</param>
    /// <param name="clusterSetName">The cluster set name</param>
    /// <param name="culture">The culture</param>
    /// <param name="caseChangeSetup">The case change setup</param>
    /// <returns>The case including the case fields and related cases</returns>
    protected CaseSet GetCase(string caseName, string clusterSetName = null, string culture = null,
        CaseChangeSetup caseChangeSetup = null) =>
        new PayrollService(HttpClient).BuildCaseAsync<CaseSet>(
            new(Tenant.Id, Payroll.Id),
            caseName: caseName,
            userId: User.Id,
            employeeId: Employee?.Id,
            clusterSetName: clusterSetName,
            culture: culture,
            regulationDate: RegulationDate,
            evaluationDate: EvaluationDate,
            caseChangeSetup: caseChangeSetup).Result;

    #endregion

    #region Case Values

    /// <summary>Get case period values</summary>
    /// <param name="caseFieldName">The case field name</param>
    /// <returns>The case values</returns>
    protected List<CaseFieldValue> GetCasePeriodValues(string caseFieldName) =>
        GetCasePeriodValues([caseFieldName]);

    /// <summary>Get case period values</summary>
    /// <param name="caseFieldNames">The case field names</param>
    /// <returns>The case values</returns>
    protected List<CaseFieldValue> GetCasePeriodValues(IEnumerable<string> caseFieldNames)
    {
        var period = Context.EvaluationPeriod;
        var caseValues = new PayrollService(HttpClient).GetAvailableCaseFieldValuesAsync(
            new(Tenant.Id, Payroll.Id), User.Id, caseFieldNames,
            period.Start, period.End, Employee?.Id, RegulationDate, EvaluationDate).Result;
        return caseValues;
    }

    /// <summary>Get case period value</summary>
    /// <param name="caseFieldName">The case field name</param>
    /// <returns>The case values</returns>
    protected CaseFieldValue GetCasePeriodValue(string caseFieldName) =>
        GetCasePeriodValues(caseFieldName)?.FirstOrDefault();

    /// <summary>Get typed case period value</summary>
    /// <param name="caseFieldName">The case field name</param>
    /// <returns>The case value</returns>
    protected T GetCasePeriodValue<T>(string caseFieldName)
    {
        var caseValue = GetCasePeriodValue(caseFieldName);
        return caseValue != null ? (T)ValueConvert.ToValue(caseValue.Value, caseValue.ValueType, Culture) : default;
    }

    /// <summary>Get case raw values</summary>
    /// <param name="caseFieldName">The case field name</param>
    /// <param name="regulationDate">The regulation date (default: UTC now)</param>
    /// <param name="evaluationDate">The evaluation date (default: value date)</param>
    /// <param name="caseSlot">The case slot</param>
    /// <returns>The case raw values</returns>
    protected List<CaseFieldValue> GetCaseValues(string caseFieldName, DateTime? regulationDate = null,
        DateTime? evaluationDate = null, string caseSlot = null)
    {
        var period = Context.EvaluationPeriod;
        var caseValues = new PayrollService(HttpClient).GetCaseValuesAsync(
            new(Tenant.Id, Payroll.Id), period.Start, period.End,
            [caseFieldName], Employee?.Id, regulationDate, evaluationDate, caseSlot).Result;
        return caseValues;
    }

    #endregion
}