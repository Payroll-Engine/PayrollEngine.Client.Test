using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using PayrollEngine.Client.Command;

namespace PayrollEngine.Client.Test;

/// <summary>Test command base</summary>
public abstract class TestCommandBase : CommandBase
{
    /// <summary>Get test file names by mask</summary>
    /// <param name="fileMask">File name or glob mask</param>
    protected static List<string> GetTestFileNames(string fileMask)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileMask);

        var files = new List<string>();
        if (File.Exists(fileMask))
        {
            files.Add(fileMask);
        }
        else
        {
            files.AddRange(new DirectoryInfo(Directory.GetCurrentDirectory())
                .GetFiles(fileMask).Select(x => x.Name));
        }
        return files;
    }

    /// <summary>Display test results using the standard mode</summary>
    /// <param name="logger">Logger</param>
    /// <param name="console">Console</param>
    /// <param name="displayMode">Display mode</param>
    /// <param name="results">Test results</param>
    protected static void DisplayTestResults<TResult>(ILogger logger, ICommandConsole console,
        TestDisplayMode displayMode, ICollection<TResult> results)
        where TResult : ScriptTestResultBase
    {
        console.DisplayTextLine("Test results...");
        foreach (var testResult in results)
        {
            if (!testResult.Failed && displayMode != TestDisplayMode.ShowAll)
            {
                continue;
            }

            if (testResult.Failed)
            {
                var message = $"-> Test {testResult.TestName} failed";
                if (!string.IsNullOrWhiteSpace(testResult.Message))
                {
                    message += $" ({testResult.Message})";
                }
                if (testResult.ErrorCode != 0)
                {
                    message += $" [{testResult.ErrorCode}]";
                }
                logger.Error(message);
            }
            else
            {
                var message = $"-> Test {testResult.TestName} succeeded";
                if (!string.IsNullOrWhiteSpace(testResult.Message))
                {
                    message += $" ({testResult.Message})";
                }
                logger.Debug(message);
            }
        }

        var totalCount = results.Count;
        var failedCount = results.Count(x => x.Failed);
        var successCount = totalCount - failedCount;
        if (successCount > 0)
        {
            var successMessage = $"Passed tests: {successCount}";
            if (failedCount > 0)
            {
                console.DisplayInfoLine(successMessage);
            }
            else
            {
                console.DisplaySuccessLine(successMessage);
            }
        }
        if (failedCount > 0)
        {
            console.DisplayErrorLine($"Failed tests: {failedCount}");
        }
    }
}
