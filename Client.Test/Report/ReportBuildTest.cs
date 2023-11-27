using PayrollEngine.Client.Model;
using System.Collections.Generic;

namespace PayrollEngine.Client.Test.Report;

/// <summary>Report build test
/// Test input: report request
/// Test output: list of report parameter
/// </summary>
public class ReportBuildTest : ReportScriptTest<ReportRequest, List<ReportParameter>>;