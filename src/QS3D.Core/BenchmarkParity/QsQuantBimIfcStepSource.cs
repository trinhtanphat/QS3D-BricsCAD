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
    /// This slice resolves QS-facing identity, spatial/type/classification relationships,
    /// property sets and base quantities; tessellation remains a renderer-adapter concern.
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
            if (stepText.IndexOf("ISO-10303-21", StringComparison.OrdinalIgnoreCase) < 0 || stepText.IndexOf("DATA;", StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidDataException("IFC STEP envelope is incomplete.");

            var records = ParseRecords(stepText);
            if (records.Count == 0) throw new InvalidDataException("IFC STEP DATA section contains no entity records.");
            var units = new QuantityUnitResolver(records);

            var storeyNames = records.Values.Where(x => x.Entity == "IFCBUILDINGSTOREY")
                .ToDictionary(x => x.Id, x => TextAt(x.Args, 2), StringComparer.OrdinalIgnoreCase);
            var productIds = new HashSet<string>(records.Values.Where(IsProduct).Select(x => x.Id), StringComparer.OrdinalIgnoreCase);
            var storeyByProduct = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var typeByProduct = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var classificationByProduct = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var propertiesByProduct = productIds.ToDictionary(x => x, x => new List<IfcPropertyNode>(), StringComparer.OrdinalIgnoreCase);
            var quantitiesByProduct = productIds.ToDictionary(x => x, x => new List<ParsedQuantity>(), StringComparer.OrdinalIgnoreCase);

            foreach (var rel in records.Values.Where(x => x.Entity == "IFCRELCONTAINEDINSPATIALSTRUCTURE"))
            {
                var storeyRef = ReferenceAt(rel.Args, 5);
                string storey;
                if (!storeyNames.TryGetValue(storeyRef, out storey)) throw new InvalidDataException("Spatial containment references missing IFCBUILDINGSTOREY " + storeyRef + ".");
                foreach (var product in ReferencesAt(rel.Args, 4).Where(productIds.Contains)) AssignUnique(storeyByProduct, product, storey, "storey containment");
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
                    if (!records.TryGetValue(propertyRef, out property) || property.Entity != "IFCPROPERTYSINGLEVALUE")
                        throw new InvalidDataException("Property set references unsupported or missing property " + propertyRef + ".");
                    nodes.Add(new IfcPropertyNode((prefix.Length == 0 ? "Pset" : prefix) + "." + TextAt(property.Args, 0), ScalarTextAt(property.Args, 2)));
                }
                propertySets.Add(set.Id, new ReadOnlyCollection<IfcPropertyNode>(nodes.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList()));
            }

            var quantitySets = new Dictionary<string, IReadOnlyList<ParsedQuantity>>(StringComparer.OrdinalIgnoreCase);
            foreach (var set in records.Values.Where(x => x.Entity == "IFCELEMENTQUANTITY"))
            {
                var quantities = new List<ParsedQuantity>();
                foreach (var quantityRef in ReferencesAt(set.Args, 5))
                {
                    StepRecord quantity;
                    if (!records.TryGetValue(quantityRef, out quantity)) throw new InvalidDataException("Quantity set references missing quantity " + quantityRef + ".");
                    quantities.Add(ParseQuantity(quantity, units));
                }
                quantitySets.Add(set.Id, new ReadOnlyCollection<ParsedQuantity>(quantities));
            }

            foreach (var rel in records.Values.Where(x => x.Entity == "IFCRELDEFINESBYPROPERTIES"))
            {
                var definitionRef = ReferenceAt(rel.Args, 5);
                IReadOnlyList<IfcPropertyNode> propertySet;
                IReadOnlyList<ParsedQuantity> quantitySet;
                if (propertySets.TryGetValue(definitionRef, out propertySet))
                {
                    foreach (var product in ReferencesAt(rel.Args, 4).Where(productIds.Contains)) propertiesByProduct[product].AddRange(propertySet);
                }
                else if (quantitySets.TryGetValue(definitionRef, out quantitySet))
                {
                    foreach (var product in ReferencesAt(rel.Args, 4).Where(productIds.Contains)) quantitiesByProduct[product].AddRange(quantitySet);
                }
                else throw new InvalidDataException("Property relationship references unsupported or missing definition " + definitionRef + ".");
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

                string storey; if (!storeyByProduct.TryGetValue(product.Id, out storey)) storey = string.Empty;
                string type; if (!typeByProduct.TryGetValue(product.Id, out type)) type = string.Empty;
                string classification; if (!classificationByProduct.TryGetValue(product.Id, out classification)) classification = string.Empty;
                var entity = CanonicalEntity(product.Entity);
                var qto = quantitiesByProduct[product.Id]
                    .Select(x => new IfcQtoItem(guid, entity, storey, classification, x.Name, x.Value, x.Unit))
                    .OrderBy(x => x.QuantityName, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Unit, StringComparer.OrdinalIgnoreCase).ToList();
                var props = propertiesByProduct[product.Id]
                    .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase).Select(x => x.Last())
                    .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
                elements.Add(new IfcStandaloneElement(guid, entity, TextAt(product.Args, 2), storey, type, classification, props, qto, "ifc-step://" + product.Id));
            }

            return new IfcStandaloneDocument(path, Revision(stepText), elements
                .OrderBy(x => x.Storey, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Entity, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Guid, StringComparer.OrdinalIgnoreCase));
        }

        private static Dictionary<string, StepRecord> ParseRecords(string text)
        {
            var dataStart = text.IndexOf("DATA;", StringComparison.OrdinalIgnoreCase);
            var dataEnd = dataStart < 0 ? -1 : text.IndexOf("ENDSEC;", dataStart + 5, StringComparison.OrdinalIgnoreCase);
            if (dataStart < 0 || dataEnd < 0) throw new InvalidDataException("IFC STEP DATA section is missing or unterminated.");
            var result = new Dictionary<string, StepRecord>(StringComparer.OrdinalIgnoreCase);
            foreach (var statement in SplitStatements(text.Substring(dataStart + 5, dataEnd - dataStart - 5)))
            {
                var value = statement.Trim();
                if (value.Length == 0) continue;
                var equals = value.IndexOf('=');
                if (equals <= 1 || value[0] != '#') throw new InvalidDataException("Malformed STEP entity statement: " + value + ".");
                var id = value.Substring(0, equals).Trim();
                var rhs = value.Substring(equals + 1).Trim();
                var open = rhs.IndexOf('(');
                if (open <= 0 || rhs[rhs.Length - 1] != ')') throw new InvalidDataException("Malformed STEP entity payload for " + id + ".");
                var record = new StepRecord(id, rhs.Substring(0, open).Trim().ToUpperInvariant(), SplitTopLevel(rhs.Substring(open + 1, rhs.Length - open - 2)));
                if (result.ContainsKey(id)) throw new InvalidDataException("Duplicate STEP entity id: " + id + ".");
                result.Add(id, record);
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
            if (quoted || builder.ToString().Trim().Length != 0) throw new InvalidDataException("Unterminated STEP entity statement.");
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
            switch (entity)
            {
                case "IFCWALL": case "IFCWALLSTANDARDCASE": return "IfcWall";
                case "IFCSLAB": return "IfcSlab";
                case "IFCBEAM": return "IfcBeam";
                case "IFCCOLUMN": return "IfcColumn";
                case "IFCDOOR": return "IfcDoor";
                case "IFCWINDOW": return "IfcWindow";
                case "IFCSPACE": return "IfcSpace";
                default: throw new InvalidDataException("Unsupported IFC product entity " + entity + ".");
            }
        }

        private static ParsedQuantity ParseQuantity(StepRecord record, QuantityUnitResolver units)
        {
            if (record.Args.Count < 4) throw new InvalidDataException(record.Entity + " has no quantity value.");
            var spec = QuantitySpec.For(record.Entity);
            var resolved = units.Resolve(record, spec);
            double number;
            if (!double.TryParse(record.Args[3], NumberStyles.Float, CultureInfo.InvariantCulture, out number) || double.IsNaN(number) || double.IsInfinity(number))
                throw new InvalidDataException("Invalid IFC quantity value in " + record.Id + ".");
            var canonical = number * resolved.Scale;
            if (double.IsNaN(canonical) || double.IsInfinity(canonical)) throw new InvalidDataException("IFC quantity conversion is non-finite in " + record.Id + ".");
            return new ParsedQuantity(TextAt(record.Args, 0), canonical, resolved.CanonicalUnit);
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

        private sealed class QuantityUnitResolver
        {
            private readonly IDictionary<string, StepRecord> _records;
            private readonly Dictionary<string, ResolvedUnit> _globalByType = new Dictionary<string, ResolvedUnit>(StringComparer.OrdinalIgnoreCase);

            internal QuantityUnitResolver(IDictionary<string, StepRecord> records)
            {
                _records = records ?? throw new ArgumentNullException("records");
                LoadGlobalUnits();
            }

            internal ResolvedUnit Resolve(StepRecord quantity, QuantitySpec spec)
            {
                var token = quantity.Args[2].Trim();
                if (spec.UnitType.Length == 0)
                {
                    if (token != "$" && token != "*") throw new InvalidDataException("Explicit IFC count units are not supported: " + token + ".");
                    return new ResolvedUnit(string.Empty, spec.CanonicalUnit, 1d);
                }

                if (token != "$" && token != "*") return ResolveReference(token, spec, "quantity " + quantity.Id);
                ResolvedUnit global;
                return _globalByType.TryGetValue(spec.UnitType, out global) ? global : new ResolvedUnit(spec.UnitType, spec.CanonicalUnit, 1d);
            }

            private void LoadGlobalUnits()
            {
                var assignmentRefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var project in _records.Values.Where(x => x.Entity == "IFCPROJECT"))
                {
                    if (project.Args.Count <= 8) continue;
                    var token = project.Args[8].Trim();
                    if (token == "$" || token == "*") continue;
                    if (!token.StartsWith("#", StringComparison.Ordinal)) throw new InvalidDataException("IFCPROJECT UnitsInContext is not a STEP reference: " + token + ".");
                    assignmentRefs.Add(token);
                }

                if (assignmentRefs.Count > 1) throw new InvalidDataException("Multiple IFCPROJECT unit assignments are ambiguous for standalone quantity normalization.");
                if (assignmentRefs.Count == 0) return;

                var assignmentRef = assignmentRefs.Single();
                StepRecord assignment;
                if (!_records.TryGetValue(assignmentRef, out assignment) || assignment.Entity != "IFCUNITASSIGNMENT")
                    throw new InvalidDataException("IFCPROJECT UnitsInContext references missing IFCUNITASSIGNMENT " + assignmentRef + ".");

                foreach (var unitRef in ReferencesAt(assignment.Args, 0))
                {
                    StepRecord unitRecord;
                    if (!_records.TryGetValue(unitRef, out unitRecord)) throw new InvalidDataException("IFCUNITASSIGNMENT references missing unit " + unitRef + ".");
                    var unitType = UnitTypeOf(unitRecord);
                    if (!QuantitySpec.IsRelevantUnitType(unitType)) continue;
                    if (unitRecord.Entity != "IFCSIUNIT") throw new InvalidDataException("Unsupported global IFC unit entity " + unitRecord.Entity + " for " + unitType + ".");
                    var resolved = ParseSiUnit(unitRecord);
                    ResolvedUnit existing;
                    if (_globalByType.TryGetValue(unitType, out existing) && (existing.CanonicalUnit != resolved.CanonicalUnit || existing.Scale != resolved.Scale))
                        throw new InvalidDataException("Conflicting global IFC units for " + unitType + ".");
                    _globalByType[unitType] = resolved;
                }
            }

            private ResolvedUnit ResolveReference(string reference, QuantitySpec spec, string context)
            {
                if (!reference.StartsWith("#", StringComparison.Ordinal)) throw new InvalidDataException("Expected IFC unit reference for " + context + ", got " + reference + ".");
                StepRecord unitRecord;
                if (!_records.TryGetValue(reference, out unitRecord)) throw new InvalidDataException("IFC unit reference " + reference + " for " + context + " is missing.");
                if (unitRecord.Entity != "IFCSIUNIT") throw new InvalidDataException("Unsupported explicit IFC unit entity " + unitRecord.Entity + " for " + context + ".");
                var resolved = ParseSiUnit(unitRecord);
                if (!string.Equals(resolved.UnitType, spec.UnitType, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("IFC unit type mismatch for " + context + ": expected " + spec.UnitType + ", got " + resolved.UnitType + ".");
                return resolved;
            }

            private static string UnitTypeOf(StepRecord record)
            {
                if (record.Args.Count <= 1) return string.Empty;
                switch (record.Entity)
                {
                    case "IFCSIUNIT":
                    case "IFCCONVERSIONBASEDUNIT":
                    case "IFCCONVERSIONBASEDUNITWITHOFFSET":
                    case "IFCCONTEXTDEPENDENTUNIT":
                        return Unquote(record.Args[1]).ToUpperInvariant();
                    default:
                        return string.Empty;
                }
            }

            private static ResolvedUnit ParseSiUnit(StepRecord record)
            {
                if (record.Args.Count < 4) throw new InvalidDataException("IFCSIUNIT " + record.Id + " is incomplete.");
                var unitType = Unquote(record.Args[1]).ToUpperInvariant();
                var prefix = Unquote(record.Args[2]).ToUpperInvariant();
                var name = Unquote(record.Args[3]).ToUpperInvariant();
                var factor = PrefixFactor(prefix);
                switch (unitType)
                {
                    case "LENGTHUNIT":
                        RequireUnitName(record, name, "METRE");
                        return new ResolvedUnit(unitType, "m", factor);
                    case "AREAUNIT":
                        RequireUnitName(record, name, "SQUARE_METRE");
                        return new ResolvedUnit(unitType, "m2", factor * factor);
                    case "VOLUMEUNIT":
                        RequireUnitName(record, name, "CUBIC_METRE");
                        return new ResolvedUnit(unitType, "m3", factor * factor * factor);
                    case "MASSUNIT":
                        RequireUnitName(record, name, "GRAM");
                        return new ResolvedUnit(unitType, "kg", factor / 1000d);
                    default:
                        throw new InvalidDataException("Unsupported IFC SI unit type " + unitType + " in " + record.Id + ".");
                }
            }

            private static void RequireUnitName(StepRecord record, string actual, string expected)
            {
                if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("IFCSIUNIT " + record.Id + " has unexpected name " + actual + " for " + Unquote(record.Args[1]) + "; expected " + expected + ".");
            }

            private static double PrefixFactor(string prefix)
            {
                switch (prefix)
                {
                    case "": return 1d;
                    case "EXA": return 1e18;
                    case "PETA": return 1e15;
                    case "TERA": return 1e12;
                    case "GIGA": return 1e9;
                    case "MEGA": return 1e6;
                    case "KILO": return 1e3;
                    case "HECTO": return 1e2;
                    case "DECA": return 1e1;
                    case "DECI": return 1e-1;
                    case "CENTI": return 1e-2;
                    case "MILLI": return 1e-3;
                    case "MICRO": return 1e-6;
                    case "NANO": return 1e-9;
                    case "PICO": return 1e-12;
                    case "FEMTO": return 1e-15;
                    case "ATTO": return 1e-18;
                    default: throw new InvalidDataException("Unsupported IFC SI prefix " + prefix + ".");
                }
            }
        }

        private sealed class QuantitySpec
        {
            private QuantitySpec(string unitType, string canonicalUnit) { UnitType = unitType; CanonicalUnit = canonicalUnit; }
            internal string UnitType { get; private set; }
            internal string CanonicalUnit { get; private set; }

            internal static QuantitySpec For(string entity)
            {
                switch (entity)
                {
                    case "IFCQUANTITYLENGTH": return new QuantitySpec("LENGTHUNIT", "m");
                    case "IFCQUANTITYAREA": return new QuantitySpec("AREAUNIT", "m2");
                    case "IFCQUANTITYVOLUME": return new QuantitySpec("VOLUMEUNIT", "m3");
                    case "IFCQUANTITYCOUNT": return new QuantitySpec(string.Empty, "count");
                    case "IFCQUANTITYWEIGHT": return new QuantitySpec("MASSUNIT", "kg");
                    default: throw new InvalidDataException("Unsupported IFC quantity entity " + entity + ".");
                }
            }

            internal static bool IsRelevantUnitType(string unitType)
            {
                return unitType == "LENGTHUNIT" || unitType == "AREAUNIT" || unitType == "VOLUMEUNIT" || unitType == "MASSUNIT";
            }
        }

        private sealed class ResolvedUnit
        {
            internal ResolvedUnit(string unitType, string canonicalUnit, double scale)
            {
                UnitType = unitType;
                CanonicalUnit = canonicalUnit;
                Scale = scale;
            }
            internal string UnitType { get; private set; }
            internal string CanonicalUnit { get; private set; }
            internal double Scale { get; private set; }
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
