namespace QS3D.Core.Features
{
    public static class ParityRebarCatalog
    {
        public static readonly FeatureId RebarId = new FeatureId("rebar");

        private const ParityWorkflowRequirement MutationRequirements =
            ParityWorkflowRequirement.ActiveDocument |
            ParityWorkflowRequirement.Project |
            ParityWorkflowRequirement.Selection |
            ParityWorkflowRequirement.AtomicMutation |
            ParityWorkflowRequirement.Audit;

        public static ParityWorkflowRegistry CreateRegistry()
        {
            return new ParityWorkflowRegistry(new[]
            {
                new ParityWorkflowBinding(
                    RebarId,
                    RebarId.ToString(),
                    ParityWorkflowKind.SemanticMutation,
                    ParityWorkflowSurface.Ui,
                    MutationRequirements)
            });
        }
    }
}
