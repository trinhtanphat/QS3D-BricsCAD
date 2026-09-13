using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace QS3D.Core.BenchmarkParity
{
    /// <summary>
    /// Host-neutral IFC STEP geometry resolver for a deliberately bounded IFC4 swept-solid subset.
    /// Geometry is for standalone scene/navigation only; QTO remains authoritative for quantities.
    /// </summary>
    public sealed class IfcStepGeometryResolver : IIfcGeometryResolver
    {
        private readonly IDictionary<string, StepRecord> _records;

        public IfcStepGeometryResolver(string stepText)
        {
            if (string.IsNullOrWhiteSpace(stepText)) throw new InvalidDataException("IFC STEP geometry content is empty.");
            _records = ParseRecords(stepText);
        }

        public IfcSceneMesh Resolve(string geometryReference)
        {
            geometryReference = QsModelElementSnapshot.Require(geometryReference, "geometryReference");
            const string prefix = "ifc-step://";
            if (!geometryReference.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Unsupported IFC geometry reference: " + geometryReference + ".");

            var productId = geometryReference.Substring(prefix.Length).Trim();
            if (productId.Length < 2 || productId[0] != '#') throw new InvalidDataException("Malformed IFC STEP geometry reference: " + geometryReference + ".");
            var product = Require(productId);
            if (product.Args.Count <= 6) throw new InvalidDataException(productId + " has no IFC product representation.");
            var representationId = Reference(product.Args[6], productId + " representation");
            var productShape = RequireEntity(representationId, "IFCPRODUCTDEFINITIONSHAPE");
            if (productShape.Args.Count <= 2) throw new InvalidDataException(representationId + " has no representations.");

            var bodies = References(productShape.Args[2]).Select(Require).Where(x => x.Entity == "IFCSHAPEREPRESENTATION" && Text(x.Args, 1).Equals("Body", StringComparison.OrdinalIgnoreCase)).ToList();
            if (bodies.Count != 1) throw new InvalidDataException(productId + " must resolve exactly one Body shape representation.");
            var body = bodies[0];
            if (body.Args.Count <= 3) throw new InvalidDataException(body.Id + " has no representation items.");
            var items = References(body.Args[3]).ToList();
            if (items.Count != 1) throw new InvalidDataException(body.Id + " must contain exactly one supported swept-solid item.");

            return ResolveExtrudedAreaSolid(RequireEntity(items[0], "IFCEXTRUDEDAREASOLID"));
        }

        private IfcSceneMesh ResolveExtrudedAreaSolid(StepRecord solid)
        {
            if (solid.Args.Count < 4) throw new InvalidDataException(solid.Id + " IFCEXTRUDEDAREASOLID is incomplete.");
            var profile = RequireEntity(Reference(solid.Args[0], solid.Id + " profile"), "IFCRECTANGLEPROFILEDEF");
            var position = RequireEntity(Reference(solid.Args[1], solid.Id + " position"), "IFCAXIS2PLACEMENT3D");
            var direction = RequireEntity(Reference(solid.Args[2], solid.Id + " direction"), "IFCDIRECTION");
            var depth = PositiveNumber(solid.Args[3], solid.Id + " depth");

            if (profile.Args.Count < 5) throw new InvalidDataException(profile.Id + " IFCRECTANGLEPROFILEDEF is incomplete.");
            var x = PositiveNumber(profile.Args[3], profile.Id + " XDim");
            var y = PositiveNumber(profile.Args[4], profile.Id + " YDim");
            var origin = ResolvePlacementOrigin(position);
            var extrusion = ResolveDirection(direction);
            var top = new[] { origin[0] + extrusion[0] * depth, origin[1] + extrusion[1] * depth, origin[2] + extrusion[2] * depth };
            var hx = x / 2d;
            var hy = y / 2d;

            // Initial bounded subset intentionally requires the profile plane to remain WCS XY.
            // This is explicit fail-closed behavior rather than silently projecting rotated geometry.
            if (position.Args.Count > 1 && !IsOmitted(position.Args[1])) throw new InvalidDataException(position.Id + " rotated Axis is outside the supported geometry subset.");
            if (position.Args.Count > 2 && !IsOmitted(position.Args[2])) throw new InvalidDataException(position.Id + " rotated RefDirection is outside the supported geometry subset.");
            if (Math.Abs(extrusion[0]) > 1e-12 || Math.Abs(extrusion[1]) > 1e-12 || Math.Abs(Math.Abs(extrusion[2]) - 1d) > 1e-12)
                throw new InvalidDataException(direction.Id + " non-vertical extrusion is outside the supported geometry subset.");

            var vertices = new[]
            {
                new IfcSceneVertex(origin[0]-hx, origin[1]-hy, origin[2]),
                new IfcSceneVertex(origin[0]+hx, origin[1]-hy, origin[2]),
                new IfcSceneVertex(origin[0]+hx, origin[1]+hy, origin[2]),
                new IfcSceneVertex(origin[0]-hx, origin[1]+hy, origin[2]),
                new IfcSceneVertex(top[0]-hx, top[1]-hy, top[2]),
                new IfcSceneVertex(top[0]+hx, top[1]-hy, top[2]),
                new IfcSceneVertex(top[0]+hx, top[1]+hy, top[2]),
                new IfcSceneVertex(top[0]-hx, top[1]+hy, top[2])
            };
            var triangles = new[]
            {
                0,2,1, 0,3,2,
                4,5,6, 4,6,7,
                0,1,5, 0,5,4,
                1,2,6, 1,6,5,
                2,3,7, 2,7,6,
                3,0,4, 3,4,7
            };
            return new IfcSceneMesh(vertices, triangles);
        }

        private double[] ResolvePlacementOrigin(StepRecord placement)
        {
            if (placement.Args.Count == 0) throw new InvalidDataException(placement.Id + " IFCAXIS2PLACEMENT3D has no Location.");
            var point = RequireEntity(Reference(placement.Args[0], placement.Id + " location"), "IFCCARTESIANPOINT");
            if (point.Args.Count == 0) throw new InvalidDataException(point.Id + " IFCCARTESIANPOINT has no coordinates.");
            var values = Numbers(point.Args[0], point.Id + " coordinates").ToList();
            if (values.Count != 3) throw new InvalidDataException(point.Id + " requires exactly three coordinates.");
            return values.ToArray();
        }

        private double[] ResolveDirection(StepRecord direction)
        {
            if (direction.Args.Count == 0) throw new InvalidDataException(direction.Id + " IFCDIRECTION has no ratios.");
            var values = Numbers(direction.Args[0], direction.Id + " direction ratios").ToList();
            if (values.Count != 3) throw new InvalidDataException(direction.Id + " requires exactly three direction ratios.");
            var length = Math.Sqrt(values.Sum(x => x * x));
            if (!Finite(length) || length <= 0d) throw new InvalidDataException(direction.Id + " has an invalid direction vector.");
            return values.Select(x => x / length).ToArray();
        }

        private StepRecord Require(string id)
        {
            StepRecord record;
            if (!_records.TryGetValue(id, out record)) throw new InvalidDataException("IFC geometry references missing entity " + id + ".");
            return record;
        }

        private StepRecord RequireEntity(string id, string entity)
        {
            var record = Require(id);
            if (!record.Entity.Equals(entity, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException(id + " expected " + entity + " but found " + record.Entity + ".");
            return record;
        }

        private static string Reference(string value, string label)
        {
            var trimmed = (value ?? string.Empty).Trim();
            if (trimmed.Length < 2 || trimmed[0] != '#') throw new InvalidDataException(label + " is not an IFC reference.");
            return trimmed;
        }

        private static IEnumerable<string> References(string value)
        {
            var trimmed = (value ?? string.Empty).Trim();
            if (trimmed.Length < 2 || trimmed[0] != '(' || trimmed[trimmed.Length - 1] != ')') throw new InvalidDataException("Expected IFC reference aggregate.");
            return SplitTopLevel(trimmed.Substring(1, trimmed.Length - 2)).Select(x => Reference(x, "aggregate item"));
        }

        private static string Text(IReadOnlyList<string> args, int index)
        {
            if (index >= args.Count) return string.Empty;
            var value = args[index].Trim();
            return value.Length >= 2 && value[0] == '\'' && value[value.Length - 1] == '\'' ? value.Substring(1, value.Length - 2).Replace("''", "'") : string.Empty;
        }

        private static double PositiveNumber(string value, string label)
        {
            double parsed;
            if (!double.TryParse((value ?? string.Empty).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out parsed) || !Finite(parsed) || parsed <= 0d)
                throw new InvalidDataException(label + " must be finite and positive.");
            return parsed;
        }

        private static IEnumerable<double> Numbers(string value, string label)
        {
            var trimmed = (value ?? string.Empty).Trim();
            if (trimmed.Length < 2 || trimmed[0] != '(' || trimmed[trimmed.Length - 1] != ')') throw new InvalidDataException(label + " must be an IFC numeric aggregate.");
            foreach (var token in SplitTopLevel(trimmed.Substring(1, trimmed.Length - 2)))
            {
                double parsed;
                if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed) || !Finite(parsed)) throw new InvalidDataException(label + " contains a non-finite number.");
                yield return parsed;
            }
        }

        private static bool IsOmitted(string value)
        {
            var x = (value ?? string.Empty).Trim();
            return x == "$" || x == "*" || x.Length == 0;
        }

        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }

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
                if (result.ContainsKey(id)) throw new InvalidDataException("Duplicate STEP entity id: " + id + ".");
                result.Add(id, new StepRecord(id, rhs.Substring(0, open).Trim().ToUpperInvariant(), SplitTopLevel(rhs.Substring(open + 1, rhs.Length - open - 2))));
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

        private sealed class StepRecord
        {
            public StepRecord(string id, string entity, IReadOnlyList<string> args) { Id = id; Entity = entity; Args = args; }
            public string Id { get; private set; }
            public string Entity { get; private set; }
            public IReadOnlyList<string> Args { get; private set; }
        }
    }
}
