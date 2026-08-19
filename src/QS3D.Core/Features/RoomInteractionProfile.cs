using System;

namespace QS3D.Core.Features
{
    public static class RoomInteractionProfile
    {
        public static readonly FeatureId RoomId = new FeatureId("model.room");
        public const string DirectRecipeId = "room.direct";
        public const string PrimaryInspectorContentKey = "Room.Detail";
        public const string SecondaryInspectorContentKey = "Room.Properties";
        public const string PropertySchemaKey = "ProjectFamilyQuickSchema.Room";

        private static readonly FeatureDescriptor DescriptorValue = CreateDescriptor();

        public static FeatureDescriptor Descriptor => DescriptorValue;

        public static FeatureRegistry CreateRegistry() => new FeatureRegistry(new[] { DescriptorValue });

        public static AddCreateStateMachine StartCreate()
        {
            var machine = new AddCreateStateMachine(DescriptorValue);
            var directive = machine.Begin();
            if (directive.Kind != AddCreateDirectiveKind.Create)
                throw new InvalidOperationException("Room primary recipe must remain direct and must not require a modal surface.");
            return machine;
        }

        public static InteractionSurfaceSnapshot SelectAndBindInspectors(
            InteractionSurfaceCoordinator coordinator,
            string? roomContextKey)
        {
            if (coordinator == null) throw new ArgumentNullException(nameof(coordinator));
            coordinator.SelectFeature(DescriptorValue);

            coordinator.Open(new InteractionSurfaceBinding(
                RoomId,
                InteractionSurface.PrimaryInspector,
                PrimaryInspectorContentKey,
                NormalizeContext(roomContextKey)));
            coordinator.Open(new InteractionSurfaceBinding(
                RoomId,
                InteractionSurface.SecondaryInspector,
                SecondaryInspectorContentKey,
                NormalizeContext(roomContextKey)));

            return coordinator.Snapshot;
        }

        private static FeatureDescriptor CreateDescriptor()
        {
            var profile = new InteractionProfile(
                FeatureOnSelectBehavior.SelectAndRefresh,
                new[] { new CreateRecipeDescriptor(DirectRecipeId, CreateInputMode.Direct) },
                DirectRecipeId,
                new[] { InteractionSurface.PrimaryInspector, InteractionSurface.SecondaryInspector },
                FeatureCapability.Create |
                FeatureCapability.EditParameters |
                FeatureCapability.Quantity |
                FeatureCapability.Locate |
                FeatureCapability.Delete,
                allowsModal: false,
                propertySchemaKey: PropertySchemaKey,
                dependencyPolicyKey: "RoomFinish.HostSource",
                semanticMappingKey: "ElementCategory.Room");

            return new FeatureDescriptor(
                RoomId,
                "model.room",
                10,
                "Feature.Room",
                profile,
                "Icon.Room");
        }

        private static string? NormalizeContext(string? roomContextKey)
        {
            return string.IsNullOrWhiteSpace(roomContextKey) ? null : roomContextKey.Trim();
        }
    }
}
