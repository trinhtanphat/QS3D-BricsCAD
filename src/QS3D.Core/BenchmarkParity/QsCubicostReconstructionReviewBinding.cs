using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace QS3D.Core.BenchmarkParity
{
    public sealed class CubicostReconstructionReviewBinding
    {
        public CubicostReconstructionReviewBinding(string componentId, string reconstructionFingerprint)
        {
            ComponentId = QsModelElementSnapshot.Require(componentId, "componentId");
            ReconstructionFingerprint = QsModelElementSnapshot.Require(reconstructionFingerprint, "reconstructionFingerprint");
        }

        public string ComponentId { get; private set; }
        public string ReconstructionFingerprint { get; private set; }
    }

    public sealed class CubicostReconstructionReviewGate
    {
        private readonly CubicostComponentReconstructionVerifier _verifier = new CubicostComponentReconstructionVerifier();

        public string Fingerprint(RecognizedQsComponent component)
        {
            if (component == null) throw new ArgumentNullException("component");
            return string.Join("|", new[]
            {
                component.Id,
                component.ComponentType,
                component.Classification,
                component.Storey,
                component.SourceKind.ToString(),
                component.Length.ToString("R", CultureInfo.InvariantCulture),
                component.Width.ToString("R", CultureInfo.InvariantCulture),
                component.Height.ToString("R", CultureInfo.InvariantCulture),
                component.Evidence.Confidence.ToString("R", CultureInfo.InvariantCulture)
            });
        }

        public CubicostReconstructionReviewBinding Bind(RecognizedQsComponent component)
        {
            if (component == null) throw new ArgumentNullException("component");
            return new CubicostReconstructionReviewBinding(component.Id, Fingerprint(component));
        }

        public IReadOnlyList<CubicostVerifiedComponentReconstruction> Verify(
            IEnumerable<RecognizedQsComponent> components,
            IEnumerable<ComponentReviewDecision> reviews,
            IEnumerable<CubicostReconstructionReviewBinding> bindings)
        {
            if (components == null) throw new ArgumentNullException("components");
            if (reviews == null) throw new ArgumentNullException("reviews");
            if (bindings == null) throw new ArgumentNullException("bindings");

            var componentSnapshot = components.ToList();
            var reviewSnapshot = reviews.ToList();
            var bindingSnapshot = bindings.ToList();
            if (bindingSnapshot.Any(x => x == null)) throw new ArgumentException("Review binding collection contains null.", "bindings");

            var bindingById = new Dictionary<string, CubicostReconstructionReviewBinding>(StringComparer.OrdinalIgnoreCase);
            foreach (var binding in bindingSnapshot)
            {
                if (bindingById.ContainsKey(binding.ComponentId))
                    throw new InvalidOperationException("Duplicate reconstruction review binding: " + binding.ComponentId + ".");
                bindingById.Add(binding.ComponentId, binding);
            }

            foreach (var component in componentSnapshot)
            {
                if (component == null) throw new ArgumentException("Component reconstruction contains null components.", "components");
                CubicostReconstructionReviewBinding binding;
                if (!bindingById.TryGetValue(component.Id, out binding))
                    throw new InvalidOperationException("Reconstructed component has no review binding: " + component.Id + ".");
                if (!string.Equals(binding.ReconstructionFingerprint, Fingerprint(component), StringComparison.Ordinal))
                    throw new InvalidOperationException("Stale review binding cannot be applied to reconstructed component: " + component.Id + ".");
            }

            foreach (var bindingId in bindingById.Keys)
            {
                if (!componentSnapshot.Any(x => string.Equals(x.Id, bindingId, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("Review binding references an unknown reconstructed component: " + bindingId + ".");
            }

            return new ReadOnlyCollection<CubicostVerifiedComponentReconstruction>(_verifier.Verify(componentSnapshot, reviewSnapshot).ToList());
        }
    }
}
