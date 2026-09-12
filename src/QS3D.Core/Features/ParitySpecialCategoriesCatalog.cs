namespace QS3D.Core.Features
{
    public static class ParitySpecialCategoriesCatalog
    {
        public static readonly FeatureId RoomId = new FeatureId("room");
        public static readonly FeatureId RoomFinishId = new FeatureId("room.finish");
        public static readonly FeatureId EarthworkId = new FeatureId("earthwork");
        public static readonly FeatureId StairId = new FeatureId("stair");
        public static readonly FeatureId RailingId = new FeatureId("railing");
        public static readonly FeatureId CurtainId = new FeatureId("curtain");
        public static readonly FeatureId DoorOpeningId = new FeatureId("door-opening");
        public static readonly FeatureId GridId = new FeatureId("grid");

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
                Binding(RoomId),
                Binding(RoomFinishId),
                Binding(EarthworkId),
                Binding(StairId),
                Binding(RailingId),
                Binding(CurtainId),
                Binding(DoorOpeningId),
                Binding(GridId)
            });
        }

        private static ParityWorkflowBinding Binding(FeatureId id)
        {
            return new ParityWorkflowBinding(
                id,
                id.ToString(),
                ParityWorkflowKind.SemanticMutation,
                ParityWorkflowSurface.Ui,
                MutationRequirements);
        }
    }
}
