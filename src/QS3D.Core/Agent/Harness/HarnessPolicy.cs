namespace QS3D.Core.Agent.Harness
{
    public sealed class HarnessPolicy
    {
        public PermissionDecision Resolve(HarnessPermission permission)
        {
            switch (permission)
            {
                case HarnessPermission.ReadRepository:
                case HarnessPermission.RunFocusedTests:
                case HarnessPermission.EditTaskBranch:
                case HarnessPermission.CommitPushCanonicalBranch:
                case HarnessPermission.CreateUpdateCarrier:
                case HarnessPermission.CadInspect:
                    return PermissionDecision.Auto;

                case HarnessPermission.MergeSameTaskPr:
                case HarnessPermission.CadMutate:
                case HarnessPermission.SaveActiveDrawing:
                    return PermissionDecision.Confirm;

                case HarnessPermission.SecretExport:
                case HarnessPermission.ForcePushProtected:
                case HarnessPermission.BypassCi:
                case HarnessPermission.BypassReservation:
                case HarnessPermission.WriteOutsideWorkspace:
                case HarnessPermission.UntypedDestructiveExternal:
                default:
                    return PermissionDecision.Deny;
            }
        }
    }
}
