using System;
using System.IO;

namespace QS3D.Core.SmokeTests
{
    internal static class DocumentBoundWindowLifetimeIdentitySmoke
    {
        internal static void Run()
        {
            var root = FindRepositoryRoot();
            var path = Path.Combine(root, "src", "QS3D.BricsCAD.V25", "UI", "DocumentBoundWindowLifetime.cs");
            var source = File.ReadAllText(path);

            Require(source.Contains("private readonly Teigha.DatabaseServices.Database _database;", StringComparison.Ordinal),
                "Document-bound window lifetime must capture stable BricsCAD Database identity.");
            Require(source.Contains("_database = document.Database;", StringComparison.Ordinal),
                "Document-bound window lifetime must capture the Database from the original wrapper.");
            Require(source.Contains("ReferenceEquals(document.Database, _database)", StringComparison.Ordinal),
                "Document wrapper matching must compare the stable Database identity.");
            Require(source.Contains("if (!IsSameDocument(document))", StringComparison.Ordinal),
                "Attach must accept wrapper drift only through stable document identity.");
            Require(source.Contains("_document = document;", StringComparison.Ordinal),
                "Equivalent wrapper attach must refresh the wrapper used for project-affinity reads.");
            Require(source.Contains("if (!IsSameDocument(e.Document)) return;", StringComparison.Ordinal),
                "Document destruction must match replacement wrappers by stable identity.");
            Require(!source.Contains("ReferenceEquals(document, _document)", StringComparison.Ordinal),
                "Attach must not use managed Document reference identity.");
            Require(!source.Contains("ReferenceEquals(e.Document, _document)", StringComparison.Ordinal),
                "Document destruction must not use managed Document reference identity.");
            Require(!source.Contains("document.Name", StringComparison.Ordinal),
                "Document-bound lifetime identity must not be path/name based; Save As must retain identity and same-name DWGs must not alias.");

            Require(source.Contains("if (_attached) return;", StringComparison.Ordinal),
                "Attach idempotence guard must remain present.");
            Require(source.Contains("Interlocked.Exchange(ref _invalidated, 1)", StringComparison.Ordinal),
                "Once-only invalidation guard must remain present.");
            Require(source.Contains("DetachDocumentManagerHandler();", StringComparison.Ordinal),
                "Document manager handler must still detach after invalidation.");
            Require(source.Contains("_attached = true;\n                    Detach();", StringComparison.Ordinal),
                "Partial attach rollback must remain authoritative.");
        }

        private static string FindRepositoryRoot()
        {
            for (var current = new DirectoryInfo(AppContext.BaseDirectory); current != null; current = current.Parent)
            {
                if (File.Exists(Path.Combine(current.FullName, "QS3D.sln")) &&
                    Directory.Exists(Path.Combine(current.FullName, "src")))
                    return current.FullName;
            }

            throw new InvalidOperationException("Could not locate the QS3D repository root for the document lifetime source guard.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
