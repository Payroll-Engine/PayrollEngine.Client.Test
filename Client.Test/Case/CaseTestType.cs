namespace PayrollEngine.Client.Test.Case;

/// <summary>Case test type</summary>
public enum CaseTestType
{
    /// <summary>Http result</summary>
    Http,

    /// <summary>Case available result</summary>
    CaseAvailable,
    /// <summary>Case available custom result</summary>
    CaseAvailableCustom,
    /// <summary>Case build result</summary>
    CaseBuild,
    /// <summary>Case build custom result</summary>
    CaseBuildCustom,
    /// <summary>Case validate result</summary>
    CaseValidate,
    /// <summary>Case validate custom result</summary>
    CaseValidateCustom
}