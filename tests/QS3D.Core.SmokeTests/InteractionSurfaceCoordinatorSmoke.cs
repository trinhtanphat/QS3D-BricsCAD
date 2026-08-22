using System;
using QS3D.Core.Features;

namespace QS3D.Core.SmokeTests
{
    internal static class InteractionSurfaceCoordinatorSmoke
    {
        public static void Run()
        {
            PersistentSurfacesReplaceDeterministically();
            ModalExclusivityFailsClosed();
            FeatureSwitchClearsTransientState();
            FloatingToolsAreKeyedAndReusable();
            FloatingToolCapacityIsBoundedAndMutationFree();
            FloatingToolCapacityIsReleasedByLifecycleTransitions();
        }

        private static void PersistentSurfacesReplaceDeterministically()
        {
            var feature = CreateFeature("model.finish", true, true, true);
            var coordinator = new InteractionSurfaceCoordinator();
            coordinator.SelectFeature(feature);

            coordinator.Open(new InteractionSurfaceBinding(feature.Id, InteractionSurface.PrimaryInspector, "finish.parameters", "room-1"));
            coordinator.Open(new InteractionSurfaceBinding(feature.Id, InteractionSurface.SecondaryInspector, "finish.host", "room-1"));
            coordinator.Open(new InteractionSurfaceBinding(feature.Id, InteractionSurface.PrimaryInspector, "finish.parameters.v2", "room-1"));

            var snapshot = coordinator.Snapshot;
            if (snapshot.PersistentInspectorCount != 2)
                throw new Exception("Surface coordinator must expose at most the requested two persistent inspector slots.");
            if (snapshot.PrimaryInspector == null || snapshot.PrimaryInspector.ContentKey != "finish.parameters.v2")
                throw new Exception("Opening the same persistent slot must replace its binding deterministically.");
            if (snapshot.SecondaryInspector == null || snapshot.SecondaryInspector.ContentKey != "finish.host")
                throw new Exception("Replacing the primary inspector must not disturb the secondary inspector.");

            var single = CreateFeature("model.room", false, false, false);
            coordinator.SelectFeature(single);
            coordinator.Open(new InteractionSurfaceBinding(single.Id, InteractionSurface.PrimaryInspector, "room.parameters"));
            ExpectInvalid(
                () => coordinator.Open(new InteractionSurfaceBinding(single.Id, InteractionSurface.SecondaryInspector, "room.secondary")),
                "A feature must not open an inspector slot absent from its InteractionProfile.");
        }

        private static void ModalExclusivityFailsClosed()
        {
            var feature = CreateFeature("model.finish-modal", true, true, false);
            var coordinator = new InteractionSurfaceCoordinator();
            coordinator.SelectFeature(feature);

            var first = new InteractionSurfaceBinding(feature.Id, InteractionSurface.ModalSheet, "finish.create", "room-7");
            coordinator.Open(first);
            coordinator.Open(first);

            ExpectInvalid(
                () => coordinator.Open(new InteractionSurfaceBinding(feature.Id, InteractionSurface.RecipeChooser, "finish.recipe", "room-7")),
                "A second blocking modal must fail closed instead of stacking another window.");

            if (!coordinator.Close(InteractionSurface.ModalSheet, "finish.create"))
                throw new Exception("The active modal must be closable by its semantic key.");
            coordinator.Open(new InteractionSurfaceBinding(feature.Id, InteractionSurface.RecipeChooser, "finish.recipe", "room-7"));
            if (coordinator.Snapshot.Modal == null || coordinator.Snapshot.Modal.Surface != InteractionSurface.RecipeChooser)
                throw new Exception("A recipe chooser may open after the prior modal closes.");
        }

        private static void FeatureSwitchClearsTransientState()
        {
            var first = CreateFeature("model.first", true, true, true);
            var second = CreateFeature("model.second", false, false, false);
            var coordinator = new InteractionSurfaceCoordinator();
            coordinator.SelectFeature(first);
            coordinator.Open(new InteractionSurfaceBinding(first.Id, InteractionSurface.PrimaryInspector, "first.parameters"));
            coordinator.Open(new InteractionSurfaceBinding(first.Id, InteractionSurface.ModalSheet, "first.modal"));
            coordinator.Open(new InteractionSurfaceBinding(first.Id, InteractionSurface.FloatingTool, "first.review"));

            coordinator.SelectFeature(second);
            var snapshot = coordinator.Snapshot;
            if (snapshot.FeatureId != second.Id || snapshot.PersistentInspectorCount != 0 || snapshot.Modal != null || snapshot.FloatingTools.Count != 0)
                throw new Exception("Switching semantic features must clear stale surface bindings before the new feature requests its surfaces.");

            coordinator.Open(new InteractionSurfaceBinding(second.Id, InteractionSurface.PrimaryInspector, "second.parameters"));
            coordinator.InvalidateContext();
            if (coordinator.Snapshot.PersistentInspectorCount != 0)
                throw new Exception("Invalidating stale host/selection context must clear bound inspector state.");
        }

        private static void FloatingToolsAreKeyedAndReusable()
        {
            var feature = CreateFeature("model.review", false, false, true);
            var coordinator = new InteractionSurfaceCoordinator();
            coordinator.SelectFeature(feature);

            coordinator.Open(new InteractionSurfaceBinding(feature.Id, InteractionSurface.FloatingTool, "quantity.review", "project-a"));
            coordinator.Open(new InteractionSurfaceBinding(feature.Id, InteractionSurface.FloatingTool, "quantity.review", "project-b"));
            if (coordinator.Snapshot.FloatingTools.Count != 1 || coordinator.Snapshot.FloatingTools[0].ContextKey != "project-b")
                throw new Exception("A floating tool semantic key must reuse one logical tool binding rather than stack duplicates.");

            if (!coordinator.Close(InteractionSurface.FloatingTool, "quantity.review") || coordinator.Snapshot.FloatingTools.Count != 0)
                throw new Exception("A keyed floating tool must close deterministically.");
        }

        private static void FloatingToolCapacityIsBoundedAndMutationFree()
        {
            if (InteractionSurfaceCoordinator.MaximumFloatingTools != FloatingToolWindowPolicy.MaximumVisibleWorkAreas)
                throw new Exception("Floating interaction-state capacity must stay aligned with the bounded floating-window policy.");

            var feature = CreateFeature("model.capacity", false, false, true);
            var coordinator = new InteractionSurfaceCoordinator();
            coordinator.SelectFeature(feature);

            for (var i = InteractionSurfaceCoordinator.MaximumFloatingTools - 1; i >= 0; i--)
            {
                coordinator.Open(new InteractionSurfaceBinding(
                    feature.Id,
                    InteractionSurface.FloatingTool,
                    "tool." + i.ToString("D3"),
                    "context." + i.ToString("D3")));
            }

            var atCapacity = coordinator.Snapshot;
            if (atCapacity.FloatingTools.Count != InteractionSurfaceCoordinator.MaximumFloatingTools)
                throw new Exception("The exact floating-tool capacity must remain accepted.");
            if (atCapacity.FloatingTools[0].ContentKey != "tool.000" ||
                atCapacity.FloatingTools[atCapacity.FloatingTools.Count - 1].ContentKey != "tool.063")
                throw new Exception("Bounded floating-tool snapshots must retain deterministic semantic-key ordering.");

            ExpectInvalid(
                () => coordinator.Open(new InteractionSurfaceBinding(feature.Id, InteractionSurface.FloatingTool, "tool.overflow", "overflow")),
                "Opening a distinct floating tool above the supported capacity must fail closed.");

            var afterRefusal = coordinator.Snapshot;
            if (afterRefusal.FloatingTools.Count != atCapacity.FloatingTools.Count ||
                Contains(afterRefusal, "tool.overflow"))
                throw new Exception("Floating-tool capacity refusal must not mutate coordinator state.");

            coordinator.Open(new InteractionSurfaceBinding(feature.Id, InteractionSurface.FloatingTool, "tool.000", "replacement"));
            var afterReplacement = coordinator.Snapshot;
            if (afterReplacement.FloatingTools.Count != InteractionSurfaceCoordinator.MaximumFloatingTools ||
                afterReplacement.FloatingTools[0].ContextKey != "replacement")
                throw new Exception("Replacing an existing floating-tool key must remain allowed at capacity without increasing state size.");

            if (!coordinator.Close(InteractionSurface.FloatingTool, "tool.001"))
                throw new Exception("A floating tool must release its capacity when closed.");
            coordinator.Open(new InteractionSurfaceBinding(feature.Id, InteractionSurface.FloatingTool, "tool.reused", "reused"));
            if (coordinator.Snapshot.FloatingTools.Count != InteractionSurfaceCoordinator.MaximumFloatingTools ||
                !Contains(coordinator.Snapshot, "tool.reused"))
                throw new Exception("Released floating-tool capacity must be reusable by a new semantic tool.");
        }

        private static void FloatingToolCapacityIsReleasedByLifecycleTransitions()
        {
            var feature = CreateFeature("model.lifecycle", false, false, true);
            var coordinator = new InteractionSurfaceCoordinator();
            coordinator.SelectFeature(feature);
            FillFloatingTools(coordinator, feature, "first");

            coordinator.InvalidateContext();
            if (coordinator.Snapshot.FloatingTools.Count != 0)
                throw new Exception("Invalidating interaction context must release all floating-tool capacity.");
            FillFloatingTools(coordinator, feature, "second");

            coordinator.ClearSelection();
            if (coordinator.SelectedFeature != null || coordinator.Snapshot.FloatingTools.Count != 0)
                throw new Exception("Clearing selection must release floating-tool state and selected-feature ownership.");

            coordinator.SelectFeature(feature);
            FillFloatingTools(coordinator, feature, "third");
            var sameFeatureWithoutFloating = CreateFeature("model.lifecycle", false, false, false);
            coordinator.SelectFeature(sameFeatureWithoutFloating);
            if (coordinator.Snapshot.FloatingTools.Count != 0)
                throw new Exception("Updating the selected feature profile to disallow floating tools must release prior floating-tool state.");
        }

        private static void FillFloatingTools(InteractionSurfaceCoordinator coordinator, FeatureDescriptor feature, string prefix)
        {
            for (var i = 0; i < InteractionSurfaceCoordinator.MaximumFloatingTools; i++)
            {
                coordinator.Open(new InteractionSurfaceBinding(
                    feature.Id,
                    InteractionSurface.FloatingTool,
                    prefix + "." + i.ToString("D3")));
            }

            if (coordinator.Snapshot.FloatingTools.Count != InteractionSurfaceCoordinator.MaximumFloatingTools)
                throw new Exception("Lifecycle capacity control must accept the exact supported floating-tool count.");
        }

        private static bool Contains(InteractionSurfaceSnapshot snapshot, string contentKey)
        {
            for (var i = 0; i < snapshot.FloatingTools.Count; i++)
                if (snapshot.FloatingTools[i].ContentKey == contentKey) return true;
            return false;
        }

        private static FeatureDescriptor CreateFeature(string id, bool secondary, bool modal, bool floating)
        {
            var surfaces = secondary
                ? new[] { InteractionSurface.PrimaryInspector, InteractionSurface.SecondaryInspector }
                : new[] { InteractionSurface.PrimaryInspector };

            return new FeatureDescriptor(
                new FeatureId(id),
                "model",
                1,
                "Feature." + id,
                new InteractionProfile(
                    FeatureOnSelectBehavior.SelectContext,
                    Array.Empty<CreateRecipeDescriptor>(),
                    null,
                    surfaces,
                    FeatureCapability.EditParameters,
                    allowsModal: modal,
                    allowsFloatingTool: floating));
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
