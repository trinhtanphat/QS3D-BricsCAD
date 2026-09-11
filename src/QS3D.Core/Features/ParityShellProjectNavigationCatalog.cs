namespace QS3D.Core.Features
{
    public static class ParityShellProjectNavigationCatalog
    {
        public static readonly FeatureId ShellStartId = new FeatureId("shell.start");
        public static readonly FeatureId ShellWorkspaceId = new FeatureId("shell.workspace");
        public static readonly FeatureId ShellRibbonId = new FeatureId("shell.ribbon");
        public static readonly FeatureId ProjectSetupId = new FeatureId("project.setup");
        public static readonly FeatureId ProjectZoneId = new FeatureId("project.zone");
        public static readonly FeatureId ProjectFloorId = new FeatureId("project.floor");
        public static readonly FeatureId ProjectFamilyId = new FeatureId("project.family");

        private const ParityWorkflowRequirement SemanticMutationRequirements =
            ParityWorkflowRequirement.ActiveDocument |
            ParityWorkflowRequirement.Project |
            ParityWorkflowRequirement.AtomicMutation |
            ParityWorkflowRequirement.Audit;

        public static ParityWorkflowRegistry CreateRegistry()
        {
            return new ParityWorkflowRegistry(new[]
            {
                Binding(ShellStartId, ParityWorkflowKind.Infrastructure,
                    ParityWorkflowSurface.Ui | ParityWorkflowSurface.Launcher,
                    ParityWorkflowRequirement.None),
                Binding(ShellWorkspaceId, ParityWorkflowKind.ReadOnly,
                    ParityWorkflowSurface.Ui, ParityWorkflowRequirement.ActiveDocument),
                Binding(ShellRibbonId, ParityWorkflowKind.ReadOnly,
                    ParityWorkflowSurface.Ui, ParityWorkflowRequirement.None),
                Binding(ProjectSetupId, ParityWorkflowKind.Infrastructure,
                    ParityWorkflowSurface.Ui, ParityWorkflowRequirement.ActiveDocument),
                Binding(ProjectZoneId, ParityWorkflowKind.SemanticMutation,
                    ParityWorkflowSurface.Ui, SemanticMutationRequirements),
                Binding(ProjectFloorId, ParityWorkflowKind.SemanticMutation,
                    ParityWorkflowSurface.Ui, SemanticMutationRequirements),
                Binding(ProjectFamilyId, ParityWorkflowKind.SemanticMutation,
                    ParityWorkflowSurface.Ui, SemanticMutationRequirements)
            });
        }

        private static ParityWorkflowBinding Binding(
            FeatureId id,
            ParityWorkflowKind kind,
            ParityWorkflowSurface surfaces,
            ParityWorkflowRequirement requirements)
        {
            return new ParityWorkflowBinding(id, id.ToString(), kind, surfaces, requirements);
        }
    }
}