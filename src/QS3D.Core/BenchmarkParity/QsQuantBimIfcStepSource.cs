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
    /// Host-neutral IFC STEP ingestion adapter for the QuantBIM workbench.
    /// Resolves QS-facing identity, relationships, properties and base quantities.
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
                    if (!records.TryGetValue(propertyRef, out property)) throw new InvalidDataException("Property set references missing property " + propertyRef + ".");
                    nodes.AddRange(ParsePropertyNodes(property, prefix, records));
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
                case "IFCWALL": case "IFCWALLSTANDARDCASE": case "IFCSLAB": case "IFCBEAM": case "IFCCOLUMN": case "IFCDOOR": case "IFCWINDOW": case "IFCSPACE":
                case "IFCFOOTING": case "IFCPILE": case "IFCROOF": case "IFCCURTAINWALL": case "IFCMEMBER": case "IFCPLATE": return true;
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
                case "IFCFOOTING": return "IfcFooting";
                case "IFCPILE": return "IfcPile";
                case "IFCROOF": return "IfcRoof";
                case "IFCCURTAINWALL": return "IfcCurtainWall";
                case "IFCMEMBER": return "IfcMember";
                case "IFCPLATE": return "IfcPlate";
                default: throw new InvalidDataException("Unsupported IFC product entity " + entity + ".");
            }
        }

        private static IReadOnlyList<IfcPropertyNode> ParsePropertyNodes(StepRecord property, string prefix, IDictionary<string, StepRecord> records)
        {
            if (property.Args.Count < 3) throw new InvalidDataException(property.Entity + " " + property.Id + " is incomplete.");
            var propertyName = TextAt(property.Args, 0);
            if (propertyName.Length == 0) throw new InvalidDataException(property.Entity + " " + property.Id + " has no property name.");
            var baseName = (prefix.Length == 0 ? "Pset" : prefix) + "." + propertyName;
            switch (property.Entity)
            {
                case "IFCPROPERTYSINGLEVALUE":
                    return new ReadOnlyCollection<IfcPropertyNode>(new List<IfcPropertyNode> { new IfcPropertyNode(baseName, ScalarTextAt(property.Args, 2)) });
                case "IFCPROPERTYENUMERATEDVALUE":
                    return ParseEnumeratedProperty(property, baseName, records);
                case "IFCPROPERTYLISTVALUE":
                    return IndexedPropertyNodes(baseName, ParsePropertyValueAggregate(property.Args[2], property.Id + " list values"));
                case "IFCPROPERTYBOUNDEDVALUE":
                    return ParseBoundedProperty(property, baseName);
                default:
                    throw new InvalidDataException("Property set references unsupported property entity " + property.Entity + " " + property.Id + ".");
            }
        }

        private static IReadOnlyList<IfcPropertyNode> ParseEnumeratedProperty(StepRecord property, string baseName, IDictionary<string, StepRecord> records)
        {
            if (property.Args.Count < 4) throw new InvalidDataException("IFCPROPERTYENUMERATEDVALUE " + property.Id + " is incomplete.");
            var selected = ParsePropertyValueAggregate(property.Args[2], property.Id + " enumeration values");
            var reference = property.Args[3].Trim();
            if (reference != "$" && reference != "*")
            {
                if (!reference.StartsWith("#", StringComparison.Ordinal)) throw new InvalidDataException("IFCPROPERTYENUMERATEDVALUE " + property.Id + " has invalid EnumerationReference " + reference + ".");
                StepRecord enumeration;
                if (!records.TryGetValue(reference, out enumeration) || enumeration.Entity != "IFCPROPERTYENUMERATION" || enumeration.Args.Count < 2)
                    throw new InvalidDataException("IFCPROPERTYENUMERATEDVALUE " + property.Id + " references invalid IFCPROPERTYENUMERATION " + reference + ".");
                var allowed = ParsePropertyValueAggregate(enumeration.Args[1], enumeration.Id + " allowed enumeration values");
                if (allowed.Count == 0) throw new InvalidDataException("IFCPROPERTYENUMERATION " + enumeration.Id + " has no allowed values.");
                if (selected.Count > 0 && !string.Equals(selected[0].Type, allowed[0].Type, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("IFC enumeration value type mismatch for " + property.Id + ".");
                var allowedKeys = new HashSet<string>(allowed.Select(x => x.Key), StringComparer.Ordinal);
                foreach (var value in selected)
                    if (!allowedKeys.Contains(value.Key)) throw new InvalidDataException("IFC enumeration value " + value.Value + " is not admitted by " + enumeration.Id + ".");
            }
            return IndexedPropertyNodes(baseName, selected);
        }

        private static IReadOnlyList<IfcPropertyNode> ParseBoundedProperty(StepRecord property, string baseName)
        {
            if (property.Args.Count < 5) throw new InvalidDataException("IFCPROPERTYBOUNDEDVALUE " + property.Id + " is incomplete.");
            var upper = ParseOptionalPropertyValue(property.Args[2], property.Id + " upper bound");
            var lower = ParseOptionalPropertyValue(property.Args[3], property.Id + " lower bound");
            var setPoint = property.Args.Count > 5 ? ParseOptionalPropertyValue(property.Args[5], property.Id + " set point") : null;
            var values = new[] { upper, lower, setPoint }.OfType<ParsedPropertyValue>().ToList();
            if (values.Count == 0) return new ReadOnlyCollection<IfcPropertyNode>(new List<IfcPropertyNode> { new IfcPropertyNode(baseName, string.Empty) });
            var type = values[0].Type;
            if (values.Any(x => !string.Equals(x.Type, type, StringComparison.OrdinalIgnoreCase))) throw new InvalidDataException("IFCPROPERTYBOUNDEDVALUE " + property.Id + " mixes value types.");
            var nodes = new List<IfcPropertyNode>();
            if (lower != null) nodes.Add(new IfcPropertyNode(baseName + ".Lower", lower.Value));
            if (upper != null) nodes.Add(new IfcPropertyNode(baseName + ".Upper", upper.Value));
            if (setPoint != null) nodes.Add(new IfcPropertyNode(baseName + ".SetPoint", setPoint.Value));
            return new ReadOnlyCollection<IfcPropertyNode>(nodes);
        }

        private static IReadOnlyList<IfcPropertyNode> IndexedPropertyNodes(string baseName, IReadOnlyList<ParsedPropertyValue> values)
        {
            if (values.Count == 0) return new ReadOnlyCollection<IfcPropertyNode>(new List<IfcPropertyNode> { new IfcPropertyNode(baseName, string.Empty) });
            var nodes = new List<IfcPropertyNode>();
            for (var i = 0; i < values.Count; i++) nodes.Add(new IfcPropertyNode(baseName + "[" + i.ToString("D10", CultureInfo.InvariantCulture) + "]", values[i].Value));
            return new ReadOnlyCollection<IfcPropertyNode>(nodes);
        }

        private static IReadOnlyList<ParsedPropertyValue> ParsePropertyValueAggregate(string token, string context)
        {
            token = (token ?? string.Empty).Trim();
            if (token == "$" || token == "*") return new ReadOnlyCollection<ParsedPropertyValue>(new List<ParsedPropertyValue>());
            if (token.Length < 2 || token[0] != '(' || token[token.Length - 1] != ')') throw new InvalidDataException("Expected IFC property value aggregate for " + context + ", got " + token + ".");
            var raw = SplitTopLevel(token.Substring(1, token.Length - 2));
            if (raw.Count == 0 || (raw.Count == 1 && raw[0].Length == 0)) throw new InvalidDataException("IFC property value aggregate is empty for " + context + ".");
            var values = raw.Select(x => ParseRequiredPropertyValue(x, context)).ToList();
            var type = values[0].Type;
            if (values.Any(x => !string.Equals(x.Type, type, StringComparison.OrdinalIgnoreCase))) throw new InvalidDataException("IFC property value aggregate mixes value types for " + context + ".");
            return new ReadOnlyCollection<ParsedPropertyValue>(values);
        }

        private static ParsedPropertyValue? ParseOptionalPropertyValue(string token, string context)
        {
            token = (token ?? string.Empty).Trim();
            return token == "$" || token == "*" ? null : ParseRequiredPropertyValue(token, context);
        }

        private static ParsedPropertyValue ParseRequiredPropertyValue(string token, string context)
        {
            token = (token ?? string.Empty).Trim();
            var open = token.IndexOf('(');
            if (open <= 0 || token[token.Length - 1] != ')') throw new InvalidDataException("Expected typed IFC property value for " + context + ", got " + token + ".");
            var type = token.Substring(0, open).Trim().ToUpperInvariant();
            if (type.Length == 0) throw new InvalidDataException("IFC property value type is empty for " + context + ".");
            var scalar = token.Substring(open + 1, token.Length - open - 2).Trim();
            if (scalar.Length == 0) throw new InvalidDataException("IFC property value is empty for " + context + ".");
            var value = ScalarTokenText(scalar);
            return new ParsedPropertyValue(type, value, PropertyValueKey(type, scalar));
        }

        private static string PropertyValueKey(string type, string scalar)
        {
            scalar = scalar.Trim();
            if (scalar == ".T.") return type + "|B:TRUE";
            if (scalar == ".F.") return type + "|B:FALSE";
            if (scalar == ".U.") return type + "|B:UNKNOWN";
            if (scalar.Length >= 2 && scalar[0] == '\'' && scalar[scalar.Length - 1] == '\'') return type + "|S:" + Unquote(scalar);
            double number;
            if (double.TryParse(scalar, NumberStyles.Float, CultureInfo.InvariantCulture, out number) && !double.IsNaN(number) && !double.IsInfinity(number))
                return type + "|N:" + number.ToString("R", CultureInfo.InvariantCulture);
            return type + "|S:" + Unquote(scalar);
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
        private static string ScalarTextAt(IReadOnlyList<string> args, int index) { return index >= args.Count ? string.Empty : ScalarTokenText(args[index]); }
        private static string ScalarTokenText(string token)
        {
            token = (token ?? string.Empty).Trim();
            var open = token.IndexOf('(');
            if (open > 0 && token[token.Length - 1] == ')') token = token.Substring(open + 1, token.Length - open - 2).Trim();
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
                    var resolved = ResolveUnitRecord(unitRef, unitType, "global unit assignment", new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                    ResolvedUnit existing;
                    if (_globalByType.TryGetValue(unitType, out existing) && (existing.CanonicalUnit != resolved.CanonicalUnit || existing.Scale != resolved.Scale))
                        throw new InvalidDataException("Conflicting global IFC units for " + unitType + ".");
                    _globalByType[unitType] = resolved;
                }
            }

            private ResolvedUnit ResolveReference(string reference, QuantitySpec spec, string context)
            {
                return ResolveUnitRecord(reference, spec.UnitType, context, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            }

            private ResolvedUnit ResolveUnitRecord(string reference, string expectedType, string context, ISet<string> stack)
            {
                if (!reference.StartsWith("#", StringComparison.Ordinal)) throw new InvalidDataException("Expected IFC unit reference for " + context + ", got " + reference + ".");
                StepRecord unitRecord;
                if (!_records.TryGetValue(reference, out unitRecord)) throw new InvalidDataException("IFC unit reference " + reference + " for " + context + " is missing.");
                if (!stack.Add(reference)) throw new InvalidDataException("Cyclic IFC conversion-unit chain detected at " + reference + " for " + context + ".");
                try
                {
                    ResolvedUnit resolved;
                    switch (unitRecord.Entity)
                    {
                        case "IFCSIUNIT": resolved = ParseSiUnit(unitRecord); break;
                        case "IFCCONVERSIONBASEDUNIT": resolved = ParseConversionBasedUnit(unitRecord, expectedType, context, stack); break;
                        case "IFCCONVERSIONBASEDUNITWITHOFFSET": throw new InvalidDataException("Offset IFC conversion units are not supported for quantity normalization: " + reference + ".");
                        case "IFCCONTEXTDEPENDENTUNIT": throw new InvalidDataException("Context-dependent IFC units are not supported for quantity normalization: " + reference + ".");
                        default: throw new InvalidDataException("Unsupported IFC unit entity " + unitRecord.Entity + " for " + context + ".");
                    }
                    if (!string.Equals(resolved.UnitType, expectedType, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("IFC unit type mismatch for " + context + ": expected " + expectedType + ", got " + resolved.UnitType + ".");
                    return resolved;
                }
                finally { stack.Remove(reference); }
            }

            private ResolvedUnit ParseConversionBasedUnit(StepRecord record, string expectedType, string context, ISet<string> stack)
            {
                if (record.Args.Count < 4) throw new InvalidDataException("IFCCONVERSIONBASEDUNIT " + record.Id + " is incomplete.");
                var unitType = Unquote(record.Args[1]).ToUpperInvariant();
                if (!QuantitySpec.IsRelevantUnitType(unitType)) throw new InvalidDataException("Unsupported IFC conversion unit type " + unitType + " in " + record.Id + ".");
                if (!string.Equals(unitType, expectedType, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("IFC unit type mismatch for " + context + ": expected " + expectedType + ", got " + unitType + ".");
                ValidateDimensions(record, unitType);
                var factorRef = record.Args[3].Trim();
                if (!factorRef.StartsWith("#", StringComparison.Ordinal)) throw new InvalidDataException("IFCCONVERSIONBASEDUNIT " + record.Id + " has invalid ConversionFactor reference " + factorRef + ".");
                StepRecord measure;
                if (!_records.TryGetValue(factorRef, out measure) || measure.Entity != "IFCMEASUREWITHUNIT")
                    throw new InvalidDataException("IFCCONVERSIONBASEDUNIT " + record.Id + " references missing IFCMEASUREWITHUNIT " + factorRef + ".");
                if (measure.Args.Count < 2) throw new InvalidDataException("IFCMEASUREWITHUNIT " + measure.Id + " is incomplete.");
                var factor = ParseConversionMeasure(measure.Args[0], unitType, measure.Id);
                var component = ResolveUnitRecord(measure.Args[1].Trim(), unitType, "conversion factor " + measure.Id, stack);
                var scale = factor * component.Scale;
                if (!(scale > 0d) || double.IsNaN(scale) || double.IsInfinity(scale)) throw new InvalidDataException("IFC conversion-unit scale is not finite and positive in " + record.Id + ".");
                return new ResolvedUnit(unitType, CanonicalUnitFor(unitType), scale);
            }

            private void ValidateDimensions(StepRecord unit, string unitType)
            {
                var reference = unit.Args[0].Trim();
                if (!reference.StartsWith("#", StringComparison.Ordinal)) throw new InvalidDataException("IFCCONVERSIONBASEDUNIT " + unit.Id + " has invalid Dimensions reference " + reference + ".");
                StepRecord dimensions;
                if (!_records.TryGetValue(reference, out dimensions) || dimensions.Entity != "IFCDIMENSIONALEXPONENTS" || dimensions.Args.Count < 7)
                    throw new InvalidDataException("IFCCONVERSIONBASEDUNIT " + unit.Id + " references invalid IFCDIMENSIONALEXPONENTS " + reference + ".");
                var expected = ExpectedDimensions(unitType);
                for (var i = 0; i < 7; i++)
                {
                    int actual;
                    if (!int.TryParse(dimensions.Args[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out actual) || actual != expected[i])
                        throw new InvalidDataException("IFC dimensional exponents mismatch for " + unit.Id + " " + unitType + ".");
                }
            }

            private static int[] ExpectedDimensions(string unitType)
            {
                switch (unitType)
                {
                    case "LENGTHUNIT": return new[] { 1, 0, 0, 0, 0, 0, 0 };
                    case "AREAUNIT": return new[] { 2, 0, 0, 0, 0, 0, 0 };
                    case "VOLUMEUNIT": return new[] { 3, 0, 0, 0, 0, 0, 0 };
                    case "MASSUNIT": return new[] { 0, 1, 0, 0, 0, 0, 0 };
                    default: throw new InvalidDataException("Unsupported IFC dimensional unit type " + unitType + ".");
                }
            }

            private static double ParseConversionMeasure(string token, string unitType, string recordId)
            {
                token = (token ?? string.Empty).Trim();
                var open = token.IndexOf('(');
                if (open <= 0 || token[token.Length - 1] != ')') throw new InvalidDataException("IFCMEASUREWITHUNIT " + recordId + " has malformed ValueComponent " + token + ".");
                var measureType = token.Substring(0, open).Trim().ToUpperInvariant();
                if (!string.Equals(measureType, ExpectedMeasureType(unitType), StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("IFCMEASUREWITHUNIT " + recordId + " has " + measureType + " for " + unitType + ".");
                var scalar = token.Substring(open + 1, token.Length - open - 2).Trim();
                double value;
                if (!double.TryParse(scalar, NumberStyles.Float, CultureInfo.InvariantCulture, out value) || !(value > 0d) || double.IsNaN(value) || double.IsInfinity(value))
                    throw new InvalidDataException("IFCMEASUREWITHUNIT " + recordId + " has invalid conversion factor " + scalar + ".");
                return value;
            }

            private static string ExpectedMeasureType(string unitType)
            {
                switch (unitType)
                {
                    case "LENGTHUNIT": return "IFCLENGTHMEASURE";
                    case "AREAUNIT": return "IFCAREAMEASURE";
                    case "VOLUMEUNIT": return "IFCVOLUMEMEASURE";
                    case "MASSUNIT": return "IFCMASSMEASURE";
                    default: throw new InvalidDataException("Unsupported IFC conversion measure unit type " + unitType + ".");
                }
            }

            private static string CanonicalUnitFor(string unitType)
            {
                switch (unitType)
                {
                    case "LENGTHUNIT": return "m";
                    case "AREAUNIT": return "m2";
                    case "VOLUMEUNIT": return "m3";
                    case "MASSUNIT": return "kg";
                    default: throw new InvalidDataException("Unsupported IFC canonical unit type " + unitType + ".");
                }
            }

            private static string UnitTypeOf(StepRecord record)
            {
                if (record.Args.Count <= 1) return string.Empty;
                switch (record.Entity)
                {
                    case "IFCSIUNIT": case "IFCCONVERSIONBASEDUNIT": case "IFCCONVERSIONBASEDUNITWITHOFFSET": case "IFCCONTEXTDEPENDENTUNIT": return Unquote(record.Args[1]).ToUpperInvariant();
                    default: return string.Empty;
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
                    case "LENGTHUNIT": RequireUnitName(record, name, "METRE"); return new ResolvedUnit(unitType, "m", factor);
                    case "AREAUNIT": RequireUnitName(record, name, "SQUARE_METRE"); return new ResolvedUnit(unitType, "m2", factor * factor);
                    case "VOLUMEUNIT": RequireUnitName(record, name, "CUBIC_METRE"); return new ResolvedUnit(unitType, "m3", factor * factor * factor);
                    case "MASSUNIT": RequireUnitName(record, name, "GRAM"); return new ResolvedUnit(unitType, "kg", factor / 1000d);
                    default: throw new InvalidDataException("Unsupported IFC SI unit type " + unitType + " in " + record.Id + ".");
                }
            }

            private static void RequireUnitName(StepRecord record, string actual, string expected)
            {
                if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("IFCSIUNIT " + record.Id + " has unexpected name " + actual + " for " + Unquote(record.Args[1]) + "; expected " + expected + ".");
            }

            private static double PrefixFactor(string prefix)
            {
                switch (prefix)
                {
                    case "": return 1d; case "EXA": return 1e18; case "PETA": return 1e15; case "TERA": return 1e12; case "GIGA": return 1e9; case "MEGA": return 1e6;
                    case "KILO": return 1e3; case "HECTO": return 1e2; case "DECA": return 1e1; case "DECI": return 1e-1; case "CENTI": return 1e-2; case "MILLI": return 1e-3;
                    case "MICRO": return 1e-6; case "NANO": return 1e-9; case "PICO": return 1e-12; case "FEMTO": return 1e-15; case "ATTO": return 1e-18;
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
            internal static bool IsRelevantUnitType(string unitType) { return unitType == "LENGTHUNIT" || unitType == "AREAUNIT" || unitType == "VOLUMEUNIT" || unitType == "MASSUNIT"; }
        }

        private sealed class ResolvedUnit
        {
            internal ResolvedUnit(string unitType, string canonicalUnit, double scale) { UnitType = unitType; CanonicalUnit = canonicalUnit; Scale = scale; }
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
        private sealed class ParsedPropertyValue
        {
            internal ParsedPropertyValue(string type, string value, string key) { Type = type; Value = value; Key = key; }
            internal string Type { get; private set; }
            internal string Value { get; private set; }
            internal string Key { get; private set; }
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