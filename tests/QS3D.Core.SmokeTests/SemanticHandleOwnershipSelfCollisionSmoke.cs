using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticHandleOwnershipSelfCollisionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            SourceAndGeneratedSelfCollisionFailsClosed();
            HostSolidAliasesRemainOneLogicalOwner();
            DistinctGeneratedSlotsSelfCollisionFailsClosed();
            DistinctElementsStillConflict();
        }

        private static void SourceAndGeneratedSelfCollisionFailsClosed()
        {
            var project = NewProject("source-generated");
            var element = NewElement("E-1");
            element.SourceHandles.Add("A1");
            element.Properties["GeneratedSolidHandle"] = "A1";
            project.Elements.Add(element);
            var beforeVersion = project.ChangeVersion;

            ExpectConflict(project, "A1", "SourceHandles", "GeneratedSolidHandle");
            RequireReadOnly(project, beforeVersion, "source/generated self-collision");
        }

        private static void HostSolidAliasesRemainOneLogicalOwner()
        {
            var project = NewProject("host-alias");
            var element = NewElement("E-1");
            element.Properties["GeneratedSolidHandle"] = "A1";
            element.Properties["PhysicalOpeningCutSolidHandle"] = "A1";
            project.Elements.Add(element);
            var beforeVersion = project.ChangeVersion;

            var resolved = SemanticHandleOwnershipResolver.Resolve(project, new[] { "A1" });
            if (resolved.Count != 1 || !ReferenceEquals(resolved[0], element))
                throw new InvalidOperationException("Host-solid ownership aliases must resolve to one semantic owner.");
            RequireReadOnly(project, beforeVersion, "host-solid logical alias resolution");
        }

        private static void DistinctGeneratedSlotsSelfCollisionFailsClosed()
        {
            var project = NewProject("generated-generated");
            var element = NewElement("E-1");
            element.Properties["GeneratedSolidHandle"] = "A1";
            element.Properties["GeneratedRebarHandles"] = "A1";
            project.Elements.Add(element);
            var beforeVersion = project.ChangeVersion;

            ExpectConflict(project, "A1", "GeneratedSolidHandle", "GeneratedRebarHandles");
            RequireReadOnly(project, beforeVersion, "distinct generated-slot self-collision");
        }

        private static void DistinctElementsStillConflict()
        {
            var project = NewProject("distinct-elements");
            var first = NewElement("E-1");
            var second = NewElement("E-2");
            first.SourceHandles.Add("A1");
            second.Properties["GeneratedSolidHandle"] = "A1";
            project.Elements.Add(first);
            project.Elements.Add(second);
            var beforeVersion = project.ChangeVersion;

            ExpectConflict(project, "A1", "SourceHandles", "GeneratedSolidHandle");
            RequireReadOnly(project, beforeVersion, "distinct-element ownership conflict");
        }

        private static void ExpectConflict(ProjectState project, string handle, string firstChannel, string secondChannel)
        {
            try
            {
                SemanticHandleOwnershipResolver.Resolve(project, new[] { handle });
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(firstChannel, StringComparison.OrdinalIgnoreCase) < 0 ||
                    ex.Message.IndexOf(secondChannel, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException("Semantic handle conflict did not identify both ownership channels.", ex);
                return;
            }
            throw new InvalidOperationException("Semantic handle ownership conflict was silently accepted.");
        }

        private static ProjectState NewProject(string suffix) =>
            new ProjectState("P-HANDLE-SELF-" + suffix, "Semantic handle self-collision");

        private static ProjectElement NewElement(string id) =>
            new ProjectElement(id, ElementCategory.ArchitecturalWall);

        private static void RequireReadOnly(ProjectState project, long beforeVersion, string label)
        {
            if (project.ChangeVersion != beforeVersion)
                throw new InvalidOperationException(label + " mutated project semantic revision.");
        }
    }
}
