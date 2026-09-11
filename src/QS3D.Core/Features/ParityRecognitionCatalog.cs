namespace QS3D.Core.Features
{
    public static class ParityRecognitionCatalog
    {
        public static readonly FeatureId RecognitionId = new FeatureId("recognition");

        private const ParityWorkflowRequirement MutationRequirements =
            ParityWorkflowRequirement.ActiveDocument |
            ParityWorkflowRequirement.Project |
            ParityWorkflowRequirement.AtomicMutation |
            ParityWorkflowRequirement.Audit;

        public static ParityWorkflowRegistry CreateRegistry()
        {
            return new ParityWorkflowRegistry(new[]
            {
                new ParityWorkflowBinding(
                    RecognitionId,
                    RecognitionId.ToString(),
                    ParityWorkflowKind.SemanticMutation,
                    ParityWorkflowSurface.Ui,
                    MutationRequirements)
            });
        }
    }
}