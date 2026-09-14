using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QS3D.Core.BenchmarkParity
{
    public sealed class CubicostVerifiedComponentReconstruction
    {
        internal CubicostVerifiedComponentReconstruction(RecognizedQsComponent source, ComponentReviewDecision review)
        {
            Source = source ?? throw new ArgumentNullException("source");
            Review = review ?? throw new ArgumentNullException("review");
            if (review.Status != ComponentRecognitionStatus.Accepted && review.Status != ComponentRecognitionStatus.Corrected)
                throw new InvalidOperationException("Verified reconstruction must be accepted or corrected.");

            var corrected = review.Status == ComponentRecognitionStatus.Corrected;
            Length = corrected ? review.CorrectedLength.GetValueOrDefault(source.Length) : source.Length;
            Width = corrected ? review.CorrectedWidth.GetValueOrDefault(source.Width) : source.Width;
            Height = corrected ? review.CorrectedHeight.GetValueOrDefault(source.Height) : source.Height;
        }

        public RecognizedQsComponent Source { get; private set; }
        public ComponentReviewDecision Review { get; private set; }
        public string ComponentId { get { return Source.Id; } }
        public ComponentRecognitionStatus Status { get { return Review.Status; } }
        public double Length { get; private set; }
        public double Width { get; private set; }
        public double Height { get; private set; }
        public QuantityEvidence Evidence { get { return Source.Evidence; } }

        public RecognizedQsComponent ToRecognizedComponent()
        {
            return new RecognizedQsComponent(
                Source.Id,
                Source.ComponentType,
                Source.Classification,
                Source.Storey,
                Length,
                Width,
                Height,
                Source.SourceKind,
                Source.Evidence);
        }
    }

    public sealed class CubicostComponentReconstructionVerifier
    {
        public IReadOnlyList<CubicostVerifiedComponentReconstruction> Verify(
            IEnumerable<RecognizedQsComponent> components,
            IEnumerable<ComponentReviewDecision> reviews)
        {
            if (components == null) throw new ArgumentNullException("components");
            if (reviews == null) throw new ArgumentNullException("reviews");

            var componentSnapshot = components.ToList();
            var reviewSnapshot = reviews.ToList();
            if (componentSnapshot.Any(x => x == null))
                throw new ArgumentException("Component reconstruction contains null components.", "components");
            if (reviewSnapshot.Any(x => x == null))
                throw new ArgumentException("Component reconstruction contains null review decisions.", "reviews");

            var componentById = UniqueComponents(componentSnapshot);
            var reviewById = UniqueReviews(reviewSnapshot);

            foreach (var reviewId in reviewById.Keys)
            {
                if (!componentById.ContainsKey(reviewId))
                    throw new InvalidOperationException("Review decision references an unknown reconstructed component: " + reviewId + ".");
            }

            var verified = new List<CubicostVerifiedComponentReconstruction>();
            foreach (var component in componentSnapshot)
            {
                ComponentReviewDecision review;
                if (!reviewById.TryGetValue(component.Id, out review))
                    throw new InvalidOperationException("Reconstructed component has no review decision: " + component.Id + ".");

                ValidateEvidence(component);
                ValidateReviewShape(component, review);

                if (review.Status == ComponentRecognitionStatus.Rejected) continue;
                if (review.Status == ComponentRecognitionStatus.Proposed)
                    throw new InvalidOperationException("Proposed reconstruction cannot enter quantity publication: " + component.Id + ".");

                verified.Add(new CubicostVerifiedComponentReconstruction(component, review));
            }

            return new ReadOnlyCollection<CubicostVerifiedComponentReconstruction>(verified
                .OrderBy(x => x.Source.Storey, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Source.Storey, StringComparer.Ordinal)
                .ThenBy(x => x.Source.Classification, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Source.Classification, StringComparer.Ordinal)
                .ThenBy(x => x.ComponentId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.ComponentId, StringComparer.Ordinal)
                .ToList());
        }

        public CubicostDomainQuantityBundle QuantifyVerified(
            IEnumerable<RecognizedQsComponent> components,
            IEnumerable<ComponentReviewDecision> reviews,
            bool includeEnds)
        {
            var verified = Verify(components, reviews);
            var effectiveComponents = verified.Select(x => x.ToRecognizedComponent()).ToList();
            var effectiveReviews = verified.Select(x => new ComponentReviewDecision(
                x.ComponentId,
                x.Status,
                x.Review.Reviewer,
                x.Review.Reason,
                x.Status == ComponentRecognitionStatus.Corrected ? x.Length : (double?)null,
                x.Status == ComponentRecognitionStatus.Corrected ? x.Width : (double?)null,
                x.Status == ComponentRecognitionStatus.Corrected ? x.Height : (double?)null)).ToList();

            return new CubicostConcreteFormworkDomainOrchestrator().Quantify(effectiveComponents, effectiveReviews, includeEnds);
        }

        private static Dictionary<string, RecognizedQsComponent> UniqueComponents(IEnumerable<RecognizedQsComponent> components)
        {
            var result = new Dictionary<string, RecognizedQsComponent>(StringComparer.OrdinalIgnoreCase);
            foreach (var component in components)
            {
                if (!result.TryAdd(component.Id, component))
                    throw new InvalidOperationException("Duplicate reconstructed component id: " + component.Id + ".");
            }
            return result;
        }

        private static Dictionary<string, ComponentReviewDecision> UniqueReviews(IEnumerable<ComponentReviewDecision> reviews)
        {
            var result = new Dictionary<string, ComponentReviewDecision>(StringComparer.OrdinalIgnoreCase);
            foreach (var review in reviews)
            {
                if (!result.TryAdd(review.ComponentId, review))
                    throw new InvalidOperationException("Duplicate component review decision: " + review.ComponentId + ".");
            }
            return result;
        }

        private static void ValidateEvidence(RecognizedQsComponent component)
        {
            if (!Enum.IsDefined(typeof(ComponentSourceKind), component.SourceKind))
                throw new InvalidOperationException("Unsupported component source kind: " + component.Id + ".");
            if (component.Evidence.Confidence <= 0d)
                throw new InvalidOperationException("Reconstructed component requires positive evidence confidence: " + component.Id + ".");
        }

        private static void ValidateReviewShape(RecognizedQsComponent component, ComponentReviewDecision review)
        {
            if (!string.Equals(component.Id, review.ComponentId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Review/component identity mismatch: " + component.Id + ".");

            if (review.Status == ComponentRecognitionStatus.Accepted &&
                (review.CorrectedLength.HasValue || review.CorrectedWidth.HasValue || review.CorrectedHeight.HasValue))
                throw new InvalidOperationException("Accepted reconstruction must not carry correction dimensions: " + component.Id + ".");

            if (review.Status == ComponentRecognitionStatus.Rejected &&
                (review.CorrectedLength.HasValue || review.CorrectedWidth.HasValue || review.CorrectedHeight.HasValue))
                throw new InvalidOperationException("Rejected reconstruction must not carry correction dimensions: " + component.Id + ".");
        }
    }
}
