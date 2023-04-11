
namespace PayrollEngine.Client.Test.Report;

/// <summary>Report custom test class</summary>
public abstract class ReportCustomTest : CustomTestBase<ReportTestContext>
{
    /// <summary>New instance of <see cref="ReportCustomTest"/></summary>
    /// <param name="httpClient">The payroll http client</param>
    /// <param name="context">The test context</param>
    protected ReportCustomTest(PayrollHttpClient httpClient, ReportTestContext context) :
        base(httpClient, context)
    {
    }
}