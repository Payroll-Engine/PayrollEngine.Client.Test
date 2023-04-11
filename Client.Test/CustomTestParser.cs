using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace PayrollEngine.Client.Test;

/// <summary>Parse custom test class</summary>
public class CustomTestParser<TFunction, TContext> : CustomTestParserBase
    where TFunction : CustomTestBase<TContext>
    where TContext : TestContextBase
{
    /// <summary>The test name</summary>
    private string TestName { get; }

    /// <summary>The custom test source file names</summary>
    private List<string> SourceFiles { get; }

    /// <summary>Custom text parser constructor</summary>
    /// <param name="testName">The test name</param>
    /// <param name="sourceFiles">THe source  files</param>
    public CustomTestParser(string testName, List<string> sourceFiles)
    {
        if (string.IsNullOrWhiteSpace(testName))
        {
            throw new ArgumentException(nameof(testName));
        }
        TestName = testName;
        SourceFiles = sourceFiles ?? throw new ArgumentNullException(nameof(sourceFiles));
    }

    private List<Type> GetTestTypes()
    {
        EnsureAssembly();
        var tests = new List<Type>();
        if (Assembly != null)
        {
            foreach (var type in Assembly.GetTypes())
            {
                if (type.IsAssignableTo(typeof(TFunction)))
                {
                    tests.Add(type);
                }
            }
        }

        return tests;
    }

    private Type GetTestType(string testName) =>
        GetTestTypes().FirstOrDefault(x => string.Equals(x.Name, testName));

    /// <summary>Get the test method</summary>
    /// <param name="testName">The test name</param>
    /// <param name="parameterTypes">The parameter types</param>
    /// <returns>THe test method info</returns>
    public MethodInfo GetTest(string testName, params Type[] parameterTypes)
    {
        if (string.IsNullOrWhiteSpace(testName))
        {
            throw new ArgumentException(nameof(testName));
        }

        // assembly
        EnsureAssembly();
        if (TestType == null)
        {
            return null;
        }

        // test methods
        var methods = TestType.GetMethods(BindingFlags.Public | BindingFlags.Instance).
            Where(x => string.Equals(x.Name, testName));

        // find method with matching parameters
        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            if (parameters.Length != parameterTypes.Length)
            {
                continue;
            }

            var matchingParameters = true;
            for (var i = 0; i < parameters.Length; i++)
            {
                if (!parameterTypes[i].IsAssignableFrom(parameters[i].ParameterType))
                {
                    matchingParameters = false;
                    break;
                }
            }
            if (matchingParameters)
            {
                return method;
            }
        }

        return null;
    }

    /// <summary>The custom tests assembly, maybe null</summary>
    private Assembly Assembly { get; set; }

    /// <summary>The custom test type, maybe null</summary>
    public Type TestType { get; private set; }

    private void EnsureAssembly()
    {
        if (Assembly != null)
        {
            return;
        }
        Assembly = BuildAssembly();
        TestType = GetTestType(TestName);
    }

    /// <summary>Get the test default references</summary>
    /// <returns>Type reference list</returns>
    protected virtual List<Type> GetDefaultReferences() => new()
    {
        // any object
        typeof(object),
        // used for object references
        typeof(AssemblyTargetedPatchBandAttribute),
        // used for dynamic objects
        typeof(DynamicAttribute),
        // used for linq
        typeof(Enumerable),
        // payroll engine core
        typeof(SystemSpecification),
        // payroll engine client
        typeof(PayrollHttpClient),
    };

    /// <summary>Get the test default assembly names</summary>
    /// <returns>Assembly name list</returns>
    protected virtual List<string> GetDefaultAssemblies() => new()
    {
        "System",
        "System.Runtime",
        // dynamic objects
        "Microsoft.CSharp",
        "netstandard",
        // JSON
        "System.Text.Json",
        // readonly collections
        "System.Collections",

        // reports
        "System.Data.Common",
        "System.ComponentModel.TypeConverter",
        "System.ComponentModel.Primitives",
        "System.ComponentModel",
        "System.Xml.ReaderWriter",
        "System.Private.Xml"
    };

    private Assembly BuildAssembly()
    {
        // parser options: supported c# language
        var languageVersion =
            (LanguageVersion)Enum.Parse(typeof(LanguageVersion), ScriptingSpecification.CSharpLanguageVersion);
        var options = CSharpParseOptions.Default.WithLanguageVersion(languageVersion);

        // parse code
        var syntaxTrees = new List<SyntaxTree>();

        // parse source codes
        foreach (var sourceFile in SourceFiles)
        {
            var source = File.ReadAllText(sourceFile);
            syntaxTrees.Add(SyntaxFactory.ParseSyntaxTree(source, options));
        }

        // assembly references
        var thisAssembly = GetType().Assembly;
        var allReferences = new Dictionary<Assembly, MetadataReference>
        {
            { thisAssembly, CreateAssemblyReference(thisAssembly) }
        };
        foreach (var defaultReference in GetDefaultReferences())
        {
            var assembly = defaultReference.Assembly;
            if (!allReferences.ContainsKey(assembly))
            {
                allReferences.Add(assembly, CreateAssemblyReference(assembly));
            }
        }
        foreach (var defaultAssembly in GetDefaultAssemblies())
        {
            var assembly = Assembly.Load(new AssemblyName(defaultAssembly));
            if (!allReferences.ContainsKey(assembly))
            {
                allReferences.Add(assembly, CreateAssemblyReference(assembly));
            }
        }

        // create bits
        using var peStream = new MemoryStream();
        var compilation = CSharpCompilation.Create(TestName,
                syntaxTrees,
                allReferences.Values,
                new(OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Release,
                    assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default))
            .Emit(peStream);

        // error handling
        if (!compilation.Success)
        {
            var failures = compilation.Diagnostics.Where(diagnostic =>
                    diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error)
                .Select(x => x.ToString())
                .ToList();
            throw new TestCompileException(failures);
        }

        return Assembly.Load(peStream.ToArray());
    }

    /// <summary>Creates an assembly reference</summary>
    /// <param name="referenceAssembly">The assembly to refer</param>
    private static MetadataReference CreateAssemblyReference(Assembly referenceAssembly) =>
        MetadataReference.CreateFromFile(referenceAssembly.Location);
}