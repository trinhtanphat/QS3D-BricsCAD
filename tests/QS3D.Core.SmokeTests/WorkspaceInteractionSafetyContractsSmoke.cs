using System;
using QS3D.Core.Features;

namespace QS3D.Core.SmokeTests
{
    internal static class WorkspaceInteractionSafetyContractsSmoke
    {
        public static void Run()
        {
            CapabilityStateIsExplicitAndFailClosed();
            CreateSemanticsHaveOnePrimaryBuildPath();
            InspectorAndInvalidInputContractsAreDeterministic();
            ToggleAndBuildSemanticsStayDistinct();
            ModalAndFloatingGuidanceRespectsEligibility();
            HostFidelityPolicyGuardsWorkspaceSafety();
        }

        private static void CapabilityStateIsExplicitAndFailClosed()
        {
            var id = new FeatureId("model.wall");
            var disabled = FeatureCapabilityState.Disabled(id, "Host dependency is unavailable.", CapabilityStateSource.Dependency);
            if (disabled.IsEnabled || disabled.DisabledReason != "Host dependency is unavailable." || disabled.DisabledSource != CapabilityStateSource.Dependency)
                throw new Exception("Disabled capability state must preserve FeatureId, reason, and source.");

            ExpectInvalid(() => FeatureCapabilityState.Disabled(id, " ", CapabilityStateSource.Dependency),
                "Disabled capability state without a deterministic reason must fail closed.");
        }

        private static void CreateSemanticsHaveOnePrimaryBuildPath()
        {
            var profile = new InteractionProfile(
                FeatureOnSelectBehavior.SelectContext,
                new[]
                {
                    new CreateRecipeDescriptor("wall.primary", CreateInputMode.FormThenPick, "wall.primary.schema"),
                    new CreateRecipeDescriptor("wall.alternate", CreateInputMode.PickThenForm, "wall.alternate.schema")
                },
                "wall.primary",
                new[] { InteractionSurface.PrimaryInspector },
                FeatureCapability.Create | FeatureCapability.EditParameters,
                allowsModal: true);

            var contract = new WorkspaceInteractionContract(
                FeatureCapabilityState.Enabled(new FeatureId("model.wall")),
                profile,
                new FeatureActivationContract(FeatureActivationKind.Build, "build.wall"),
                new[] { InspectorSelectionState.None, InspectorSelectionState.SingleSelect, InspectorSelectionState.RefreshReload });

            if (!contract.Activation.CreatesDomainObject || profile.PrimaryRecipeId != "wall.primary" || profile.Recipes.Count != 2)
                throw new Exception("Build features must retain recipe-first semantics with exactly one declared primary path.");
        }

        private static void InspectorAndInvalidInputContractsAreDeterministic()
        {
            var invalid = new InvalidInputContract(new[]
            {
                new InvalidInputDiagnostic("height", InvalidInputKind.OutOfRange, 20),
                new InvalidInputDiagnostic("family", InvalidInputKind.Required, 10)
            });

            if (invalid.FirstInvalid == null || invalid.FirstInvalid.FieldKey != "family" || invalid.FocusPolicy != InvalidInputFocusPolicy.FirstBySchemaOrder)
                throw new Exception("First-invalid-focus must deterministically follow schema order.");

            var direct = new InteractionProfile(
                FeatureOnSelectBehavior.SelectAndRefresh,
                new[] { new CreateRecipeDescriptor("direct", CreateInputMode.Direct) },
                "direct",
                Array.Empty<InteractionSurface>(),
                FeatureCapability.Create);
            var contract = new WorkspaceInteractionContract(
                FeatureCapabilityState.Enabled(new FeatureId("model.room")),
                direct,
                new FeatureActivationContract(FeatureActivationKind.Build, "build.room"),
                new[]
                {
                    InspectorSelectionState.None,
                    InspectorSelectionState.SingleSelect,
                    InspectorSelectionState.MultiSelect,
                    InspectorSelectionState.Unsupported,
                    InspectorSelectionState.RefreshReload
                });

            if (contract.InspectorStates.Count != 5)
                throw new Exception("Inspector contract must represent none, single, multi, unsupported, and refresh/reload states.");
        }

        private static void ToggleAndBuildSemanticsStayDistinct()
        {
            var toggleProfile = new InteractionProfile(
                FeatureOnSelectBehavior.SelectAndRefresh,
                Array.Empty<CreateRecipeDescriptor>(),
                null,
                Array.Empty<InteractionSurface>(),
                FeatureCapability.Locate);
            var toggle = new WorkspaceInteractionContract(
                FeatureCapabilityState.Enabled(new FeatureId("view.semantic-tags")),
                toggleProfile,
                new FeatureActivationContract(FeatureActivationKind.Toggle, "toggle.semantic-tags"),
                new[] { InspectorSelectionState.None });

            if (toggle.Activation.CreatesDomainObject)
                throw new Exception("Toggle semantics must never masquerade as a domain-object build action.");

            ExpectInvalid(() => new WorkspaceInteractionContract(
                FeatureCapabilityState.Enabled(new FeatureId("model.wall")),
                toggleProfile,
                new FeatureActivationContract(FeatureActivationKind.Build, "build.wall"),
                Array.Empty<InspectorSelectionState>()),
                "Build activation without Create capability must fail closed.");
        }

        private static void ModalAndFloatingGuidanceRespectsEligibility()
        {
            var profile = new InteractionProfile(
                FeatureOnSelectBehavior.SelectContext,
                new[] { new CreateRecipeDescriptor("wizard", CreateInputMode.Wizard, "wizard.schema") },
                "wizard",
                Array.Empty<InteractionSurface>(),
                FeatureCapability.Create,
                allowsModal: true,
                allowsFloatingTool: true);

            if (TaskPresentationGuidance.Resolve(profile, mustBlockWorkspaceInput: true, benefitsFromPersistentReference: false) != TaskPresentation.Modal)
                throw new Exception("Blocking task flows must resolve to eligible modal presentation.");
            if (TaskPresentationGuidance.Resolve(profile, mustBlockWorkspaceInput: false, benefitsFromPersistentReference: true) != TaskPresentation.Floating)
                throw new Exception("Reference-heavy non-blocking tasks must resolve to eligible floating presentation.");

            var direct = new InteractionProfile(
                FeatureOnSelectBehavior.SelectContext,
                new[] { new CreateRecipeDescriptor("direct", CreateInputMode.Direct) },
                "direct",
                Array.Empty<InteractionSurface>(),
                FeatureCapability.Create);
            ExpectInvalid(() => TaskPresentationGuidance.Resolve(direct, mustBlockWorkspaceInput: true, benefitsFromPersistentReference: false),
                "Modal presentation must not bypass profile eligibility.");
        }

        private static void HostFidelityPolicyGuardsWorkspaceSafety()
        {
            var policy = WorkspaceHostFidelityPolicy.Strict;
            if (!policy.CustomModalRemainsModelessWhenParentOrTopLevelHides)
                throw new Exception("Custom modal must remain modeless when parent/top-level host hides.");
            if (!policy.RequiresMultiInstanceIsolationAndAffinity)
                throw new Exception("Host contract must require multi-instance isolation and affinity.");
            if (!policy.FloatingToolRequiresOwnerAffinity || !policy.FloatingToolRequiresShowBounds)
                throw new Exception("Floating tools must retain owner affinity and show-bounds guards.");
            if (!policy.AllowsNative3DWorkspace || policy.AllowsFake2DCanvasOwnership)
                throw new Exception("Workspace contract must allow native 3D while forbidding fake 2D canvas ownership.");
            if (policy.MessageFilterOwnerIssue != "#3111")
                throw new Exception("Message-filter ownership must remain explicitly delegated to issue #3111.");
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
