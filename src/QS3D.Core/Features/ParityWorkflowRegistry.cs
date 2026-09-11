using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QS3D.Core.Features
{
    [Flags]
    public enum ParityWorkflowSurface { None = 0, Ui = 1, Mcp = 2, Launcher = 4 }

    public enum ParityWorkflowKind { ReadOnly = 0, SemanticMutation = 1, Infrastructure = 2 }

    [Flags]
    public enum ParityWorkflowRequirement
    {
        None = 0, ActiveDocument = 1, Project = 2, Zone = 4, Floor = 8, Family = 16,
        Selection = 32, AtomicMutation = 64, Audit = 128
    }

    public sealed class ParityWorkflowBinding
    {
        private const ParityWorkflowSurface KnownSurfaces = ParityWorkflowSurface.Ui | ParityWorkflowSurface.Mcp | ParityWorkflowSurface.Launcher;
        private const ParityWorkflowRequirement KnownRequirements = ParityWorkflowRequirement.ActiveDocument | ParityWorkflowRequirement.Project |
            ParityWorkflowRequirement.Zone | ParityWorkflowRequirement.Floor | ParityWorkflowRequirement.Family |
            ParityWorkflowRequirement.Selection | ParityWorkflowRequirement.AtomicMutation | ParityWorkflowRequirement.Audit;
        private const ParityWorkflowRequirement MutationMinimum = ParityWorkflowRequirement.ActiveDocument |
            ParityWorkflowRequirement.Project | ParityWorkflowRequirement.AtomicMutation | ParityWorkflowRequirement.Audit;

        public ParityWorkflowBinding(FeatureId featureId, string workflowKey, ParityWorkflowKind kind,
            ParityWorkflowSurface surfaces, ParityWorkflowRequirement requirements)
        {
            if (string.IsNullOrEmpty(featureId.ToString())) throw new ArgumentException("FeatureId cannot be default.", nameof(featureId));
            if (string.IsNullOrWhiteSpace(workflowKey)) throw new ArgumentException("Workflow key cannot be blank.", nameof(workflowKey));
            if (surfaces == ParityWorkflowSurface.None || (surfaces & ~KnownSurfaces) != 0)
                throw new ArgumentException("Workflow surfaces must contain only known non-empty surface flags.", nameof(surfaces));
            if ((requirements & ~KnownRequirements) != 0)
                throw new ArgumentException("Workflow requirements contain unknown flags.", nameof(requirements));
            if (!Enum.IsDefined(typeof(ParityWorkflowKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (kind == ParityWorkflowKind.SemanticMutation && (requirements & MutationMinimum) != MutationMinimum)
                throw new ArgumentException("Semantic mutation workflows require ActiveDocument, Project, AtomicMutation and Audit.", nameof(requirements));

            FeatureId = featureId;
            WorkflowKey = workflowKey.Trim().ToLowerInvariant();
            Kind = kind;
            Surfaces = surfaces;
            Requirements = requirements;
        }

        public FeatureId FeatureId { get; }
        public string WorkflowKey { get; }
        public ParityWorkflowKind Kind { get; }
        public ParityWorkflowSurface Surfaces { get; }
        public ParityWorkflowRequirement Requirements { get; }
    }

    public sealed class ParityWorkflowRegistry
    {
        private readonly IReadOnlyList<ParityWorkflowBinding> _bindings;
        private readonly Dictionary<FeatureId, ParityWorkflowBinding> _byFeatureId;

        public ParityWorkflowRegistry(IEnumerable<ParityWorkflowBinding> bindings)
        {
            if (bindings == null) throw new ArgumentNullException(nameof(bindings));
            var materialized = bindings.ToArray();
            if (materialized.Any(x => x == null)) throw new InvalidOperationException("Workflow registry cannot contain null bindings.");
            if (materialized.GroupBy(x => x.FeatureId).Any(g => g.Count() > 1))
                throw new InvalidOperationException("Workflow registry contains duplicate FeatureId values.");
            if (materialized.GroupBy(x => x.WorkflowKey, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1))
                throw new InvalidOperationException("Workflow registry contains duplicate workflow keys.");

            _bindings = new ReadOnlyCollection<ParityWorkflowBinding>(materialized);
            _byFeatureId = materialized.ToDictionary(x => x.FeatureId);
        }

        public IReadOnlyList<ParityWorkflowBinding> Bindings => _bindings;

        public bool TryGet(FeatureId id, out ParityWorkflowBinding binding)
        {
            if (_byFeatureId.TryGetValue(id, out var found)) { binding = found; return true; }
            binding = null!;
            return false;
        }

        public ParityWorkflowBinding GetRequired(FeatureId id)
        {
            if (TryGet(id, out var binding)) return binding;
            throw new KeyNotFoundException("Parity workflow is not registered: " + id + ".");
        }
    }
}