using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.Services
{
    public static class RoomFinishSynchronizationService
    {
        private static readonly ElementCategory[] FinishCategories =
        {
            ElementCategory.FloorFinish,
            ElementCategory.Waterproofing,
            ElementCategory.Skirting,
            ElementCategory.WallFinish,
            ElementCategory.CeilingFinish
        };

        private static readonly string[] RoomMetricKeys =
        {
            "AreaM2",
            "PerimeterM",
            "HeightM",
            "OpeningAreaM2",
            "DoorWidthM"
        };

        public static IReadOnlyList<ElementCategory> Categories { get; } = Array.AsReadOnly(FinishCategories);

        public static IReadOnlyList<ProjectElement> SynchronizeExisting(ProjectState project, ProjectElement room)
        {
            ValidateRoom(project, room);
            var rollback = ProjectStateSnapshot.Capture(project);
            try
            {
                var synchronized = new List<ProjectElement>();
                foreach (var category in FinishCategories)
                {
                    var finish = RoomFinishIdentityService.FindExisting(project, room, category);
                    if (finish == null) continue;
                    SynchronizeCore(project, room, finish);
                    synchronized.Add(finish);
                }
                return synchronized.AsReadOnly();
            }
            catch (Exception operationError)
            {
                RestoreOrThrow(project, rollback, operationError, "Room finish batch synchronization");
                throw;
            }
        }

        public static void Synchronize(ProjectState project, ProjectElement room, ProjectElement finish)
        {
            ValidateRoom(project, room);
            ValidateFinish(project, finish);
            var rollback = ProjectStateSnapshot.Capture(project);
            try
            {
                SynchronizeCore(project, room, finish);
            }
            catch (Exception operationError)
            {
                RestoreOrThrow(project, rollback, operationError, "Room finish synchronization");
                throw;
            }
        }

        private static void SynchronizeCore(ProjectState project, ProjectElement room, ProjectElement finish)
        {
            ValidateRoom(project, room);
            ValidateFinish(project, finish);

            var linkedRoomId = AutoRoomLifecycle.ResolveRoomReferenceId(project, finish);
            if (linkedRoomId.Length > 0 && !string.Equals(linkedRoomId, room.Id, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Room finish " + finish.Id + " references another Room: " + linkedRoomId + ".");

            finish.FloorId = room.FloorId;
            finish.ZoneId = room.ZoneId;
            finish.DrawingFingerprint = room.DrawingFingerprint;
            finish.Properties[AutoRoomLifecycle.RoomSourceIdKey] = room.Id;
            EnsureSingleRoomDependency(finish, room.Id);

            foreach (var key in RoomMetricKeys)
                ReplaceMetric(room, finish, key);

            finish.MarkDirty(ElementDirtyFlags.All);
            project.Touch();
        }

        private static void ValidateRoom(ProjectState project, ProjectElement room)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (room == null) throw new ArgumentNullException(nameof(room));
            if (room.Category != ElementCategory.Room)
                throw new ArgumentException("Source element must be a Room.", nameof(room));
            EnsureOwned(project, room, nameof(room));
            if (AutoRoomLifecycle.IsStaleAutoRoom(room))
                throw new InvalidOperationException("Cannot synchronize HT_Phòng from stale AutoRoom " + room.Id + ".");
        }

        private static void ValidateFinish(ProjectState project, ProjectElement finish)
        {
            if (finish == null) throw new ArgumentNullException(nameof(finish));
            if (!AutoRoomLifecycle.IsRoomFinishCategory(finish.Category))
                throw new ArgumentException("Target element must be an HT_Phòng finish.", nameof(finish));
            EnsureOwned(project, finish, nameof(finish));
        }

        private static void EnsureOwned(ProjectState project, ProjectElement element, string parameterName)
        {
            var owned = project.FindElement(element.Id);
            if (!ReferenceEquals(owned, element))
                throw new ArgumentException("Element must be the ProjectElement instance owned by the supplied project: " + element.Id + ".", parameterName);
        }

        private static void EnsureSingleRoomDependency(ProjectElement finish, string roomId)
        {
            for (var index = finish.DependsOn.Count - 1; index >= 0; index--)
            {
                var dependency = (finish.DependsOn[index] ?? string.Empty).Trim();
                if (string.Equals(dependency, roomId, StringComparison.OrdinalIgnoreCase)) finish.DependsOn.RemoveAt(index);
            }
            finish.DependsOn.Add(roomId);
        }

        private static void RestoreOrThrow(ProjectState project, ProjectStateSnapshot rollback, Exception operationError, string operation)
        {
            try
            {
                rollback.Restore(project);
            }
            catch (Exception restoreError)
            {
                throw new InvalidOperationException(
                    operation + " failed and project rollback also failed.",
                    new AggregateException(operationError, restoreError));
            }
        }

        private static void ReplaceMetric(ProjectElement room, ProjectElement finish, string key)
        {
            if (TryMetric(room, key, out var value))
            {
                finish.Properties[key] = value.ToString("R", CultureInfo.InvariantCulture);
                return;
            }
            finish.Properties.Remove(key);
        }

        private static bool TryMetric(ProjectElement room, string key, out double value)
        {
            if (room.Properties.TryGetValue(key, out var raw) && !string.IsNullOrWhiteSpace(raw))
            {
                if (!double.TryParse(raw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
                    double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                    throw new InvalidOperationException(room.Id + "/" + key + " must be a finite non-negative invariant number.");
                return true;
            }

            if (room.Quantities.TryGetValue(key, out value))
            {
                if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                    throw new InvalidOperationException(room.Id + "/" + key + " quantity must be finite and non-negative.");
                return true;
            }

            value = 0d;
            return false;
        }
    }
}
