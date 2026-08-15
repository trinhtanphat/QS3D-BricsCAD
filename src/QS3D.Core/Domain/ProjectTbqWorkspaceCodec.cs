using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using QS3D.Core.Cost;

namespace QS3D.Core.Domain
{
    internal static class ProjectTbqWorkspaceCodec
    {
        internal const string ReservedRoot = "QS3D.TBQ.";
        internal const string Prefix = "QS3D.TBQ.v1.";
        internal const string WorkspaceKey = Prefix + "Workspace";
        private const string PayloadVersion = "1";
        private const int MaxPayloadChars = 1024 * 1024;
        private const int MaxBillItems = 10000;
        private const int MaxBuildUpRates = 10000;
        private const int MaxRateReferences = 50000;
        private const int MaxLibraryEntries = 10000;

        internal static bool IsReservedKey(string key) =>
            key != null && key.StartsWith(ReservedRoot, StringComparison.OrdinalIgnoreCase);

        internal static TbqProjectWorkspaceState? Read(IEnumerable<KeyValuePair<string, string>> metadata)
        {
            if (metadata == null) throw new ArgumentNullException(nameof(metadata));
            var found = false;
            string? payload = null;
            foreach (var pair in metadata)
            {
                if (!IsReservedKey(pair.Key)) continue;
                if (!string.Equals(pair.Key, WorkspaceKey, StringComparison.Ordinal))
                    throw new FormatException("TBQ project metadata contains an unsupported or non-canonical reserved key: " + pair.Key + ".");
                if (found) throw new FormatException("TBQ project metadata contains duplicate workspace state.");
                found = true;
                payload = pair.Value ?? string.Empty;
            }
            return found ? Decode(payload ?? string.Empty) : null;
        }

        internal static string Value(TbqProjectWorkspaceState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var fields = new List<string>();
            fields.Add(PayloadVersion);
            fields.Add(state.Currency);
            fields.Add(Decimal(state.CfaM2));
            fields.Add(Decimal(state.AdjustmentRatioPercent));
            fields.Add(Decimal(state.MarkupRatioPercent));
            fields.Add(state.LibraryId);

            fields.Add(Int(state.BillItems.Count));
            for (var i = 0; i < state.BillItems.Count; i++)
            {
                var item = state.BillItems[i];
                fields.Add(item.ItemCode);
                fields.Add(item.Description);
                fields.Add(item.Unit);
                fields.Add(item.TradeCode);
                fields.Add(Decimal(item.Quantity));
                fields.Add(Decimal(item.UnitRate));
                fields.Add(item.RateCode);
            }

            fields.Add(Int(state.BuildUpRates.Count));
            for (var i = 0; i < state.BuildUpRates.Count; i++)
            {
                var rate = state.BuildUpRates[i];
                fields.Add(rate.RateCode);
                fields.Add(Decimal(rate.UnitRate));
            }

            fields.Add(Int(state.RateReferences.Edges.Count));
            for (var i = 0; i < state.RateReferences.Edges.Count; i++)
            {
                var edge = state.RateReferences.Edges[i];
                fields.Add(edge.SourceRateCode);
                fields.Add(Int((int)edge.TargetKind));
                fields.Add(edge.TargetId);
            }

            fields.Add(Int(state.Library.Entries.Count));
            for (var i = 0; i < state.Library.Entries.Count; i++)
            {
                var entry = state.Library.Entries[i];
                fields.Add(entry.ItemCode);
                fields.Add(entry.Description);
                fields.Add(entry.Unit);
                fields.Add(entry.CategoryPath);
                fields.Add(entry.ReferenceUnitRate.HasValue ? "1" : "0");
                fields.Add(entry.ReferenceUnitRate.HasValue ? Decimal(entry.ReferenceUnitRate.Value) : string.Empty);
            }

            var builder = new StringBuilder();
            for (var i = 0; i < fields.Count; i++) AppendField(builder, fields[i]);
            var payload = builder.ToString();
            if (payload.Length > MaxPayloadChars)
                throw new InvalidOperationException("TBQ project workspace exceeds the maximum supported metadata payload of 1 MiB characters.");
            PersistedTextXml.Verify(payload, nameof(state), "TBQ project workspace metadata");
            Decode(payload);
            return payload;
        }

        private static TbqProjectWorkspaceState Decode(string payload)
        {
            try
            {
                if (payload.Length > MaxPayloadChars)
                    throw new FormatException("TBQ project workspace exceeds the maximum supported metadata payload of 1 MiB characters.");
                PersistedTextXml.Verify(payload, nameof(payload), "TBQ project workspace metadata");
                var offset = 0;
                var version = ReadField(payload, ref offset);
                if (!string.Equals(version, PayloadVersion, StringComparison.Ordinal))
                    throw new FormatException("TBQ project workspace payload version is unsupported: " + version + ".");

                var currency = ReadField(payload, ref offset);
                var cfaM2 = ReadDecimal(payload, ref offset, "CFA");
                var adjustment = ReadDecimal(payload, ref offset, "adjustment ratio");
                var markup = ReadDecimal(payload, ref offset, "markup ratio");
                var libraryId = ReadField(payload, ref offset);

                var billCount = ReadCount(payload, ref offset, MaxBillItems, "bill item");
                var billItems = new List<TbqBillItem>(billCount);
                for (var i = 0; i < billCount; i++)
                {
                    billItems.Add(new TbqBillItem(
                        ReadField(payload, ref offset),
                        ReadField(payload, ref offset),
                        ReadField(payload, ref offset),
                        ReadField(payload, ref offset),
                        ReadDecimal(payload, ref offset, "bill quantity"),
                        ReadDecimal(payload, ref offset, "bill unit rate"),
                        EmptyToNull(ReadField(payload, ref offset))));
                }

                var buildUpCount = ReadCount(payload, ref offset, MaxBuildUpRates, "build-up rate");
                var buildUps = new List<BuildUpRateSnapshot>(buildUpCount);
                for (var i = 0; i < buildUpCount; i++)
                    buildUps.Add(new BuildUpRateSnapshot(ReadField(payload, ref offset), ReadDecimal(payload, ref offset, "build-up unit rate")));

                var referenceCount = ReadCount(payload, ref offset, MaxRateReferences, "rate reference");
                var references = new List<RateReferenceEdge>(referenceCount);
                for (var i = 0; i < referenceCount; i++)
                {
                    var source = ReadField(payload, ref offset);
                    var kindValue = ReadInt(payload, ref offset, "rate reference kind");
                    if (!Enum.IsDefined(typeof(RateReferenceTargetKind), kindValue))
                        throw new FormatException("TBQ rate reference kind is undefined: " + kindValue + ".");
                    references.Add(new RateReferenceEdge(source, (RateReferenceTargetKind)kindValue, ReadField(payload, ref offset)));
                }

                var libraryCount = ReadCount(payload, ref offset, MaxLibraryEntries, "BQ library entry");
                var libraryEntries = new List<BqLibraryEntry>(libraryCount);
                for (var i = 0; i < libraryCount; i++)
                {
                    var itemCode = ReadField(payload, ref offset);
                    var description = ReadField(payload, ref offset);
                    var unit = ReadField(payload, ref offset);
                    var categoryPath = ReadField(payload, ref offset);
                    var hasRate = ReadField(payload, ref offset);
                    var rateValue = ReadField(payload, ref offset);
                    decimal? referenceRate;
                    if (string.Equals(hasRate, "0", StringComparison.Ordinal))
                    {
                        if (rateValue.Length != 0) throw new FormatException("TBQ BQ library entry has a rate value without a presence marker.");
                        referenceRate = null;
                    }
                    else if (string.Equals(hasRate, "1", StringComparison.Ordinal))
                    {
                        if (rateValue.Length == 0) throw new FormatException("TBQ BQ library entry rate marker requires a value.");
                        referenceRate = ParseDecimal(rateValue, "BQ library reference unit rate");
                    }
                    else
                    {
                        throw new FormatException("TBQ BQ library entry rate marker must be 0 or 1.");
                    }
                    libraryEntries.Add(new BqLibraryEntry(itemCode, description, unit, categoryPath, referenceRate));
                }

                if (offset != payload.Length) throw new FormatException("TBQ project workspace contains trailing data.");
                return new TbqProjectWorkspaceState(currency, cfaM2, billItems, buildUps, references, libraryId, libraryEntries, adjustment, markup);
            }
            catch (FormatException) { throw; }
            catch (ArgumentException ex) { throw new FormatException("TBQ project workspace metadata is invalid.", ex); }
            catch (OverflowException ex) { throw new FormatException("TBQ project workspace metadata overflowed a supported numeric range.", ex); }
            catch (InvalidOperationException ex) { throw new FormatException("TBQ project workspace metadata is inconsistent.", ex); }
        }

        private static void AppendField(StringBuilder builder, string value)
        {
            value = value ?? string.Empty;
            builder.Append(value.Length.ToString(CultureInfo.InvariantCulture));
            builder.Append(':');
            builder.Append(value);
        }

        private static string ReadField(string payload, ref int offset)
        {
            var colon = payload.IndexOf(':', offset);
            if (colon <= offset) throw new FormatException("TBQ project workspace field length is missing.");
            var lengthToken = payload.Substring(offset, colon - offset);
            if (!int.TryParse(lengthToken, NumberStyles.None, CultureInfo.InvariantCulture, out var length) || length < 0 ||
                !string.Equals(lengthToken, length.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal))
                throw new FormatException("TBQ project workspace field length is invalid or non-canonical.");
            offset = colon + 1;
            if (length > payload.Length - offset) throw new FormatException("TBQ project workspace field exceeds available data.");
            var value = payload.Substring(offset, length);
            offset += length;
            return value;
        }

        private static int ReadCount(string payload, ref int offset, int maximum, string label)
        {
            var value = ReadInt(payload, ref offset, label + " count");
            if (value < 0 || value > maximum)
                throw new FormatException("TBQ " + label + " count is outside the supported range 0.." + maximum + ".");
            return value;
        }

        private static int ReadInt(string payload, ref int offset, string label)
        {
            var token = ReadField(payload, ref offset);
            if (!int.TryParse(token, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value) ||
                !string.Equals(token, value.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal))
                throw new FormatException("TBQ " + label + " is invalid or non-canonical.");
            return value;
        }

        private static decimal ReadDecimal(string payload, ref int offset, string label) => ParseDecimal(ReadField(payload, ref offset), label);

        private static decimal ParseDecimal(string token, string label)
        {
            const NumberStyles styles = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent;
            if (!decimal.TryParse(token, styles, CultureInfo.InvariantCulture, out var value) ||
                !string.Equals(token, Decimal(value), StringComparison.Ordinal))
                throw new FormatException("TBQ " + label + " is invalid or non-canonical.");
            return value == 0m ? 0m : value;
        }

        private static string Decimal(decimal value) => value.ToString("G29", CultureInfo.InvariantCulture);
        private static string Int(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static string? EmptyToNull(string value) => value.Length == 0 ? null : value;
    }
}
