using System;
using System.Collections.Generic;
using QS3D.Core.Features;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomFinishInteractionProfilesSmoke
    {
        public static void Run()
        {
            RegistryUsesGenericInspectorAndSchemaContracts();
            FloorFinishSupportsHostDerivedAndPreCreateFormRecipes();
            WaterproofingOrdersFormAndCadInput();
            HostSelectionAndInvalidationAreActionable();
            CancellationLeavesNoCreateHandoff();
            InvalidFormInputIsRejectedBeforeMutation();
            CreateCompletionProducesRegenerationHandoff();
        }

        private static void RegistryUsesGenericInspectorAndSchemaContracts()
        {
            var registry = RoomFinishInteractionProfiles.CreateRegistry();
            var floor = registry.GetRequired(RoomFinishInteractionProfiles.FloorFinishId);
            var waterproofing = registry.GetRequired(RoomFinishInteractionProfiles.WaterproofingId);
            var skirting = registry.GetRequired(RoomFinishInteractionProfiles.SkirtingId);

            if (floor.InteractionProfile.PersistentSurfaces.Count != 2 ||
                floor.InteractionProfile.PersistentSurfaces[0] != InteractionSurface.PrimaryInspector ||
                floor.InteractionProfile.PersistentSurfaces[1] != InteractionSurface.SecondaryInspector)
                throw new Exception("Floor Finish must request generic 0/1/2-slot inspector surfaces, not a bespoke window.");
            if (skirting.InteractionProfile.PersistentSurfaces.Count != 1 ||
                skirting.InteractionProfile.PersistentSurfaces[0] != InteractionSurface.PrimaryInspector)
                throw new Exception("Skirting must use the generic inspector host contract.");
            if (floor.InteractionProfile.PropertySchemaKey != "ProjectFamilyQuickSchema.FloorFinish" ||
                waterproofing.InteractionProfile.PropertySchemaKey != "ProjectFamilyQuickSchema.Waterproofing" ||
                skirting.InteractionProfile.PropertySchemaKey != "ProjectFamilyQuickSchema.Skirting")
                throw new Exception("Room-finish profiles must reuse the authoritative ProjectFamilyQuickSchema renderer keys.");
        }

        private static void FloorFinishSupportsHostDerivedAndPreCreateFormRecipes()
        {
            var host = new RoomFinishHostContext("room-1");
            var direct = RoomFinishInteractionProfiles.Start(RoomFinishInteractionProfiles.FloorFinishId, host);
            if (direct.Begin().Kind != AddCreateDirectiveKind.Create)
                throw new Exception("Floor Finish primary recipe must support direct-ish creation from the selected Room host.");
            if (direct.GetCreateRequest().Recipe.Id != "floor-finish.from-room")
                throw new Exception("Floor Finish primary host-derived recipe identity is unstable.");

            var form = RoomFinishInteractionProfiles.Start(RoomFinishInteractionProfiles.FloorFinishId, host);
            var directive = form.Begin("floor-finish.material-thickness");
            if (directive.Kind != AddCreateDirectiveKind.ShowForm || directive.SchemaKey != "ProjectFamilyQuickSchema.FloorFinish")
                throw new Exception("Floor Finish optional material/thickness recipe must use the shared schema surface.");
            directive = form.SubmitForm(new[]
            {
                new KeyValuePair<string, string>("material", "Tile"),
                new KeyValuePair<string, string>("thickness", "12")
            });
            if (directive.Kind != AddCreateDirectiveKind.Create)
                throw new Exception("Validated Floor Finish form must continue through the generic create state machine.");
        }

        private static void WaterproofingOrdersFormAndCadInput()
        {
            var session = RoomFinishInteractionProfiles.Start(RoomFinishInteractionProfiles.WaterproofingId, new RoomFinishHostContext("room-2"));
            if (session.Begin().Kind != AddCreateDirectiveKind.ShowForm)
                throw new Exception("Waterproofing must request scope/material/thickness before CAD input in its primary recipe.");
            var afterForm = session.SubmitForm(new[]
            {
                new KeyValuePair<string, string>("scope", "wet-area"),
                new KeyValuePair<string, string>("material", "membrane"),
                new KeyValuePair<string, string>("thickness", "2")
            });
            if (afterForm.Kind != AddCreateDirectiveKind.RequestCadInput || session.State != AddCreateState.WaitingForCadInput)
                throw new Exception("Waterproofing form recipe must hand off to CAD face input only after validation.");
            if (session.SubmitCadInput("face-42").Kind != AddCreateDirectiveKind.Create)
                throw new Exception("Waterproofing must reach create handoff after CAD input arrives.");
        }

        private static void HostSelectionAndInvalidationAreActionable()
        {
            ExpectInvalid(
                () => RoomFinishInteractionProfiles.Start(RoomFinishInteractionProfiles.SkirtingId, new RoomFinishHostContext("room-dirty", isDirty: true)).Begin(),
                "pending changes");
            ExpectInvalid(
                () => RoomFinishInteractionProfiles.Start(RoomFinishInteractionProfiles.SkirtingId, new RoomFinishHostContext("room-invalid", isValid: false)).Begin(),
                "no longer valid");
        }

        private static void CancellationLeavesNoCreateHandoff()
        {
            var session = RoomFinishInteractionProfiles.Start(RoomFinishInteractionProfiles.WaterproofingId, new RoomFinishHostContext("room-3"));
            session.Begin();
            session.Cancel();
            if (session.State != AddCreateState.Cancelled || session.Directive.Kind != AddCreateDirectiveKind.None)
                throw new Exception("Cancelling a room-finish form must clear transient generic orchestration state.");
        }

        private static void InvalidFormInputIsRejectedBeforeMutation()
        {
            var session = RoomFinishInteractionProfiles.Start(RoomFinishInteractionProfiles.SkirtingId, new RoomFinishHostContext("room-4"));
            session.Begin("skirting.profile-height-material");
            ExpectInvalid(
                () => session.SubmitForm(new[]
                {
                    new KeyValuePair<string, string>("profile", "40x10"),
                    new KeyValuePair<string, string>("height", "-80"),
                    new KeyValuePair<string, string>("material", "wood")
                }),
                "validation failed");
            if (session.State != AddCreateState.Preparing)
                throw new Exception("Invalid room-finish inputs must be rejected before mutation and leave the session usable.");
        }

        private static void CreateCompletionProducesRegenerationHandoff()
        {
            var session = RoomFinishInteractionProfiles.Start(RoomFinishInteractionProfiles.SkirtingId, new RoomFinishHostContext("room-5"));
            session.Begin();
            var handoff = session.CompleteCreate();
            if (!handoff.RequiresRegeneration || handoff.RoomId != "room-5" || handoff.FeatureId != RoomFinishInteractionProfiles.SkirtingId)
                throw new Exception("Successful room-finish creation must preserve host identity for regeneration handoff.");
            if (session.State != AddCreateState.Created)
                throw new Exception("Room-finish create session must reach Created after regeneration handoff is produced.");
        }

        private static void ExpectInvalid(Action action, string expectedMessagePart)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(expectedMessagePart, StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
                throw new Exception("Unexpected validation message: " + ex.Message);
            }

            throw new Exception("Expected InvalidOperationException containing: " + expectedMessagePart);
        }
    }
}
