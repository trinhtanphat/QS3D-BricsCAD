using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QS3D.Core.Features
{
    public sealed class InteractionSurfaceBinding : IEquatable<InteractionSurfaceBinding>
    {
        public InteractionSurfaceBinding(FeatureId featureId, InteractionSurface surface, string contentKey, string? contextKey = null)
        {
            if (string.IsNullOrWhiteSpace(contentKey)) throw new ArgumentException("Surface content key cannot be blank.", nameof(contentKey));
            FeatureId = featureId;
            Surface = surface;
            ContentKey = contentKey.Trim();
            ContextKey = NormalizeOptional(contextKey);
        }

        public FeatureId FeatureId { get; }
        public InteractionSurface Surface { get; }
        public string ContentKey { get; }
        public string? ContextKey { get; }

        public bool Equals(InteractionSurfaceBinding? other)
        {
            return other != null
                && FeatureId == other.FeatureId
                && Surface == other.Surface
                && StringComparer.Ordinal.Equals(ContentKey, other.ContentKey)
                && StringComparer.Ordinal.Equals(ContextKey, other.ContextKey);
        }

        public override bool Equals(object? obj) => Equals(obj as InteractionSurfaceBinding);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = FeatureId.GetHashCode();
                hash = (hash * 397) ^ Surface.GetHashCode();
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(ContentKey);
                hash = (hash * 397) ^ (ContextKey == null ? 0 : StringComparer.Ordinal.GetHashCode(ContextKey));
                return hash;
            }
        }

        private static string? NormalizeOptional(string? value)
        {
            if (value == null) return null;
            if (string.IsNullOrWhiteSpace(value)) return null;
            return value.Trim();
        }
    }

    public sealed class InteractionSurfaceSnapshot
    {
        internal InteractionSurfaceSnapshot(
            FeatureId? featureId,
            InteractionSurfaceBinding? primaryInspector,
            InteractionSurfaceBinding? secondaryInspector,
            InteractionSurfaceBinding? modal,
            IEnumerable<InteractionSurfaceBinding> floatingTools)
        {
            FeatureId = featureId;
            PrimaryInspector = primaryInspector;
            SecondaryInspector = secondaryInspector;
            Modal = modal;
            FloatingTools = new ReadOnlyCollection<InteractionSurfaceBinding>((floatingTools ?? Enumerable.Empty<InteractionSurfaceBinding>())
                .OrderBy(x => x.ContentKey, StringComparer.Ordinal)
                .ToArray());
        }

        public FeatureId? FeatureId { get; }
        public InteractionSurfaceBinding? PrimaryInspector { get; }
        public InteractionSurfaceBinding? SecondaryInspector { get; }
        public InteractionSurfaceBinding? Modal { get; }
        public IReadOnlyList<InteractionSurfaceBinding> FloatingTools { get; }
        public int PersistentInspectorCount => (PrimaryInspector == null ? 0 : 1) + (SecondaryInspector == null ? 0 : 1);
    }

    public sealed class InteractionSurfaceCoordinator
    {
        public const int MaximumFloatingTools = FloatingToolWindowPolicy.MaximumVisibleWorkAreas;

        private FeatureDescriptor? _selectedFeature;
        private InteractionSurfaceBinding? _primaryInspector;
        private InteractionSurfaceBinding? _secondaryInspector;
        private InteractionSurfaceBinding? _modal;
        private readonly Dictionary<string, InteractionSurfaceBinding> _floatingTools = new Dictionary<string, InteractionSurfaceBinding>(StringComparer.Ordinal);

        public FeatureDescriptor? SelectedFeature => _selectedFeature;

        public InteractionSurfaceSnapshot Snapshot => new InteractionSurfaceSnapshot(
            _selectedFeature?.Id,
            _primaryInspector,
            _secondaryInspector,
            _modal,
            _floatingTools.Values);

        public void SelectFeature(FeatureDescriptor descriptor)
        {
            if (descriptor == null) throw new ArgumentNullException(nameof(descriptor));

            if (_selectedFeature != null && _selectedFeature.Id == descriptor.Id)
            {
                _selectedFeature = descriptor;
                TrimUnsupportedPersistentSurfaces(descriptor.InteractionProfile);
                if (!descriptor.InteractionProfile.AllowsModal) _modal = null;
                if (!descriptor.InteractionProfile.AllowsFloatingTool) _floatingTools.Clear();
                return;
            }

            _selectedFeature = descriptor;
            _primaryInspector = null;
            _secondaryInspector = null;
            _modal = null;
            _floatingTools.Clear();
        }

        public void ClearSelection()
        {
            _selectedFeature = null;
            _primaryInspector = null;
            _secondaryInspector = null;
            _modal = null;
            _floatingTools.Clear();
        }

        public void InvalidateContext()
        {
            _primaryInspector = null;
            _secondaryInspector = null;
            _modal = null;
            _floatingTools.Clear();
        }

        public void Open(InteractionSurfaceBinding binding)
        {
            if (binding == null) throw new ArgumentNullException(nameof(binding));
            var selected = RequireSelectedFeature();
            if (binding.FeatureId != selected.Id)
                throw new InvalidOperationException("Surface request FeatureId does not match the selected feature.");

            var profile = selected.InteractionProfile;
            switch (binding.Surface)
            {
                case InteractionSurface.PrimaryInspector:
                    RequirePersistentSurface(profile, InteractionSurface.PrimaryInspector);
                    _primaryInspector = binding;
                    break;
                case InteractionSurface.SecondaryInspector:
                    RequirePersistentSurface(profile, InteractionSurface.SecondaryInspector);
                    _secondaryInspector = binding;
                    break;
                case InteractionSurface.ModalSheet:
                case InteractionSurface.RecipeChooser:
                    if (!profile.AllowsModal)
                        throw new InvalidOperationException("Selected feature does not allow modal interaction.");
                    if (_modal != null && !_modal.Equals(binding))
                        throw new InvalidOperationException("Only one blocking modal surface may be open at a time.");
                    _modal = binding;
                    break;
                case InteractionSurface.FloatingTool:
                    if (!profile.AllowsFloatingTool)
                        throw new InvalidOperationException("Selected feature does not allow a floating tool.");
                    if (!_floatingTools.ContainsKey(binding.ContentKey) && _floatingTools.Count >= MaximumFloatingTools)
                        throw new InvalidOperationException("Selected feature already has the maximum supported floating tools open.");
                    _floatingTools[binding.ContentKey] = binding;
                    break;
                default:
                    throw new InvalidOperationException("Unsupported interaction surface request: " + binding.Surface);
            }
        }

        public bool Close(InteractionSurface surface, string? contentKey = null)
        {
            switch (surface)
            {
                case InteractionSurface.PrimaryInspector:
                    if (_primaryInspector == null) return false;
                    _primaryInspector = null;
                    return true;
                case InteractionSurface.SecondaryInspector:
                    if (_secondaryInspector == null) return false;
                    _secondaryInspector = null;
                    return true;
                case InteractionSurface.ModalSheet:
                case InteractionSurface.RecipeChooser:
                    if (_modal == null) return false;
                    if (contentKey != null && !StringComparer.Ordinal.Equals(_modal.ContentKey, contentKey)) return false;
                    _modal = null;
                    return true;
                case InteractionSurface.FloatingTool:
                    if (contentKey == null)
                        throw new ArgumentException("Floating tool close requires its semantic content key.", nameof(contentKey));
                    if (string.IsNullOrWhiteSpace(contentKey))
                        throw new ArgumentException("Floating tool close requires its semantic content key.", nameof(contentKey));
                    return _floatingTools.Remove(contentKey.Trim());
                default:
                    return false;
            }
        }

        private FeatureDescriptor RequireSelectedFeature()
        {
            return _selectedFeature ?? throw new InvalidOperationException("A feature must be selected before requesting an interaction surface.");
        }

        private static void RequirePersistentSurface(InteractionProfile profile, InteractionSurface surface)
        {
            if (!profile.PersistentSurfaces.Contains(surface))
                throw new InvalidOperationException("Selected feature profile does not request persistent surface: " + surface);
        }

        private void TrimUnsupportedPersistentSurfaces(InteractionProfile profile)
        {
            if (!profile.PersistentSurfaces.Contains(InteractionSurface.PrimaryInspector)) _primaryInspector = null;
            if (!profile.PersistentSurfaces.Contains(InteractionSurface.SecondaryInspector)) _secondaryInspector = null;
        }
    }
}
