using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticHandleBoundaryOwnershipBoundSmoke
    {
        private const int MaxBoundarySourceHandleCount = 5000;

        [ModuleInitializer]
        internal static void Initialize()
        {
            ExactBoundaryCountRemainsSelectable();
            OverLimitBoundaryMetadataFailsClosed();
            NonCanonicalBoundaryMetadataFailsClosed();
            ExplicitSourceHandlesStillSuppressBoundaryAliases();
        }

        private static void ExactBoundaryCountRemainsSelectable()
        {
            var project = NewProject();
            var room = AutoRoom("ROOM-BOUND", BoundaryHandles(MaxBoundarySourceHandleCount));
            project.Elements.Add(room);

            var resolved = SemanticHandleOwnershipResolver.Resolve(project, new[] { "1388" });
            if (resolved.Count != 1 || !ReferenceEquals(resolved[0], room))
                throw new InvalidOperationException("Semantic boundary ownership must preserve the exact 5,000-handle Auto Room boundary contract.");
        }

        private static void OverLimitBoundaryMetadataFailsClosed()
        {
            var project = NewProject();
            project.Elements.Add(AutoRoom("ROOM-OVER", BoundaryHandles(MaxBoundarySourceHandleCount + 1)));

            ThrowsWithMessage<InvalidOperationException>(
                () => SemanticHandleOwnershipResolver.Resolve(project, new[] { "1" }),
                "cannot exceed 5000 entries");
        }

        private static void NonCanonicalBoundaryMetadataFailsClosed()
        {
            var project = NewProject();
            project.Elements.Add(AutoRoom("ROOM-NONCANON", "A;;B"));

            ThrowsWithMessage<InvalidOperationException>(
                () => SemanticHandleOwnershipResolver.Resolve(project, new[] { "A" }),
                "non-canonical BoundarySourceHandles");
        }

        private static void ExplicitSourceHandlesStillSuppressBoundaryAliases()
        {
            var project = NewProject();
            var room = AutoRoom("ROOM-EXPLICIT", BoundaryHandles(MaxBoundarySourceHandleCount + 1));
            room.SourceHandles.Add("E1");
            project.Elements.Add(room);

            if (SemanticHandleOwnershipResolver.Resolve(project, new[] { "1" }).Count != 0)
                throw new InvalidOperationException("Explicit SourceHandles must continue to suppress Auto Room boundary aliases before boundary metadata parsing.");
            var resolved = SemanticHandleOwnershipResolver.Resolve(project, new[] { "E1" });
            if (resolved.Count != 1 || !ReferenceEquals(resolved[0], room))
                throw new InvalidOperationException("Explicit SourceHandles ownership must remain selectable when dormant boundary metadata exceeds the Auto Room ceiling.");
        }

        private static string BoundaryHandles(int count)
        {
            return string.Join(";", Enumerable.Range(1, count)
                .Select(index => index.ToString("X"))
                .OrderBy(handle => handle, StringComparer.OrdinalIgnoreCase));
        }

        private static ProjectElement AutoRoom(string id, string boundaryHandles)
        {
            var room = new ProjectElement(id, ElementCategory.Room, string.Empty, string.Empty, string.Empty);
            room.Properties[AutoRoomLifecycle.BoundaryModeKey] = AutoRoomLifecycle.BoundaryModeAutoNetwork;
            room.Properties[AutoRoomLifecycle.BoundarySourceHandlesKey] = boundaryHandles;
            return room;
        }

        private static ProjectState NewProject()
        {
            return new ProjectState("semantic-boundary-ownership-bound", "Semantic Boundary Ownership Bound");
        }

        private static void ThrowsWithMessage<TException>(Action action, string messageFragment) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                if (ex.Message.IndexOf(messageFragment, StringComparison.Ordinal) < 0)
                    throw new InvalidOperationException("Unexpected semantic boundary ownership diagnostic: " + ex.Message);
                return;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + " for hostile semantic boundary ownership metadata.");
        }
    }
}
