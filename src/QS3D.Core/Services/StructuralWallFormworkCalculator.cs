using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Services
{
    public enum StructuralWallFormworkFace
    {
        SideA = 1,
        SideB = 2,
        StartEnd = 3,
        EndEnd = 4
    }

    /// <summary>
    /// One measured concrete-contact rectangle on a vertical wall formwork face.
    /// U is the local horizontal coordinate of that face and Z is elevation from
    /// the wall bottom. Rectangles are clipped to the host face before unioning.
    /// </summary>
    public sealed class StructuralWallConcreteContactPatch
    {
        public StructuralWallConcreteContactPatch(
            StructuralWallFormworkFace face,
            double u0M,
            double u1M,
            double z0M,
            double z1M)
        {
            if (!Enum.IsDefined(typeof(StructuralWallFormworkFace), face))
                throw new ArgumentOutOfRangeException(nameof(face));
            RequireFinite(u0M, nameof(u0M));
            RequireFinite(u1M, nameof(u1M));
            RequireFinite(z0M, nameof(z0M));
            RequireFinite(z1M, nameof(z1M));
            Face = face;
            U0M = u0M;
            U1M = u1M;
            Z0M = z0M;
            Z1M = z1M;
        }

        public StructuralWallFormworkFace Face { get; }
        public double U0M { get; }
        public double U1M { get; }
        public double Z0M { get; }
        public double Z1M { get; }

        private static void RequireFinite(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(name, "Contact coordinate must be finite.");
        }
    }

    /// <summary>
    /// Through-opening dimensions used to retain broad-face deductions and the
    /// formwork on opening reveals. Doors normally omit the bottom/sill reveal.
    /// </summary>
    public sealed class StructuralWallFormworkOpening
    {
        public StructuralWallFormworkOpening(
            double widthM,
            double heightM,
            int count = 1,
            bool includeBottomReveal = true)
        {
            RequirePositiveFinite(widthM, nameof(widthM));
            RequirePositiveFinite(heightM, nameof(heightM));
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
            WidthM = widthM;
            HeightM = heightM;
            Count = count;
            IncludeBottomReveal = includeBottomReveal;
        }

        public double WidthM { get; }
        public double HeightM { get; }
        public int Count { get; }
        public bool IncludeBottomReveal { get; }

        internal double BroadFaceDeductionM2 => 2d * WidthM * HeightM * Count;

        internal double RevealAreaM2(double thicknessM)
        {
            var revealPerimeterM = 2d * HeightM + WidthM + (IncludeBottomReveal ? WidthM : 0d);
            return thicknessM * revealPerimeterM * Count;
        }

        private static void RequirePositiveFinite(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || !(value > 0d))
                throw new ArgumentOutOfRangeException(name, "Opening dimension must be finite and positive.");
        }
    }

    public sealed class StructuralWallFormworkResult
    {
        internal StructuralWallFormworkResult(
            double grossFormworkM2,
            double concreteContactDeductionM2,
            double openingFaceDeductionM2,
            double openingRevealM2,
            double formworkM2)
        {
            GrossFormworkM2 = grossFormworkM2;
            ConcreteContactDeductionM2 = concreteContactDeductionM2;
            OpeningFaceDeductionM2 = openingFaceDeductionM2;
            OpeningRevealM2 = openingRevealM2;
            FormworkM2 = formworkM2;
        }

        public double GrossFormworkM2 { get; }
        public double ConcreteContactDeductionM2 { get; }
        public double OpeningFaceDeductionM2 { get; }
        public double OpeningRevealM2 { get; }
        public double OpeningRevealAdjustmentM2 => OpeningRevealM2 - OpeningFaceDeductionM2;
        public double FormworkM2 { get; }
    }

    /// <summary>
    /// Structural wall formwork contract. Gross formwork is the two broad
    /// vertical faces plus both vertical end faces; top and bottom are excluded.
    /// Concrete contacts are unioned per face so overlapping neighbours cannot
    /// double-deduct the same formwork patch.
    /// </summary>
    public static class StructuralWallFormworkCalculator
    {
        public static StructuralWallFormworkResult Calculate(
            double lengthM,
            double thicknessM,
            double heightM,
            IReadOnlyList<StructuralWallConcreteContactPatch>? concreteContacts = null,
            IReadOnlyList<StructuralWallFormworkOpening>? openings = null)
        {
            RequirePositiveFinite(lengthM, nameof(lengthM));
            RequirePositiveFinite(thicknessM, nameof(thicknessM));
            RequirePositiveFinite(heightM, nameof(heightM));

            var broadFaceArea = lengthM * heightM;
            var endFaceArea = thicknessM * heightM;
            var gross = 2d * broadFaceArea + 2d * endFaceArea;
            RequireFiniteNonNegative(gross, "gross formwork");

            var contacts = concreteContacts ?? Array.Empty<StructuralWallConcreteContactPatch>();
            if (contacts.Any(x => x == null))
                throw new ArgumentException("Concrete contact collection cannot contain null entries.", nameof(concreteContacts));

            var contactDeduction = 0d;
            foreach (StructuralWallFormworkFace face in Enum.GetValues(typeof(StructuralWallFormworkFace)))
            {
                var faceWidth = face == StructuralWallFormworkFace.SideA || face == StructuralWallFormworkFace.SideB
                    ? lengthM
                    : thicknessM;
                contactDeduction += UnionAreaOnFace(contacts, face, faceWidth, heightM);
            }
            RequireFiniteNonNegative(contactDeduction, "concrete contact deduction");
            if (contactDeduction > gross + 1e-9d)
                throw new InvalidOperationException("Concrete contact union cannot exceed gross wall formwork.");

            var openingList = openings ?? Array.Empty<StructuralWallFormworkOpening>();
            if (openingList.Any(x => x == null))
                throw new ArgumentException("Opening collection cannot contain null entries.", nameof(openings));

            var openingFaceDeduction = 0d;
            var openingReveal = 0d;
            foreach (var opening in openingList)
            {
                openingFaceDeduction += opening.BroadFaceDeductionM2;
                openingReveal += opening.RevealAreaM2(thicknessM);
            }
            RequireFiniteNonNegative(openingFaceDeduction, "opening face deduction");
            RequireFiniteNonNegative(openingReveal, "opening reveal formwork");
            if (openingFaceDeduction > 2d * broadFaceArea + 1e-9d)
                throw new InvalidOperationException("Opening face deductions cannot exceed the two broad wall faces.");

            var net = gross - contactDeduction - openingFaceDeduction + openingReveal;
            if (net < -1e-9d)
                throw new InvalidOperationException("Wall formwork deductions exceed the available gross formwork.");
            if (net < 0d) net = 0d;
            RequireFiniteNonNegative(net, "net formwork");

            return new StructuralWallFormworkResult(
                gross,
                contactDeduction,
                openingFaceDeduction,
                openingReveal,
                net);
        }

        private static double UnionAreaOnFace(
            IReadOnlyList<StructuralWallConcreteContactPatch> contacts,
            StructuralWallFormworkFace face,
            double faceWidthM,
            double heightM)
        {
            var rectangles = new List<Rectangle>();
            foreach (var contact in contacts)
            {
                if (contact.Face != face) continue;
                var u0 = Math.Max(0d, Math.Min(contact.U0M, contact.U1M));
                var u1 = Math.Min(faceWidthM, Math.Max(contact.U0M, contact.U1M));
                var z0 = Math.Max(0d, Math.Min(contact.Z0M, contact.Z1M));
                var z1 = Math.Min(heightM, Math.Max(contact.Z0M, contact.Z1M));
                if (!(u1 > u0) || !(z1 > z0)) continue;
                rectangles.Add(new Rectangle(u0, u1, z0, z1));
            }
            if (rectangles.Count == 0) return 0d;

            var cuts = rectangles.SelectMany(x => new[] { x.U0, x.U1 }).Distinct().OrderBy(x => x).ToArray();
            var area = 0d;
            for (var i = 0; i + 1 < cuts.Length; i++)
            {
                var left = cuts[i];
                var right = cuts[i + 1];
                if (!(right > left)) continue;

                var intervals = rectangles
                    .Where(x => x.U0 < right && x.U1 > left)
                    .Select(x => new Interval(x.Z0, x.Z1))
                    .OrderBy(x => x.Start)
                    .ThenBy(x => x.End)
                    .ToArray();
                if (intervals.Length == 0) continue;

                var covered = 0d;
                var start = intervals[0].Start;
                var end = intervals[0].End;
                for (var j = 1; j < intervals.Length; j++)
                {
                    var next = intervals[j];
                    if (next.Start <= end)
                    {
                        end = Math.Max(end, next.End);
                        continue;
                    }
                    covered += end - start;
                    start = next.Start;
                    end = next.End;
                }
                covered += end - start;
                area += (right - left) * covered;
            }
            return area;
        }

        private static void RequirePositiveFinite(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || !(value > 0d))
                throw new ArgumentOutOfRangeException(name, "Wall dimension must be finite and positive.");
        }

        private static void RequireFiniteNonNegative(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                throw new InvalidOperationException(label + " must be finite and non-negative.");
        }

        private sealed class Rectangle
        {
            public Rectangle(double u0, double u1, double z0, double z1)
            {
                U0 = u0;
                U1 = u1;
                Z0 = z0;
                Z1 = z1;
            }
            public double U0 { get; }
            public double U1 { get; }
            public double Z0 { get; }
            public double Z1 { get; }
        }

        private sealed class Interval
        {
            public Interval(double start, double end)
            {
                Start = start;
                End = end;
            }
            public double Start { get; }
            public double End { get; }
        }
    }

    public static class StructuralWallFormworkQuantityWriter
    {
        public const string GrossFormworkM2 = "GrossFormworkM2";
        public const string ConcreteContactDeductionM2 = "ConcreteContactDeductionM2";
        public const string OpeningFaceDeductionM2 = "OpeningFormworkFaceDeductionM2";
        public const string OpeningRevealM2 = "OpeningRevealFormworkM2";
        public const string OpeningRevealAdjustmentM2 = "OpeningRevealAdjustmentM2";
        public const string NetFormworkM2 = "FormworkM2";

        public static void Persist(ProjectElement wall, StructuralWallFormworkResult result)
        {
            if (wall == null) throw new ArgumentNullException(nameof(wall));
            if (result == null) throw new ArgumentNullException(nameof(result));

            wall.SetQuantity(GrossFormworkM2, result.GrossFormworkM2);
            wall.SetQuantity(ConcreteContactDeductionM2, result.ConcreteContactDeductionM2);
            wall.SetQuantity(OpeningFaceDeductionM2, result.OpeningFaceDeductionM2);
            wall.SetQuantity(OpeningRevealM2, result.OpeningRevealM2);
            wall.SetQuantity(OpeningRevealAdjustmentM2, Math.Abs(result.OpeningRevealAdjustmentM2));
            wall.SetQuantity(NetFormworkM2, result.FormworkM2);
        }
    }
}
