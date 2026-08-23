using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bricscad.ApplicationServices;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25.Cad
{
    internal sealed class WallJunctionNativeRecord
    {
        public ObjectId ObjectId { get; set; }
        public string Handle { get; set; } = string.Empty;
        public string ProjectIdentity { get; set; } = string.Empty;
        public string DrawingIdentity { get; set; } = string.Empty;
        public string GroupToken { get; set; } = string.Empty;
        public string OwnerToken { get; set; } = string.Empty;
        public string InputFingerprint { get; set; } = string.Empty;
        public WallJunctionKind JunctionKind { get; set; }
        public int OccurrenceIndex { get; set; }
        public Point2 JunctionPoint { get; set; }
        public double BottomM { get; set; }
        public double TopM { get; set; }
        public double MinThicknessM { get; set; }
        public double MaxThicknessM { get; set; }
        public IReadOnlyList<string> OwnerIdentities { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> SourceIdentities { get; set; } = Array.Empty<string>();
    }

    internal static class GeneratedWallJunctionNativeOwnershipService
    {
        internal const string RegAppName = "QS3D_WALL_JUNCTION";
        internal const string OwnershipVersion = "1";
        private const int MaxOwnersPerJunction = 64;
        private const int MaxSourcesPerJunction = 128;
        private const int MaxIdentityLength = 256;
        private const string ProjectIdentityPrefix = "WJPR1:";
        private const string DrawingIdentityPrefix = "WJDR1:";
        private const string OwnerIdentityPrefix = "WJOW1:";
        private const string SourceIdentityPrefix = "WJSG1:";
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        public static string ProjectIdentity(string projectId) => Identity(ProjectIdentityPrefix, projectId, "project id");
        public static string DrawingIdentity(string drawingFingerprint) => Identity(DrawingIdentityPrefix, drawingFingerprint, "drawing fingerprint");
        public static string OwnerIdentity(string elementId) => Identity(OwnerIdentityPrefix, elementId, "wall element id");
        public static string SourceIdentity(string segmentId) => Identity(SourceIdentityPrefix, segmentId, "wall source segment id");

        public static void MarkGenerated(
            Document document,
            Transaction transaction,
            Solid3d solid,
            WallJunctionOwnershipPlan plan)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            if (solid == null) throw new ArgumentNullException(nameof(solid));
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            ValidatePlan(plan);
            EnsureRegApp(document.Database, transaction);

            var owners = plan.OwnerWallIds.Select(OwnerIdentity).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            var sources = plan.SourceSegmentIds.Select(SourceIdentity).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            var values = new List<TypedValue>(17 + owners.Length + sources.Length)
            {
                StringValue(DxfCode.ExtendedDataRegAppName, RegAppName),
                StringValue(DxfCode.ExtendedDataAsciiString, OwnershipVersion),
                StringValue(DxfCode.ExtendedDataAsciiString, ProjectIdentity(plan.ProjectId)),
                StringValue(DxfCode.ExtendedDataAsciiString, DrawingIdentity(plan.DrawingFingerprint)),
                StringValue(DxfCode.ExtendedDataAsciiString, plan.GroupToken),
                StringValue(DxfCode.ExtendedDataAsciiString, plan.OwnerToken),
                StringValue(DxfCode.ExtendedDataAsciiString, plan.InputFingerprint),
                StringValue(DxfCode.ExtendedDataAsciiString, plan.JunctionKind.ToString()),
                new TypedValue((int)DxfCode.ExtendedDataInteger32, plan.OccurrenceIndex),
                new TypedValue((int)DxfCode.ExtendedDataReal, plan.JunctionPoint.X),
                new TypedValue((int)DxfCode.ExtendedDataReal, plan.JunctionPoint.Y),
                new TypedValue((int)DxfCode.ExtendedDataReal, plan.BottomM),
                new TypedValue((int)DxfCode.ExtendedDataReal, plan.TopM),
                new TypedValue((int)DxfCode.ExtendedDataReal, plan.MinThicknessM),
                new TypedValue((int)DxfCode.ExtendedDataReal, plan.MaxThicknessM),
                new TypedValue((int)DxfCode.ExtendedDataInteger32, owners.Length)
            };
            foreach (var owner in owners) values.Add(StringValue(DxfCode.ExtendedDataAsciiString, owner));
            values.Add(new TypedValue((int)DxfCode.ExtendedDataInteger32, sources.Length));
            foreach (var source in sources) values.Add(StringValue(DxfCode.ExtendedDataAsciiString, source));

            using (var marker = new ResultBuffer(values.ToArray())) solid.XData = marker;
        }

        public static IReadOnlyList<WallJunctionNativeRecord> ReadAllStrict(Document document, Transaction transaction)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
            var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead);
            var result = new List<WallJunctionNativeRecord>();
            foreach (ObjectId id in modelSpace)
            {
                var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                if (entity == null || entity.IsErased) continue;
                using (var marker = entity.GetXDataForApplication(RegAppName))
                {
                    if (marker == null) continue;
                }
                if (!(entity is Solid3d))
                    throw new InvalidOperationException("Wall Junction ownership marker is attached to a live non-Solid3d entity: " + entity.Handle + ". Refusing native mutation.");
                if (!TryRead(entity, out var record, out var errorCode))
                    throw new InvalidOperationException("Wall Junction ownership marker is invalid (" + errorCode + ") on handle " + entity.Handle + ". Refusing native mutation.");
                result.Add(record);
            }
            EnsureUniqueOwnerTokens(result);
            return result.OrderBy(x => x.GroupToken, StringComparer.Ordinal).ThenBy(x => x.OwnerToken, StringComparer.Ordinal).ToList().AsReadOnly();
        }

        public static bool TryRead(Entity entity, out WallJunctionNativeRecord record, out string errorCode)
        {
            record = null!;
            errorCode = "NONE";
            if (entity == null)
            {
                errorCode = "ENTITY_NULL";
                return false;
            }

            try
            {
                using (var marker = entity.GetXDataForApplication(RegAppName))
                {
                    if (marker == null)
                    {
                        errorCode = "MARKER_MISSING";
                        return false;
                    }
                    var values = marker.AsArray();
                    if (values.Length < 19) return Fail("MARKER_TOO_SHORT", out errorCode);
                    RequireString(values, 0, DxfCode.ExtendedDataRegAppName, RegAppName);
                    RequireString(values, 1, DxfCode.ExtendedDataAsciiString, OwnershipVersion);
                    var projectIdentity = ReadHashIdentity(values, 2, ProjectIdentityPrefix);
                    var drawingIdentity = ReadHashIdentity(values, 3, DrawingIdentityPrefix);
                    var groupToken = ReadCoreHashToken(values, 4, "WJP1:");
                    var ownerToken = ReadOwnerToken(values, 5);
                    var inputFingerprint = ReadCoreHashToken(values, 6, "WJF1:");
                    var kindText = ReadString(values, 7, DxfCode.ExtendedDataAsciiString);
                    if (!Enum.TryParse(kindText, false, out WallJunctionKind kind) || !IsPhysical(kind))
                        throw new FormatException("JUNCTION_KIND_INVALID");
                    var occurrence = ReadInt32(values, 8);
                    if (occurrence < 0 || occurrence > 9999) throw new FormatException("OCCURRENCE_INVALID");
                    if (!string.Equals(ownerToken, groupToken.Replace("WJP1:", "WJX1:") + ":" + occurrence.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal))
                        throw new FormatException("OWNER_OCCURRENCE_MISMATCH");
                    var pointX = ReadFiniteDouble(values, 9, "POINT_X_INVALID");
                    var pointY = ReadFiniteDouble(values, 10, "POINT_Y_INVALID");
                    var bottom = ReadFiniteDouble(values, 11, "BOTTOM_INVALID");
                    var top = ReadFiniteDouble(values, 12, "TOP_INVALID");
                    var minThickness = ReadFiniteDouble(values, 13, "MIN_THICKNESS_INVALID");
                    var maxThickness = ReadFiniteDouble(values, 14, "MAX_THICKNESS_INVALID");
                    if (!(top > bottom)) throw new FormatException("VERTICAL_RANGE_INVALID");
                    if (!(minThickness > 0d) || maxThickness < minThickness) throw new FormatException("THICKNESS_RANGE_INVALID");

                    var ownerCount = ReadInt32(values, 15);
                    if (ownerCount < 2 || ownerCount > MaxOwnersPerJunction) throw new FormatException("OWNER_COUNT_INVALID");
                    var cursor = 16;
                    var owners = new List<string>(ownerCount);
                    for (var index = 0; index < ownerCount; index++, cursor++) owners.Add(ReadHashIdentity(values, cursor, OwnerIdentityPrefix));
                    if (owners.Distinct(StringComparer.Ordinal).Count() != owners.Count) throw new FormatException("OWNER_IDENTITY_DUPLICATE");
                    if (!owners.SequenceEqual(owners.OrderBy(x => x, StringComparer.Ordinal))) throw new FormatException("OWNER_IDENTITY_ORDER_INVALID");

                    if (cursor >= values.Length) throw new FormatException("SOURCE_COUNT_MISSING");
                    var sourceCount = ReadInt32(values, cursor++);
                    if (sourceCount < 2 || sourceCount > MaxSourcesPerJunction) throw new FormatException("SOURCE_COUNT_INVALID");
                    if (values.Length != cursor + sourceCount) throw new FormatException("MARKER_LENGTH_INVALID");
                    var sources = new List<string>(sourceCount);
                    for (var index = 0; index < sourceCount; index++, cursor++) sources.Add(ReadHashIdentity(values, cursor, SourceIdentityPrefix));
                    if (sources.Distinct(StringComparer.Ordinal).Count() != sources.Count) throw new FormatException("SOURCE_IDENTITY_DUPLICATE");
                    if (!sources.SequenceEqual(sources.OrderBy(x => x, StringComparer.Ordinal))) throw new FormatException("SOURCE_IDENTITY_ORDER_INVALID");

                    record = new WallJunctionNativeRecord
                    {
                        ObjectId = entity.ObjectId,
                        Handle = entity.Handle.ToString(),
                        ProjectIdentity = projectIdentity,
                        DrawingIdentity = drawingIdentity,
                        GroupToken = groupToken,
                        OwnerToken = ownerToken,
                        InputFingerprint = inputFingerprint,
                        JunctionKind = kind,
                        OccurrenceIndex = occurrence,
                        JunctionPoint = new Point2(pointX, pointY),
                        BottomM = bottom,
                        TopM = top,
                        MinThicknessM = minThickness,
                        MaxThicknessM = maxThickness,
                        OwnerIdentities = owners.AsReadOnly(),
                        SourceIdentities = sources.AsReadOnly()
                    };
                    return true;
                }
            }
            catch (FormatException ex)
            {
                errorCode = SanitizeCode(ex.Message);
                return false;
            }
            catch
            {
                errorCode = "MARKER_READ_FAILED";
                return false;
            }
        }

        public static void RequireCurrentProject(WallJunctionNativeRecord record, ProjectState project)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (!string.Equals(record.ProjectIdentity, ProjectIdentity(project.ProjectId), StringComparison.Ordinal) ||
                !string.Equals(record.DrawingIdentity, DrawingIdentity(project.DrawingFingerprint), StringComparison.Ordinal))
                throw new InvalidOperationException("Wall Junction output " + record.Handle + " belongs to another project or drawing. Refusing destructive mutation.");
        }

        public static bool MatchesPlan(WallJunctionNativeRecord record, WallJunctionOwnershipPlan plan)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            ValidatePlan(plan);
            return string.Equals(record.ProjectIdentity, ProjectIdentity(plan.ProjectId), StringComparison.Ordinal) &&
                   string.Equals(record.DrawingIdentity, DrawingIdentity(plan.DrawingFingerprint), StringComparison.Ordinal) &&
                   string.Equals(record.GroupToken, plan.GroupToken, StringComparison.Ordinal) &&
                   string.Equals(record.OwnerToken, plan.OwnerToken, StringComparison.Ordinal) &&
                   string.Equals(record.InputFingerprint, plan.InputFingerprint, StringComparison.Ordinal) &&
                   record.JunctionKind == plan.JunctionKind &&
                   record.OccurrenceIndex == plan.OccurrenceIndex &&
                   Equal(record.JunctionPoint.X, plan.JunctionPoint.X) &&
                   Equal(record.JunctionPoint.Y, plan.JunctionPoint.Y) &&
                   Equal(record.BottomM, plan.BottomM) &&
                   Equal(record.TopM, plan.TopM) &&
                   Equal(record.MinThicknessM, plan.MinThicknessM) &&
                   Equal(record.MaxThicknessM, plan.MaxThicknessM) &&
                   record.OwnerIdentities.SequenceEqual(plan.OwnerWallIds.Select(OwnerIdentity).OrderBy(x => x, StringComparer.Ordinal)) &&
                   record.SourceIdentities.SequenceEqual(plan.SourceSegmentIds.Select(SourceIdentity).OrderBy(x => x, StringComparer.Ordinal));
        }

        public static int PrepareOwnerInvalidation(
            Document document,
            Transaction transaction,
            ProjectState project,
            IEnumerable<ProjectElement> owners)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (owners == null) throw new ArgumentNullException(nameof(owners));
            var targets = new HashSet<string>(
                owners.Where(x => x != null && IsWall(x.Category)).Select(x => OwnerIdentity(x.Id)),
                StringComparer.Ordinal);
            if (targets.Count == 0) return 0;

            var records = ReadAllStrict(document, transaction);
            foreach (var record in records) RequireCurrentProject(record, project);
            var erased = 0;
            foreach (var group in records.GroupBy(x => x.GroupToken, StringComparer.Ordinal))
            {
                ValidateGroupOwnerSet(group);
                if (!group.Any(x => x.OwnerIdentities.Any(targets.Contains))) continue;
                foreach (var record in group)
                {
                    var entity = transaction.GetObject(record.ObjectId, OpenMode.ForWrite, false) as Solid3d;
                    if (entity == null || entity.IsErased)
                        throw new InvalidOperationException("Wall Junction output disappeared before invalidation: " + record.Handle + ".");
                    entity.Erase();
                    erased++;
                }
            }
            return erased;
        }

        public static void ValidateGroupOwnerSet(IEnumerable<WallJunctionNativeRecord> records)
        {
            var list = records?.ToList() ?? throw new ArgumentNullException(nameof(records));
            if (list.Count == 0) return;
            var expected = list[0].OwnerIdentities;
            foreach (var record in list)
            {
                if (!record.OwnerIdentities.SequenceEqual(expected))
                    throw new InvalidOperationException("Wall Junction group " + record.GroupToken + " has inconsistent persisted owner membership. Refusing destructive mutation.");
            }
        }

        private static void EnsureUniqueOwnerTokens(IEnumerable<WallJunctionNativeRecord> records)
        {
            var owners = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var record in records)
            {
                if (owners.TryGetValue(record.OwnerToken, out var existing))
                    throw new InvalidOperationException("Wall Junction owner token " + record.OwnerToken + " is duplicated by handles " + existing + " and " + record.Handle + ". Refusing native mutation.");
                owners[record.OwnerToken] = record.Handle;
            }
        }

        private static void ValidatePlan(WallJunctionOwnershipPlan plan)
        {
            if (plan.OwnerWallIds == null || plan.OwnerWallIds.Count < 2 || plan.OwnerWallIds.Count > MaxOwnersPerJunction)
                throw new InvalidOperationException("Wall Junction native output requires 2.." + MaxOwnersPerJunction.ToString(CultureInfo.InvariantCulture) + " semantic owners.");
            if (plan.SourceSegmentIds == null || plan.SourceSegmentIds.Count < 2 || plan.SourceSegmentIds.Count > MaxSourcesPerJunction)
                throw new InvalidOperationException("Wall Junction native output requires 2.." + MaxSourcesPerJunction.ToString(CultureInfo.InvariantCulture) + " source segments.");
            if (!IsPhysical(plan.JunctionKind)) throw new InvalidOperationException("Wall Junction native output supports only L/T/X/Multi plans.");
            if (!Finite(plan.JunctionPoint.X) || !Finite(plan.JunctionPoint.Y) ||
                !Finite(plan.BottomM) || !Finite(plan.TopM) || !(plan.TopM > plan.BottomM) ||
                !Finite(plan.MinThicknessM) || !Finite(plan.MaxThicknessM) || !(plan.MinThicknessM > 0d) || plan.MaxThicknessM < plan.MinThicknessM)
                throw new InvalidOperationException("Wall Junction native plan contains invalid geometry/profile values.");
            _ = ReadCoreToken(plan.GroupToken, "WJP1:");
            _ = ReadCoreToken(plan.InputFingerprint, "WJF1:");
            _ = ReadOwnerToken(plan.OwnerToken);
        }

        private static bool IsPhysical(WallJunctionKind kind) =>
            kind == WallJunctionKind.L || kind == WallJunctionKind.T || kind == WallJunctionKind.X || kind == WallJunctionKind.Multi;

        private static bool IsWall(ElementCategory category) =>
            category == ElementCategory.ArchitecturalWall ||
            category == ElementCategory.GlassWall ||
            category == ElementCategory.WallPier;

        private static TypedValue StringValue(DxfCode code, string value) => new TypedValue((int)code, value);

        private static string ReadString(TypedValue[] values, int index, DxfCode expectedCode)
        {
            if (index < 0 || index >= values.Length || values[index].TypeCode != (int)expectedCode)
                throw new FormatException("MARKER_TYPE_INVALID");
            var text = Convert.ToString(values[index].Value, CultureInfo.InvariantCulture) ?? string.Empty;
            if (text.Length == 0) throw new FormatException("MARKER_STRING_EMPTY");
            return text;
        }

        private static void RequireString(TypedValue[] values, int index, DxfCode expectedCode, string expected)
        {
            if (!string.Equals(ReadString(values, index, expectedCode), expected, StringComparison.Ordinal))
                throw new FormatException("MARKER_HEADER_INVALID");
        }

        private static int ReadInt32(TypedValue[] values, int index)
        {
            if (index < 0 || index >= values.Length || values[index].TypeCode != (int)DxfCode.ExtendedDataInteger32)
                throw new FormatException("MARKER_INTEGER_INVALID");
            return Convert.ToInt32(values[index].Value, CultureInfo.InvariantCulture);
        }

        private static double ReadFiniteDouble(TypedValue[] values, int index, string code)
        {
            if (index < 0 || index >= values.Length || values[index].TypeCode != (int)DxfCode.ExtendedDataReal)
                throw new FormatException(code);
            var value = Convert.ToDouble(values[index].Value, CultureInfo.InvariantCulture);
            if (!Finite(value)) throw new FormatException(code);
            return value;
        }

        private static string ReadHashIdentity(TypedValue[] values, int index, string prefix)
        {
            var value = ReadString(values, index, DxfCode.ExtendedDataAsciiString);
            if (!IsHashToken(value, prefix)) throw new FormatException("IDENTITY_TOKEN_INVALID");
            return value;
        }

        private static string ReadCoreHashToken(TypedValue[] values, int index, string prefix)
        {
            var value = ReadString(values, index, DxfCode.ExtendedDataAsciiString);
            return ReadCoreToken(value, prefix);
        }

        private static string ReadCoreToken(string value, string prefix)
        {
            if (!IsHashToken(value, prefix)) throw new FormatException("CORE_TOKEN_INVALID");
            return value;
        }

        private static string ReadOwnerToken(TypedValue[] values, int index) =>
            ReadOwnerToken(ReadString(values, index, DxfCode.ExtendedDataAsciiString));

        private static string ReadOwnerToken(string value)
        {
            const string prefix = "WJX1:";
            if (string.IsNullOrWhiteSpace(value) || !value.StartsWith(prefix, StringComparison.Ordinal) || value.Length < prefix.Length + 66)
                throw new FormatException("OWNER_TOKEN_INVALID");
            var hash = value.Substring(prefix.Length, 64);
            if (!IsLowerHex(hash)) throw new FormatException("OWNER_TOKEN_HASH_INVALID");
            if (value[prefix.Length + 64] != ':') throw new FormatException("OWNER_TOKEN_SEPARATOR_INVALID");
            if (!int.TryParse(value.Substring(prefix.Length + 65), NumberStyles.None, CultureInfo.InvariantCulture, out var occurrence) || occurrence < 0 || occurrence > 9999)
                throw new FormatException("OWNER_TOKEN_OCCURRENCE_INVALID");
            return value;
        }

        private static bool IsHashToken(string value, string prefix) =>
            !string.IsNullOrEmpty(value) && value.Length == prefix.Length + 64 && value.StartsWith(prefix, StringComparison.Ordinal) && IsLowerHex(value.Substring(prefix.Length));

        private static bool IsLowerHex(string value)
        {
            if (value.Length != 64) return false;
            foreach (var ch in value)
                if (!((ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'f'))) return false;
            return true;
        }

        private static string Identity(string prefix, string raw, string label)
        {
            if (string.IsNullOrWhiteSpace(raw)) throw new InvalidOperationException("Wall Junction " + label + " is required.");
            var normalized = raw.Trim().ToUpperInvariant();
            if (normalized.Length > MaxIdentityLength)
                throw new InvalidOperationException("Wall Junction " + label + " exceeds " + MaxIdentityLength.ToString(CultureInfo.InvariantCulture) + " characters.");
            byte[] bytes;
            try { bytes = StrictUtf8.GetBytes(normalized); }
            catch (EncoderFallbackException ex) { throw new InvalidOperationException("Wall Junction " + label + " must contain well-formed Unicode text.", ex); }
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(bytes);
                var builder = new StringBuilder(prefix.Length + hash.Length * 2);
                builder.Append(prefix);
                foreach (var item in hash) builder.Append(item.ToString("x2", CultureInfo.InvariantCulture));
                return builder.ToString();
            }
        }

        private static bool Equal(double left, double right) => left == right;
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        private static bool Fail(string code, out string errorCode)
        {
            errorCode = code;
            return false;
        }

        private static string SanitizeCode(string raw)
        {
            var normalized = (raw ?? string.Empty).Trim().ToUpperInvariant();
            if (normalized.Length == 0 || normalized.Length > 64) return "MARKER_FORMAT_INVALID";
            foreach (var ch in normalized)
                if (!((ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9') || ch == '_')) return "MARKER_FORMAT_INVALID";
            return normalized;
        }

        private static void EnsureRegApp(Database database, Transaction transaction)
        {
            var table = (RegAppTable)transaction.GetObject(database.RegAppTableId, OpenMode.ForRead);
            if (table.Has(RegAppName)) return;
            table.UpgradeOpen();
            var record = new RegAppTableRecord { Name = RegAppName };
            table.Add(record);
            transaction.AddNewlyCreatedDBObject(record, true);
        }
    }
}
