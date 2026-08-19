using System;
using QS3D.Core.Features;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomInteractionProfileSmoke
    {
        public static void Run()
        {
            StableRoomIdentityAndDirectRecipe();
            DirectAddNeverRequestsModal();
            GenericInspectorsCarrySelectedRoomContext();
            SelectionRefreshRebindsWithoutStacking();
            RoomPropertySchemaAndDependencyContractsRemainExplicit();
        }

        private static void StableRoomIdentityAndDirectRecipe()
        {
            var room = RoomInteractionProfile.CreateRegistry().GetRequired(RoomInteractionProfile.RoomId);
            if (room.Id.ToString() != "model.room" || room.LabelKey != "Feature.Room")
                throw new Exception("Room must resolve through a stable FeatureId rather than visible navigation text.");
            if (room.InteractionProfile.PrimaryRecipeId != RoomInteractionProfile.DirectRecipeId ||
                room.InteractionProfile.Recipes.Count != 1 ||
                room.InteractionProfile.Recipes[0].InputMode != CreateInputMode.Direct)
                throw new Exception("Room primary Add must remain one direct recipe.");
        }

        private static void DirectAddNeverRequestsModal()
        {
            var machine = RoomInteractionProfile.StartCreate();
            if (machine.State != AddCreateState.Creating || machine.Directive.Kind != AddCreateDirectiveKind.Create)
                throw new Exception("Room direct Add must enter Creating without chooser/form/CAD-input detours.");
            if (RoomInteractionProfile.Descriptor.InteractionProfile.AllowsModal)
                throw new Exception("Room direct Add must not opt into a blocking modal.");
        }

        private static void GenericInspectorsCarrySelectedRoomContext()
        {
            var coordinator = new InteractionSurfaceCoordinator();
            var snapshot = RoomInteractionProfile.SelectAndBindInspectors(coordinator, "room-42");
            if (snapshot.FeatureId != RoomInteractionProfile.RoomId || snapshot.PersistentInspectorCount != 2)
                throw new Exception("Room must request the generic two-slot inspector surface contract.");
            if (snapshot.PrimaryInspector?.ContentKey != RoomInteractionProfile.PrimaryInspectorContentKey ||
                snapshot.PrimaryInspector.ContextKey != "room-42")
                throw new Exception("Room detail inspector must retain selected semantic Room identity.");
            if (snapshot.SecondaryInspector?.ContentKey != RoomInteractionProfile.SecondaryInspectorContentKey ||
                snapshot.SecondaryInspector.ContextKey != "room-42")
                throw new Exception("Room property inspector must retain selected semantic Room identity.");
        }

        private static void SelectionRefreshRebindsWithoutStacking()
        {
            var coordinator = new InteractionSurfaceCoordinator();
            RoomInteractionProfile.SelectAndBindInspectors(coordinator, "room-a");
            var snapshot = RoomInteractionProfile.SelectAndBindInspectors(coordinator, "room-b");
            if (snapshot.PersistentInspectorCount != 2 ||
                snapshot.PrimaryInspector?.ContextKey != "room-b" ||
                snapshot.SecondaryInspector?.ContextKey != "room-b")
                throw new Exception("Changing selected Room must replace inspector context instead of stacking duplicate surfaces.");
        }

        private static void RoomPropertySchemaAndDependencyContractsRemainExplicit()
        {
            var profile = RoomInteractionProfile.Descriptor.InteractionProfile;
            if (profile.PropertySchemaKey != RoomInteractionProfile.PropertySchemaKey)
                throw new Exception("Room parameter editing must use the shared Room property schema key.");
            if (profile.DependencyPolicyKey != "RoomFinish.HostSource" || profile.SemanticMappingKey != "ElementCategory.Room")
                throw new Exception("Room finish-host dependency and semantic category mapping must remain explicit.");
        }
    }
}
