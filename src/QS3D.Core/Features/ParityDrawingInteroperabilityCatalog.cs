namespace QS3D.Core.Features
{
    public static class ParityDrawingInteroperabilityCatalog
    {
        public static readonly FeatureId DrawingManagerId = new FeatureId("drawing-manager");
        public static readonly FeatureId IfcId = new FeatureId("ifc");

        public static ParityWorkflowRegistry CreateRegistry()
        {
            return new ParityWorkflowRegistry(new[]
            {
                Binding(DrawingManagerId),
                Binding(IfcId)
            });
        }

        private static ParityWorkflowBinding Binding(FeatureId id)
        {
            return new ParityWorkflowBinding(
                id,
                id.ToString(),
                ParityWorkflowKind.Infrastructure,
                ParityWorkflowSurface.Ui,
                ParityWorkflowRequirement.ActiveDocument);
        }
    }
}
