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
        }

        private static void RejectsInsufficientRevisionCapacityBeforeMutation()
        {
            var requiredAdvances = MeasureFreshOnboardingRevisionAdvances();
            True(requiredAdvances > 1L, "Fresh onboarding must exercise multiple revision advances.");

            var project = new ProjectState("P-ONBOARD-REV-REJECT", "revision reject");
            SetPersistenceVersion(project, long.MaxValue - requiredAdvances + 1L);
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;

            var threw = false;
            try
            {
                ProjectOnboardingService.Bootstrap(
                    project,
                    new ProjectOnboardingRequest(null, LengthUnit.Millimeter, Materials()));
            }
            catch (InvalidOperationException ex)
            {
                threw = ex.Message.IndexOf("revision", StringComparison.OrdinalIgnoreCase) >= 0 &&
                        ex.Message.IndexOf("onboarding", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch (OverflowException)
            {
                throw new InvalidOperationException(
                    "Onboarding exhausted ChangeVersion after beginning mutation instead of rejecting the plan atomically.");
            }

            True(threw, "Insufficient onboarding revision capacity must fail closed before mutation.");
            Equal(beforeVersion, project.ChangeVersion, "Rejected onboarding revision");
            Equal(beforeUpdatedUtc, project.UpdatedUtc, "Rejected onboarding timestamp");
            Equal(0, project.Floors.Count, "Rejected onboarding Floors");
            Equal(0, project.Families.Count, "Rejected onboarding Families");
            Equal(string.Empty, project.ActiveFloorId, "Rejected onboarding active Floor");
            False(project.Metadata.ContainsKey(DrawingUnitResolutionPolicy.OverrideMetadataKey),
                "Rejected onboarding must not persist the drawing-unit override.");
        }

        private static void AcceptsExactRevisionCapacity()
        {
            var requiredAdvances = MeasureFreshOnboardingRevisionAdvances();
            var project = new ProjectState("P-ONBOARD-REV-EXACT", "revision exact");
            SetPersistenceVersion(project, long.MaxValue - requiredAdvances);

            var result = ProjectOnboardingService.Bootstrap(
                project,
                new ProjectOnboardingRequest(null, LengthUnit.Millimeter, Materials()));

            Equal(ProjectOnboardingStatus.ReadyForFirstObject, result.Status,
                "Exact onboarding revision capacity should remain accepted.");
            Equal(long.MaxValue, project.ChangeVersion,
                "Exact onboarding revision capacity should be consumed without overflow.");
            Equal(1, project.Floors.Count, "Exact-capacity onboarding Floor count");
            Equal(6, project.Families.Count, "Exact-capacity onboarding Family count");
            Equal(ProjectOnboardingService.StarterFloorId, project.ActiveFloorId,
                "Exact-capacity onboarding active Floor");
        }

        private static long MeasureFreshOnboardingRevisionAdvances()
        {
            var project = new ProjectState("P-ONBOARD-REV-MEASURE", "revision measure");
            var before = project.ChangeVersion;
            var result = ProjectOnboardingService.Bootstrap(
                project,
                new ProjectOnboardingRequest(null, LengthUnit.Millimeter, Materials()));
            Equal(ProjectOnboardingStatus.ReadyForFirstObject, result.Status,
                "Revision measurement onboarding must be ready.");
            return project.ChangeVersion - before;
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
