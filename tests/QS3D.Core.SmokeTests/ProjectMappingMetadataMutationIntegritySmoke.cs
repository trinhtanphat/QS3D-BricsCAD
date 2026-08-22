using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Mapping;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectMappingMetadataMutationIntegritySmoke
    {
        private const string Prefix = "QS3D.Mapping.v1.";

        [ModuleInitializer]
        internal static void Initialize()
        {
            DirectReservedMutationsAdvanceRevision();
            GenericMetadataTracksPublicRevision();
            InvalidAndOverflowMutationsFailBeforeWrite();
            QsdbHydrationPreservesPersistedMaxRevision();
            SnapshotRestoreBypassesSyntheticRevision();
        }

        private static void DirectReservedMutationsAdvanceRevision()
        {
            var first = Entry("map-a", ElementCategory.Room, "AreaM2", "class-a", "work-a");
            var replacement = Entry("map-a", ElementCategory.Room, "AreaM2", "class-b", "work-b");
            var project = NewProject("direct");
            var baseline = new DateTime(2026, 8, 14, 0, 0, 0, DateTimeKind.Utc);
            project.UpdatedUtc = baseline;

            project.Metadata.Add(first.Key, first.Value);
            Equal(1L, project.ChangeVersion, "direct reserved add revision");
            True(project.UpdatedUtc > baseline, "direct reserved add timestamp");
            Equal("class-a", Resolve(project).ClassificationId, "direct reserved add mapping state");

            var afterAddVersion = project.ChangeVersion;
            var afterAddUpdated = project.UpdatedUtc;
            project.Metadata[first.Key] = first.Value;
            Equal(afterAddVersion, project.ChangeVersion, "same-value reserved set revision");
            Equal(afterAddUpdated, project.UpdatedUtc, "same-value reserved set timestamp");

            project.Metadata[first.Key] = replacement.Value;
            Equal(afterAddVersion + 1L, project.ChangeVersion, "real reserved set revision");
            Equal("class-b", Resolve(project).ClassificationId, "real reserved set mapping state");

            var afterSetVersion = project.ChangeVersion;
            var afterSetUpdated = project.UpdatedUtc;
            False(project.Metadata.Remove(new KeyValuePair<string, string>(first.Key, first.Value)), "non-matching reserved pair removal");
            Equal(afterSetVersion, project.ChangeVersion, "non-matching pair removal revision");
            Equal(afterSetUpdated, project.UpdatedUtc, "non-matching pair removal timestamp");

            True(project.Metadata.Remove(new KeyValuePair<string, string>(replacement.Key, replacement.Value)), "matching reserved pair removal");
            Equal(afterSetVersion + 1L, project.ChangeVersion, "matching reserved pair removal revision");
            Equal(0, project.MeasurementWorkItemMappings.Count, "matching reserved pair removal mapping count");

            project.Metadata.Add(first.Key, first.Value);
            var beforeNamedRemove = project.ChangeVersion;
            True(project.Metadata.Remove(first.Key), "named reserved removal");
            Equal(beforeNamedRemove + 1L, project.ChangeVersion, "named reserved removal revision");
            False(project.Metadata.Remove(first.Key), "missing reserved removal");
            Equal(beforeNamedRemove + 1L, project.ChangeVersion, "missing reserved removal revision");

            project.Metadata.Add(first.Key, first.Value);
            project.Metadata["UI.Transient"] = "expanded";
            var beforeClear = project.ChangeVersion;
            project.Metadata.Clear();
            Equal(beforeClear + 1L, project.ChangeVersion, "reserved metadata clear revision");
            Equal(0, project.Metadata.Count, "reserved metadata clear count");
            Equal(0, project.MeasurementWorkItemMappings.Count, "reserved metadata clear mapping count");
        }

        private static void GenericMetadataTracksPublicRevision()
        {
            var project = NewProject("generic");
            var baselineVersion = project.ChangeVersion;
            var baselineUpdated = new DateTime(2026, 8, 14, 0, 0, 0, DateTimeKind.Utc);
            project.UpdatedUtc = baselineUpdated;

            project.Metadata["UI.State"] = "expanded";
            Equal(baselineVersion + 1L, project.ChangeVersion, "generic metadata first set revision");
            True(project.UpdatedUtc > baselineUpdated, "generic metadata first set timestamp");

            var afterFirstSetVersion = project.ChangeVersion;
            var afterFirstSetUpdated = project.UpdatedUtc;
            project.Metadata["UI.State"] = "expanded";
            Equal(afterFirstSetVersion, project.ChangeVersion, "generic metadata same-value set revision");
            Equal(afterFirstSetUpdated, project.UpdatedUtc, "generic metadata same-value set timestamp");

            project.Metadata["UI.State"] = "collapsed";
            Equal(baselineVersion + 2L, project.ChangeVersion, "generic metadata changed set revision");
            project.Metadata.Add("UI.Filter", "walls");
            Equal(baselineVersion + 3L, project.ChangeVersion, "generic metadata add revision");
            True(project.Metadata.Remove("UI.Filter"), "generic metadata remove");
            Equal(baselineVersion + 4L, project.ChangeVersion, "generic metadata remove revision");
            project.Metadata.Clear();
            Equal(baselineVersion + 5L, project.ChangeVersion, "generic metadata clear revision");
            Equal(0, project.Metadata.Count, "generic metadata clear count");
        }

        private static void InvalidAndOverflowMutationsFailBeforeWrite()
        {
            var first = Entry("map-a", ElementCategory.Room, "AreaM2", "class-a", "work-a");
            var ambiguous = Entry("map-b", ElementCategory.Room, "AreaM2", "class-b", "work-b");
            var invalid = NewProject("invalid");
            invalid.Metadata.Add(first.Key, first.Value);
            var beforeInvalidVersion = invalid.ChangeVersion;
            var beforeInvalidUpdated = invalid.UpdatedUtc;
            var beforeInvalidCount = invalid.Metadata.Count;
            ExpectValidationFailure(() => invalid.Metadata.Add(ambiguous.Key, ambiguous.Value));
            Equal(beforeInvalidVersion, invalid.ChangeVersion, "ambiguous metadata rejection revision");
            Equal(beforeInvalidUpdated, invalid.UpdatedUtc, "ambiguous metadata rejection timestamp");
            Equal(beforeInvalidCount, invalid.Metadata.Count, "ambiguous metadata rejection count");

            var overflow = AtVersion(NewProject("overflow"), long.MaxValue);
            var beforeOverflowUpdated = overflow.UpdatedUtc;
            var entry = Entry("map-overflow", ElementCategory.Column, "VolumeM3", "class-column", "work-column");
            Throws<OverflowException>(() => overflow.Metadata.Add(entry.Key, entry.Value));
            Equal(long.MaxValue, overflow.ChangeVersion, "reserved overflow revision");
            Equal(beforeOverflowUpdated, overflow.UpdatedUtc, "reserved overflow timestamp");
            False(overflow.Metadata.ContainsKey(entry.Key), "reserved overflow backing write");

            Throws<OverflowException>(() => overflow.Metadata["UI.Safe"] = "blocked");
            Equal(long.MaxValue, overflow.ChangeVersion, "generic metadata overflow revision");
            Equal(beforeOverflowUpdated, overflow.UpdatedUtc, "generic metadata overflow timestamp");
            False(overflow.Metadata.ContainsKey("UI.Safe"), "generic metadata overflow backing write");
        }

        private static void QsdbHydrationPreservesPersistedMaxRevision()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-mapping-metadata-max-" + Guid.NewGuid().ToString("N") + ".qsdb");
            try
            {
                var source = NewProject("qsdb-max");
                Add(source, "map-a", ElementCategory.Room, "AreaM2", "class-room", "work-room");
                var store = new QsdbProjectStore();
                store.SaveNew(source, path);

                var document = XDocument.Load(path);
                var root = document.Root ?? throw new Exception("Mapping metadata max-version fixture has no root element.");
                root.SetAttributeValue("changeVersion", long.MaxValue.ToString(CultureInfo.InvariantCulture));
                document.Save(path, SaveOptions.DisableFormatting);

                var loaded = store.Load(path);
                Equal(long.MaxValue, loaded.ChangeVersion, "QSDB mapping hydration max revision");
                Equal(1, loaded.MeasurementWorkItemMappings.Count, "QSDB mapping hydration count");
                Equal("class-room", Resolve(loaded).ClassificationId, "QSDB mapping hydration state");
            }
            finally
            {
                TryDelete(path);
                TryDelete(path + ".bak");
            }
        }

        private static void SnapshotRestoreBypassesSyntheticRevision()
        {
            var project = NewProject("snapshot-max");
            Add(project, "map-a", ElementCategory.Room, "AreaM2", "class-room", "work-room");
            AtVersion(project, long.MaxValue);
            var snapshot = ProjectStateSnapshot.Capture(project);

            SetPersistenceMetadataWithoutRevision(project, "UI.Transient", "temporary");
            Equal(long.MaxValue, project.ChangeVersion, "snapshot fixture no-touch metadata revision");
            True(project.Metadata.ContainsKey("UI.Transient"), "snapshot fixture transient generic metadata");
            snapshot.Restore(project);

            Equal(long.MaxValue, project.ChangeVersion, "snapshot restored max revision");
            Equal(1, project.MeasurementWorkItemMappings.Count, "snapshot restored mapping count");
            False(project.Metadata.ContainsKey("UI.Transient"), "snapshot removed transient generic metadata");
            Equal("class-room", Resolve(project).ClassificationId, "snapshot restored mapping state");
        }

        private static void SetPersistenceMetadataWithoutRevision(ProjectState project, string key, string value)
        {
            var method = project.Metadata.GetType().GetMethod(
                "SetPersistenceValue",
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new Exception("Project metadata persistence setter is unavailable.");
            method.Invoke(project.Metadata, new object[] { key, value });
        }

        private static KeyValuePair<string, string> Entry(
            string id,
            ElementCategory category,
            string item,
            string classification,
            string work)
        {
            var source = NewProject("entry-" + id + "-" + classification);
            Add(source, id, category, item, classification, work);
            return source.Metadata.Single(x => x.Key.StartsWith(Prefix, StringComparison.Ordinal));
        }

        private static MeasurementWorkItemMapping Resolve(ProjectState project) =>
            project.MeasurementWorkItemMappings.Single();

        private static void Add(ProjectState project, string id, ElementCategory category, string item, string classification, string work) =>
            project.MeasurementWorkItemMappings.Add(new MeasurementWorkItemMapping(id, category, item, classification, work));

        private static ProjectState AtVersion(ProjectState project, long version)
        {
            var property = typeof(ProjectState).GetProperty(nameof(ProjectState.ChangeVersion))
                ?? throw new Exception("Project ChangeVersion property is unavailable.");
            var setter = property.GetSetMethod(true)
                ?? throw new Exception("Project ChangeVersion private setter is unavailable.");
            setter.Invoke(project, new object[] { version });
            return project;
        }

        private static ProjectState NewProject(string id) =>
            new ProjectState("mapping-metadata-" + id, "Mapping metadata mutation integrity smoke");

        private static void ExpectValidationFailure(Action action)
        {
            try { action(); }
            catch (ArgumentException) { return; }
            catch (FormatException) { return; }
            throw new Exception("Expected reserved mapping metadata validation failure.");
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }

        private static void True(bool value, string label)
        {
            if (!value) throw new Exception("ProjectMappingMetadataMutationIntegritySmoke expected true: " + label + ".");
        }

        private static void False(bool value, string label)
        {
            if (value) throw new Exception("ProjectMappingMetadataMutationIntegritySmoke expected false: " + label + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("ProjectMappingMetadataMutationIntegritySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("ProjectMappingMetadataMutationIntegritySmoke expected " + typeof(TException).Name + ".");
        }
    }
}
