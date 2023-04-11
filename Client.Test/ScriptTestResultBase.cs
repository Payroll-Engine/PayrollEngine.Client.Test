
namespace PayrollEngine.Client.Test;

/// <summary>Report script test result</summary>
public abstract class ScriptTestResultBase
{
    /// <summary>The test name</summary>
    public string TestName { get; set; }

    /// <summary>The failed state</summary>
    public bool Failed { get; set; }

    /// <summary>The result message</summary>
    public string Message { get; set; }

    /// <summary>The expected state</summary>
    public string Expected { get; set; }

    /// <summary>The received state</summary>
    public string Received { get; set; }

    /// <summary>The http status code</summary>
    public int? HttpStatusCode { get; set; }

    /// <summary>The error code</summary>
    public int ErrorCode { get; set; }
}