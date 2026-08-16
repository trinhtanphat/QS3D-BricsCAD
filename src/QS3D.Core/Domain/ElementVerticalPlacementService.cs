using System;
using System.Collections.Generic;
using System.Globalization;

namespace QS3D.Core.Domain
{
    public sealed class ElementVerticalPlacement
    {
        public ElementVerticalPlacement(bool usesBottomLevel, bool usesTopLevel, double bottomElevationM, double topElevationM)
        {
            if (double.IsNaN(bottomElevationM) || double.IsInfinity(bottomElevationM)) throw new ArgumentOutOfRangeException(nameof(bottomElevationM));
            if (double.IsNaN(topElevationM) || double.IsInfinity(topElevationM)) throw new ArgumentOutOfRangeException(nameof(topElevationM));
            if (topElevationM <= bottomElevationM) throw new ArgumentOutOfRangeException(nameof(topElevationM), "Top elevation must be above bottom elevation.");
            var heightM = topElevationM - bottomElevationM;
            if (double.IsNaN(heightM) || double.IsInfinity(heightM))
                throw new ArgumentOutOfRangeException(nameof(topElevationM), "Vertical placement height must be finite.");
            UsesBottomLevel = usesBottomLevel;
            UsesTopLevel = usesTopLevel;
            BottomElevationM = CanonicalZero(bottomElevationM);
            TopElevationM = CanonicalZero(topElevationM);
            HeightM = heightM;
        }

        public bool UsesBottomLevel { get; }
        public bool UsesTopLevel { get; }
        public double BottomElevationM { get; }
        public double TopElevationM { get; }
        public double HeightM { get; }

        private static double CanonicalZero(double value) => value == 0d ? 0d : value;
    }

    public sealed class HostedOpeningVerticalPlacement
    {
        public HostedOpeningVerticalPlacement(
            ElementVerticalPlacement host,
            ElementVerticalPlacement opening,
            double relativeSillM)
        {
            Host = host ?? throw new ArgumentNullException(nameof(host));
            Opening = opening ?? throw new ArgumentNullException(nameof(opening));
            if (double.IsNaN(relativeSillM) || double.IsInfinity(relativeSillM) || relativeSillM < 0d)
                throw new ArgumentOutOfRangeException(nameof(relativeSillM));
            RelativeSillM = relativeSillM == 0d ? 0d : relativeSillM;
        }

        public ElementVerticalPlacement Host { get; }
        public ElementVerticalPlacement Opening { get; }
        public double RelativeSillM { get; }
    }

    public static class ElementVerticalPlacementService
    {
        public static double ResolveEffectiveHeight(
            ProjectState project,
            ProjectElement element,
            double legacyHeightM)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (LevelReferenceProperty(element, ProjectFloorService.BottomLevelIdKey).Length == 0 &&
                LevelReferenceProperty(element, ProjectFloorService.TopLevelIdKey).Length == 0 &&
                !HasConfiguredProperty(element, ProjectFloorService.BottomLevelOffsetKey) &&
                !HasConfiguredProperty(element, ProjectFloorService.TopLevelOffsetKey))
                return Positive(legacyHeightM, nameof(legacyHeightM));
            return Resolve(project, element, 0d, legacyHeightM, 0d).HeightM;
        }

        public static HostedOpeningVerticalPlacement ResolveHostedOpening(
            ProjectState project,
            ProjectElement host,
            ProjectElement opening,
            double hostSourceBaseElevationM,
            double hostLegacyHeightM,
            double hostLegacyBottomOffsetM,
            double openingLegacyHeightM,
            double openingLegacySillM)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (host == null) throw new ArgumentNullException(nameof(host));
            if (opening == null) throw new ArgumentNullException(nameof(opening));

            var hostPlacement = Resolve(
                project,
                host,
                hostSourceBaseElevationM,
                hostLegacyHeightM,
                hostLegacyBottomOffsetM);
            return ResolveHostedOpening(
                project,
                hostPlacement,
                opening,
                openingLegacyHeightM,
                openingLegacySillM);
        }

        public static HostedOpeningVerticalPlacement ResolveHostedOpening(
            ProjectState project,
            ElementVerticalPlacement hostPlacement,
            ProjectElement opening,
            double openingLegacyHeightM,
            double openingLegacySillM)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (hostPlacement == null) throw new ArgumentNullException(nameof(hostPlacement));
            if (opening == null) throw new ArgumentNullException(nameof(opening));

            var openingPlacement = Resolve(
                project,
                opening,
                hostPlacement.BottomElevationM,
                openingLegacyHeightM,
                openingLegacySillM);
            var relativeSillM = Add(
                openingPlacement.BottomElevationM,
                -hostPlacement.BottomElevationM,
                opening.Id + "/relative sill elevation");
            const double toleranceM = 1e-9d;
            if (relativeSillM < -toleranceM)
                throw new InvalidOperationException("Opening " + opening.Id + " is below its host.");
            if (openingPlacement.TopElevationM > hostPlacement.TopElevationM + toleranceM)
                throw new InvalidOperationException("Opening " + opening.Id + " exceeds the top of its host.");
            return new HostedOpeningVerticalPlacement(hostPlacement, openingPlacement, Math.Max(0d, relativeSillM));
        }

        public static bool HasAnyLevelConfiguration(ProjectElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return HasConfiguredProperty(element, ProjectFloorService.BottomLevelIdKey) ||
                   HasConfiguredProperty(element, ProjectFloorService.BottomLevelOffsetKey) ||
                   HasConfiguredProperty(element, ProjectFloorService.TopLevelIdKey) ||
                   HasConfiguredProperty(element, ProjectFloorService.TopLevelOffsetKey);
        }

        public static ElementVerticalPlacement Resolve(
            ProjectState project,
            ProjectElement element,
            double sourceBaseElevationM,
            double legacyHeightM,
            double legacyBottomOffsetM)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (element == null) throw new ArgumentNullException(nameof(element));

            var bottomLevelId = LevelReferenceProperty(element, ProjectFloorService.BottomLevelIdKey);
            var topLevelId = LevelReferenceProperty(element, ProjectFloorService.TopLevelIdKey);
            if (bottomLevelId.Length == 0)
            {
                if (topLevelId.Length > 0)
                    throw new InvalidOperationException("TopLevelId requires BottomLevelId on element " + element.Id + ".");
                if (HasConfiguredProperty(element, ProjectFloorService.BottomLevelOffsetKey) || HasConfiguredProperty(element, ProjectFloorService.TopLevelOffsetKey))
                    throw new InvalidOperationException("Level offset requires its level reference on element " + element.Id + ".");
                Finite(sourceBaseElevationM, nameof(sourceBaseElevationM));
                Positive(legacyHeightM, nameof(legacyHeightM));
                Finite(legacyBottomOffsetM, nameof(legacyBottomOffsetM));
                var bottom = Add(sourceBaseElevationM, legacyBottomOffsetM, element.Id + "/legacy bottom elevation");
                var top = Add(bottom, legacyHeightM, element.Id + "/legacy top elevation");
                return new ElementVerticalPlacement(false, false, bottom, top);
            }

            ValidateFloorIdentityCollection(project);
            var bottomLevel = FindFloor(project, bottomLevelId, element.Id + "/BottomLevelId");
            var bottomOffset = OptionalFiniteProperty(element, ProjectFloorService.BottomLevelOffsetKey, 0d);
            var bottomElevation = Add(bottomLevel.ElevationM, bottomOffset, element.Id + "/bottom level elevation");

            if (topLevelId.Length == 0)
            {
                if (HasConfiguredProperty(element, ProjectFloorService.TopLevelOffsetKey))
                    throw new InvalidOperationException("TopLevelOffsetM requires TopLevelId on element " + element.Id + ".");
                Positive(legacyHeightM, nameof(legacyHeightM));
                var top = Add(bottomElevation, legacyHeightM, element.Id + "/top elevation");
                return new ElementVerticalPlacement(true, false, bottomElevation, top);
            }

            var topLevel = FindFloor(project, topLevelId, element.Id + "/TopLevelId");
            var topOffset = OptionalFiniteProperty(element, ProjectFloorService.TopLevelOffsetKey, 0d);
            var topElevation = Add(topLevel.ElevationM, topOffset, element.Id + "/top level elevation");
            if (topElevation <= bottomElevation)
                throw new InvalidOperationException("Top level elevation must be above bottom level elevation on element " + element.Id + ".");
            return new ElementVerticalPlacement(true, true, bottomElevation, topElevation);
        }

        public static double ReadLevelOffset(ProjectElement element, string key)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Property key is required.", nameof(key));
            return OptionalFiniteProperty(element, key.Trim(), 0d);
        }

        private static FloorDefinition FindFloor(ProjectState project, string floorId, string label)
        {
            return project.FindFloor(floorId)
                ?? throw new InvalidOperationException(label + " references missing floor/level " + floorId + ".");
        }

        private static void ValidateFloorIdentityCollection(ProjectState project)
        {
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var floor in project.Floors)
            {
                if (floor == null)
                    throw new InvalidOperationException("Project floor collection contains a null floor.");
                if (!seenIds.Add(floor.Id))
                    throw new InvalidOperationException("Project contains duplicate floor id: " + floor.Id + ".");
            }
        }

        private static string LevelReferenceProperty(ProjectElement element, string key)
        {
            if (!element.Properties.TryGetValue(key, out var raw) || raw == null || raw.Length == 0)
                return string.Empty;
            var canonical = raw.Trim();
            if (!string.Equals(raw, canonical, StringComparison.Ordinal))
                throw new InvalidOperationException(element.Id + "/" + key + " must use a canonical Floor/Level reference without surrounding whitespace.");
            return canonical;
        }

        private static bool HasConfiguredProperty(ProjectElement element, string key)
        {
            return element.Properties.ContainsKey(key);
        }

        private static double OptionalFiniteProperty(ProjectElement element, string key, double fallback)
        {
            if (!element.Properties.TryGetValue(key, out var raw)) return fallback;
            if (string.IsNullOrWhiteSpace(raw) ||
                !double.TryParse(raw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidOperationException(element.Id + "/" + key + " must be a finite invariant number.");
            return CanonicalZero(value);
        }

        private static double Add(double left, double right, string label)
        {
            var value = left + right;
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new InvalidOperationException(label + " must be finite.");
            if (right != 0d && value == left)
                throw new InvalidOperationException(label + " loses a non-zero additive term due to floating-point precision.");
            return CanonicalZero(value);
        }

        private static double Finite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(parameterName, "Value must be finite.");
            return CanonicalZero(value);
        }

        private static double Positive(double value, string parameterName)
        {
            Finite(value, parameterName);
            if (value <= 0d) throw new ArgumentOutOfRangeException(parameterName, "Value must be > 0.");
            return value;
        }

        private static double CanonicalZero(double value) => value == 0d ? 0d : value;
    }
}
