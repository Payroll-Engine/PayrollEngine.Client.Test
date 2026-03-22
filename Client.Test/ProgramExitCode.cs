namespace PayrollEngine.Client.Test;

/// <summary>Program exit codes for commands</summary>
public enum ProgramExitCode
{
    /// <summary>No error</summary>
    Ok = 0,

    /// <summary>Generic application error</summary>
    GenericError = -1,

    /// <summary>Command file error</summary>
    CommandFile = -2,

    /// <summary>Backend connection error</summary>
    ConnectionError = 2,

    /// <summary>Http error</summary>
    HttpError = 3,

    /// <summary>Failed test</summary>
    FailedTest = 4,

    /// <summary>Invalid options</summary>
    InvalidOptions = 5,

    /// <summary>Invalid input (e.g. missing or invalid source files)</summary>
    InvalidInput = 6
}
