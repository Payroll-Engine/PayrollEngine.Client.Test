using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Service.Api;
using PayrollEngine.Serialization;

namespace PayrollEngine.Client.Test.Case;

/// <summary>Case available function test runner</summary>
public abstract class CaseScriptTestRunner : TestRunnerBase
{
    /// <summary>The test context</summary>
    public CaseTestContext Context { get; }

    /// <summary>The Tenant</summary>
    public Tenant Tenant => Context.Tenant;

    /// <summary>The user</summary>
    public User User => Context.User;

    /// <summary>The payroll</summary>
    public Payroll Payroll => Context.Payroll;

    /// <summary>The employee</summary>
    public Employee Employee => Context.Employee;

    /// <summary>The evaluation date, default is now</summary>
    public DateTime? EvaluationDate => Context.EvaluationDate;

    /// <summary>The regulation date, default is now</summary>
    public DateTime? RegulationDate => Context.RegulationDate;

    /// <summary>new instance of <see cref="CaseScriptTestRunner"/>see</summary>
    /// <param name="httpClient">The payroll http client</param>
    /// <param name="context">The test context</param>
    protected CaseScriptTestRunner(PayrollHttpClient httpClient, CaseTestContext context) :
        base(httpClient)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    #region Test

    /// <summary>Create new test result</summary>
    /// <param name="testType">The case test type</param>
    /// <param name="testName">The test name</param>
    /// <param name="message">The test message</param>
    /// <param name="expected">The expected value</param>
    /// <param name="received">The received value</param>
    /// <returns>The new test result</returns>
    protected CaseScriptTestResult NewResult(CaseTestType testType, string testName, string message,
        object expected = null, object received = null) =>
        new()
        {
            TestName = testName,
            TestType = testType,
            Expected = expected != null ? DefaultJsonSerializer.Serialize(expected) : null,
            Received = received != null ? DefaultJsonSerializer.Serialize(received) : null,
            Message = message
        };

    /// <summary>Create new test result with failed condition</summary>
    /// <param name="failed">The failed state</param>
    /// <param name="testType">The case test type</param>
    /// <param name="testName">The test name</param>
    /// <param name="message">The test message</param>
    /// <param name="expected">The expected value</param>
    /// <param name="received">The received value</param>
    /// <returns>The new test or test failed result</returns>
    protected CaseScriptTestResult NewResult(bool failed, CaseTestType testType, string testName, string message,
        object expected = null, object received = null) => failed
        ? NewFailedResult(testType, testName, message, expected, received)
        : NewResult(testType, testName, message, expected, received);

    /// <summary>Create new failed test result</summary>
    /// <param name="testType">The case test type</param>
    /// <param name="testName">The test name</param>
    /// <param name="message">The test message</param>
    /// <param name="expected">The expected value</param>
    /// <param name="received">The received value</param>
    /// <returns>The new failed test result</returns>
    protected CaseScriptTestResult NewFailedResult(CaseTestType testType, string testName, string message,
        object expected = null, object received = null) =>
        new()
        {
            Failed = true,
            TestName = testName,
            TestType = testType,
            Expected = expected != null ? DefaultJsonSerializer.Serialize(expected) : null,
            Received = received != null ? DefaultJsonSerializer.Serialize(received) : null,
            Message = message
        };

    /// <summary>Create new test result from http</summary>
    /// <param name="exception">The http request exception</param>
    /// <param name="testName">The test name</param>
    /// <param name="expected">The expected value</param>
    /// <returns>The new test result</returns>
    protected CaseScriptTestResult NewResult(HttpRequestException exception, string testName,
        object expected = null)
    {
        if (exception == null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

        var statusCode = exception.StatusCode.HasValue ? (int)exception.StatusCode.Value : 0;
        var success = statusCode >= 200 && statusCode <= 299;
        return new()
        {
            Failed = !success,
            TestName = testName,
            TestType = CaseTestType.Http,
            ErrorCode = statusCode,
            HttpStatusCode = statusCode != 0 ? statusCode : default,
            Expected = expected != null ? DefaultJsonSerializer.Serialize(expected) : default,
            Message = exception.GetBaseMessage()
        };
    }

    #endregion

    #region Case

    /// <summary>Get case if available</summary>
    /// <param name="caseName">The case name</param>
    /// <returns>The case if available, otherwise null</returns>
    protected CaseSet GetAvailableCase(string caseName) =>
        GetAvailableCaseAsync(caseName).Result;

    /// <summary>Get case if available</summary>
    /// <param name="caseName">The case name</param>
    /// <returns>The case if available, otherwise null</returns>
    protected virtual async Task<CaseSet> GetAvailableCaseAsync(string caseName)
    {
        if (string.IsNullOrWhiteSpace(caseName))
        {
            throw new ArgumentException(nameof(caseName));
        }

        var @case = await GetCaseAsync(caseName);
        if (@case == null)
        {
            throw new PayrollException($"Unknown case {caseName}");
        }

        var cases = await new PayrollService(HttpClient).GetAvailableCasesAsync<CaseSet>(
            new(Tenant.Id, Payroll.Id),
            userId: User.Id,
            caseType: @case.CaseType,
            caseNames: new[] { caseName },
            employeeId: Employee?.Id,
            evaluationDate: EvaluationDate,
            regulationDate: RegulationDate);

        return cases.Any() ? @case : null;
    }

    /// <summary>Get case by name</summary>
    /// <param name="caseName">The case name</param>
    /// <param name="caseChangeSetup">The case setup (optional)</param>
    /// <returns>The case including the case fields and related cases</returns>
    protected CaseSet GetCase(string caseName, CaseChangeSetup caseChangeSetup = null) =>
        GetCaseAsync(caseName, caseChangeSetup).Result;

    /// <summary>Get case by name async</summary>
    /// <param name="caseName">The case name</param>
    /// <param name="caseChangeSetup">The case setup (optional)</param>
    /// <returns>The case including the case fields and related cases</returns>
    protected virtual async Task<CaseSet> GetCaseAsync(string caseName, CaseChangeSetup caseChangeSetup = null)
    {
        if (string.IsNullOrWhiteSpace(caseName))
        {
            throw new ArgumentException(nameof(caseName));
        }
        return await new PayrollService(HttpClient).BuildCaseAsync<CaseSet>(
            new(Tenant.Id, Payroll.Id),
            caseName: caseName,
            userId: User.Id,
            employeeId: Employee?.Id,
            evaluationDate: EvaluationDate,
            regulationDate: RegulationDate,
            caseChangeSetup: caseChangeSetup);
    }

    /// <summary>Add new case</summary>
    /// <param name="caseChangeSetup">The case setup</param>
    /// <returns>The creation result</returns>
    protected virtual async Task<CaseChange> AddCaseAsync(CaseChangeSetup caseChangeSetup)
    {
        if (caseChangeSetup == null)
        {
            throw new ArgumentNullException(nameof(caseChangeSetup));
        }

        return await new PayrollService(HttpClient).AddCaseAsync<CaseChangeSetup, CaseChange>(
            new(Tenant.Id, Payroll.Id),
            caseChangeSetup: caseChangeSetup);
    }

    #endregion

}