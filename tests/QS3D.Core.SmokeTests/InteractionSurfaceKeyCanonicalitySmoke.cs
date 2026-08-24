using System;
using QS3D.Core.Features;

namespace QS3D.Core.SmokeTests
{
    internal static class InteractionSurfaceKeyCanonicalitySmoke
    {
        public static void Run()
        {
            BindingIdentityRejectsEdgeWhitespace();
            ModalCloseRejectsPaddedKeysWithoutMutation();
            FloatingCloseRejectsPaddedKeysWithoutMutation();
        }

        private static void BindingIdentityRejectsEdgeWhitespace()
        {
            var featureId = new FeatureId("model.key-canonicality");
            var canonical = new InteractionSurfaceBinding(
                featureId,
                InteractionSurface.FloatingTool,
                "quantity.review",
                "project-a");

            if (canonical.ContentKey != "quantity.review" || canonical.ContextKey != "project-a")
                throw new Exception("Canonical interaction surface keys must be preserved exactly.");

            var blankContext = new InteractionSurfaceBinding(
                featureId,
                InteractionSurface.FloatingTool,
                "quantity.review",
                " \t\r\n ");
            if (blankContext.ContextKey != null)
                throw new Exception("Whitespace-only optional interaction context must remain equivalent to no context.");

            foreach (var padded in new[] { " quantity.review", "quantity.review ", "\tquantity.review", "quantity.review\t", "\nquantity.review", "quantity.review\n" })
            {
                ExpectArgument(
                    () => new InteractionSurfaceBinding(featureId, InteractionSurface.FloatingTool, padded),
                    "Padded interaction content keys must fail closed instead of aliasing the canonical key.");
            }

            foreach (var paddedContext in new[] { " project-a", "project-a ", "\tproject-a", "project-a\t", "\nproject-a", "project-a\n" })
            {
                ExpectArgument(
                    () => new InteractionSurfaceBinding(featureId, InteractionSurface.FloatingTool, "quantity.review", paddedContext),
                    "Nonblank padded interaction context keys must fail closed instead of being silently trimmed.");
            }
        }

        private static void ModalCloseRejectsPaddedKeysWithoutMutation()
        {
            foreach (var surface in new[] { InteractionSurface.ModalSheet, InteractionSurface.RecipeChooser })
            {
                foreach (var padded in new[] { " modal.action", "modal.action ", "\tmodal.action", "modal.action\t", "\nmodal.action", "modal.action\n" })
                {
                    var feature = CreateFeature("model.modal-key", allowsModal: true, allowsFloating: false);
                    var coordinator = new InteractionSurfaceCoordinator();
                    coordinator.SelectFeature(feature);
                    coordinator.Open(new InteractionSurfaceBinding(feature.Id, surface, "modal.action", "context-a"));

                    ExpectArgument(
                        () => coordinator.Close(surface, padded),
                        "Padded modal close keys must fail before coordinator state mutates.");

                    if (coordinator.Snapshot.Modal == null || coordinator.Snapshot.Modal.ContentKey != "modal.action")
                        throw new Exception("Rejected padded modal close keys must leave the active modal unchanged.");
                    if (!coordinator.Close(surface, "modal.action") || coordinator.Snapshot.Modal != null)
                        throw new Exception("Canonical modal close keys must continue to close the active modal.");
                }
            }
        }

        private static void FloatingCloseRejectsPaddedKeysWithoutMutation()
        {
            foreach (var padded in new[] { " quantity.review", "quantity.review ", "\tquantity.review", "quantity.review\t", "\nquantity.review", "quantity.review\n" })
            {
                var feature = CreateFeature("model.floating-key", allowsModal: false, allowsFloating: true);
                var coordinator = new InteractionSurfaceCoordinator();
                coordinator.SelectFeature(feature);
                coordinator.Open(new InteractionSurfaceBinding(feature.Id, InteractionSurface.FloatingTool, "quantity.review", "project-a"));

                ExpectArgument(
                    () => coordinator.Close(InteractionSurface.FloatingTool, padded),
                    "Padded floating-tool close keys must fail before coordinator state mutates.");

                if (coordinator.Snapshot.FloatingTools.Count != 1 || coordinator.Snapshot.FloatingTools[0].ContentKey != "quantity.review")
                    throw new Exception("Rejected padded floating-tool close keys must leave the floating tool unchanged.");
                if (!coordinator.Close(InteractionSurface.FloatingTool, "quantity.review") || coordinator.Snapshot.FloatingTools.Count != 0)
                    throw new Exception("Canonical floating-tool close keys must continue to close the keyed tool.");
            }
        }

        private static FeatureDescriptor CreateFeature(string id, bool allowsModal, bool allowsFloating)
        {
            return new FeatureDescriptor(
                new FeatureId(id),
                "model",
                1,
                "Feature." + id,
                new InteractionProfile(
                    FeatureOnSelectBehavior.SelectContext,
                    Array.Empty<CreateRecipeDescriptor>(),
                    null,
                    new[] { InteractionSurface.PrimaryInspector },
                    FeatureCapability.EditParameters,
                    allowsModal: allowsModal,
                    allowsFloatingTool: allowsFloating));
        }

        private static void ExpectArgument(Action action, string message)
        {
            try
            {
                action();
            }
            catch (ArgumentException)
            {
                return;
            }

            throw new Exception(message);
        }
    }
}
