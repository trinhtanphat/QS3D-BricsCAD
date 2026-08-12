using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Diagnostics
{
    public sealed class GeneratedBeamStirrupHealthService
    {
        private const string HandlesKey = "GeneratedBeamStirrupHandles";
        private const string CountKey = "GeneratedBeamStirrupCount";
        private const string DiameterKey = "GeneratedBeamStirrupDiameterMm";
        private const string ActualSpacingKey = "GeneratedBeamStirrupActualSpacingM";
        private const string ModeKey = "GeneratedBeamStirrupMode";
        private const string CenterlineKey = "GeneratedBeamStirrupCenterlineLengthM";
        private const string TotalCenterlineKey = "GeneratedBeamStirrupTotalCenterlineLengthM";
        private const string PolylineKey = "GeneratedBeamStirrupPolylineLengthM";
        private const string BendRadiusKey = "GeneratedBeamStirrupBendRadiusM";
        private const string HookLengthKey = "GeneratedBeamStirrupHookLengthM";
        private const string HookAngleKey = "GeneratedBeamStirrupHookTailAngleDeg";

        public IReadOnlyList<ModelHealthIssue> Inspect(ProjectState project, ISet<string>? liveSolidHandles = null)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var issues = new List<ModelHealthIssue>();
            var ownership = BuildOwnershipIndex(project);
            foreach (var element in project.Elements)
            {
                if (element == null)
                    throw new InvalidOperationException("Beam stirrup health cannot inspect a null project element.");
                if (!element.Properties.TryGetValue(HandlesKey, out var raw) || string.IsNullOrWhiteSpace(raw)) continue;
                var local = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var validCount = 0;
                foreach (var item in raw.Split(new[] { ';' }, StringSplitOptions.None))
                {
                    var handleText = item ?? string.Empty;
                    var handle = handleText.Trim();
                    if (handle.Length == 0 || !long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
                    {
                        issues.Add(new ModelHealthIssue("INVALID_BEAM_STIRRUP_GENERATED_HANDLE", HealthSeverity.Error, HandlesKey + " chứa handle không hợp lệ.", element.Id));
                        continue;
                    }
                    if (!string.Equals(handleText, handle, StringComparison.Ordinal))
                        issues.Add(new ModelHealthIssue("BEAM_STIRRUP_GENERATED_HANDLE_NON_CANONICAL", HealthSeverity.Error, HandlesKey + " không được có khoảng trắng đầu/cuối ở từng handle.", element.Id));
                    if (!local.Add(handle))
                    {
                        issues.Add(new ModelHealthIssue("DUPLICATE_BEAM_STIRRUP_GENERATED_HANDLE", HealthSeverity.Error, "Một beam stirrup handle bị lặp trong cùng element: " + handle, element.Id));
                        continue;
                    }
                    validCount++;
                    var expectedOwner = element.Id + "/" + HandlesKey;
                    if (ownership.IsConflicted(handle, expectedOwner))
                        issues.Add(new ModelHealthIssue("BEAM_STIRRUP_GENERATED_OWNERSHIP_CONFLICT", HealthSeverity.Error, "Generated beam stirrup solid xung đột owner/project handle khác: " + ownership.Describe(handle), element.Id));
                    if (element.SourceHandles.Any(x => string.Equals((x ?? string.Empty).Trim(), handle, StringComparison.OrdinalIgnoreCase)))
                        issues.Add(new ModelHealthIssue("BEAM_STIRRUP_GENERATED_HANDLE_IN_SOURCE", HealthSeverity.Error, "Generated beam stirrup handle không được nằm trong SourceHandles.", element.Id));
                    if (liveSolidHandles != null && !liveSolidHandles.Contains(handle))
                        issues.Add(new ModelHealthIssue("BEAM_STIRRUP_GENERATED_SOLID_MISSING", HealthSeverity.Error, "Không còn tìm thấy generated beam stirrup Solid3d: " + handle, element.Id));
                }

                if (!element.Properties.TryGetValue(CountKey, out var countText) ||
                    !int.TryParse(countText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) || count < 0 || count != validCount)
                {
                    issues.Add(new ModelHealthIssue("BEAM_STIRRUP_GENERATED_COUNT_MISMATCH", HealthSeverity.Warning, CountKey + " không khớp số handle hợp lệ.", element.Id));
                }
                else if (!string.Equals(countText, count.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal))
                {
                    issues.Add(new ModelHealthIssue("BEAM_STIRRUP_GENERATED_COUNT_NON_CANONICAL", HealthSeverity.Error, CountKey + " phải dùng đúng invariant integer spelling: " + count.ToString(CultureInfo.InvariantCulture) + ".", element.Id));
                }

                if (!element.Properties.TryGetValue(DiameterKey, out var diameterText) ||
                    !double.TryParse(diameterText, NumberStyles.Float, CultureInfo.InvariantCulture, out var diameter) ||
                    double.IsNaN(diameter) || double.IsInfinity(diameter) || diameter <= 0d)
                {
                    issues.Add(new ModelHealthIssue("BEAM_STIRRUP_GENERATED_DIAMETER_INVALID", HealthSeverity.Warning, DiameterKey + " thiếu hoặc không hợp lệ.", element.Id));
                }
                else
                {
                    var canonicalDiameter = diameter.ToString("R", CultureInfo.InvariantCulture);
                    if (!string.Equals(diameterText, canonicalDiameter, StringComparison.Ordinal))
                        issues.Add(new ModelHealthIssue("BEAM_STIRRUP_GENERATED_DIAMETER_NON_CANONICAL", HealthSeverity.Error, DiameterKey + " phải dùng đúng round-trip invariant numeric spelling: " + canonicalDiameter + ".", element.Id));
                }

                InspectActualSpacing(element, issues);

                if (element.Category != ElementCategory.Beam)
                    issues.Add(new ModelHealthIssue("BEAM_STIRRUP_CATEGORY_MISMATCH", HealthSeverity.Error, "Generated beam stirrup metadata chỉ hợp lệ trên Beam element.", element.Id));

                InspectAdvancedMetadata(element, validCount, issues);

                if (element.IsGeneratedBeamStirrupStale())
                    issues.Add(new ModelHealthIssue("BEAM_STIRRUP_GENERATED_STALE", HealthSeverity.Warning, "Generated beam stirrup snapshot không còn khớp semantic/source hiện tại; rebuild stirrups trước khi phát hành bản vẽ.", element.Id));
            }
            return issues.AsReadOnly();
        }

        private static void InspectActualSpacing(ProjectElement element, ICollection<ModelHealthIssue> issues)
        {
            if (!element.Properties.TryGetValue(ActualSpacingKey, out var raw) || string.IsNullOrWhiteSpace(raw)) return;
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
            {
                issues.Add(new ModelHealthIssue("BEAM_STIRRUP_ACTUAL_SPACING_INVALID", HealthSeverity.Warning, ActualSpacingKey + " phải là số hữu hạn >= 0.", element.Id));
                return;
            }

            var canonical = value.ToString("R", CultureInfo.InvariantCulture);
            if (!string.Equals(raw, canonical, StringComparison.Ordinal))
                issues.Add(new ModelHealthIssue("BEAM_STIRRUP_ACTUAL_SPACING_NON_CANONICAL", HealthSeverity.Error, ActualSpacingKey + " phải dùng đúng round-trip invariant numeric spelling: " + canonical + ".", element.Id));
        }

        private static void InspectAdvancedMetadata(ProjectElement element, int validCount, ICollection<ModelHealthIssue> issues)
        {
            var hasAdvancedMetadata =
                element.Properties.ContainsKey(CenterlineKey) ||
                element.Properties.ContainsKey(TotalCenterlineKey) ||
                element.Properties.ContainsKey(PolylineKey) ||
                element.Properties.ContainsKey(BendRadiusKey) ||
                element.Properties.ContainsKey(HookLengthKey) ||
                element.Properties.ContainsKey(HookAngleKey);

            element.Properties.TryGetValue(ModeKey, out var rawMode);
            var modeText = rawMode ?? string.Empty;
            var mode = modeText.Trim();
            var isClosed = string.Equals(mode, "Beam.Line.RectangularClosedLoop", StringComparison.OrdinalIgnoreCase);
            var isRounded = string.Equals(mode, "Beam.Line.RectangularRoundedLoop", StringComparison.OrdinalIgnoreCase);
            var isHooked = string.Equals(mode, "Beam.Line.RectangularHookedPath", StringComparison.OrdinalIgnoreCase);
            var canonicalMode = isClosed
                ? "Beam.Line.RectangularClosedLoop"
                : isRounded
                    ? "Beam.Line.RectangularRoundedLoop"
                    : isHooked
                        ? "Beam.Line.RectangularHookedPath"
                        : string.Empty;
            if (mode.Length > 0 && canonicalMode.Length == 0)
                issues.Add(new ModelHealthIssue("BEAM_STIRRUP_GENERATED_MODE_INVALID", HealthSeverity.Warning, ModeKey + " không phải mode beam stirrup được hỗ trợ.", element.Id));
            else if (canonicalMode.Length > 0 && !string.Equals(modeText, canonicalMode, StringComparison.Ordinal))
                issues.Add(new ModelHealthIssue("BEAM_STIRRUP_GENERATED_MODE_NON_CANONICAL", HealthSeverity.Error, ModeKey + " phải dùng đúng writer-owned token: " + canonicalMode + ".", element.Id));

            if (!hasAdvancedMetadata) return;
            if (mode.Length == 0)
                issues.Add(new ModelHealthIssue("BEAM_STIRRUP_GENERATED_MODE_INVALID", HealthSeverity.Warning, ModeKey + " bắt buộc khi advanced stirrup metadata đã tồn tại.", element.Id));

            if (!TryNumber(element, CenterlineKey, out var centerline) || centerline <= 0d)
            {
                issues.Add(InvalidMetadata(element, CenterlineKey + " phải là số hữu hạn > 0."));
                return;
            }
            ValidateAdvancedCanonicality(element, CenterlineKey, centerline, issues);

            if (!TryNumber(element, TotalCenterlineKey, out var totalCenterline) || totalCenterline <= 0d)
            {
                issues.Add(InvalidMetadata(element, TotalCenterlineKey + " phải là số hữu hạn > 0."));
            }
            else
            {
                ValidateAdvancedCanonicality(element, TotalCenterlineKey, totalCenterline, issues);
            }

            if (!TryNumber(element, PolylineKey, out var polyline) || polyline <= 0d)
            {
                issues.Add(InvalidMetadata(element, PolylineKey + " phải là số hữu hạn > 0."));
            }
            else
            {
                ValidateAdvancedCanonicality(element, PolylineKey, polyline, issues);
                if (polyline > centerline + Math.Max(1e-9d, centerline * 1e-9d))
                    issues.Add(InvalidMetadata(element, PolylineKey + " không được dài hơn exact centerline length."));
            }

            if (!TryNumber(element, BendRadiusKey, out var bendRadius) || bendRadius < 0d)
            {
                issues.Add(InvalidMetadata(element, BendRadiusKey + " phải là số hữu hạn >= 0."));
            }
            else
            {
                ValidateAdvancedCanonicality(element, BendRadiusKey, bendRadius, issues);
            }

            if (!TryNumber(element, HookLengthKey, out var hookLength) || hookLength < 0d)
            {
                issues.Add(InvalidMetadata(element, HookLengthKey + " phải là số hữu hạn >= 0."));
            }
            else
            {
                ValidateAdvancedCanonicality(element, HookLengthKey, hookLength, issues);
            }

            if (!TryNumber(element, HookAngleKey, out var hookAngle))
            {
                issues.Add(InvalidMetadata(element, HookAngleKey + " phải là số hữu hạn."));
            }
            else
            {
                ValidateAdvancedCanonicality(element, HookAngleKey, hookAngle, issues);
            }

            if (TryNumber(element, TotalCenterlineKey, out totalCenterline))
            {
                var expected = centerline * validCount;
                if (double.IsNaN(expected) || double.IsInfinity(expected))
                {
                    issues.Add(InvalidMetadata(element, TotalCenterlineKey + " expected value overflowed the finite numeric range."));
                }
                else
                {
                    var tolerance = Math.Max(1e-9d, Math.Abs(expected) * 1e-9d);
                    if (Math.Abs(totalCenterline - expected) > tolerance)
                        issues.Add(new ModelHealthIssue("BEAM_STIRRUP_GENERATED_LENGTH_MISMATCH", HealthSeverity.Warning, TotalCenterlineKey + " không khớp centerline length × số stirrup handle.", element.Id));
                }
            }

            if (TryNumber(element, BendRadiusKey, out bendRadius) && TryNumber(element, HookLengthKey, out hookLength) && TryNumber(element, HookAngleKey, out hookAngle))
            {
                const double epsilon = 1e-12d;
                if (hookLength > epsilon && !(hookAngle > 0d && hookAngle < 180d))
                    issues.Add(InvalidMetadata(element, HookAngleKey + " phải nằm trong (0,180) khi hook length > 0."));
                if (hookLength <= epsilon && Math.Abs(hookAngle) > epsilon)
                    issues.Add(InvalidMetadata(element, HookAngleKey + " phải bằng 0 khi hook length bằng 0."));
                if (isClosed && (bendRadius > epsilon || hookLength > epsilon))
                    issues.Add(new ModelHealthIssue("BEAM_STIRRUP_GENERATED_MODE_MISMATCH", HealthSeverity.Warning, "ClosedLoop mode không khớp bend/hook metadata.", element.Id));
                if (isRounded && (bendRadius <= epsilon || hookLength > epsilon))
                    issues.Add(new ModelHealthIssue("BEAM_STIRRUP_GENERATED_MODE_MISMATCH", HealthSeverity.Warning, "RoundedLoop mode yêu cầu bend radius > 0 và không có hook tail.", element.Id));
                if (isHooked && hookLength <= epsilon)
                    issues.Add(new ModelHealthIssue("BEAM_STIRRUP_GENERATED_MODE_MISMATCH", HealthSeverity.Warning, "HookedPath mode yêu cầu hook length > 0.", element.Id));
            }
        }

        private static void ValidateAdvancedCanonicality(ProjectElement element, string key, double value, ICollection<ModelHealthIssue> issues)
        {
            if (!element.Properties.TryGetValue(key, out var raw)) return;
            var canonical = value.ToString("R", CultureInfo.InvariantCulture);
            if (!string.Equals(raw, canonical, StringComparison.Ordinal))
                issues.Add(new ModelHealthIssue("BEAM_STIRRUP_GENERATED_METADATA_NON_CANONICAL", HealthSeverity.Error, key + " phải dùng đúng round-trip invariant numeric spelling: " + canonical + ".", element.Id));
        }

        private static ModelHealthIssue InvalidMetadata(ProjectElement element, string message) =>
            new ModelHealthIssue("BEAM_STIRRUP_GENERATED_METADATA_INVALID", HealthSeverity.Warning, message, element.Id);

        private static bool TryNumber(ProjectElement element, string key, out double value)
        {
            value = 0d;
            if (!element.Properties.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) return false;
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value)) return false;
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private sealed class OwnershipIndex
        {
            public Dictionary<string, string> Owners { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> Conflicts { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            public bool IsConflicted(string handle, string expectedOwner)
            {
                if (Conflicts.Contains(handle)) return true;
                return Owners.TryGetValue(handle, out var owner) && !string.Equals(owner, expectedOwner, StringComparison.OrdinalIgnoreCase);
            }

            public string Describe(string handle)
            {
                if (Conflicts.Contains(handle)) return "multiple owners";
                return Owners.TryGetValue(handle, out var owner) ? owner : "unknown owner";
            }
        }

        private static OwnershipIndex BuildOwnershipIndex(ProjectState project)
        {
            var index = new OwnershipIndex();
            foreach (var element in project.Elements)
            {
                if (element == null)
                    throw new InvalidOperationException("Beam stirrup health cannot inspect a null project element.");
                foreach (var handle in element.SourceHandles) Reserve(index, handle, element.Id + "/SourceHandles");
                foreach (var property in element.Properties)
                {
                    if (!GeneratedHandleOwnershipPolicy.IsOwnerSlot(property.Key)) continue;
                    ReserveProperty(index, element, property.Key, property.Value);
                }
            }
            return index;
        }

        private static void ReserveProperty(OwnershipIndex index, ProjectElement element, string key, string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return;
            foreach (var handle in raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase))
                Reserve(index, handle, element.Id + "/" + key);
        }

        private static void Reserve(OwnershipIndex index, string? handle, string token)
        {
            var normalized = (handle ?? string.Empty).Trim();
            if (normalized.Length == 0) return;
            if (!index.Owners.TryGetValue(normalized, out var existing))
            {
                index.Owners[normalized] = token;
                return;
            }
            if (!string.Equals(existing, token, StringComparison.OrdinalIgnoreCase))
                index.Conflicts.Add(normalized);
        }
    }
}
