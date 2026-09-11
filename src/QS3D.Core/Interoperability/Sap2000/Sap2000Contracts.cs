using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace QS3D.Core.Interoperability.Sap2000
{
    /// <summary>
    /// Vendor-neutral Cartesian point expressed in metres before crossing the SAP2000 OAPI boundary.
    /// </summary>
    public struct Sap2000Point3
    {
        public Sap2000Point3(double x, double y, double z)
        {
            EnsureFinite(x, nameof(x));
            EnsureFinite(y, nameof(y));
            EnsureFinite(z, nameof(z));

            X = x;
            Y = y;
            Z = z;
        }

        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public double DistanceSquaredTo(Sap2000Point3 other)
        {
            var dx = X - other.X;
            var dy = Y - other.Y;
            var dz = Z - other.Z;
            return (dx * dx) + (dy * dy) + (dz * dz);
        }

        private static void EnsureFinite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(parameterName, "SAP2000 coordinates must be finite.");
            }
        }
    }

    public sealed class Sap2000FrameMember
    {
        private const double MinimumLengthSquaredMetres = 1e-18;

        public Sap2000FrameMember(
            string userName,
            Sap2000Point3 start,
            Sap2000Point3 end,
            string sectionName = "Default")
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new ArgumentException("A stable SAP2000 frame name is required.", nameof(userName));
            }

            if (start.DistanceSquaredTo(end) <= MinimumLengthSquaredMetres)
            {
                throw new ArgumentException("A SAP2000 frame must have two distinct end points.", nameof(end));
            }

            UserName = userName.Trim();
            Start = start;
            End = end;
            SectionName = NormalizePropertyName(sectionName);
        }

        public string UserName { get; }
        public Sap2000Point3 Start { get; }
        public Sap2000Point3 End { get; }
        public string SectionName { get; }

        private static string NormalizePropertyName(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "Default" : value.Trim();
        }
    }

    public sealed class Sap2000AreaMember
    {
        public Sap2000AreaMember(
            string userName,
            IEnumerable<Sap2000Point3> vertices,
            string propertyName = "Default")
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new ArgumentException("A stable SAP2000 area name is required.", nameof(userName));
            }

            if (vertices == null)
            {
                throw new ArgumentNullException(nameof(vertices));
            }

            var materialized = new List<Sap2000Point3>(vertices);
            if (materialized.Count < 3)
            {
                throw new ArgumentException("A SAP2000 area requires at least three vertices.", nameof(vertices));
            }

            UserName = userName.Trim();
            Vertices = new ReadOnlyCollection<Sap2000Point3>(materialized);
            PropertyName = string.IsNullOrWhiteSpace(propertyName) ? "Default" : propertyName.Trim();
        }

        public string UserName { get; }
        public IReadOnlyList<Sap2000Point3> Vertices { get; }
        public string PropertyName { get; }
    }

    public sealed class Sap2000ExportPlan
    {
        public Sap2000ExportPlan(
            IEnumerable<Sap2000FrameMember> frames,
            IEnumerable<Sap2000AreaMember> areas,
            IEnumerable<string> warnings)
        {
            Frames = Freeze(frames, nameof(frames));
            Areas = Freeze(areas, nameof(areas));
            Warnings = Freeze(warnings, nameof(warnings));
        }

        public IReadOnlyList<Sap2000FrameMember> Frames { get; }
        public IReadOnlyList<Sap2000AreaMember> Areas { get; }
        public IReadOnlyList<string> Warnings { get; }
        public int ObjectCount => Frames.Count + Areas.Count;

        private static IReadOnlyList<T> Freeze<T>(IEnumerable<T> values, string parameterName)
        {
            if (values == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            return new ReadOnlyCollection<T>(new List<T>(values));
        }
    }

    public sealed class Sap2000ExportSummary
    {
        public Sap2000ExportSummary(int framesAdded, int areasAdded, int failures)
        {
            if (framesAdded < 0 || areasAdded < 0 || failures < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(framesAdded), "Export counters cannot be negative.");
            }

            FramesAdded = framesAdded;
            AreasAdded = areasAdded;
            Failures = failures;
        }

        public int FramesAdded { get; }
        public int AreasAdded { get; }
        public int Failures { get; }
        public int Added => FramesAdded + AreasAdded;
    }
}
