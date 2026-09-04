namespace QS3D.Core.Agent.Harness
{
    public enum HarnessPermission
    {
        ReadRepository,
        RunFocusedTests,
        EditTaskBranch,
        CommitPushCanonicalBranch,
        CreateUpdateCarrier,
        MergeSameTaskPr,
        CadInspect,
        CadMutate,
        SaveActiveDrawing,
        SecretExport,
        ForcePushProtected,
        BypassCi,
        BypassReservation,
        WriteOutsideWorkspace,
        UntypedDestructiveExternal
    }

    public enum PermissionDecision
    {
        Auto,
        Confirm,
        Deny
    }
}
