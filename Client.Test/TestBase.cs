using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text.Json;

namespace PayrollEngine.Client.Test;

/// <summary>Script base test</summary>
/// <typeparam name="TIn">The test input type</typeparam>
/// <typeparam name="TOut">The test output type</typeparam>
public abstract class TestBase<TIn, TOut>
{
    /// <summary>The test name</summary>
    [Required]
    public string TestName { get; set; }

    /// <summary>The test description</summary>
    public string TestDescription { get; set; }

    /// <summary>The test category</summary>
    public string TestCategory { get; set; }

    /// <summary>The test input</summary>
    public TIn Input { get; set; }

    /// <summary>The test input file name</summary>
    public string InputFile { get; set; }

    /// <summary>The test output</summary>
    public TOut Output { get; set; }

    /// <summary>The test output file name</summary>
    public string OutputFile { get; set; }

    /// <summary>Init the test</summary>
    public virtual void InitTest()
    {
        // input file
        if (typeof(TIn).IsClass && Equals(Input, default(TIn)) &&
            !string.IsNullOrWhiteSpace(InputFile) &&
            File.Exists(InputFile))
        {
            Input = JsonSerializer.Deserialize<TIn>(File.ReadAllText(InputFile));
        }

        // output file
        if (typeof(TOut).IsClass && Equals(Output, default(TOut)) &&
            !string.IsNullOrWhiteSpace(OutputFile) &&
            File.Exists(OutputFile))
        {
            Output = JsonSerializer.Deserialize<TOut>(File.ReadAllText(OutputFile));
        }
    }

}