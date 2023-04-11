using PayrollEngine.Client.Model;

namespace PayrollEngine.Client.Test;

/// <summary>The test context base</summary>
public class TestContextBase
{
    /// <summary>The test name</summary>
    public string TestName { get; set; }

    /// <summary>The tenant</summary>
    public Tenant Tenant { get; set; }

    /// <summary>The user</summary>
    public User User { get; set; }
}