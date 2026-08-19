using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QS3D.Core.Features
{
    public enum CapabilityStateSource
    {
        Registry,
        Dependency,
        Host,
        License,
        ProjectState
    }

    public sealed class FeatureCapabilityState
    {
        private FeatureCapabilityState(FeatureId featureId, bool isEnabled, string? disabledReason, CapabilityStateSource? disabledSource)
        {
            FeatureId = featureId;
            IsEnabled = isEnabled;
            DisabledReason = Normalize(disabledReason);
            DisabledSource = disabledSource;

            if (IsEnabled && (DisabledReason != null || DisabledSource != null))
                throw new InvalidOperationException("Enabled capability state cannot carry disabled metadata.");
            if (!IsEnabled && (DisabledReason == null || DisabledSource == null))
                throw new InvalidOperationException("Disabled capability state requires both reason and source.");
        }

        public FeatureId FeatureId { get; }
        public bool IsEnabled { get; }
        public string? DisabledReason { get; }
        public CapabilityStateSource? DisabledSource { get; }

        public static FeatureCapabilityState Enabled(FeatureId featureId) => new FeatureCapabilityState(featureId, true, null, null);

        public static FeatureCapabilityState Disabled(FeatureId featureId, string reason, CapabilityStateSource source) =>
            new FeatureCapabilityState(featureId, false, reason, source);

        private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public enum InspectorSelectionState
    {
        None,
        SingleSelect,
        MultiSelect,
        Unsupported,
        RefreshReload
    }

    public enum InvalidInputKind
    {
        Required,
        InvalidFormat,
        OutOfRange,
        MissingSelection,
        UnsupportedValue,
        DependencyUnavailable
    }

    public enum InvalidInputFocusPolicy
    {
        FirstBySchemaOrder
    }

    public sealed class InvalidInputDiagnostic
    {
        public InvalidInputDiagnostic(string fieldKey, InvalidInputKind kind, int schemaOrder)
        {
            if (string.IsNullOrWhiteSpace(fieldKey)) throw new ArgumentException("Invalid-input field key cannot be blank.", nameof(fieldKey));
            if (schemaOrder < 0) throw new ArgumentOutOfRangeException(nameof(schemaOrder), "Schema order cannot be negative.");
            FieldKey = fieldKey.Trim();
            Kind = kind;
            SchemaOrder = schemaOrder;
        }

        public string FieldKey { get; }
        public InvalidInputKind Kind { get; }
        public int SchemaOrder { get; }
    }

    public sealed class InvalidInputContract
    {
        public InvalidInputContract(IEnumerable<InvalidInputDiagnostic> diagnostics, InvalidInputFocusPolicy focusPolicy = InvalidInputFocusPolicy.FirstBySchemaOrder)
        {
            Diagnostics = new ReadOnlyCollection<InvalidInputDiagnostic>((diagnostics ?? Enumerable.Empty<InvalidInputDiagnostic>()).ToArray());
            if (Diagnostics.Any(x => x == null)) throw new InvalidOperationException("Invalid-input diagnostics cannot contain null entries.");
            if (Diagnostics.GroupBy(x => x.FieldKey, StringComparer.Ordinal).Any(g => g.Count() > 1))
                throw new InvalidOperationException("Invalid-input diagnostics must have unique field keys.");
            FocusPolicy = focusPolicy;
        }

        public IReadOnlyList<InvalidInputDiagnostic> Diagnostics { get; }
        public InvalidInputFocusPolicy FocusPolicy { get; }

        public InvalidInputDiagnostic? FirstInvalid => Diagnostics
            .OrderBy(x => x.SchemaOrder)
            .ThenBy(x => x.FieldKey, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    public enum FeatureActivationKind
    {
        Build,
        Toggle
    }

    public sealed class FeatureActivationContract
    {
        public FeatureActivationContract(FeatureActivationKind kind, string actionKey)
        {
            if (string.IsNullOrWhiteSpace(actionKey)) throw new ArgumentException("Action key cannot be blank.", nameof(actionKey));
            Kind = kind;
            ActionKey = actionKey.Trim();
        }

        public FeatureActivationKind Kind { get; }
        public string ActionKey { get; }
        public bool CreatesDomainObject => Kind == FeatureActivationKind.Build;
    }

    public enum TaskPresentation
    {
        Modal,
        Floating
    }

    public static class TaskPresentationGuidance
    {
        public static TaskPresentation Resolve(InteractionProfile profile, bool mustBlockWorkspaceInput, bool benefitsFromPersistentReference)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (mustBlockWorkspaceInput)
            {
                if (!profile.AllowsModal) throw new InvalidOperationException("This feature does not permit modal task presentation.");
                return TaskPresentation.Modal;
            }

            if (benefitsFromPersistentReference)
            {
                if (!profile.AllowsFloatingTool) throw new InvalidOperationException("This feature does not permit floating task presentation.");
                return TaskPresentation.Floating;
            }

            if (profile.AllowsFloatingTool) return TaskPresentation.Floating;
            if (profile.AllowsModal) return TaskPresentation.Modal;
            throw new InvalidOperationException("Feature profile does not permit a task presentation surface.");
        }
    }

    public sealed class WorkspaceInteractionContract
    {
        public WorkspaceInteractionContract(
            FeatureCapabilityState capabilityState,
            InteractionProfile profile,
            FeatureActivationContract activation,
            IEnumerable<InspectorSelectionState> inspectorStates)
        {
            CapabilityState = capabilityState ?? throw new ArgumentNullException(nameof(capabilityState));
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            Activation = activation ?? throw new ArgumentNullException(nameof(activation));
            InspectorStates = new ReadOnlyCollection<InspectorSelectionState>((inspectorStates ?? Enumerable.Empty<InspectorSelectionState>()).Distinct().ToArray());

            if (Activation.Kind == FeatureActivationKind.Build && (Profile.Capabilities & FeatureCapability.Create) == 0)
                throw new InvalidOperationException("Build activation requires the Create capability.");
            if (Activation.Kind == FeatureActivationKind.Toggle && Profile.Recipes.Count != 0)
                throw new InvalidOperationException("Toggle activation cannot own create recipes.");
        }

        public FeatureCapabilityState CapabilityState { get; }
        public InteractionProfile Profile { get; }
        public FeatureActivationContract Activation { get; }
        public IReadOnlyList<InspectorSelectionState> InspectorStates { get; }
    }

    public sealed class WorkspaceHostFidelityPolicy
    {
        private WorkspaceHostFidelityPolicy()
        {
        }

        public static WorkspaceHostFidelityPolicy Strict { get; } = new WorkspaceHostFidelityPolicy();

        public bool CustomModalRemainsModelessWhenParentOrTopLevelHides => true;
        public bool RequiresMultiInstanceIsolationAndAffinity => true;
        public bool FloatingToolRequiresOwnerAffinity => true;
        public bool FloatingToolRequiresShowBounds => true;
        public bool AllowsNative3DWorkspace => true;
        public bool AllowsFake2DCanvasOwnership => false;
        public string MessageFilterOwnerIssue => "#3111";
    }
}
