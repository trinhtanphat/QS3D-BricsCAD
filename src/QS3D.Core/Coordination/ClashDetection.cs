using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace QS3D.Core.Coordination
{
    public sealed class AxisAlignedBox
    {
        public AxisAlignedBox(
            double minX,
            double minY,
            double minZ,
            double maxX,
            double maxY,
            double maxZ)
        {
            MinX = RequireFinite(minX, nameof(minX));
            MinY = RequireFinite(minY, nameof(minY));
            MinZ = RequireFinite(minZ, nameof(minZ));
            MaxX = RequireFinite(maxX, nameof(maxX));
            MaxY = RequireFinite(maxY, nameof(maxY));
            MaxZ = RequireFinite(maxZ, nameof(maxZ));
            if (MaxX < MinX || MaxY < MinY || MaxZ < MinZ)
                throw new ArgumentException("Coordination bounds must have max coordinates greater than or equal to min coordinates.");
        }

        public double MinX { get; }
        public double MinY { get; }
        public double MinZ { get; }
        public double MaxX { get; }
        public double MaxY { get; }
        public double MaxZ { get; }

        private static double RequireFinite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(parameterName, "Coordination coordinates must be finite.");
            return value == 0d ? 0d : value;
        }
    }

    public sealed class CoordinationElement
    {
        public CoordinationElement(
            string elementId,
            string discipline,
            string category,
            string system,
            string region,
            AxisAlignedBox bounds)
        {
            ElementId = RequireCanonicalId(elementId, nameof(elementId));
            Discipline = RequireText(discipline, nameof(discipline));
            Category = RequireText(category, nameof(category));
            System = RequireText(system, nameof(system));
            Region = RequireText(region, nameof(region));
            Bounds = bounds ?? throw new ArgumentNullException(nameof(bounds));
        }

        public string ElementId { get; }
        public string Discipline { get; }
        public string Category { get; }
        public string System { get; }
        public string Region { get; }
        public AxisAlignedBox Bounds { get; }

        private static string RequireCanonicalId(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Coordination element id is required.", parameterName);
            var normalized = value.Trim();
            if (!string.Equals(value, normalized, StringComparison.Ordinal))
                throw new ArgumentException("Coordination element id must not contain surrounding whitespace.", parameterName);
            return value;
        }

        private static string RequireText(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Coordination classification text is required.", parameterName);
            return value.Trim();
        }
    }

    public enum ClashKind
    {
        Hard = 0,
        Clearance = 1
    }

    public sealed class ClashResult
    {
        internal ClashResult(
            string leftElementId,
            string rightElementId,
            ClashKind kind,
            double separationM,
            double overlapXM,
            double overlapYM,
            double overlapZM)
        {
            LeftElementId = leftElementId;
            RightElementId = rightElementId;
            Kind = kind;
            SeparationM = separationM;
            OverlapXM = overlapXM;
            OverlapYM = overlapYM;
            OverlapZM = overlapZM;
        }

        public string LeftElementId { get; }
        public string RightElementId { get; }
        public ClashKind Kind { get; }
        public double SeparationM { get; }
        public double OverlapXM { get; }
        public double OverlapYM { get; }
        public double OverlapZM { get; }
    }

    public sealed class ClashDetectionService
    {
        private const int MaximumElements = 500;

        public IReadOnlyList<ClashResult> Detect(
            IEnumerable<CoordinationElement> elements,
            double clearanceM = 0d,
            bool includeSameDiscipline = false)
        {
            if (elements == null) throw new ArgumentNullException(nameof(elements));
            if (double.IsNaN(clearanceM) || double.IsInfinity(clearanceM) || clearanceM < 0d)
                throw new ArgumentOutOfRangeException(nameof(clearanceM));

            RequireKnownCountWithinLimit(elements);

            var snapshot = new List<CoordinationElement>();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var index = 0;
            foreach (var element in elements)
            {
                if (index == MaximumElements)
                    throw TooManyElements();
                if (element == null)
                    throw new ArgumentException("Coordination input contains a null element at index " + index + ".", nameof(elements));
                if (!ids.Add(element.ElementId))
                    throw new ArgumentException("Duplicate coordination element id: " + element.ElementId + ".", nameof(elements));
                snapshot.Add(element);
                index++;
            }
            snapshot.Sort(CompareElements);

            var results = new List<ClashResult>();
            for (var i = 0; i < snapshot.Count; i++)
            {
                for (var j = i + 1; j < snapshot.Count; j++)
                {
                    var left = snapshot[i];
                    var right = snapshot[j];
                    if (!includeSameDiscipline &&
                        StringComparer.OrdinalIgnoreCase.Equals(left.Discipline, right.Discipline))
                        continue;

                    var overlapX = Overlap(left.Bounds.MinX, left.Bounds.MaxX, right.Bounds.MinX, right.Bounds.MaxX);
                    var overlapY = Overlap(left.Bounds.MinY, left.Bounds.MaxY, right.Bounds.MinY, right.Bounds.MaxY);
                    var overlapZ = Overlap(left.Bounds.MinZ, left.Bounds.MaxZ, right.Bounds.MinZ, right.Bounds.MaxZ);
                    if (overlapX > 0d && overlapY > 0d && overlapZ > 0d)
                    {
                        results.Add(new ClashResult(
                            left.ElementId,
                            right.ElementId,
                            ClashKind.Hard,
                            0d,
                            overlapX,
                            overlapY,
                            overlapZ));
                        continue;
                    }

                    if (clearanceM <= 0d) continue;
                    var gapX = Gap(left.Bounds.MinX, left.Bounds.MaxX, right.Bounds.MinX, right.Bounds.MaxX);
                    var gapY = Gap(left.Bounds.MinY, left.Bounds.MaxY, right.Bounds.MinY, right.Bounds.MaxY);
                    var gapZ = Gap(left.Bounds.MinZ, left.Bounds.MaxZ, right.Bounds.MinZ, right.Bounds.MaxZ);
                    var distance = EuclideanDistance(gapX, gapY, gapZ);
                    if (distance <= clearanceM)
                    {
                        results.Add(new ClashResult(
                            left.ElementId,
                            right.ElementId,
                            ClashKind.Clearance,
                            distance,
                            Math.Max(0d, overlapX),
                            Math.Max(0d, overlapY),
                            Math.Max(0d, overlapZ)));
                    }
                }
            }

            return new ReadOnlyCollection<ClashResult>(results.ToArray());
        }

        private static void RequireKnownCountWithinLimit(IEnumerable<CoordinationElement> elements)
        {
            if (elements is ICollection<CoordinationElement> collection && collection.Count > MaximumElements)
                throw TooManyElements();
            if (elements is IReadOnlyCollection<CoordinationElement> readOnlyCollection && readOnlyCollection.Count > MaximumElements)
                throw TooManyElements();
            if (elements is ICollection nonGenericCollection && nonGenericCollection.Count > MaximumElements)
                throw TooManyElements();
        }

        private static InvalidOperationException TooManyElements()
        {
            return new InvalidOperationException(
                "Coordination clash detection supports at most " + MaximumElements + " elements per operation.");
        }

        private static int CompareElements(CoordinationElement left, CoordinationElement right)
        {
            var compare = StringComparer.OrdinalIgnoreCase.Compare(left.ElementId, right.ElementId);
            return compare != 0 ? compare : StringComparer.Ordinal.Compare(left.ElementId, right.ElementId);
        }

        private static double EuclideanDistance(double x, double y, double z)
        {
            var scale = Math.Max(x, Math.Max(y, z));
            if (scale <= 0d) return 0d;

            var scaledX = x / scale;
            var scaledY = y / scale;
            var scaledZ = z / scale;
            var distance = scale * Math.Sqrt(
                (scaledX * scaledX) +
                (scaledY * scaledY) +
                (scaledZ * scaledZ));
            if (double.IsNaN(distance) || double.IsInfinity(distance))
                throw new OverflowException("Coordination separation distance exceeded the finite double range.");
            return distance == 0d ? 0d : distance;
        }

        private static double Overlap(double aMin, double aMax, double bMin, double bMax)
        {
            if (aMax <= bMin || bMax <= aMin) return 0d;
            var upper = Math.Min(aMax, bMax);
            var lower = Math.Max(aMin, bMin);
            return SubtractFinite(upper, lower, "Coordination overlap extent");
        }

        private static double Gap(double aMin, double aMax, double bMin, double bMax)
        {
            if (aMax < bMin) return SubtractFinite(bMin, aMax, "Coordination gap extent");
            if (bMax < aMin) return SubtractFinite(aMin, bMax, "Coordination gap extent");
            return 0d;
        }

        private static double SubtractFinite(double left, double right, string operation)
        {
            var result = left - right;
            if (double.IsNaN(result) || double.IsInfinity(result))
                throw new OverflowException(operation + " exceeded the finite double range.");
            return result == 0d ? 0d : result;
        }
    }
}
