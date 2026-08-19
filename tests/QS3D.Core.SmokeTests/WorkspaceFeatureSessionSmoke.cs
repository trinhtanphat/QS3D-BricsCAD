using System;
using QS3D.Core.Features;

namespace QS3D.Core.SmokeTests
{
    internal static class WorkspaceFeatureSessionSmoke
    {
        public static void Run()
        {
            MenuSwitchCancelsTransientCreate();
            SemanticSelectionDoesNotChangeFeatureDuringModal();
            HostDeletionBecomesActionable();
            RefreshRehydratesWithoutZombieCreate();
            ProjectSwitchClearsProjectBoundContext();
            DuplicateCreateSessionIsRejected();
        }

        private static void MenuSwitchCancelsTransientCreate()
        {
            var session = new WorkspaceFeatureSession();
            var wall = Feature("model.wall", CreateInputMode.PickThenForm, true);
            session.SelectFeature(wall.Id);
            var create = session.BeginCreate(wall);
            if (create.State != AddCreateState.WaitingForCadInput)
                throw new Exception("Precondition failed: create session must be waiting for CAD input.");

            session.SelectFeature(new FeatureId("model.column"));
            if (create.State != AddCreateState.Cancelled || session.ActiveCreateSession != null)
                throw new Exception("Switching menu feature must cancel incompatible transient create state.");
        }

        private static void SemanticSelectionDoesNotChangeFeatureDuringModal()
        {
            var session = new WorkspaceFeatureSession();
            var featureId = new FeatureId("model.wall");
            session.SelectFeature(featureId);
            session.OpenModal();
            session.UpdateSemanticSelection("wall-family", "wall-42", "host-7");

            if (!session.FeatureId.HasValue || session.FeatureId.Value != featureId)
                throw new Exception("CAD/semantic selection changes must not unexpectedly change feature semantics.");
            if (session.InstanceId != "wall-42" || !session.IsModalOpen)
                throw new Exception("Selection changes during a modal must update semantic context without closing the modal.");
        }

        private static void HostDeletionBecomesActionable()
        {
            var session = new WorkspaceFeatureSession();
            session.UpdateSemanticSelection("door-family", "door-1", "wall-host");
            session.MarkHostDeleted("wall-host");

            if (session.Health != WorkspaceContextHealth.MissingHost || string.IsNullOrWhiteSpace(session.ActionableMessage))
                throw new Exception("Deleted hosts must produce an actionable stale-context state.");
            if (session.HostId != null)
                throw new Exception("Deleted host references must not remain active.");
        }

        private static void RefreshRehydratesWithoutZombieCreate()
        {
            var session = new WorkspaceFeatureSession();
            var feature = Feature("model.column", CreateInputMode.PickThenForm, true);
            session.SelectFeature(feature.Id);
            session.BeginCreate(feature);

            session.Rehydrate(
                "project-1",
                feature.Id,
                "column-family",
                "column-9",
                "floor-host",
                id => id == "column-family",
                id => id == "column-9",
                id => id == "floor-host");

            if (session.ActiveCreateSession != null || session.Health != WorkspaceContextHealth.Ready)
                throw new Exception("Refresh must rehydrate coherent context without restoring transient create sessions.");

            session.Rehydrate(
                "project-1",
                feature.Id,
                "missing-family",
                null,
                null,
                _ => false,
                _ => true,
                _ => true);
            if (session.Health != WorkspaceContextHealth.MissingFamily || string.IsNullOrWhiteSpace(session.ActionableMessage))
                throw new Exception("Refresh must surface stale family references as actionable state.");
        }

        private static void ProjectSwitchClearsProjectBoundContext()
        {
            var session = new WorkspaceFeatureSession();
            session.SwitchProject("project-a");
            session.SelectFeature(new FeatureId("model.wall"));
            session.UpdateSemanticSelection("family-a", "instance-a", "host-a");
            session.OpenModal();

            session.SwitchProject("project-b");
            if (session.ProjectId != "project-b" || session.FamilyId != null || session.InstanceId != null || session.HostId != null)
                throw new Exception("Project switch must clear project-bound semantic references.");
            if (session.IsModalOpen || session.Health != WorkspaceContextHealth.Empty)
                throw new Exception("Project switch must close transient UI context and return to coherent empty state.");
        }

        private static void DuplicateCreateSessionIsRejected()
        {
            var session = new WorkspaceFeatureSession();
            var feature = Feature("model.beam", CreateInputMode.PickThenForm, true);
            session.SelectFeature(feature.Id);
            session.BeginCreate(feature);
            ExpectInvalid(() => session.BeginCreate(feature), "Workspace must reject overlapping create sessions.");
        }

        private static FeatureDescriptor Feature(string id, CreateInputMode mode, bool allowsModal)
        {
            var recipe = new CreateRecipeDescriptor(id + ".recipe", mode, mode == CreateInputMode.Direct ? null : id + ".schema");
            var profile = new InteractionProfile(
                FeatureOnSelectBehavior.SelectContext,
                new[] { recipe },
                recipe.Id,
                Array.Empty<InteractionSurface>(),
                FeatureCapability.Create,
                allowsModal: allowsModal);
            return new FeatureDescriptor(new FeatureId(id), "model", 0, "Feature.Test", profile);
        }

        private static void ExpectInvalid(Action action, string message)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException)
            {
                return;
            }
            throw new Exception(message);
        }
    }
}
