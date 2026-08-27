using System;
using QS3D.Core.Domain;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Mutable adapter used by specialized Workspace property editors while the canonical
    /// ProjectFamilyService continues to expose FamilyPropertyUpdateResult.
    /// </summary>
    internal sealed class ProjectFamilyPropertyMutationResult
    {
        private readonly FamilyPropertyUpdateResult _inner;

        private ProjectFamilyPropertyMutationResult(FamilyPropertyUpdateResult inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public int InheritedInstancesUpdated
        {
            get => _inner.InheritedInstancesUpdated;
            set => _inner.InheritedInstancesUpdated = value;
        }

        public int OverridesPreserved
        {
            get => _inner.OverridesPreserved;
            set => _inner.OverridesPreserved = value;
        }

        public static implicit operator ProjectFamilyPropertyMutationResult(FamilyPropertyUpdateResult value) =>
            new ProjectFamilyPropertyMutationResult(value);
    }
}
