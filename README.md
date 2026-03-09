# Payroll Engine Client Test

> Part of the [Payroll Engine](https://github.com/Payroll-Engine/PayrollEngine) open-source payroll automation framework.
> Full documentation at [payrollengine.org](https://payrollengine.org).

Client library for testing Payroll Engine regulations and payrun results. Provides three test categories — **Case**, **Payrun**, and **Report** — each with dedicated runners, JSON schemas, and support for custom C# test extensions.

## Test Categories

### Case Test

Tests the case lifecycle in three phases:

| Phase | Runner | Description |
|:--|:--|:--|
| Available | `CaseAvailableTestRunner` | Verify case availability under given conditions |
| Build | `CaseBuildTestRunner` | Verify computed field values after case build |
| Validate | `CaseValidateTestRunner` | Verify validation rules on case input |

Test definition class: `CaseTest` — specifies tenant, user, payroll, optional employee, evaluation date, optional init cases, and lists of `AvailableTests`, `BuildTests`, and `ValidateTests`.

JSON schema: `PayrollEngine.CaseTest.schema.json`

### Payrun Test

Tests payrun results for wage types, collectors, and custom results. Three runners cover different test scenarios:

| Runner | Description |
|:--|:--|
| `PayrunTestRunner` | Full cycle: imports exchange data, executes payrun, validates results, cleans up |
| `PayrunEmployeeTestRunner` | Runs on a copy of an existing employee — tenant data stays unchanged |
| `PayrunEmployeePreviewTestRunner` | Uses the preview endpoint — no data is persisted, no cleanup required |

All runners extend `PayrunTestRunnerBase` and accept a `PayrunTestSettings` configuration object.

### Report Test

Tests report parameter handling and execution output in two phases:

| Phase | Runner | Description |
|:--|:--|:--|
| Build | `ReportBuildTestRunner` | Verify report parameters are built correctly |
| Execute | `ReportExecuteTestRunner` | Verify report execution output |

Test definition class: `ReportTest` — specifies tenant, user, regulation, and lists of `BuildTests` and `ExecuteTests`.

JSON schema: `PayrollEngine.ReportTest.schema.json`

## Test Settings

`PayrunTestSettings` controls payrun test behavior:

| Setting | Type | Default | Description |
|:--|:--|:--|:--|
| `TestPrecision` | `TestPrecision` | `TestPrecision2` | Decimal places compared in result assertions |
| `ResultMode` | `TestResultMode` | `CleanTest` | Cleanup behavior after test run |
| `Owner` | `string` | — | Optional owner tag applied to all payrun jobs |
| `ResultRetryDelay` | `int` | `1000` ms | Polling interval for async payrun job completion |
| `ResultRetryCount` | `int` | `120` | Maximum number of polling attempts |

### TestResultMode

| Value | Description |
|:--|:--|
| `CleanTest` | Delete all test data after run (default) |
| `KeepFailedTest` | Keep data for failed tests to allow manual inspection |
| `KeepTest` | Keep all test data (manual cleanup required) |

### TestPrecision

Numeric comparison precision from `TestPrecisionOff` (no rounding) to `TestPrecision6` (6 decimal places). Default is `TestPrecision2`.

## Custom Tests

All three test categories support custom C# test extensions. Custom tests are compiled at runtime from source files and invoked alongside the standard assertions.

Implement a custom test class with a constructor accepting `PayrollHttpClient` and the test context (`CaseTestContext` or `ReportTestContext`), and use `Assert` for assertions:

```csharp
public class MyCustomTests(PayrollHttpClient httpClient, CaseTestContext context)
    : CaseCustomTest(httpClient, context)
{
    public void MyBuildTest(CaseBuildTest test)
    {
        Assert.AreEqual("ExpectedValue", test.BuildCase.Fields["MyField"].Value);
    }
}
```

Register the source file in the test definition:

```json
{
  "customTestFiles": ["MyCustomTests.cs"],
  "buildTests": [
    { "testName": "MyBuildTest", "caseName": "MyCaseName" }
  ]
}
```

## JSON Schemas

The library generates two JSON schemas during build (requires `PayrollEngineSchemaDir` to be set):

| Schema | Root type |
|:--|:--|
| `PayrollEngine.CaseTest.schema.json` | `CaseTest` |
| `PayrollEngine.ReportTest.schema.json` | `ReportTest` |

Schemas are included in the NuGet package and can be referenced in test files for IDE validation:

```json
{
  "$schema": "../../Schemas/PayrollEngine.CaseTest.schema.json",
  "testName": "MySalaryCaseTest",
  ...
}
```

## Build

Environment variables used during build:

| Variable | Description |
|:--|:--|
| `PayrollEngineSchemaDir` | Output directory for generated JSON schemas (optional) |
| `PayrollEnginePackageDir` | Output directory for the NuGet package (optional) |

## NuGet Package

Available on [NuGet.org](https://www.nuget.org/profiles/PayrollEngine):

```sh
dotnet add package PayrollEngine.Client.Test
```

## See Also

- [Testing](https://payrollengine.org/roles/regulator/testing) — test-driven payroll development
- [Payroll Console](https://github.com/Payroll-Engine/PayrollEngine.PayrollConsole) — CLI runner for `.et.json` and `.pt.json` test files
- [Client Services](https://github.com/Payroll-Engine/PayrollEngine.Client.Services) — SDK used by the test runners
