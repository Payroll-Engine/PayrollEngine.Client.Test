using System;

namespace PayrollEngine.Client.Test;

/// <summary>Result of test</summary>
public abstract class TestResultBase<T>
    where T : IAttributeObject
{
    /// <summary>The expected result</summary>
    public T ExpectedResult { get; }

    /// <summary>The actual result</summary>
    public T ActualResult { get; }

    /// <summary>Initializes a new instance of the <see cref="TestResultBase{T}"/> class</summary>
    /// <param name="expectedResult">The expected result</param>
    /// <param name="actualResult">The actual result</param>
    protected TestResultBase(T expectedResult, T actualResult = default)
    {
        ExpectedResult = expectedResult ?? throw new ArgumentNullException(nameof(expectedResult));
        ActualResult = actualResult;
    }

    /// <summary>Test failed test</summary>
    public bool Failed() => !ValidValue() || !ValidCulture();

    /// <summary>Get for valid culture</summary>
    public abstract bool ValidCulture();

    /// <summary>Get for valid value</summary>
    public abstract bool ValidValue();

    /// <summary>Test attribute values</summary>
    public bool ValidAttributes() => FirstInvalidAttribute() == null;

    /// <summary>Test attribute values</summary>
    public virtual Tuple<string, object, object> FirstInvalidAttribute() =>
        ExpectedResult?.Attributes?.FirstInvalidAttribute(ActualResult?.Attributes);
}