using System;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SlabOpeningContractOwnershipSmoke
    {
        internal static void Run()
        {
            RejectsSameIdForeignOpeningBeforeMutation();
            RejectsForeignAndAbsentHostsBeforeMutation();
            RejectsCorruptProjectElementCollectionsBeforeMutation();
            CanonicalOwnedBindingStillSucceeds();
        }

        private static void RejectsSameIdForeignOpeningBeforeMutation()
        {
            var fixture = CreateFixture("foreign-opening");
            var foreignOpening = new ProjectElement(
                fixture.Opening.Id,
                ElementCategory.WallOpening,
                SlabOpeningContract.FamilyKey,
                string.Empty,
                string.Empty);
            foreignOpening.SetProperty("Sentinel", "keep");
            foreignOpening.DependsOn.Add("existing");
            foreignOpening.MarkClean(ElementDirtyFlags.All);

            var snapshot = Snapshot(fixture.Project, foreignOpening);
            Throws<InvalidOperationException>(() => SlabOpeningContract.Bind(fixture.Project, foreignOpening, fixture.Host));
            RequireUnchanged(fixture.Project, foreignOpening, snapshot, "same-id foreign opening");
            Require(!fixture.Opening.Properties.ContainsKey(SlabOpeningContract.ContractKey), "Foreign-opening rejection mutated the canonical project opening.");
        }

        private static void RejectsForeignAndAbsentHostsBeforeMutation()
        {
            var sameIdFixture = CreateFixture("foreign-host");
            PrepareOpeningForAtomicityCheck(sameIdFixture.Opening);
            var sameIdSnapshot = Snapshot(sameIdFixture.Project, sameIdFixture.Opening);
            var foreignHost = new ProjectElement(sameIdFixture.Host.Id, ElementCategory.Slab);

            Throws<InvalidOperationException>(() => SlabOpeningContract.Bind(sameIdFixture.Project, sameIdFixture.Opening, foreignHost));
            RequireUnchanged(sameIdFixture.Project, sameIdFixture.Opening, sameIdSnapshot, "same-id foreign host");

            var absentFixture = CreateFixture("absent-host");
            PrepareOpeningForAtomicityCheck(absentFixture.Opening);
            var absentSnapshot = Snapshot(absentFixture.Project, absentFixture.Opening);
            var absentHost = new ProjectElement("S-absent", ElementCategory.Slab);

            Throws<InvalidOperationException>(() => SlabOpeningContract.Bind(absentFixture.Project, absentFixture.Opening, absentHost));
            RequireUnchanged(absentFixture.Project, absentFixture.Opening, absentSnapshot, "absent host");
        }

        private static void RejectsCorruptProjectElementCollectionsBeforeMutation()
        {
            var nullFixture = CreateFixture("null-entry");
            PrepareOpeningForAtomicityCheck(nullFixture.Opening);
            var nullSnapshot = Snapshot(nullFixture.Project, nullFixture.Opening);
            nullFixture.Project.Elements.Add(null!);

            Throws<InvalidOperationException>(() => SlabOpeningContract.Bind(nullFixture.Project, nullFixture.Opening, nullFixture.Host));
            RequireUnchanged(nullFixture.Project, nullFixture.Opening, nullSnapshot, "null project element entry");

            var duplicateFixture = CreateFixture("duplicate-id");
            PrepareOpeningForAtomicityCheck(duplicateFixture.Opening);
            var duplicateSnapshot = Snapshot(duplicateFixture.Project, duplicateFixture.Opening);
            duplicateFixture.Project.Elements.Add(new ProjectElement(duplicateFixture.Host.Id.ToUpperInvariant(), ElementCategory.Slab));

            Throws<InvalidOperationException>(() => SlabOpeningContract.Bind(duplicateFixture.Project, duplicateFixture.Opening, duplicateFixture.Host));
            RequireUnchanged(duplicateFixture.Project, duplicateFixture.Opening, duplicateSnapshot, "duplicate project element id");
        }

        private static void CanonicalOwnedBindingStillSucceeds()
        {
            var fixture = CreateFixture("canonical");
            fixture.Opening.MarkClean(ElementDirtyFlags.All);

            SlabOpeningContract.Bind(fixture.Project, fixture.Opening, fixture.Host);

            Require(fixture.Opening.Properties.TryGetValue(SlabOpeningContract.ContractKey, out var contract) && contract == SlabOpeningContract.ContractValue,
                "Canonical binding did not write the slabOpen contract marker.");
            Require(fixture.Opening.Properties.TryGetValue(SlabOpeningContract.HostSlabIdKey, out var hostId) && hostId == fixture.Host.Id,
                "Canonical binding did not write the host Slab id.");
            Require(fixture.Opening.DependsOn.Count(x => string.Equals(x, fixture.Host.Id, StringComparison.OrdinalIgnoreCase)) == 1,
                "Canonical binding did not create exactly one host dependency.");
            Require(SlabOpeningContract.IsSlabOpening(fixture.Project, fixture.Opening),
                "Canonical binding no longer satisfies the slabOpen semantic contract.");
        }

        private static Fixture CreateFixture(string suffix)
        {
            var project = new ProjectState("slab-open-ownership-" + suffix, "Slab opening ownership");
            project.Families.Add(new ProjectFamily(
                SlabOpeningContract.FamilyKey,
                SlabOpeningContract.FamilyKey,
                ElementCategory.WallOpening));

            var opening = new ProjectElement(
                "O-" + suffix,
                ElementCategory.WallOpening,
                SlabOpeningContract.FamilyKey,
                string.Empty,
                string.Empty);
            var host = new ProjectElement("S-" + suffix, ElementCategory.Slab);
            project.Elements.Add(opening);
            project.Elements.Add(host);
            return new Fixture(project, opening, host);
        }

        private static void PrepareOpeningForAtomicityCheck(ProjectElement opening)
        {
            opening.SetProperty("Sentinel", "keep");
            opening.DependsOn.Add("existing");
            opening.MarkClean(ElementDirtyFlags.All);
        }

        private static MutationSnapshot Snapshot(ProjectState project, ProjectElement opening)
        {
            return new MutationSnapshot(
                project.ChangeVersion,
                opening.UpdatedUtc,
                opening.Dirty,
                opening.Properties.Count,
                opening.DependsOn.Count);
        }

        private static void RequireUnchanged(
            ProjectState project,
            ProjectElement opening,
            MutationSnapshot snapshot,
            string scenario)
        {
            Require(project.ChangeVersion == snapshot.ProjectChangeVersion, scenario + " changed the project revision.");
            Require(opening.UpdatedUtc == snapshot.OpeningUpdatedUtc, scenario + " changed the opening timestamp.");
            Require(opening.Dirty == snapshot.OpeningDirty, scenario + " changed the opening dirty flags.");
            Require(opening.Properties.Count == snapshot.PropertyCount, scenario + " changed opening properties.");
            Require(opening.DependsOn.Count == snapshot.DependencyCount, scenario + " changed opening dependencies.");
            Require(opening.Properties.TryGetValue("Sentinel", out var sentinel) && sentinel == "keep", scenario + " changed the sentinel property.");
            Require(!opening.Properties.ContainsKey(SlabOpeningContract.ContractKey), scenario + " wrote the slabOpen contract marker.");
            Require(!opening.Properties.ContainsKey(SlabOpeningContract.HostSlabIdKey), scenario + " wrote the host Slab id.");
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }

        private readonly struct Fixture
        {
            public Fixture(ProjectState project, ProjectElement opening, ProjectElement host)
            {
                Project = project;
                Opening = opening;
                Host = host;
            }

            public ProjectState Project { get; }
            public ProjectElement Opening { get; }
            public ProjectElement Host { get; }
        }

        private readonly struct MutationSnapshot
        {
            public MutationSnapshot(
                long projectChangeVersion,
                DateTime openingUpdatedUtc,
                ElementDirtyFlags openingDirty,
                int propertyCount,
                int dependencyCount)
            {
                ProjectChangeVersion = projectChangeVersion;
                OpeningUpdatedUtc = openingUpdatedUtc;
                OpeningDirty = openingDirty;
                PropertyCount = propertyCount;
                DependencyCount = dependencyCount;
            }

            public long ProjectChangeVersion { get; }
            public DateTime OpeningUpdatedUtc { get; }
            public ElementDirtyFlags OpeningDirty { get; }
            public int PropertyCount { get; }
            public int DependencyCount { get; }
        }
    }
}
