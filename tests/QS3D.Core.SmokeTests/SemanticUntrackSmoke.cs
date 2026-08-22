using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticUntrackSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            SourceHandleUntracksOwner();
            GeneratedHandleUntracksOwner();
            ExternalDependentBlocksUntrack();
            CompleteDependentBatchCanUntrack();
            PredicateLimitsTargets();
            DuplicateIdSameHandleFailsClosed();
            DuplicateIdAcrossSelectedHandlesFailsClosed();
        }

        private static void SourceHandleUntracksOwner()
        {
            var project = Project("source");
            var wall = Element("W1", ElementCategory.ArchitecturalWall, "10");
            project.Elements.Add(wall);
            var result = SemanticUntrackService.Untrack(project, new[] { "10" });
            Equal(1, result.Count);
            True(project.FindElement("W1") == null);
        }

        private static void GeneratedHandleUntracksOwner()
        {
            var project = Project("generated");
            var wall = Element("W1", ElementCategory.ArchitecturalWall, "10");
            wall.Properties["GeneratedSolidHandle"] = "AA";
            project.Elements.Add(wall);
            var result = SemanticUntrackService.Untrack(project, new[] { "aa" });
            Equal(1, result.Count);
            True(project.FindElement("W1") == null);
        }

        private static void ExternalDependentBlocksUntrack()
        {
            var project = Project("blocked");
            var wall = Element("W1", ElementCategory.ArchitecturalWall, "10");
            var door = Element("D1", ElementCategory.Door, "20");
            door.DependsOn.Add(wall.Id);
            project.Elements.Add(wall);
            project.Elements.Add(door);
            Throws<InvalidOperationException>(() => SemanticUntrackService.Untrack(project, new[] { "10" }));
            True(project.FindElement("W1") != null);
            True(project.FindElement("D1") != null);
        }

        private static void CompleteDependentBatchCanUntrack()
        {
            var project = Project("batch");
            var wall = Element("W1", ElementCategory.ArchitecturalWall, "10");
            var door = Element("D1", ElementCategory.Door, "20");
            door.DependsOn.Add(wall.Id);
            project.Elements.Add(wall);
            project.Elements.Add(door);
            var result = SemanticUntrackService.Untrack(project, new[] { "10", "20" });
            Equal(2, result.Count);
            Equal(0, project.Elements.Count);
        }

        private static void PredicateLimitsTargets()
        {
            var project = Project("predicate");
            var room = Element("R1", ElementCategory.Room, "30");
            var finish = Element("F1", ElementCategory.FloorFinish, "40");
            finish.DependsOn.Add(room.Id);
            finish.Properties["GeneratedSolidHandle"] = "BB";
            project.Elements.Add(room);
            project.Elements.Add(finish);
            var result = SemanticUntrackService.Untrack(project, new[] { "30", "bb" }, x => x.Category == ElementCategory.FloorFinish);
            Equal(1, result.Count);
            True(project.FindElement("R1") != null);
            True(project.FindElement("F1") == null);
        }

        private static void DuplicateIdSameHandleFailsClosed()
        {
            var project = Project("duplicate-same-handle");
            project.Elements.Add(Element("W1", ElementCategory.ArchitecturalWall, "10"));
            project.Elements.Add(Element("W1", ElementCategory.ArchitecturalWall, "10"));
            Throws<InvalidOperationException>(() => SemanticUntrackService.Untrack(project, new[] { "10" }));
            Equal(2, project.Elements.Count);
        }

        private static void DuplicateIdAcrossSelectedHandlesFailsClosed()
        {
            var project = Project("duplicate-selected-handles");
            project.Elements.Add(Element("W1", ElementCategory.ArchitecturalWall, "10"));
            project.Elements.Add(Element("W1", ElementCategory.ArchitecturalWall, "20"));
            Throws<InvalidOperationException>(() => SemanticUntrackService.Untrack(project, new[] { "10", "20" }));
            Equal(2, project.Elements.Count);
        }

        private static ProjectState Project(string suffix) => new ProjectState("untrack-" + suffix, "Untrack " + suffix);

        private static ProjectElement Element(string id, ElementCategory category, string sourceHandle)
        {
            var element = new ProjectElement(id, category, string.Empty, string.Empty, string.Empty);
            element.SourceHandles.Add(sourceHandle);
            return element;
        }

        private static void Equal(int expected, int actual)
        {
            if (expected != actual) throw new InvalidOperationException("Semantic untrack smoke expected " + expected + " but got " + actual + ".");
        }

        private static void True(bool condition)
        {
            if (!condition) throw new InvalidOperationException("Semantic untrack smoke assertion failed.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
        }
    }
}
