using System;
using System.Collections.Generic;

namespace PayrollEngine.Client.Test.Case;

/// <summary>Parse case custom test class</summary>
public class CaseCustomTestParser : CustomTestParser<CaseCustomTest, CaseTestContext>
{
    /// <summary>Case custom text parser constructor</summary>
    /// <param name="testName">The test name</param>
    /// <param name="sourceFiles">THe source  files</param>
    public CaseCustomTestParser(string testName, List<string> sourceFiles) :
        base(testName, sourceFiles)
    {
    }

    /// <inheritdoc />
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