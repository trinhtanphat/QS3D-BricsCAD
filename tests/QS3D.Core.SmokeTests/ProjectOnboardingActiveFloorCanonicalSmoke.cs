using System;
using System.Collections.Generic;
using QS3D.Core.Domain;
using QS3D.Core.Units;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectOnboardingActiveFloorCanonicalSmoke
    {
        internal static void Run()
        {
            RejectsWhitespaceBeforeOnboarding();
            CanonicalizesReachableCaseVariant();
            PreservesExactCanonicalIdentity();
        }

        private static void RejectsWhitespaceBeforeOnboarding()
        {
            var project = new ProjectState("P-ONBOARD-PADDED-FLOOR", "padded active floor");
            ProjectFloorService.Create(project, "Floor-Main", "Main Floor", 0d);
            var canonical = project.ActiveFloorId;

            Throws<ArgumentException>(() => project.ActiveFloorId = "  floor-main  ",
                "Public ProjectState assignment must reject padded active Floor identity before onboarding can observe it.");
            Equal(canonical, project.ActiveFloorId,
                "Rejected padded active Floor identity must not mutate the canonical active Floor.");
        }

        private static void CanonicalizesReachableCaseVariant()
        {
            var project = new ProjectState("P-ONBOARD-CANONICAL-FLOOR", "canonical active floor");
            ProjectFloorService.Create(project, "Floor-Main", "Main Floor", 0d);
            project.ActiveFloorId = "floor-main";

            var result = ProjectOnboardingService.Bootstrap(
                project,
                new ProjectOnboardingRequest(LengthUnit.Millimeter, null, Materials()));

            Equal(ProjectOnboardingStatus.ReadyForFirstObject, result.Status,
                "A case-variant active Floor token should allow onboarding to become ready.");
            Equal("Floor-Main", project.ActiveFloorId,
                "Ready ProjectState must store the exact canonical Floor catalog identity.");
            Equal("Floor-Main", result.ActiveFloorId,
                "Ready result must expose the exact canonical Floor catalog identity.");
            Equal(1, project.Floors.Count,
                "Canonicalizing the active Floor identity must not replace or duplicate the Floor catalog.");
        }

        private static void PreservesExactCanonicalIdentity()
        {
            var project = new ProjectState("P-ONBOARD-EXACT-FLOOR", "exact active floor");
            ProjectFloorService.Create(project, "floor-exact", "Exact Floor", 0d);
            var exactIdentity = project.ActiveFloorId;

            var result = ProjectOnboardingService.Bootstrap(
                project,
                new ProjectOnboardingRequest(LengthUnit.Millimeter, null, Materials()));

            Equal(ProjectOnboardingStatus.ReadyForFirstObject, result.Status,
                "An exact active Floor identity should remain ready.");
            Equal(exactIdentity, project.ActiveFloorId,
                "Exact canonical active Floor identity must remain unchanged.");
            Equal(exactIdentity, result.ActiveFloorId,
                "Ready result must preserve an already-canonical active Floor identity.");
        }

        private static Dictionary<ElementCategory, string> Materials()
        {
            return new Dictionary<ElementCategory, string>
            {
                [ElementCategory.ArchitecturalWall] = "Masonry",
                [ElementCategory.Beam] = "Concrete C30",
                [ElementCategory.Column] = "Concrete C30",
                [ElementCategory.Slab] = "Concrete C30",
                [ElementCategory.StructuralWall] = "Concrete C30",
                [ElementCategory.Foundation] = "Concrete C30"
            };
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<TException>(Action action, string message)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new Exception(message + " Expected exception=" + typeof(TException).Name + ".");
        }
    }
}
