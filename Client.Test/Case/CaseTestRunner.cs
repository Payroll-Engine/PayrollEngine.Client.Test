using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using PayrollEngine.Client.Model;
using PayrollEngine.Client.Service;
using PayrollEngine.Client.Service.Api;
using PayrollEngine.Serialization;
using Task = System.Threading.Tasks.Task;

namespace PayrollEngine.Client.Test.Case;

/// <summary>Case test runner</summary>
public class CaseTestRunner : FileTestRunner
{

    /// <summary>Parse case custom test class</summary>
    private sealed class CaseCustomTestParser : CustomTestParser<CaseCustomTest, CaseTestContext>
    {
        internal CaseCustomTestParser(string testName, List<string> sourceFiles) :
            base(testName, sourceFiles)
        {
        }

        protected override List<Type> GetDefaultReferences()
        {
            var references = base.GetDefaultReferences();
            references.Add(typeof(CaseTestContext));
            references.Add(typeof(CaseAvailableTest));
            references.Add(typeof(CaseBuildTest));
            references.Add(typeof(CaseValidateTest));
            return references;
        }
    }

    /// <summary>Initializes a new instance of the <see cref="CaseTestRunner"/> class</summary>
    /// <param name="httpClient">The payroll engine http client</param>
    /// <param name="fileName">Name of the file</param>
    public CaseTestRunner(PayrollHttpClient httpClient, string fileName) :
        base(httpClient, fileName)
    {
    }

    /// <summary>Test case</summary>
    /// <returns>The case test results</returns>
    public virtual async Task<CaseTestResult> TestAsync()
    {
        // load test
        var caseTest = await LoadCaseTest();

        // test context
        var context = await CreateTestContext(caseTest);

        // init test
        InitTest(caseTest, context);

        // custom tests
        var testParser = new CaseCustomTestParser(caseTest.TestName, caseTest.CustomTestFiles);

        // case test init cases
        if (caseTest.InitCases != null)
        {
            await SetupInitCases(caseTest.InitCases, context);
        }

        // prepare test result
        var result = new CaseTestResult();

        // case available tests
        if (caseTest.AvailableTests != null && caseTest.AvailableTests.Any())
        {
            var testRunner = new CaseAvailableTestRunner(HttpClient, context);
            foreach (var test in caseTest.AvailableTests)
            {
                // available test init cases
                if (test.InitCases != null)
                {
                    await SetupInitCases(test.InitCases, context);
                }

                var customTest = testParser.GetTest(test.TestName, test.GetType());
                if (customTest != null)
                {
                    // custom test
                    result.Results.Add(
                        RunCustomTest(test, CaseTestType.CaseAvailableCustom, testParser.TestType, customTest, context));
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

        // case build tests
        if (caseTest.BuildTests != null && caseTest.BuildTests.Any())
        {
            var testRunner = new CaseBuildTestRunner(HttpClient, context);
            foreach (var test in caseTest.BuildTests)
            {
                // build test init cases
                if (test.InitCases != null)
                {
                    await SetupInitCases(test.InitCases, context);
                }

                var customTest = testParser.GetTest(test.TestName, test.GetType());
                if (customTest != null)
                {
                    // custom test
                    result.Results.Add(
                        RunCustomTest(test, CaseTestType.CaseBuildCustom, testParser.TestType, customTest, context));
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

        // case validate tests
        if (caseTest.ValidateTests != null && caseTest.ValidateTests.Any())
        {
            var testRunner = new CaseValidateTestRunner(HttpClient, context);
            foreach (var test in caseTest.ValidateTests)
            {
                // validate test init cases
                if (test.InitCases != null)
                {
                    await SetupInitCases(test.InitCases, context);
                }

                var customTest = testParser.GetTest(test.TestName, test.GetType());
                if (customTest != null)
                {
                    // custom test
                    result.Results.Add(
                        RunCustomTest(test, CaseTestType.CaseValidateCustom, testParser.TestType, customTest, context));
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
    /// <param name="caseTest">The test</param>
    /// <param name="context">The test context</param>
    protected virtual void InitTest(CaseTest caseTest, CaseTestContext context)
    {
        caseTest.AvailableTests?.ForEach(x => x.InitTest());
        caseTest.BuildTests?.ForEach(x => x.InitTest());

        // case validate
        if (caseTest.ValidateTests != null && caseTest.ValidateTests.Any())
        {
            var divisions = new Dictionary<string, Division>();
            foreach (var test in caseTest.ValidateTests)
            {
                test.InitTest();

                if (test.Input != null)
                {
                    // update user
                    test.Input.UserId = context.User.Id;

                    // update employee
                    if (!test.Input.EmployeeId.HasValue &&
                        string.Equals(test.Input.EmployeeIdentifier, context.Employee.Identifier))
                    {
                        test.Input.EmployeeId = context.Employee.Id;
                    }

                    // update division
                    var divisionName = test.Input.DivisionName;
                    if (!test.Input.DivisionId.HasValue &&
                        !string.IsNullOrWhiteSpace(divisionName))
                    {
                        Division division;
                        if (divisions.TryGetValue(divisionName, out var divisionByName))
                        {
                            division = divisionByName;
                        }
                        else
                        {
                            division = GetDivisionAsync(context.Tenant.Id, divisionName).Result;
                            if (division == null)
                            {
                                throw new PayrollException($"Unknown division {divisionName}");
                            }
                            divisions.Add(divisionName, division);
                        }
                        test.Input.DivisionId = division.Id;
                    }
                }
            }
        }
    }

    /// <summary>Setup initialization cases</summary>
    /// <param name="initCases">The cases to setup</param>
    /// <param name="testContext">The test context</param>
    protected virtual async Task SetupInitCases(List<CaseChangeSetup> initCases, CaseTestContext testContext)
    {
        if (!initCases.Any())
        {
            return;
        }

        var service = new PayrollService(HttpClient);
        var serviceContext = new PayrollServiceContext(testContext.Tenant.Id, testContext.Payroll.Id);
        foreach (var initCase in initCases)
        {
            // user
            // enforce test user as case change user
            initCase.UserIdentifier = testContext.User.Identifier;
            initCase.UserId = testContext.User.Id;

            // division
            if (!string.IsNullOrWhiteSpace(initCase.DivisionName))
            {
                var division = await GetDivisionAsync(testContext.Tenant.Id, initCase.DivisionName);
                if (division == null)
                {
                    throw new PayrollException($"Unknown division {initCase.DivisionName}");
                }

                initCase.DivisionId = division.Id;
            }

            // add case
            try
            {
                await service.AddCaseAsync<CaseChangeSetup, CaseChange>(serviceContext, initCase);
            }
            catch (Exception exception)
            {
                throw new PayrollException($"{exception.GetBaseMessage()}", exception);
            }
        }
    }

    /// <summary>Run the test</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="test"></param>
    /// <param name="caseTestType"></param>
    /// <param name="testType"></param>
    /// <param name="testMethod"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    /// <exception cref="TestCompileException"></exception>
    protected virtual CaseScriptTestResult RunCustomTest<T>(T test, CaseTestType caseTestType, Type testType,
        MethodInfo testMethod, CaseTestContext context)
    {
        try
        {
            // ctor signature: public MyTest(PayrollHttpClient httpClient, CaseTestContext context);
            var instance = Activator.CreateInstance(testType, args: new object[]
            {
                HttpClient,
                context
            });
            // test method signature: public void MyTest(CaseAvailableTest test)
            // test method signature: public void MyTest(CaseBuildTest test)
            // test method signature: public void MyTest(CaseValidateTest test)
            testMethod.Invoke(instance, new object[] { test });
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
                        TestType = caseTestType,
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
            TestType = caseTestType,
            Failed = false
        };
    }

    /// <summary>Create the test context</summary>
    /// <param name="caseTest">The test</param>
    /// <returns>The test context</returns>
    protected virtual async Task<CaseTestContext> CreateTestContext(CaseTest caseTest)
    {
        // init dates
        var evaluationDate = caseTest.EvaluationDate ?? Date.Now;
        var regulationDate = caseTest.RegulationDate ?? evaluationDate;

        // build the test context
        var context = new CaseTestContext
        {
            TestName = caseTest.TestName,
            EvaluationDate = evaluationDate,
            RegulationDate = regulationDate,
            // tenant
            Tenant = await GetTenantAsync(caseTest.TenantIdentifier)
        };
        if (context.Tenant == null)
        {
            throw new PayrollException($"Missing tenant {caseTest.TenantIdentifier}");
        }

        // user
        context.User = await GetUserAsync(context.Tenant.Id, caseTest.UserIdentifier);
        if (context.User == null)
        {
            throw new PayrollException($"Missing user {caseTest.UserIdentifier}");
        }

        // payroll
        context.Payroll = await GetPayrollAsync(context.Tenant.Id, caseTest.PayrollName);
        if (context.Payroll == null)
        {
            throw new PayrollException($"Missing payroll {caseTest.PayrollName}");
        }

        // setup dates
        context.EvaluationPeriod = await new TenantService(HttpClient).GetCalendarPeriodAsync(context.Tenant.Id,
            context.Payroll.CalendarCalculationMode, evaluationDate);

        // employee
        if (!string.IsNullOrWhiteSpace(caseTest.EmployeeIdentifier))
        {
            context.Employee = await GetEmployeeAsync(context.Tenant.Id, caseTest.EmployeeIdentifier);
            if (context.Employee == null)
            {
                throw new PayrollException($"Missing employee {caseTest.EmployeeIdentifier}");
            }
        }

        return context;
    }

    /// <summary>Loads the case test</summary>
    protected virtual async Task<CaseTest> LoadCaseTest()
    {
        // import file
        if (!File.Exists(FileName))
        {
            throw new PayrollException($"Missing import file {FileName}");
        }
        var json = await File.ReadAllTextAsync(FileName);
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new PayrollException($"Invalid import file {FileName}");
        }

        // import from JSON
        var caseTest = DefaultJsonSerializer.Deserialize<CaseTest>(json);
        if (string.IsNullOrWhiteSpace(caseTest.TenantIdentifier))
        {
            throw new PayrollException($"Missing tenant {caseTest.TenantIdentifier}");
        }
        if (string.IsNullOrWhiteSpace(caseTest.UserIdentifier))
        {
            throw new PayrollException($"Missing user {caseTest.UserIdentifier}");
        }
        if (string.IsNullOrWhiteSpace(caseTest.PayrollName))
        {
            throw new PayrollException($"Missing payroll {caseTest.PayrollName}");
        }

        return caseTest;
    }

    /// <summary>Get tenant</summary>
    /// <param name="tenantId">The tenant id</param>
    /// <param name="divisionName">The division name</param>
    private async Task<Division> GetDivisionAsync(int tenantId, string divisionName) =>
        await new DivisionService(HttpClient).GetAsync<Division>(new(tenantId), divisionName);

    /// <summary>Get tenant</summary>
    /// <param name="tenantId">The tenant id</param>
    /// <param name="payrollName">The payrun job name</param>
    private async Task<Payroll> GetPayrollAsync(int tenantId, string payrollName) =>
        await new PayrollService(HttpClient).GetAsync<Payroll>(new(tenantId), payrollName);

}