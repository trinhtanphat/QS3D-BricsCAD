using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Revisions;

namespace QS3D.Core.SmokeTests
{
    internal static class RevisionDependencyCanonicalCaptureSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            CanonicalDependenciesCaptureDeterministically();
            BlankDependencyFailsClosed();
            PaddedDependencyFailsClosed();
            DuplicateDependencyFailsClosed();
        }

        private static void CanonicalDependenciesCaptureDeterministically()
        {
            var project = CreateProject("revision-dependency-canonical");
            var element = project.FindElement("E1") ?? throw new Exception("Fixture element missing.");
            element.DependsOn.Add("B");
            element.DependsOn.Add("A");

            var snapshot = new RevisionService().Capture(project, "R1");
            var captured = Find(snapshot, "E1");
            if (captured.Dependencies.Count != 2 ||
                !string.Equals(captured.Dependencies[0], "A", StringComparison.Ordinal) ||
                !string.Equals(captured.Dependencies[1], "B", StringComparison.Ordinal))
                throw new Exception("Revision capture did not preserve canonical dependencies in deterministic order.");
        }

        private static void BlankDependencyFailsClosed()
        {
            var project = CreateProject("revision-dependency-blank");
            var element = project.FindElement("E1") ?? throw new Exception("Fixture element missing.");
            element.DependsOn.Add("   ");
            Throws<InvalidOperationException>(() => new RevisionService().Capture(project, "R1"));
        }

        private static void PaddedDependencyFailsClosed()
        {
            var project = CreateProject("revision-dependency-padded");
            var element = project.FindElement("E1") ?? throw new Exception("Fixture element missing.");
            element.DependsOn.Add(" A ");
            Throws<InvalidOperationException>(() => new RevisionService().Capture(project, "R1"));
        }

        private static void DuplicateDependencyFailsClosed()
        {
            var project = CreateProject("revision-dependency-duplicate");
            var element = project.FindElement("E1") ?? throw new Exception("Fixture element missing.");
            element.DependsOn.Add("A");
            element.DependsOn.Add("a");
            Throws<InvalidOperationException>(() => new RevisionService().Capture(project, "R1"));
        }

        private static ProjectState CreateProject(string id)
        {
            var project = new ProjectState(id, "Revision dependency integrity");
            project.Elements.Add(new ProjectElement("A", ElementCategory.Beam));
            project.Elements.Add(new ProjectElement("B", ElementCategory.Beam));
            project.Elements.Add(new ProjectElement("E1", ElementCategory.Beam));
            return project;
        }

        private static RevisionElementSnapshot Find(RevisionSnapshot snapshot, string elementId)
        {
            foreach (var element in snapshot.Elements)
                if (string.Equals(element.ElementId, elementId, StringComparison.Ordinal)) return element;
            throw new Exception("Captured revision element missing: " + elementId + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new Exception("Expected " + typeof(TException).Name + ".");
        }
    }
}
