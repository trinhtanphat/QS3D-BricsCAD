using System;
using QS3D.Core.Features;

namespace QS3D.Core.SmokeTests
{
    internal static class InspectorHostLayoutSmoke
    {
        public static void Run()
        {
            EmptyLayoutHasNoPhantomColumns();
            SingleInspectorPreservesCenterWidth();
            TwoInspectorModeIsDeterministic();
            CompactWidthFallsBackToTwoColumnMode();
            PreferredWidthsAreClamped();
        }

        private static void EmptyLayoutHasNoPhantomColumns()
        {
            var coordinator = CreateCoordinator(false);
            var layout = InspectorHostLayoutPlanner.Plan(coordinator.Snapshot, 900d);

            if (layout.VisibleInspectorCount != 0 || layout.ReservedInspectorWidth != 0d)
                throw new Exception("An empty inspector host must not reserve phantom separator or pane columns.");
        }

        private static void SingleInspectorPreservesCenterWidth()
        {
            var coordinator = CreateCoordinator(false);
            coordinator.Open(new InteractionSurfaceBinding(
                coordinator.SelectedFeature!.Id,
                InteractionSurface.PrimaryInspector,
                "host.primary"));

            var layout = InspectorHostLayoutPlanner.Plan(coordinator.Snapshot, 700d);
            if (!layout.PrimaryVisible || layout.SecondaryVisible || layout.VisibleInspectorCount != 1)
                throw new Exception("A single-inspector request must produce exactly one persistent pane.");
            if (layout.PrimaryWidth < InspectorHostLayoutPlanner.MinimumInspectorWidth)
                throw new Exception("The visible inspector must retain its minimum usable width.");
            if (700d - layout.ReservedInspectorWidth < InspectorHostLayoutPlanner.MinimumCenterWidth)
                throw new Exception("A single inspector must not consume the minimum center workspace width.");
        }

        private static void TwoInspectorModeIsDeterministic()
        {
            var coordinator = CreateCoordinator(true);
            OpenBoth(coordinator);

            var first = InspectorHostLayoutPlanner.Plan(coordinator.Snapshot, 1200d);
            var second = InspectorHostLayoutPlanner.Plan(coordinator.Snapshot, 1200d);

            if (!first.PrimaryVisible || !first.SecondaryVisible || first.VisibleInspectorCount != 2)
                throw new Exception("A sufficiently wide host must expose both requested inspector slots.");
            if (first.PrimaryWidth != second.PrimaryWidth || first.SecondaryWidth != second.SecondaryWidth)
                throw new Exception("The same surface snapshot and host width must produce a deterministic layout.");
            if (1200d - first.ReservedInspectorWidth < InspectorHostLayoutPlanner.MinimumCenterWidth)
                throw new Exception("Two-inspector mode must preserve the minimum center workspace width.");
        }

        private static void CompactWidthFallsBackToTwoColumnMode()
        {
            var coordinator = CreateCoordinator(true);
            OpenBoth(coordinator);

            var layout = InspectorHostLayoutPlanner.Plan(coordinator.Snapshot, 700d);
            if (!layout.PrimaryVisible || layout.SecondaryVisible || layout.VisibleInspectorCount != 1)
                throw new Exception("Compact width must collapse the secondary inspector instead of squeezing three unusable columns.");
            if (700d - layout.ReservedInspectorWidth < InspectorHostLayoutPlanner.MinimumCenterWidth)
                throw new Exception("Compact two-column mode must keep usable center content.");
        }

        private static void PreferredWidthsAreClamped()
        {
            var coordinator = CreateCoordinator(true);
            OpenBoth(coordinator);

            var layout = InspectorHostLayoutPlanner.Plan(coordinator.Snapshot, 1400d, 999d, double.NaN);
            if (layout.PrimaryWidth > InspectorHostLayoutPlanner.MaximumInspectorWidth)
                throw new Exception("Primary inspector width must respect the configured maximum.");
            if (layout.SecondaryWidth < InspectorHostLayoutPlanner.MinimumInspectorWidth)
                throw new Exception("Invalid preferred secondary widths must fail safe to the configured minimum.");
        }

        private static InteractionSurfaceCoordinator CreateCoordinator(bool secondary)
        {
            var surfaces = secondary
                ? new[] { InteractionSurface.PrimaryInspector, InteractionSurface.SecondaryInspector }
                : new[] { InteractionSurface.PrimaryInspector };
            var feature = new FeatureDescriptor(
                new FeatureId(secondary ? "host.two" : "host.one"),
                "model",
                1,
                secondary ? "Feature.HostTwo" : "Feature.HostOne",
                new InteractionProfile(
                    FeatureOnSelectBehavior.SelectContext,
                    Array.Empty<CreateRecipeDescriptor>(),
                    null,
                    surfaces,
                    FeatureCapability.EditParameters,
                    allowsModal: false,
                    allowsFloatingTool: false));
            var coordinator = new InteractionSurfaceCoordinator();
            coordinator.SelectFeature(feature);
            return coordinator;
        }

        private static void OpenBoth(InteractionSurfaceCoordinator coordinator)
        {
            var feature = coordinator.SelectedFeature;
            if (feature == null) throw new Exception("Inspector host smoke setup requires a selected feature.");
            coordinator.Open(new InteractionSurfaceBinding(feature.Id, InteractionSurface.PrimaryInspector, "host.primary"));
            coordinator.Open(new InteractionSurfaceBinding(feature.Id, InteractionSurface.SecondaryInspector, "host.secondary"));
        }
    }
}
