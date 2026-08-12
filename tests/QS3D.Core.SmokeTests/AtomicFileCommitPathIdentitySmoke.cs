using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class AtomicFileCommitPathIdentitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var type = typeof(QsdbProjectStore).Assembly
                .GetType("QS3D.Core.Persistence.AtomicFileCommit", throwOnError: true)!;

            var publishNew = GetMethod(type, "PublishNew");
            var replaceWithBackup = GetMethod(type, "ReplaceWithBackup");
            var replaceWithoutBackup = GetMethod(type, "ReplaceWithoutBackup");

            RejectsCanonicalDestinationBackupAliasBeforePublish(publishNew);
            RejectsDestinationBackupAliasBeforeReplace(replaceWithBackup);
            RejectsTempDestinationAliasBeforeReplace(replaceWithoutBackup);
            PublishesDistinctPaths(publishNew);
        }

        private static void RejectsCanonicalDestinationBackupAliasBeforePublish(MethodInfo method)
        {
            WithDirectory(dir =>
            {
                var temp = Path.Combine(dir, "new.tmp");
                var destination = Path.Combine(dir, "project.qsdb");
                var backupAlias = Path.Combine(dir, "unused", "..", "project.qsdb");
                File.WriteAllText(temp, "new");

                ExpectArgument(() => Invoke(method, temp, destination, backupAlias), "PublishNew accepted aliased destination/backup paths");

                Equal("new", File.ReadAllText(temp), "PublishNew mutated the temp before alias rejection");
                Require(!File.Exists(destination), "PublishNew created then removed/mutated an aliased destination");
            });
        }

        private static void RejectsDestinationBackupAliasBeforeReplace(MethodInfo method)
        {
            WithDirectory(dir =>
            {
                var temp = Path.Combine(dir, "replace.tmp");
                var destination = Path.Combine(dir, "replace.qsdb");
                File.WriteAllText(temp, "new");
                File.WriteAllText(destination, "old");

                ExpectArgument(() => Invoke(method, temp, destination, destination), "ReplaceWithBackup accepted aliased destination/backup paths");

                Equal("new", File.ReadAllText(temp), "ReplaceWithBackup mutated temp before alias rejection");
                Equal("old", File.ReadAllText(destination), "ReplaceWithBackup mutated destination before alias rejection");
            });
        }

        private static void RejectsTempDestinationAliasBeforeReplace(MethodInfo method)
        {
            WithDirectory(dir =>
            {
                var path = Path.Combine(dir, "same.bin");
                File.WriteAllText(path, "stable");

                ExpectArgument(() => Invoke(method, path, path), "ReplaceWithoutBackup accepted aliased temp/destination paths");

                Equal("stable", File.ReadAllText(path), "ReplaceWithoutBackup mutated aliased input before rejection");
            });
        }

        private static void PublishesDistinctPaths(MethodInfo method)
        {
            WithDirectory(dir =>
            {
                var temp = Path.Combine(dir, "valid.tmp");
                var destination = Path.Combine(dir, "valid.qsdb");
                var backup = destination + ".bak";
                File.WriteAllText(temp, "new");

                Invoke(method, temp, destination, backup);

                Equal("new", File.ReadAllText(destination), "distinct-path PublishNew did not publish the primary");
                Require(!File.Exists(temp), "distinct-path PublishNew retained the temp file");
                Require(!File.Exists(backup), "distinct-path PublishNew unexpectedly created a backup");
            });
        }

        private static MethodInfo GetMethod(Type type, string name)
        {
            return type.GetMethod(name, BindingFlags.Static | BindingFlags.Public)
                ?? throw new InvalidOperationException("AtomicFileCommitPathIdentitySmoke: " + name + " was not found.");
        }

        private static void Invoke(MethodInfo method, params string[] paths)
        {
            method.Invoke(null, paths);
        }

        private static void ExpectArgument(Action action, string message)
        {
            try
            {
                action();
            }
            catch (TargetInvocationException ex) when (ex.InnerException is ArgumentException)
            {
                return;
            }

            throw new InvalidOperationException("AtomicFileCommitPathIdentitySmoke: " + message + ".");
        }

        private static void WithDirectory(Action<string> action)
        {
            var dir = Path.Combine(Path.GetTempPath(), "qs3d-atomic-path-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try { action(dir); }
            finally
            {
                try { Directory.Delete(dir, true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static void Equal(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException("AtomicFileCommitPathIdentitySmoke: " + message + ". Expected '" + expected + "', got '" + actual + "'.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("AtomicFileCommitPathIdentitySmoke: " + message + ".");
        }
    }
}
