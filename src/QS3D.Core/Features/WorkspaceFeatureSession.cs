using System;

namespace QS3D.Core.Features
{
    public enum WorkspaceContextHealth
    {
        Empty,
        Ready,
        MissingFamily,
        MissingInstance,
        MissingHost
    }

    public sealed class WorkspaceFeatureSession
    {
        private AddCreateStateMachine? _createSession;

        public string? ProjectId { get; private set; }
        public FeatureId? FeatureId { get; private set; }
        public string? FamilyId { get; private set; }
        public string? InstanceId { get; private set; }
        public string? HostId { get; private set; }
        public bool IsModalOpen { get; private set; }
        public WorkspaceContextHealth Health { get; private set; } = WorkspaceContextHealth.Empty;
        public string? ActionableMessage { get; private set; }
        public AddCreateStateMachine? ActiveCreateSession => _createSession != null && !_createSession.IsTerminal ? _createSession : null;

        public void SwitchProject(string? projectId)
        {
            var normalized = NormalizeOptional(projectId);
            if (string.Equals(ProjectId, normalized, StringComparison.OrdinalIgnoreCase)) return;

            CancelTransientCreate();
            ProjectId = normalized;
            FamilyId = null;
            InstanceId = null;
            HostId = null;
            IsModalOpen = false;
            SetHealth(WorkspaceContextHealth.Empty, normalized == null ? "Open a project to establish Workspace context." : null);
        }

        public void SelectFeature(FeatureId featureId)
        {
            if (FeatureId.HasValue && FeatureId.Value == featureId) return;

            CancelTransientCreate();
            FeatureId = featureId;
            FamilyId = null;
            InstanceId = null;
            HostId = null;
            IsModalOpen = false;
            SetHealth(WorkspaceContextHealth.Empty, null);
        }

        public void UpdateSemanticSelection(string? familyId, string? instanceId, string? hostId = null)
        {
            FamilyId = NormalizeOptional(familyId);
            InstanceId = NormalizeOptional(instanceId);
            HostId = NormalizeOptional(hostId);
            SetHealth(FamilyId == null && InstanceId == null ? WorkspaceContextHealth.Empty : WorkspaceContextHealth.Ready, null);
        }

        public AddCreateStateMachine BeginCreate(FeatureDescriptor feature, string? recipeId = null)
        {
            if (feature == null) throw new ArgumentNullException(nameof(feature));
            if (FeatureId.HasValue && FeatureId.Value != feature.Id)
                throw new InvalidOperationException("Create feature does not match the active Workspace feature context.");
            if (ActiveCreateSession != null)
                throw new InvalidOperationException("Only one create session may be active per Workspace panel.");

            FeatureId = feature.Id;
            _createSession = new AddCreateStateMachine(feature);
            try
            {
                _createSession.Begin(recipeId);
                return _createSession;
            }
            catch
            {
                _createSession = null;
                throw;
            }
        }

        public void OpenModal()
        {
            IsModalOpen = true;
        }

        public void CloseModal()
        {
            IsModalOpen = false;
        }

        public void MarkHostDeleted(string hostId)
        {
            var normalized = NormalizeRequired(hostId, nameof(hostId));
            if (!string.Equals(HostId, normalized, StringComparison.OrdinalIgnoreCase)) return;

            HostId = null;
            SetHealth(WorkspaceContextHealth.MissingHost, "The selected instance host no longer exists. Reselect a valid host or clear the selection.");
        }

        public void Rehydrate(
            string? projectId,
            FeatureId? featureId,
            string? familyId,
            string? instanceId,
            string? hostId,
            Func<string, bool> familyExists,
            Func<string, bool> instanceExists,
            Func<string, bool> hostExists)
        {
            if (familyExists == null) throw new ArgumentNullException(nameof(familyExists));
            if (instanceExists == null) throw new ArgumentNullException(nameof(instanceExists));
            if (hostExists == null) throw new ArgumentNullException(nameof(hostExists));

            CancelTransientCreate();
            ProjectId = NormalizeOptional(projectId);
            FeatureId = featureId;
            FamilyId = NormalizeOptional(familyId);
            InstanceId = NormalizeOptional(instanceId);
            HostId = NormalizeOptional(hostId);
            IsModalOpen = false;

            if (FamilyId != null && !familyExists(FamilyId))
            {
                SetHealth(WorkspaceContextHealth.MissingFamily, "The selected family no longer exists. Choose another family.");
                return;
            }
            if (InstanceId != null && !instanceExists(InstanceId))
            {
                SetHealth(WorkspaceContextHealth.MissingInstance, "The selected instance no longer exists. Reselect an instance.");
                return;
            }
            if (HostId != null && !hostExists(HostId))
            {
                SetHealth(WorkspaceContextHealth.MissingHost, "The selected instance host no longer exists. Reselect a valid host or clear the selection.");
                return;
            }

            SetHealth(FamilyId == null && InstanceId == null ? WorkspaceContextHealth.Empty : WorkspaceContextHealth.Ready, null);
        }

        private void CancelTransientCreate()
        {
            if (_createSession == null || _createSession.IsTerminal)
            {
                _createSession = null;
                return;
            }

            if (_createSession.CreateWasHandedOff)
                throw new InvalidOperationException("Cannot switch Workspace context while a create mutation handoff is in progress.");

            _createSession.Cancel();
            _createSession = null;
        }

        private void SetHealth(WorkspaceContextHealth health, string? message)
        {
            Health = health;
            ActionableMessage = message;
        }

        private static string? NormalizeOptional(string? value)
        {
            return value == null || string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string NormalizeRequired(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value cannot be blank.", parameterName);
            return value.Trim();
        }
    }
}
