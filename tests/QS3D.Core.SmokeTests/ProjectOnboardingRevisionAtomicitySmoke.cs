using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Units;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectOnboardingRevisionAtomicitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsInsufficientRevisionCapacityBeforeMutation();
            AcceptsExactRevisionCapacity();
            RejectsBoundUnitMetadataInsufficientCapacityBeforeMutation();
            AcceptsExactBoundUnitMetadataCapacity();
        }

        private static void RejectsInsufficientRevisionCapacityBeforeMutation()
        {
            var requiredAdvances = MeasureOnboardingRevisionAdvances(CreateFreshProject("MEASURE-FRESH"));
            True(requiredAdvances > 1L, "Fresh onboarding must exercise multiple revision advances.");

            var project = CreateFreshProject("REJECT-FRESH");
            SetPersistenceVersion(project, long.MaxValue - requiredAdvances + 1L);
            AssertRejectedAtomically(project, "fresh onboarding");
        }

        private static void AcceptsExactRevisionCapacity()
        {
            var requiredAdvances = MeasureOnboardingRevisionAdvances(CreateFreshProject("MEASURE-EXACT"));
            var project = CreateFreshProject("EXACT-FRESH");
            SetPersistenceVersion(project, long.MaxValue - requiredAdvances);

            var result = Bootstrap(project);

            AssertSuccessfulAtMax(project, result, "Exact onboarding revision capacity");
        }

        private static void RejectsBoundUnitMetadataInsufficientCapacityBeforeMutation()
        {
            var requiredAdvances = MeasureOnboardingRevisionAdvances(CreateBoundProject("MEASURE-BOUND"));
            var freshAdvances = MeasureOnboardingRevisionAdvances(CreateFreshProject("MEASURE-BOUND-CONTROL"));
            True(requiredAdvances > freshAdvances,
                "Bound-unit onboarding must account for the additional semantic metadata writes performed by SetProjectOverride.");

            var project = CreateBoundProject("REJECT-BOUND");
            SetPersistenceVersion(project, long.MaxValue - requiredAdvances + 1L);
            AssertRejectedAtomically(project, "bound-unit onboarding");
            False(project.Metadata.ContainsKey(DrawingUnitResolutionPolicy.EffectiveUnitMetadataKey),
                "Rejected bound-unit onboarding must not persist the effective-unit metadata.");
            False(project.Metadata.ContainsKey(DrawingUnitResolutionPolicy.BindingSourceMetadataKey),
                "Rejected bound-unit onboarding must not persist the binding-source metadata.");
        }

        private static void AcceptsExactBoundUnitMetadataCapacity()
        {
            var requiredAdvances = MeasureOnboardingRevisionAdvances(CreateBoundProject("MEASURE-BOUND-EXACT"));
            var project = CreateBoundProject("EXACT-BOUND");
            SetPersistenceVersion(project, long.MaxValue - requiredAdvances);

            var result = Bootstrap(project);

            AssertSuccessfulAtMax(project, result, "Exact bound-unit onboarding revision capacity");
            Equal(LengthUnit.Millimeter.ToString(), project.Metadata[DrawingUnitResolutionPolicy.EffectiveUnitMetadataKey],
                "Exact bound-unit onboarding effective unit");
            Equal(DrawingUnitResolutionSource.ProjectOverride.ToString(),
                project.Metadata[DrawingUnitResolutionPolicy.BindingSourceMetadataKey],
                "Exact bound-unit onboarding binding source");
        }

        private static void AssertRejectedAtomically(ProjectState project, string label)
        {
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;
            var beforeMetadata = project.Metadata.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);

            var threw = false;
            try
            {
                Bootstrap(project);
            }
            catch (InvalidOperationException ex)
            {
                threw = ex.Message.IndexOf("revision", StringComparison.OrdinalIgnoreCase) >= 0 &&
                        ex.Message.IndexOf("onboarding", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch (OverflowException)
            {
                throw new InvalidOperationException(
                    label + " exhausted ChangeVersion after beginning mutation instead of rejecting the plan atomically.");
            }

            True(threw, "Insufficient " + label + " revision capacity must fail closed before mutation.");
            Equal(beforeVersion, project.ChangeVersion, "Rejected " + label + " revision");
            Equal(beforeUpdatedUtc, project.UpdatedUtc, "Rejected " + label + " timestamp");
            Equal(0, project.Floors.Count, "Rejected " + label + " Floors");
            Equal(0, project.Families.Count, "Rejected " + label + " Families");
            Equal(string.Empty, project.ActiveFloorId, "Rejected " + label + " active Floor");
            Equal(beforeMetadata.Count, project.Metadata.Count, "Rejected " + label + " metadata count");
            foreach (var item in beforeMetadata)
            {
                True(project.Metadata.TryGetValue(item.Key, out var actual),
                    "Rejected " + label + " lost metadata key " + item.Key + ".");
                Equal(item.Value, actual, "Rejected " + label + " metadata value " + item.Key);
            }
            False(project.Metadata.ContainsKey(DrawingUnitResolutionPolicy.OverrideMetadataKey),
                "Rejected " + label + " must not persist the drawing-unit override.");
        }

        private static void AssertSuccessfulAtMax(
            ProjectState project,
            ProjectOnboardingResult result,
            string label)
        {
            Equal(ProjectOnboardingStatus.ReadyForFirstObject, result.Status, label + " status");
            Equal(long.MaxValue, project.ChangeVersion, label + " final revision");
            Equal(1, project.Floors.Count, label + " Floor count");
            Equal(6, project.Families.Count, label + " Family count");
            Equal(ProjectOnboardingService.StarterFloorId, project.ActiveFloorId, label + " active Floor");
        }

        private static long MeasureOnboardingRevisionAdvances(ProjectState project)
        {
            var before = project.ChangeVersion;
            var result = Bootstrap(project);
            Equal(ProjectOnboardingStatus.ReadyForFirstObject, result.Status,
                "Revision measurement onboarding must be ready.");
            return project.ChangeVersion - before;
        }

        private static ProjectState CreateFreshProject(string suffix)
        {
            return new ProjectState("P-ONBOARD-REV-" + suffix, "revision " + suffix.ToLowerInvariant());
        }

        private static ProjectState CreateBoundProject(string suffix)
        {
            var project = CreateFreshProject(suffix);
            project.Metadata[DrawingUnitResolutionPolicy.BoundMetadataKey] = LengthUnit.Millimeter.ToString();
            return project;
        }

        private static ProjectOnboardingResult Bootstrap(ProjectState project)
        {
            return ProjectOnboardingService.Bootstrap(
                project,
                new ProjectOnboardingRequest(null, LengthUnit.Millimeter, Materials()));
        }

        private static void SetPersistenceVersion(ProjectState project, long changeVersion)
        {
            var method = typeof(ProjectState).GetMethod(
                "RestorePersistenceState",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
                throw new InvalidOperationException("Project persistence-state restore boundary was not found.");
            method.Invoke(project, new object[] { DateTime.UtcNow, changeVersion });
        }

        private static Dictionary<ElementCategory, string> Materials()
        {
            return ProjectOnboardingService.StarterCategories.ToDictionary(
                category => category,
                category => category == ElementCategory.ArchitecturalWall ? "Masonry" : "Concrete C30");
        }

        private static void True(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private static void False(bool value, string message)
        {
            if (value) throw new InvalidOperationException(message);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + ": expected " + expected + ", actual " + actual + ".");
        }
    }
}
