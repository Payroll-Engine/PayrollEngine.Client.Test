
namespace PayrollEngine.Client.Test;

/// <summary>A collection of helper classes to test various conditions within unit tests.
/// If the condition being tested is not met, an exception is thrown</summary>
public static class Assert
{
    /// <summary>Tests whether the specified values are equal and throws an exception if the two values are not equal.
    /// Different numeric types are treated as unequal even if the logical values are equal. 42L is not equal to 42</summary>
    /// <param name="expected">The expected value</param>
    /// <param name="actual">The actual value</param>
    /// <param name="message">The assert failed message</param>
    public static void AreEqual<T>(T expected, T actual, string message = null)
    {
        if (!Equals(expected, actual))
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                message = $"AreEqual - Expected: {expected}, Actual: {actual}";
            }
            Fail(message);
        }
    }

    /// <summary>Tests whether the specified values are not equal and throws an exception if the two values are not equal.
    /// Different numeric types are treated as unequal even if the logical values are equal. 42L is not equal to 42</summary>
    /// <param name="expected">The expected value</param>
    /// <param name="actual">The actual value</param>
    /// <param name="message">The assert failed message</param>
    public static void AreNotEqual<T>(T expected, T actual, string message = null)
    {
        if (Equals(expected, actual))
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                message = $"AreNotEqual - Expected: {expected}, Actual: {actual}";
            }
            Fail(message);
        }
    }

    /// <summary>Tests whether the specified condition is false and throws an exception if the condition is true</summary>
    /// <param name="condition">The condition the test expects to be false</param>
    /// <param name="message">The message to include in the exception when condition is true. The message is shown in test results</param>
    public static void IsFalse(bool condition, string message = null)
    {
        if (condition)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                message = "IsFalse";
            }
            Fail(message);
        }
    }

    /// <summary>Tests whether the specified condition is true and throws an exception if the condition is false</summary>
    /// <param name="condition">The condition the test expects to be false</param>
    /// <param name="message">The message to include in the exception when condition is true. The message is shown in test results</param>
    public static void IsTrue(bool condition, string message = null)
    {
        if (!condition)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                message = "IsTrue";
            }
            Fail(message);
        }
    }

    /// <summary>Tests whether the specified object is null and throws an exception if it is not</summary>
    /// <param name="value">The object the test expects to be null</param>
    /// <param name="message">The message to include in the exception when value is not null</param>
    public static void IsNull(object value, string message = null)
    {
        if (value != null)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                message = "IsNull";
            }
            Fail(message);
        }
    }

    /// <summary>Tests whether the specified object is non-null and throws an exception if it is null</summary>
    /// <param name="value">The object the test expects not to be null</param>
    /// <param name="message">The message to include in the exception when value is null</param>
    public static void IsNotNull(object value, string message = null)
    {
        if (value == null)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                message = "IsNotNull";
            }
            Fail(message);
        }
    }

    /// <summary>Throws an AssertFailedException</summary>
    /// <param name="message">The exception message</param>
    public static void Fail(string message = null)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new AssertFailedException();
        }
        throw new AssertFailedException(message);
    }
}