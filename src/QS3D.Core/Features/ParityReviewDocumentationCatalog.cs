namespace QS3D.Core.Features
{
    public static class ParityReviewDocumentationCatalog
    {
        public static readonly FeatureId ViewId = new FeatureId("view");
        public static readonly FeatureId QuantityId = new FeatureId("quantity");
        public static readonly FeatureId RevisionId = new FeatureId("revision");

        public static ParityWorkflowRegistry CreateRegistry()
        {
            return new ParityWorkflowRegistry(new[]
            {
                Binding(ViewId, ParityWorkflowRequirement.ActiveDocument),
                Binding(QuantityId, ParityWorkflowRequirement.ActiveDocument),
                Binding(RevisionId, ParityWorkflowRequirement.ActiveDocument | ParityWorkflowRequirement.Project)
            });
        }

        private static ParityWorkflowBinding Binding(
            FeatureId id,
            ParityWorkflowRequirement requirements)
        {
            return new ParityWorkflowBinding(
                id,
                id.ToString(),
                ParityWorkflowKind.ReadOnly,
                ParityWorkflowSurface.Ui,
                requirements);
        }
    }
}
