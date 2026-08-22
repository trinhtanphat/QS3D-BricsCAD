using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class AtomicFileCommitFallbackSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var method = typeof(QsdbProjectStore).Assembly
                .GetType("QS3D.Core.Persistence.AtomicFileCommit", throwOnError: true)!
                .GetMethod("MoveWithRecovery", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("AtomicFileCommitFallbackSmoke: MoveWithRecovery was not found.");

            CommitsAndKeepsBackup(method);
            CommitsAndRemovesSafetyBackup(method);
            RestoresDestinationAndPriorBackupWhenInstallFails(method);
        }

        private static void CommitsAndKeepsBackup(MethodInfo method)
        {
            WithDirectory(dir =>
            {
                var destination = Path.Combine(dir, "project.qsdb");
                var temp = Path.Combine(dir, "project.tmp");
                var backup = destination + ".bak";
                File.WriteAllText(destination, "old");
                File.WriteAllText(temp, "new");

                Invoke(method, temp, destination, backup, true);

                Equal("new", File.ReadAllText(destination), "destination was not replaced");
                Equal("old", File.ReadAllText(backup), "backup did not preserve previous destination");
                Require(!File.Exists(temp), "successful move left temp file behind");
            });
        }

        private static void CommitsAndRemovesSafetyBackup(MethodInfo method)
        {
            WithDirectory(dir =>
            {
                var destination = Path.Combine(dir, "state.bin");
                var temp = Path.Combine(dir, "state.tmp");
                var backup = Path.Combine(dir, "state.safety.bak");
                File.WriteAllText(destination, "old");
                File.WriteAllText(temp, "new");

                Invoke(method, temp, destination, backup, false);

                Equal("new", File.ReadAllText(destination), "destination was not replaced without persistent backup");
                Require(!File.Exists(backup), "successful no-backup commit retained safety backup");
            });
        }

        private static void RestoresDestinationAndPriorBackupWhenInstallFails(MethodInfo method)
        {
            WithDirectory(dir =>
            {
                var destination = Path.Combine(dir, "recover.qsdb");
                var missingTemp = Path.Combine(dir, "missing.tmp");
                var backup = destination + ".bak";
                File.WriteAllText(destination, "old-good");
                File.WriteAllText(backup, "older-good-backup");

                var threw = false;
                try { Invoke(method, missingTemp, destination, backup, true); }
                catch (TargetInvocationException ex) when (ex.InnerException is FileNotFoundException)
                {
                    threw = true;
                }
                Require(threw, "missing temp did not fail fallback installation");
                Equal("old-good", File.ReadAllText(destination), "failed fallback did not restore previous destination");
                Equal("older-good-backup", File.ReadAllText(backup), "failed fallback destroyed or replaced the pre-existing backup");
                Require(!Directory.GetFiles(dir, "*.previous").Any(), "successful recovery left a staged previous-backup file behind");
            });
        }

        private static void Invoke(MethodInfo method, string temp, string destination, string backup, bool keepBackup)
        {
            method.Invoke(null, new object[] { temp, destination, backup, keepBackup });
        }

        private static void WithDirectory(Action<string> action)
        {
            var dir = Path.Combine(Path.GetTempPath(), "qs3d-atomic-fallback-" + Guid.NewGuid().ToString("N"));
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
                throw new InvalidOperationException("AtomicFileCommitFallbackSmoke: " + message + ". Expected '" + expected + "', got '" + actual + "'.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("AtomicFileCommitFallbackSmoke: " + message);
        }
    }
}
