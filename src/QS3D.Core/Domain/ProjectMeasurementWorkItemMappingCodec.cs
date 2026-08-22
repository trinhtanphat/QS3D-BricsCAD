using System;
using System.Collections.Generic;
using System.Globalization;
using QS3D.Core.Mapping;

namespace QS3D.Core.Domain
{
    internal static class ProjectMeasurementWorkItemMappingCodec
    {
        internal const string Prefix = "QS3D.Mapping.v1.";

        internal static bool IsReservedKey(string key) => key != null && key.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase);
        internal static string Key(MeasurementWorkItemMapping mapping) => Prefix + mapping.MappingId;
        internal static string Value(MeasurementWorkItemMapping mapping)
        {
            var category = Enum.GetName(typeof(ElementCategory), mapping.Category) ?? throw new ArgumentOutOfRangeException(nameof(mapping));
            return category + "|" + Field(mapping.MeasurementItemId) + Field(mapping.ClassificationId) + Field(mapping.WorkItemId);
        }

        internal static IReadOnlyList<MeasurementWorkItemMapping> Read(IEnumerable<KeyValuePair<string, string>> metadata)
        {
            var mappings = new List<MeasurementWorkItemMapping>();
            try
            {
                foreach (var pair in metadata)
                {
                    if (!IsReservedKey(pair.Key)) continue;
                    if (!pair.Key.StartsWith(Prefix, StringComparison.Ordinal)) throw new FormatException("Mapping metadata prefix casing is not canonical.");
                    var raw = pair.Value ?? string.Empty;
                    var separator = raw.IndexOf('|');
                    if (separator <= 0) throw new FormatException("Mapping metadata category separator is missing.");
                    var categoryToken = raw.Substring(0, separator);
                    if (!Enum.TryParse(categoryToken, false, out ElementCategory category) || !Enum.IsDefined(typeof(ElementCategory), category) ||
                        !string.Equals(categoryToken, Enum.GetName(typeof(ElementCategory), category), StringComparison.Ordinal))
                        throw new FormatException("Mapping metadata category is invalid or non-canonical.");
                    var offset = separator + 1;
                    var measurementItemId = ReadField(raw, ref offset);
                    var classificationId = ReadField(raw, ref offset);
                    var workItemId = ReadField(raw, ref offset);
                    if (offset != raw.Length) throw new FormatException("Mapping metadata contains trailing data.");
                    mappings.Add(new MeasurementWorkItemMapping(pair.Key.Substring(Prefix.Length), category, measurementItemId, classificationId, workItemId));
                }
                return new MeasurementWorkItemMappingCatalog(mappings).Mappings;
            }
            catch (FormatException) { throw; }
            catch (ArgumentException ex) { throw new FormatException("Project measurement/work-item mapping metadata is invalid.", ex); }
        }

        private static string Field(string value) => value.Length.ToString(CultureInfo.InvariantCulture) + ":" + value;

        private static string ReadField(string raw, ref int offset)
        {
            var colon = raw.IndexOf(':', offset);
            if (colon <= offset) throw new FormatException("Mapping metadata field length is missing.");
            var lengthToken = raw.Substring(offset, colon - offset);
            if (!int.TryParse(lengthToken, NumberStyles.None, CultureInfo.InvariantCulture, out var length) || length < 0 ||
                !string.Equals(lengthToken, length.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal))
                throw new FormatException("Mapping metadata field length is invalid or non-canonical.");
            offset = colon + 1;
            if (length > raw.Length - offset) throw new FormatException("Mapping metadata field exceeds available data.");
            var value = raw.Substring(offset, length);
            offset += length;
            return value;
        }
    }
}
