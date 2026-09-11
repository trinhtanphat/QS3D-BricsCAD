using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace QS3D.Core.BenchmarkParity
{
    /// <summary>
    /// Host-neutral IFC STEP ingestion adapter for the standalone QuantBIM workbench.
    /// The parser intentionally targets the quantity/property/spatial subset needed by
    /// QS workflows and leaves tessellation to a separate renderer adapter.
    /// </summary>
    public sealed class IfcStepStandaloneSource : IIfcStandaloneSource
    {
        public IfcStandaloneDocument Open(string path)
        {
            path = QsModelElementSnapshot.Require(path, "path");
            return Parse(path, File.ReadAllText(path));
        }

        public IfcStandaloneDocument Parse(string path, string stepText)
        {
            path = QsModelElementSnapshot.Require(path, "path");
            if (string.IsNullOrWhiteSpace(stepText)) throw new InvalidDataException("IFC STEP content is empty.");
            if (stepText.IndexOf("ISO-10303-21", StringComparison.OrdinalIgnoreCase) < 0 || stepText.IndexOf("DATA;", StringComparison.OrdinalIgnoreCase) < 0 || stepText.IndexOf("ENDSEC;", StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidDataException("IFC STEP envelope is incomplete.");

            var records = ParseRecords(stepText);
            if (records.Count == 0) throw new InvalidDataException("IFC STEP DATA section contains no entity records.");

            var storeyNames = records.Values.Where(x => x.Entity == "IFCBUILDINGSTOREY").ToDictionary(x => x.Id, x => TextAt(x.Args, 2), StringComparer.OrdinalIgnoreCase);
            var productIds = new HashSet<string>(records.Values.Where(IsProduct).Select(x => x.Id), StringComparer.OrdinalIgnoreCase);
            var storeyByProduct = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var typeByProduct = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var classificationByProduct = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var propertiesByProduct = productIds.ToDictionary(x => x, _ => new List<IfcPropertyNode>(), StringComparer.OrdinalIgnoreCase);
            var quantitiesByProduct = productIds.ToDictionary(x => x, _ => new List<ParsedQuantity>(), StringComparer.OrdinalIgnoreCase);

            foreach (var rel in records.Values.Where(x => x.Entity == "IFCRELCONTAINEDINSPATIALSTRUCTURE"))
            {
                var products = ReferencesAt(rel.Args, 4);
                var storeyRef = ReferenceAt(rel.Args, 5);
                string storey;
                if (!storeyNames.TryGetValue(storeyRef, out storey)) throw new InvalidDataException("Spatial containment references missing IFCBUILDINGSTOREY " + storeyRef + ".");
                foreach (var product in products.Where(productIds.Contains)) AssignUnique(storeyByProduct, product, storey, "storey containment");
            }

            foreach (var rel in records.Values.Where(x => x.Entity == "IFCRELDEFINESBYTYPE"))
            {
                var typeRef = ReferenceAt(rel.Args, 5);
                StepRecord typeRecord;
                if (!records.TryGetValue(typeRef, out typeRecord)) throw new InvalidDataException("Type relationship references missing entity " + typeRef + ".");
                var typeName = TextAt(typeRecord.Args, 2);
                foreach (var product in ReferencesAt(rel.Args, 4).Where(productIds.Contains)) AssignUnique(typeByProduct, product, typeName, "type relationship");
            }

            var propertySets = new Dictionary<string, IReadOnlyList<IfcPropertyNode>>(StringComparer.OrdinalIgnoreCase);
            foreach (var set in records.Values.Where(x => x.Entity == "IFCPROPERTYSET"))
            {
                var prefix = TextAt(set.Args, 2);
                var nodes = new List<IfcPropertyNode>();
                foreach (var propertyRef in ReferencesAt(set.Args, 4))
                {
                    StepRecord property;
                    if (!records.TryGetValue(propertyRef, out property) || property.Entity != "IFCPROPERTYSINGLEVALUE") throw new InvalidDataException("Property set references unsupported or missing property " + propertyRef + ".");
                    var name = TextAt(property.Args, 0);
                    var value = ScalarTextAt(property.Args, 2);
                    nodes.Add(new IfcPropertyNode((prefix.Length == 0 ? "Pset" : prefix) + "." + name, value));
                }
                propertySets.Add(set.Id, new ReadOnlyCollection<IfcPropertyNode>(nodes.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList()));
            }

            var quantitySets = new Dictionary<string, IReadOnlyList<ParsedQuantity>>(StringComparer.OrdinalIgnoreCase);
            foreach (var set in records.Values.Where(x => x.Entity == "IFCELEMENTQUANTITY"))
            {
                var parsed = new List<ParsedQuantity>();
                foreach (var quantityRef in ReferencesAt(set.Args, 5))
                {
                    StepRecord quantity;
                    if (!records.TryGetValue(quantityRef, out quantity)) throw new InvalidDataException("Quantity set references missing quantity " + quantityRef + ".");
                    parsed.Add(ParseQuantity(quantity));
                }
                quantitySets.Add(set.Id, new ReadOnlyCollection<ParsedQuantity>(parsed));
            }

            foreach (var rel in records.Values.Where(x => x.Entity == "IFCRELDEFINESBYPROPERTIES"))
            {
                var definitionRef = ReferenceAt(rel.Args, 5);
                IReadOnlyList<IfcPropertyNode> propertySet;
                IReadOnlyList<ParsedQuantity> quantitySet;
                if (!propertySets.TryGetValue(definitionRef, out propertySet) && !quantitySets.TryGetValue(definitionRef, out quantitySet))
                    throw new InvalidDataException("Property relationship references unsupported or missing definition " + definitionRef + ".");

                foreach (var product in ReferencesAt(rel.Args, 4).Where(productIds.Contains))
                {
                    if (propertySet != null) propertiesByProduct[product].AddRange(propertySet);
                    if (quantitySet != null) quantitiesByProduct[product].AddRange(quantitySet);
                }
            }

            var classifications = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in records.Values.Where(x => x.Entity == "IFCCLASSIFICATIONREFERENCE"))
            {
                var code = TextAt(item.Args, 1);
                if (code.Length == 0) code = TextAt(item.Args, 0);
                classifications.Add(item.Id, code);
            }
            foreach (var rel in records.Values.Where(x => x.Entity == "IFCRELASSOCIATESCLASSIFICATION"))
            {
                var classificationRef = ReferenceAt(rel.Args, 5);
                string code;
                if (!classifications.TryGetValue(classificationRef, out code)) throw new InvalidDataException("Classification relationship references missing IFCCLASSIFICATIONREFERENCE " + classificationRef + ".");
                foreach (var product in ReferencesAt(rel.Args, 4).Where(productIds.Contains)) AssignUnique(classificationByProduct, product, code, "classification relationship");
            }

            var elements = new List<IfcStandaloneElement>();
            var guids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var product in records.Values.Where(IsProduct).OrderBy(x => NumericId(x.Id)))
            {
                var guid = TextAt(product.Args, 0);
                if (guid.Length == 0) throw new InvalidDataException(product.Id + " " + product.Entity + " has no GlobalId.");
                if (!guids.Add(guid)) throw new InvalidDataException("Duplicate IFC GlobalId: " + guid + ".");
                var entity = CanonicalEntity(product.Entity);
                string storey; storeyByProduct.TryGetValue(product.Id, out storey);
                string type; typeByProduct.TryGetValue(product.Id, out type);
                string classification; classificationByProduct.TryGetValue(product.Id, out classification);
                storey = storey ?? string.Empty;
                type = type ?? string.Empty;
                classification = classification ?? string.Empty;

                var qto = quantitiesByProduct[product.Id]
                    .Select(x => new IfcQtoItem(guid, entity, storey, classification, x.Name, x.Value, x.Unit))
                    .OrderBy(x => x.QuantityName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(x => x.Unit, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var props = propertiesByProduct[product.Id]
                    .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(x => x.Last())
                    .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                elements.Add(new IfcStandaloneElement(guid, entity, TextAt(product.Args, 2), storey, type, classification, props, qto, "ifc-step://" + product.Id));
            }

            return new IfcStandaloneDocument(path, Revision(stepText), elements.OrderBy(x => x.Storey, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Entity, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Guid, StringComparer.OrdinalIgnoreCase));
        }

        private static Dictionary<string, StepRecord> ParseRecords(string text)
        {
            var dataStart = text.IndexOf("DATA;", StringComparison.OrdinalIgnoreCase);
            var dataEnd = text.IndexOf("ENDSEC;", dataStart + 5, StringComparison.OrdinalIgnoreCase);
            if (dataStart < 0 || dataEnd < 0) throw new InvalidDataException("IFC STEP DATA section is missing.");
            var data = text.Substring(dataStart + 5, dataEnd - dataStart - 5);
            var result = new Dictionary<string, StepRecord>(StringComparer.OrdinalIgnoreCase);
            foreach (var statement in SplitStatements(data))
            {
                var trimmed = statement.Trim();
                if (trimmed.Length == 0) continue;
                var equals = trimmed.IndexOf('=');
                if (equals <= 1 || trimmed[0] != '#') throw new InvalidDataException("Malformed STEP entity statement: " + trimmed + ".");
                var id = trimmed.Substring(0, equals).Trim();
                var rhs = trimmed.Substring(equals + 1).Trim();
                var open = rhs.IndexOf('(');
                if (open <= 0 || rhs[rhs.Length - 1] != ')') throw new InvalidDataException("Malformed STEP entity payload for " + id + ".");
                var entity = rhs.Substring(0, open).Trim().ToUpperInvariant();
                var args = SplitTopLevel(rhs.Substring(open + 1, rhs.Length - open - 2));
                if (result.ContainsKey(id)) throw new InvalidDataException("Duplicate STEP entity id: " + id + ".");
                result.Add(id, new StepRecord(id, entity, args));
            }
            return result;
        }

        private static IEnumerable<string> SplitStatements(string data)
        {
            var builder = new StringBuilder();
            var quoted = false;
            for (var i = 0; i < data.Length; i++)
            {
                var c = data[i];
                if (c == '\'' && quoted && i + 1 < data.Length && data[i + 1] == '\'') { builder.Append(c).Append(data[++i]); continue; }
                if (c == '\'') quoted = !quoted;
                if (c == ';' && !quoted) { yield return builder.ToString(); builder.Length = 0; }
                else builder.Append(c);
            }
            if (builder.ToString().Trim().Length != 0) throw new InvalidDataException("Unterminated STEP entity statement.");
        }

        private static List<string> SplitTopLevel(string value)
        {
            var result = new List<string>();
            var builder = new StringBuilder();
            var depth = 0;
            var quoted = false;
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (c == '\'' && quoted && i + 1 < value.Length && value[i + 1] == '\'') { builder.Append(c).Append(value[++i]); continue; }
                if (c == '\'') quoted = !quoted;
                if (!quoted) { if (c == '(') depth++; else if (c == ')') depth--; }
                if (c == ',' && !quoted && depth == 0) { result.Add(builder.ToString().Trim()); builder.Length = 0; }
                else builder.Append(c);
                if (depth < 0) throw new InvalidDataException("Unbalanced STEP parentheses.");
            }
            if (quoted || depth != 0) throw new InvalidDataException("Unbalanced STEP argument payload.");
            result.Add(builder.ToString().Trim());
            return result;
        }

        private static bool IsProduct(StepRecord record)
        {
            switch (record.Entity)
            {
                case "IFCWALL": case "IFCWALLSTANDARDCASE": case "IFCSLAB": case "IFCBEAM": case "IFCCOLUMN": case "IFCDOOR": case "IFCWINDOW": case "IFCSPACE": return true;
                default: return false;
            }
        }

        private static string CanonicalEntity(string entity)
        {
            var value = entity.StartsWith("IFC", StringComparison.Ordinal) ? "Ifc" + entity.Substring(3).ToLowerInvariant() : entity;
            return value.Length <= 3 ? value : value.Substring(0, 3) + char.ToUpperInvariant(value[3]) + value.Substring(4);
        }

        private static ParsedQuantity ParseQuantity(StepRecord record)
        {
            string unit;
            switch (record.Entity)
            {
                case "IFCQUANTITYLENGTH": unit = "m"; break;
                case "IFCQUANTITYAREA": unit = "m2"; break;
                case "IFCQUANTITYVOLUME": unit = "m3"; break;
                case "IFCQUANTITYCOUNT": unit = "count"; break;
                case "IFCQUANTITYWEIGHT": unit = "kg"; break;
                default: throw new InvalidDataException("Unsupported IFC quantity entity " + record.Entity + ".");
            }
            var explicitUnit = record.Args.Count > 2 ? record.Args[2] : "$";
            if (explicitUnit != "$" && explicitUnit != "*") throw new InvalidDataException("Explicit IFC unit references require unit-assignment resolution and are not supported by this ingestion slice: " + explicitUnit + ".");
            if (record.Args.Count < 4) throw new InvalidDataException(record.Entity + " has no quantity value.");
            double number;
            if (!double.TryParse(record.Args[3], NumberStyles.Float, CultureInfo.InvariantCulture, out number) || double.IsNaN(number) || double.IsInfinity(number)) throw new InvalidDataException("Invalid IFC quantity value in " + record.Id + ".");
            return new ParsedQuantity(TextAt(record.Args, 0), number, unit);
        }

        private static void AssignUnique(IDictionary<string, string> map, string key, string value, string relationship)
        {
            string existing;
            if (map.TryGetValue(key, out existing) && !string.Equals(existing, value, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Conflicting " + relationship + " for " + key + ".");
            map[key] = value ?? string.Empty;
        }

        private static string TextAt(IReadOnlyList<string> args, int index) { return index >= args.Count ? string.Empty : Unquote(args[index]); }
        private static string ScalarTextAt(IReadOnlyList<string> args, int index)
        {
            if (index >= args.Count) return string.Empty;
            var token = args[index].Trim();
            var open = token.IndexOf('(');
            if (open > 0 && token[token.Length - 1] == ')') token = token.Substring(open + 1, token.Length - open - 2);
            if (token == ".T.") return "TRUE";
            if (token == ".F.") return "FALSE";
            return Unquote(token);
        }
        private static string Unquote(string token)
        {
            token = (token ?? string.Empty).Trim();
            if (token == "$" || token == "*") return string.Empty;
            return token.Length >= 2 && token[0] == '\'' && token[token.Length - 1] == '\'' ? token.Substring(1, token.Length - 2).Replace("''", "'") : token.Trim('.');
        }
        private static string ReferenceAt(IReadOnlyList<string> args, int index)
        {
            if (index >= args.Count) throw new InvalidDataException("STEP relationship is missing a reference argument.");
            var value = args[index].Trim();
            if (!value.StartsWith("#", StringComparison.Ordinal)) throw new InvalidDataException("Expected STEP entity reference, got " + value + ".");
            return value;
        }
        private static IReadOnlyList<string> ReferencesAt(IReadOnlyList<string> args, int index)
        {
            if (index >= args.Count) throw new InvalidDataException("STEP relationship is missing a reference list.");
            var value = args[index].Trim();
            if (value.Length < 2 || value[0] != '(' || value[value.Length - 1] != ')') throw new InvalidDataException("Expected STEP entity reference list, got " + value + ".");
            var refs = SplitTopLevel(value.Substring(1, value.Length - 2));
            if (refs.Any(x => !x.StartsWith("#", StringComparison.Ordinal))) throw new InvalidDataException("STEP reference list contains a non-reference value.");
            return new ReadOnlyCollection<string>(refs);
        }
        private static int NumericId(string id) { int value; return int.TryParse(id.TrimStart('#'), NumberStyles.Integer, CultureInfo.InvariantCulture, out value) ? value : int.MaxValue; }
        private static string Revision(string text)
        {
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(text.Replace("\r\n", "\n")));
                return "IFCSTEP-" + BitConverter.ToString(hash).Replace("-", string.Empty).Substring(0, 16);
            }
        }

        private sealed class StepRecord
        {
            internal StepRecord(string id, string entity, IReadOnlyList<string> args) { Id = id; Entity = entity; Args = args; }
            internal string Id { get; private set; }
            internal string Entity { get; private set; }
            internal IReadOnlyList<string> Args { get; private set; }
        }
        private sealed class ParsedQuantity
        {
            internal ParsedQuantity(string name, double value, string unit) { Name = QsModelElementSnapshot.Require(name, "name"); Value = value; Unit = unit; }
            internal string Name { get; private set; }
            internal double Value { get; private set; }
            internal string Unit { get; private set; }
        }
    }
}
