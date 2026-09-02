using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using QS3D.Core.Units;

namespace QS3D.Core.Domain
{
    public enum ProjectOnboardingStatus
    {
        NeedsUnitConfirmation,
        NeedsMaterialConfirmation,
        ReadyForFirstObject
    }

    public sealed class ProjectOnboardingRequest
    {
        private readonly IReadOnlyDictionary<ElementCategory, string> _starterMaterials;

        public ProjectOnboardingRequest(
            LengthUnit? nativeDrawingUnit,
            LengthUnit? confirmedDrawingUnit,
            IDictionary<ElementCategory, string>? starterMaterials)
        {
            NativeDrawingUnit = nativeDrawingUnit;
            ConfirmedDrawingUnit = confirmedDrawingUnit;
            var copy = new Dictionary<ElementCategory, string>();
            if (starterMaterials != null)
                foreach (var pair in starterMaterials)
                    copy[pair.Key] = pair.Value ?? string.Empty;
            _starterMaterials = copy;
        }

        public LengthUnit? NativeDrawingUnit { get; }
        public LengthUnit? ConfirmedDrawingUnit { get; }
        public IReadOnlyDictionary<ElementCategory, string> StarterMaterials => _starterMaterials;
    }

    public sealed class ProjectOnboardingResult
    {
        internal ProjectOnboardingResult(
            ProjectOnboardingStatus status,
            LengthUnit? effectiveDrawingUnit,
            IEnumerable<ElementCategory> missingMaterialCategories,
            IEnumerable<string> createdFamilyIds,
            IEnumerable<string> reusedFamilyIds,
            string activeFloorId)
        {
            Status = status;
            EffectiveDrawingUnit = effectiveDrawingUnit;
            MissingMaterialCategories = missingMaterialCategories.ToList().AsReadOnly();
            CreatedFamilyIds = createdFamilyIds.ToList().AsReadOnly();
            ReusedFamilyIds = reusedFamilyIds.ToList().AsReadOnly();
            ActiveFloorId = activeFloorId ?? string.Empty;
        }

        public ProjectOnboardingStatus Status { get; }
        public LengthUnit? EffectiveDrawingUnit { get; }
        public IReadOnlyList<ElementCategory> MissingMaterialCategories { get; }
        public IReadOnlyList<string> CreatedFamilyIds { get; }
        public IReadOnlyList<string> ReusedFamilyIds { get; }
        public string ActiveFloorId { get; }
        public bool IsReady => Status == ProjectOnboardingStatus.ReadyForFirstObject;
        public string NextAuthoringAction => ProjectOnboardingService.FirstAuthoringAction;
        public string NextQuantityAction => ProjectOnboardingService.FirstQuantityAction;
    }

    /// <summary>
    /// Bounded first-run bootstrap for a QS project. This class intentionally orchestrates the
    /// existing project, Floor, Family and drawing-unit contracts instead of introducing a second store.
    /// It never authors model geometry or quantities; those remain owned by their dedicated workflows.
    /// </summary>
    public static class ProjectOnboardingService
    {
        public const string FirstAuthoringAction = "Tạo mới";
        public const string FirstQuantityAction = "Khối lượng";
        public const string StarterFloorId = "starter-floor-1";
        public const string StarterFloorName = "Tầng 1";

        private const int MaxFamilies = 10000;
        private const string MaterialKey = "Material";
        private const string UnconfirmedMaterialPlaceholder = "Khác";

        private static readonly ElementCategory[] Categories =
        {
            ElementCategory.ArchitecturalWall,
            ElementCategory.Beam,
            ElementCategory.Column,
            ElementCategory.Slab,
            ElementCategory.StructuralWall,
            ElementCategory.Foundation
        };

        private sealed class StarterPlan
        {
            public ElementCategory Category { get; set; }
            public string Material { get; set; } = string.Empty;
            public IReadOnlyDictionary<string, string> Values { get; set; } =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            public ProjectFamily? ReusedFamily { get; set; }
        }

        public static IReadOnlyList<ElementCategory> StarterCategories => Array.AsReadOnly(Categories);

        public static ProjectOnboardingResult Bootstrap(ProjectState project, ProjectOnboardingRequest request)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (request == null) throw new ArgumentNullException(nameof(request));
            ValidateProjectCollections(project);

            if (!TryResolveEffectiveUnit(project, request, out var effectiveUnit, out var needsOverride))
                return Result(ProjectOnboardingStatus.NeedsUnitConfirmation, null, Array.Empty<ElementCategory>(), project);

            DrawingUnitResolutionPolicy.ValidateQuantityCompatibility(
                project.Metadata,
                project.Elements.Count > 0,
                effectiveUnit);

            var plans = new List<StarterPlan>();
            var missingMaterials = new List<ElementCategory>();
            foreach (var category in Categories)
            {
                var values = DefaultValues(category);
                var explicitMaterial = ReadExplicitMaterial(request.StarterMaterials, category);
                var dimensionMatches = ProjectFamilyQuickSchemaService.FindIdentityMatches(project, category, values, string.Empty);

                if (explicitMaterial.Length == 0)
                {
                    var trusted = dimensionMatches.Where(HasTrustedMaterial).ToList();
                    if (trusted.Count == 1)
                    {
                        plans.Add(new StarterPlan
                        {
                            Category = category,
                            Material = TrustedMaterial(trusted[0]),
                            Values = values,
                            ReusedFamily = trusted[0]
                        });
                    }
                    else missingMaterials.Add(category);
                    continue;
                }

                ValidateMaterial(explicitMaterial, category);
                var exact = ProjectFamilyQuickSchemaService.FindIdentityMatches(project, category, values, explicitMaterial);
                if (exact.Count > 1)
                    throw new InvalidOperationException(
                        "Starter onboarding found multiple exact Family matches for " + category +
                        ". Repair the Family catalog before onboarding so canonical reuse is unambiguous.");
                plans.Add(new StarterPlan
                {
                    Category = category,
                    Material = explicitMaterial,
                    Values = values,
                    ReusedFamily = exact.Count == 1 ? exact[0] : null
                });
            }

            if (missingMaterials.Count > 0)
                return Result(ProjectOnboardingStatus.NeedsMaterialConfirmation, effectiveUnit, missingMaterials, project);

            var existingFloorToActivate = ResolveExistingFloorActivationPlan(project);
            var createCount = plans.Count(x => x.ReusedFamily == null);
            if (project.Families.Count + createCount > MaxFamilies)
                throw new InvalidOperationException("Starter onboarding would exceed the supported 10000 Family limit.");

            RequireRevisionCapacity(project, needsOverride, existingFloorToActivate, plans);

            // All user decisions and catalog preconditions are validated before the first mutation.
            if (needsOverride)
                DrawingUnitResolutionPolicy.SetProjectOverride(project.Metadata, effectiveUnit);

            if (project.Floors.Count == 0)
                ProjectFloorService.Create(project, StarterFloorId, StarterFloorName, 0d);
            else if (existingFloorToActivate != null)
                ProjectFloorService.SetActive(project, existingFloorToActivate.Id);

            var created = new List<string>();
            var reused = new List<string>();
            foreach (var plan in plans)
            {
                if (plan.ReusedFamily != null)
                {
                    reused.Add(plan.ReusedFamily.Id);
                    continue;
                }

                var name = ProjectFamilyQuickSchemaService.MakeUniqueName(
                    project,
                    plan.Category,
                    ProjectFamilyQuickSchemaService.SuggestName(plan.Category, plan.Values));
                var family = ProjectFamilyService.Create(project, MakeUniqueFamilyId(project, plan.Category), name, plan.Category);
                foreach (var pair in plan.Values)
                    ProjectFamilyService.SetProperty(project, family.Id, pair.Key, pair.Value);
                ProjectFamilyService.SetProperty(project, family.Id, MaterialKey, plan.Material);
                created.Add(family.Id);
            }

            return new ProjectOnboardingResult(
                ProjectOnboardingStatus.ReadyForFirstObject,
                effectiveUnit,
                Array.Empty<ElementCategory>(),
                created,
                reused,
                project.ActiveFloorId);
        }

        private static void RequireRevisionCapacity(
            ProjectState project,
            bool needsOverride,
            FloorDefinition? existingFloorToActivate,
            IReadOnlyList<StarterPlan> plans)
        {
            long requiredAdvances = needsOverride ? 1L : 0L;

            if (project.Floors.Count == 0 || existingFloorToActivate != null)
                requiredAdvances = checked(requiredAdvances + 1L);

            foreach (var plan in plans)
            {
                if (plan.ReusedFamily != null) continue;
                // One Touch for Create, one for each new default property, and one for Material.
                requiredAdvances = checked(requiredAdvances + 2L + plan.Values.Count);
            }

            if (requiredAdvances > long.MaxValue - project.ChangeVersion)
                throw new InvalidOperationException(
                    "Starter onboarding cannot complete because the project revision has insufficient remaining capacity.");
        }

        private static bool TryResolveEffectiveUnit(
            ProjectState project,
            ProjectOnboardingRequest request,
            out LengthUnit effectiveUnit,
            out bool needsOverride)
        {
            needsOverride = false;
            if (DrawingUnitResolutionPolicy.TryResolve(request.NativeDrawingUnit, project.Metadata, out var resolved))
            {
                effectiveUnit = resolved.Unit;
                if (request.ConfirmedDrawingUnit.HasValue && request.ConfirmedDrawingUnit.Value != effectiveUnit)
                    throw new InvalidOperationException(
                        "Confirmed drawing unit " + request.ConfirmedDrawingUnit.Value +
                        " conflicts with the resolved drawing unit " + effectiveUnit + ".");
                return true;
            }

            if (!request.ConfirmedDrawingUnit.HasValue)
            {
                effectiveUnit = default(LengthUnit);
                return false;
            }

            effectiveUnit = request.ConfirmedDrawingUnit.Value;
            if (!Enum.IsDefined(typeof(LengthUnit), effectiveUnit))
                throw new ArgumentOutOfRangeException(nameof(request), "Confirmed drawing unit must be a defined LengthUnit.");
            needsOverride = true;
            return true;
        }

        private static IReadOnlyDictionary<string, string> DefaultValues(ElementCategory category)
        {
            var schema = ProjectFamilyQuickSchemaService.GetSchema(category);
            if (!schema.SupportsQuickForm)
                throw new InvalidOperationException("Starter category does not expose a canonical Family quick schema: " + category + ".");

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in schema.DefaultsM)
                values[pair.Key] = pair.Value.ToString("R", CultureInfo.InvariantCulture);
            return values;
        }

        private static string ReadExplicitMaterial(
            IReadOnlyDictionary<ElementCategory, string> materials,
            ElementCategory category)
        {
            if (!materials.TryGetValue(category, out var raw)) return string.Empty;
            var material = raw ?? string.Empty;
            if (material.Any(char.IsControl))
                throw new ArgumentException(
                    "Starter material cannot contain control characters.",
                    nameof(materials));
            return material.Trim();
        }

        private static void ValidateMaterial(string material, ElementCategory category)
        {
            if (material.Length == 0)
                throw new InvalidOperationException("Starter material must be explicitly confirmed for " + category + ".");
            if (string.Equals(material, UnconfirmedMaterialPlaceholder, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Material 'Khác' is an unconfirmed placeholder and cannot be used as an onboarding default for " + category + ".");
            if (material.Length > 1000)
                throw new ArgumentException("Starter material exceeds the supported 1000-character property value limit.", nameof(material));
            if (material.Any(char.IsControl))
                throw new ArgumentException("Starter material cannot contain control characters.", nameof(material));
            try { XmlConvert.VerifyXmlChars(material); }
            catch (XmlException ex)
            {
                throw new ArgumentException("Starter material contains characters that are invalid in XML.", nameof(material), ex);
            }
        }

        private static bool HasTrustedMaterial(ProjectFamily family)
        {
            if (family == null) return false;
            if (!family.Properties.TryGetValue(MaterialKey, out var raw)) return false;
            var material = raw ?? string.Empty;
            if (material.Any(char.IsControl)) return false;
            material = material.Trim();
            try
            {
                ValidateMaterial(material, family.Category);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private static string TrustedMaterial(ProjectFamily family)
        {
            if (!HasTrustedMaterial(family)) return string.Empty;
            return (family.Properties[MaterialKey] ?? string.Empty).Trim();
        }

        private static FloorDefinition? ResolveExistingFloorActivationPlan(ProjectState project)
        {
            var rawActiveFloorId = project.ActiveFloorId ?? string.Empty;
            var activeFloorId = rawActiveFloorId.Trim();
            if (activeFloorId.Length > 0)
            {
                var activeFloor = project.Floors.SingleOrDefault(
                    x => string.Equals(x.Id, activeFloorId, StringComparison.OrdinalIgnoreCase));
                if (activeFloor == null)
                    throw new InvalidOperationException(
                        "Project active Floor '" + activeFloorId + "' was not found in the current Floor catalog. Repair the active Floor reference before onboarding.");

                return string.Equals(rawActiveFloorId, activeFloor.Id, StringComparison.Ordinal)
                    ? null
                    : activeFloor;
            }

            if (project.Floors.Count == 0) return null;
            if (project.Floors.Count == 1) return project.Floors[0];

            throw new InvalidOperationException(
                "Project contains multiple Floors but no active Floor is selected. Select the intended active Floor before onboarding.");
        }

        private static string MakeUniqueFamilyId(ProjectState project, ElementCategory category)
        {
            var baseId = "starter-" + CategoryToken(category);
            if (!project.Families.Any(x => string.Equals(x.Id, baseId, StringComparison.OrdinalIgnoreCase))) return baseId;
            for (var index = 2; index <= MaxFamilies; index++)
            {
                var candidate = baseId + "-" + index.ToString(CultureInfo.InvariantCulture);
                if (!project.Families.Any(x => string.Equals(x.Id, candidate, StringComparison.OrdinalIgnoreCase))) return candidate;
            }
            throw new InvalidOperationException("Cannot allocate a unique starter Family id for " + category + ".");
        }

        private static string CategoryToken(ElementCategory category)
        {
            switch (category)
            {
                case ElementCategory.ArchitecturalWall: return "wall";
                case ElementCategory.Beam: return "beam";
                case ElementCategory.Column: return "column";
                case ElementCategory.Slab: return "slab";
                case ElementCategory.StructuralWall: return "structural-wall";
                case ElementCategory.Foundation: return "foundation";
                default: throw new ArgumentOutOfRangeException(nameof(category));
            }
        }

        private static void ValidateProjectCollections(ProjectState project)
        {
            if (project.Families.Any(x => x == null))
                throw new InvalidOperationException("Project Family collection contains a null Family.");
            var familyIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var family in project.Families)
                if (!familyIds.Add(family.Id))
                    throw new InvalidOperationException("Project contains duplicate Family id: " + family.Id + ". Repair the catalog before onboarding.");

            if (project.Floors.Any(x => x == null))
                throw new InvalidOperationException("Project Floor collection contains a null Floor.");
            var floorIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var floor in project.Floors)
                if (!floorIds.Add(floor.Id))
                    throw new InvalidOperationException("Project contains duplicate Floor id: " + floor.Id + ". Repair the project before onboarding.");
        }

        private static ProjectOnboardingResult Result(
            ProjectOnboardingStatus status,
            LengthUnit? unit,
            IEnumerable<ElementCategory> missingMaterials,
            ProjectState project) =>
            new ProjectOnboardingResult(
                status,
                unit,
                missingMaterials,
                Array.Empty<string>(),
                Array.Empty<string>(),
                project.ActiveFloorId);
    }
}
