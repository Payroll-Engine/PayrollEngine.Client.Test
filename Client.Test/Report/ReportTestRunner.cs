using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Service.Api;

namespace PayrollEngine.Client.Test.Report;

/// <summary>Report test runner</summary>
public class ReportTestRunner : TestRunnerBase
{
    /// <summary>Parse report custom test class</summary>
    private sealed class ReportCustomTestParser : CustomTestParser<ReportCustomTest, ReportTestContext>
    {
        internal ReportCustomTestParser(string testName, List<string> sourceFiles) :
            base(testName, sourceFiles)
        {
        }

        protected override List<Type> GetDefaultReferences()
        {
            var references = base.GetDefaultReferences();
            references.Add(typeof(ReportTestContext));
            references.Add(typeof(ReportBuildTest));
            references.Add(typeof(ReportExecuteTest));
            return references;
        }
    }

    /// <summary>Initializes a new instance of the class</summary>
    /// <param name="httpClient">The payroll engine http client</param>
    public ReportTestRunner(PayrollHttpClient httpClient) :
        base(httpClient)
    {
    }

    /// <summary>Test case</summary>
    /// <returns>The report test results</returns>
    public virtual async Task<ReportTestResult> TestAsync(ReportTest reportTest)
    {
        // test context
        var context = await CreateTestContext(reportTest);

        // init test
        InitTest(reportTest, context);

        // custom tests
        var testParser = new ReportCustomTestParser(reportTest.TestName, reportTest.CustomTestFiles);

        // prepare test result
        var result = new ReportTestResult();

        // report build tests
        if (reportTest.BuildTests != null && reportTest.BuildTests.Any())
        {
            var testRunner = new ReportBuildTestRunner(HttpClient, context);
            foreach (var test in reportTest.BuildTests)
            {
                var customTest = testParser.GetTest(test.TestName, test.GetType());
                if (customTest != null)
                {
                    // custom test
                    result.Results.Add(
                        RunCustomTest(test, ReportTestType.ReportBuildCustom, testParser.TestType, customTest, context));
                }
                else
                {
                    // input/output test
                    result.Results.AddRange(await testRunner.Test(test));
                }

                // ignore further tests
                if (result.IsFailed())
                {
                    return result;
                }
            }
        }

        // report execute tests
        if (reportTest.ExecuteTests != null && reportTest.ExecuteTests.Any())
        {
            var testRunner = new ReportExecuteTestRunner(HttpClient, context);
            foreach (var test in reportTest.ExecuteTests)
            {
                var customTest = testParser.GetTest(test.TestName, test.GetType());
                if (customTest != null)
                {
                    // custom test
                    result.Results.Add(
                        RunCustomTest(test, ReportTestType.ReportExecuteCustom, testParser.TestType, customTest, context));
                }
                else
                {
                    // input/output test
                    result.Results.AddRange(await testRunner.Test(test));
                }

                // ignore further tests
                if (result.IsFailed())
                {
                    return result;
                }
            }
        }

        return result;
    }

    /// <summary>Initialize the test</summary>
    /// <param name="reportTest">The test</param>
    /// <param name="context">The test context</param>
    protected virtual void InitTest(ReportTest reportTest, ReportTestContext context)
    {
        reportTest.BuildTests?.ForEach(x => x.InitTest());
        reportTest.ExecuteTests?.ForEach(x => x.InitTest());
    }

    /// <summary>Run the test</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="test"></param>
    /// <param name="reportTestType"></param>
    /// <param name="testType"></param>
    /// <param name="testMethod"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    /// <exception cref="TestCompileException"></exception>
    protected virtual ReportScriptTestResult RunCustomTest<T>(T test, ReportTestType reportTestType, Type testType,
        MethodInfo testMethod, ReportTestContext context)
    {
        try
        {
            // ctor signature: public MyTest(PayrollHttpClient httpClient, ReportTestContext context);
            var instance = Activator.CreateInstance(testType, args:
            [
                HttpClient,
                context
            ]);
            // test method signature: public void MyTest(ReportBuildTest test)
            // test method signature: public void MyTest(ReportStartTest test)
            // test method signature: public void MyTest(ReportEndTest test)
            testMethod.Invoke(instance, [test]);
        }
        catch (Exception exception)
        {
            var baseException = exception;
            while (baseException != null)
            {
                if (baseException is AssertFailedException)
                {
                    return new()
                    {
                        TestName = testMethod.Name,
                        TestType = reportTestType,
                        Failed = true,
                        Message = baseException.Message
                    };
                }
                baseException = baseException.InnerException;
            }
            throw new TestCompileException($"{exception.GetBaseMessage()}", exception);
        }

        return new()
        {
            TestName = testMethod.Name,
            TestType = reportTestType,
            Failed = false
        };
    }

    /// <summary>Create the test context</summary>
    /// <param name="reportTest">The test</param>
    /// <returns>The test context</returns>
    protected virtual async Task<ReportTestContext> CreateTestContext(ReportTest reportTest)
    {
        // build the test context
        var context = new ReportTestContext
        {
            TestName = reportTest.TestName,
            // tenant
            Tenant = await GetTenantAsync(reportTest.TenantIdentifier)
        };
        if (context.Tenant == null)
        {
            throw new PayrollException($"Missing tenant {reportTest.TenantIdentifier}");
        }

        // user
        context.User = await GetUserAsync(context.Tenant.Id, reportTest.UserIdentifier);
        if (context.User == null)
        {
            throw new PayrollException($"Missing user {reportTest.UserIdentifier}");
        }

        // regulation
        context.Regulation = await GetRegulationAsync(context.Tenant.Id, reportTest.RegulationName);
        if (context.Regulation == null)
        {
            throw new PayrollException($"Missing regulation {reportTest.RegulationName}");
        }

        return context;
    }

    /// <summary>Get regulation</summary>
    /// <param name="tenantId">The tenant id</param>
    /// <param name="name">The regulation name</param>
    protected async Task<Regulation> GetRegulationAsync(int tenantId, string name) =>
        await new RegulationService(HttpClient).GetAsync<Regulation>(new(tenantId), name);
}