namespace QS3D.Core.Features
{
    public static class ParityBimEditingModelingCatalog
    {
        public static readonly FeatureId BimAuthoringId = new FeatureId("bim.authoring");
        public static readonly FeatureId DrawId = new FeatureId("draw");
        public static readonly FeatureId ToolEditingId = new FeatureId("tool.editing");
        public static readonly FeatureId ModelingId = new FeatureId("modeling");

        private const ParityWorkflowRequirement MutationRequirements =
            ParityWorkflowRequirement.ActiveDocument |
            ParityWorkflowRequirement.Project |
            ParityWorkflowRequirement.AtomicMutation |
            ParityWorkflowRequirement.Audit;

        public static ParityWorkflowRegistry CreateRegistry()
        {
            return new ParityWorkflowRegistry(new[]
            {
                Binding(BimAuthoringId, MutationRequirements |
                    ParityWorkflowRequirement.Zone | ParityWorkflowRequirement.Floor |
                    ParityWorkflowRequirement.Family),
                Binding(DrawId, MutationRequirements),
                Binding(ToolEditingId, MutationRequirements | ParityWorkflowRequirement.Selection),
                Binding(ModelingId, MutationRequirements | ParityWorkflowRequirement.Selection)
            });
        }

        private static ParityWorkflowBinding Binding(FeatureId id, ParityWorkflowRequirement requirements)
        {
            return new ParityWorkflowBinding(id, id.ToString(), ParityWorkflowKind.SemanticMutation,
                ParityWorkflowSurface.Ui, requirements);
        }
    }
}
