using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using QS3D.Core.Domain;

namespace QS3D.Core.Services
{
    public sealed class AutomaticRoomLifecycleResult
    {
        public IList<string> RemovedRoomIds { get; } = new List<string>();
        public IList<string> RemovedDependentIds { get; } = new List<string>();
        public IList<string> RetainedStaleRoomIds { get; } = new List<string>();
    }

    public static class AutomaticRoomLifecycleService
    {
        private static readonly HashSet<ElementCategory> GeneratedFinishCategories = new HashSet<ElementCategory>
        {
            ElementCategory.FloorFinish,
            ElementCategory.Waterproofing,
            ElementCategory.Skirting,
            ElementCategory.WallFinish,
            ElementCategory.CeilingFinish
        };

        public static bool IsManaged(ProjectElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (element.Category != ElementCategory.Room) return false;
            if (element.Properties.TryGetValue("BoundaryMode", out var mode) && string.Equals(mode, "AutoNetwork", StringComparison.OrdinalIgnoreCase)) return true;
            return element.Properties.TryGetValue("AutoBoundaryManaged", out var managed) && string.Equals(managed, "true", StringComparison.OrdinalIgnoreCase);
        }

        public static string NormalizeSourceSignature(IEnumerable<string> sourceIds)
        {
            if (sourceIds == null) throw new ArgumentNullException(nameof(sourceIds));
            return string.Join("|", sourceIds
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim().ToUpperInvariant())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal));
        }

        public static string GetSourceSignature(ProjectElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (element.SourceHandles.Count > 0) return NormalizeSourceSignature(element.SourceHandles);
            if (element.Properties.TryGetValue("BoundarySourceHandles", out var serialized) && !string.IsNullOrWhiteSpace(serialized))
                return NormalizeSourceSignature(serialized.Split(new[] { ';', '|', ',' }, StringSplitOptions.RemoveEmptyEntries));
            if (element.Properties.TryGetValue("AutoBoundarySourceSignature", out var legacy) && !string.IsNullOrWhiteSpace(legacy))
                return NormalizeSourceSignature(legacy.Split(new[] { ';', '|', ',' }, StringSplitOptions.RemoveEmptyEntries));
            return string.Empty;
        }

        public static string BuildStableElementId(string sourceSignature, string boundaryKey, bool disambiguateWithBoundaryKey)
        {
            var signature = NormalizeSourceSignature((sourceSignature ?? string.Empty).Split(new[] { '|', ';', ',' }, StringSplitOptions.RemoveEmptyEntries));
            var key = (boundaryKey ?? string.Empty).Trim();
            if (signature.Length == 0 && key.Length == 0) throw new ArgumentException("Automatic room identity requires source handles or a boundary key.");
            var material = signature.Length == 0 ? key : signature;
            if (disambiguateWithBoundaryKey && key.Length > 0) material += "|BOUNDARY|" + key;
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(material));
                return "ROOMAUTO-" + BitConverter.ToString(hash, 0, 8).Replace("-", string.Empty);
            }
        }

        public static AutomaticRoomLifecycleResult ReconcileStale(ProjectState project, IEnumerable<string> currentRoomIds, IEnumerable<string> selectedSourceIds)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (currentRoomIds == null) throw new ArgumentNullException(nameof(currentRoomIds));
            if (selectedSourceIds == null) throw new ArgumentNullException(nameof(selectedSourceIds));

            var current = new HashSet<string>(currentRoomIds.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()), StringComparer.OrdinalIgnoreCase);
            var selected = new HashSet<string>(selectedSourceIds.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()), StringComparer.OrdinalIgnoreCase);
            var result = new AutomaticRoomLifecycleResult();
            if (selected.Count == 0) return result;

            var staleRooms = project.Elements
                .Where(x => IsManaged(x) && !current.Contains(x.Id) && SourceHandles(x).Any(selected.Contains))
                .ToList();

            var changed = false;
            foreach (var room in staleRooms)
            {
                var removalIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { room.Id };
                var queue = new Queue<string>();
                queue.Enqueue(room.Id);
                var protectedDependency = false;

                while (queue.Count > 0 && !protectedDependency)
                {
                    var parentId = queue.Dequeue();
                    var dependents = project.Elements
                        .Where(x => x.DependsOn.Any(id => string.Equals(id, parentId, StringComparison.OrdinalIgnoreCase)))
                        .ToList();
                    foreach (var dependent in dependents)
                    {
                        if (removalIds.Contains(dependent.Id)) continue;
                        if (!GeneratedFinishCategories.Contains(dependent.Category))
                        {
                            protectedDependency = true;
                            break;
                        }
                        removalIds.Add(dependent.Id);
                        queue.Enqueue(dependent.Id);
                    }
                }

                if (protectedDependency)
                {
                    room.Properties["AutoBoundaryStale"] = "true";
                    room.MarkDirty(ElementDirtyFlags.Properties | ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity);
                    result.RetainedStaleRoomIds.Add(room.Id);
                    changed = true;
                    continue;
                }

                var dependentsToRemove = project.Elements.Where(x => removalIds.Contains(x.Id) && !string.Equals(x.Id, room.Id, StringComparison.OrdinalIgnoreCase)).ToList();
                foreach (var dependent in dependentsToRemove)
                {
                    project.Elements.Remove(dependent);
                    result.RemovedDependentIds.Add(dependent.Id);
                }
                project.Elements.Remove(room);
                result.RemovedRoomIds.Add(room.Id);
                changed = true;
            }

            if (changed) project.Touch();
            return result;
        }

        private static IEnumerable<string> SourceHandles(ProjectElement element)
        {
            if (element.SourceHandles.Count > 0) return element.SourceHandles;
            if (element.Properties.TryGetValue("BoundarySourceHandles", out var serialized) && !string.IsNullOrWhiteSpace(serialized))
                return serialized.Split(new[] { ';', '|', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());
            return Array.Empty<string>();
        }
    }
}
