using System;
using System.Net.Http;
using System.Threading.Tasks;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Service.Api;
using PayrollEngine.Serialization;

namespace PayrollEngine.Client.Test.Report;

/// <summary>Report available function test runner</summary>
public abstract class ReportScriptTestRunner : TestRunnerBase
{
    /// <summary>The test context</summary>
    public ReportTestContext Context { get; }

    /// <summary>The Tenant</summary>
    public Tenant Tenant => Context.Tenant;

    /// <summary>The user</summary>
    public User User => Context.User;

    /// <summary>new instance of <see cref="ReportScriptTestRunner"/>see</summary>
    /// <param name="httpClient">The payroll http client</param>
    /// <param name="context">The test context</param>
    protected ReportScriptTestRunner(PayrollHttpClient httpClient, ReportTestContext context) :
        base(httpClient)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    #region Test

    /// <summary>Create new test result</summary>
    /// <param name="testType">The report test type</param>
    /// <param name="testName">The test name</param>
    /// <param name="message">The test message</param>
    /// <param name="expected">The expected value</param>
    /// <param name="received">The received value</param>
    /// <returns>The new test result</returns>
    protected ReportScriptTestResult NewResult(ReportTestType testType, string testName, string message,
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
    /// <param name="testType">The report test type</param>
    /// <param name="testName">The test name</param>
    /// <param name="message">The test message</param>
    /// <param name="expected">The expected value</param>
    /// <param name="received">The received value</param>
    /// <returns>The new test or test failed result</returns>
    protected ReportScriptTestResult NewResult(bool failed, ReportTestType testType, string testName, string message,
        object expected = null, object received = null) => failed
        ? NewFailedResult(testType, testName, message, expected, received)
        : NewResult(testType, testName, message, expected, received);

    /// <summary>Create new failed test result</summary>
    /// <param name="testType">The report test type</param>
    /// <param name="testName">The test name</param>
    /// <param name="message">The test message</param>
    /// <param name="expected">The expected value</param>
    /// <param name="received">The received value</param>
    /// <returns>The new failed test result</returns>
    protected ReportScriptTestResult NewFailedResult(ReportTestType testType, string testName, string message,
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
    protected ReportScriptTestResult NewResult(HttpRequestException exception, string testName,
        object expected = null)
    {
        if (exception == null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

        var success = exception.StatusCode.HasValue &&
                      (int)exception.StatusCode.Value >= 200 &&
                      (int)exception.StatusCode.Value <= 299;
        return new()
        {
            Failed = !success,
            TestName = testName,
            TestType = ReportTestType.Http,
            HttpStatusCode = exception.StatusCode.HasValue ? (int)exception.StatusCode.Value : default,
            Expected = expected != null ? DefaultJsonSerializer.Serialize(expected) : default,
            Message = exception.GetBaseMessage()
        };
    }

    #endregion

    #region Report

    /// <summary>Get regulation</summary>
    /// <param name="tenantId">The tenant id</param>
    /// <param name="name">The regulation name</param>
    protected async Task<Regulation> GetRegulationAsync(int tenantId, string name) =>
        await new RegulationService(HttpClient).GetAsync<Regulation>(new(tenantId), name);

    /// <summary>Get report</summary>
    /// <param name="tenantId">The tenant id</param>
    /// <param name="regulationId">The regulation id</param>
    /// <param name="name">The report name</param>
    protected async Task<Model.Report> GetReportAsync(int tenantId, int regulationId, string name) =>
        await new ReportService(HttpClient).GetAsync<Model.Report>(new(tenantId, regulationId), name);

    /// <summary>Build report</summary>
    /// <param name="tenantId">The tenant id</param>
    /// <param name="regulationId">The regulation id</param>
    /// <param name="reportId">The report id</param>
    /// <param name="reportRequest">The report request</param>
    protected async Task<ReportSet> BuildReportAsync(int tenantId, int regulationId,
        int reportId, ReportRequest reportRequest) =>
        await new ReportSetService(HttpClient).GetAsync<ReportSet>(new(tenantId, regulationId),
            reportId, reportRequest);

    #endregion

}