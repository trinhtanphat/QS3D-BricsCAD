using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticHandleOwnershipCanonicalSourceSmoke
    {
        internal static void Run()
        {
            CanonicalStoredHandleResolvesInBothPaths();
            PaddedStoredHandleFailsClosedInBothPaths();
            BlankStoredHandleFailsClosedInBothPaths();
        }

        private static void CanonicalStoredHandleResolvesInBothPaths()
        {
            var project = NewProject("AB12");
            var element = project.Elements[0];
            var beforeVersion = project.ChangeVersion;

            var owner = SemanticHandleOwnershipResolver.ResolveUniqueSourceOwner(project, " ab12 ");
            if (!ReferenceEquals(element, owner))
                throw new Exception("Canonical stored SourceHandle must resolve to the exact project-owned semantic element.");

            var selected = SemanticHandleOwnershipResolver.Resolve(project, new[] { " ab12 " });
            Equal(1, selected.Count);
            if (!ReferenceEquals(element, selected[0]))
                throw new Exception("Selected canonical SourceHandle must resolve to the exact project-owned semantic element.");
            Equal(beforeVersion, project.ChangeVersion);
        }

        private static void PaddedStoredHandleFailsClosedInBothPaths()
        {
            var project = NewPersistedProject(" AB12 ");
            var beforeVersion = project.ChangeVersion;

            Throws<InvalidOperationException>(() => SemanticHandleOwnershipResolver.ResolveUniqueSourceOwner(project, "AB12"));
            Throws<InvalidOperationException>(() => SemanticHandleOwnershipResolver.Resolve(project, new[] { "AB12" }));
            Equal(beforeVersion, project.ChangeVersion);
        }

        private static void BlankStoredHandleFailsClosedInBothPaths()
        {
            var project = NewPersistedProject("   ");
            var beforeVersion = project.ChangeVersion;

            Throws<InvalidOperationException>(() => SemanticHandleOwnershipResolver.ResolveUniqueSourceOwner(project, "OTHER"));
            Throws<InvalidOperationException>(() => SemanticHandleOwnershipResolver.Resolve(project, new[] { "OTHER" }));
            Equal(beforeVersion, project.ChangeVersion);
        }

        private static ProjectState NewProject(string storedHandle)
        {
            var project = new ProjectState("P-SOURCE-HANDLE", "Source handle ownership");
            var element = new ProjectElement("E-1", ElementCategory.Beam);
            element.SourceHandles.Add(storedHandle);
            project.Elements.Add(element);
            return project;
        }

        private static ProjectState NewPersistedProject(string storedHandle)
        {
            var project = new ProjectState("P-SOURCE-HANDLE", "Source handle ownership");
            var element = new ProjectElement("E-1", ElementCategory.Beam);
            SeedPersistedSourceHandle(element, storedHandle);
            project.Elements.Add(element);
            return project;
        }

        private static void SeedPersistedSourceHandle(ProjectElement element, string value)
        {
            var valuesField = element.SourceHandles.GetType().GetField("_values", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Unable to seed malformed persisted SourceHandles state.");
            var values = valuesField.GetValue(element.SourceHandles) as List<string>
                ?? throw new InvalidOperationException("Unexpected ProjectElement.SourceHandles backing collection.");
            values.Add(value);
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected " + expected + " but got " + actual + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("Expected " + typeof(TException).Name + ".");
        }
    }

    internal static class SemanticHandleOwnershipCanonicalSourceSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => SemanticHandleOwnershipCanonicalSourceSmoke.Run();
    }
}
