using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class TbqProjectWorkspaceEncodingBoundSmoke
    {
        private const int MaxPayloadChars = 1024 * 1024;
        private static readonly MethodInfo AppendFieldMethod =
            typeof(ProjectState).Assembly
                .GetType("QS3D.Core.Domain.ProjectTbqWorkspaceCodec", throwOnError: true)!
                .GetMethod("AppendField", BindingFlags.NonPublic | BindingFlags.Static)!;

        internal static void Run()
        {
            ExactBoundaryAcceptsSupplementaryUnicode();
            PrefixDigitsParticipateInBound();
            OverflowRejectsBeforeBuilderMutation();
            LateFieldOverflowPreservesAcceptedPrefix();
        }

        private static void ExactBoundaryAcceptsSupplementaryUnicode()
        {
            var builder = Filled(MaxPayloadChars - 4);
            InvokeAppend(builder, "😀");
            Equal(MaxPayloadChars, builder.Length);
            True(builder.ToString(builder.Length - 4, 4) == "2:😀");
        }

        private static void PrefixDigitsParticipateInBound()
        {
            var builder = Filled(MaxPayloadChars - 6);
            InvokeAppend(builder, "1234");
            Equal(MaxPayloadChars, builder.Length);
            True(builder.ToString(builder.Length - 6, 6) == "4:1234");
        }

        private static void OverflowRejectsBeforeBuilderMutation()
        {
            var builder = Filled(MaxPayloadChars - 2);
            var beforeLength = builder.Length;
            ExpectPayloadTooLarge(() => InvokeAppend(builder, "A"));
            Equal(beforeLength, builder.Length);
        }

        private static void LateFieldOverflowPreservesAcceptedPrefix()
        {
            var builder = Filled(MaxPayloadChars - 3);
            InvokeAppend(builder, "A");
            Equal(MaxPayloadChars, builder.Length);
            var acceptedTail = builder.ToString(builder.Length - 3, 3);

            ExpectPayloadTooLarge(() => InvokeAppend(builder, string.Empty));
            Equal(MaxPayloadChars, builder.Length);
            Equal(acceptedTail, builder.ToString(builder.Length - 3, 3));
        }

        private static StringBuilder Filled(int length) => new StringBuilder(new string('x', length));

        private static void InvokeAppend(StringBuilder builder, string value)
        {
            try
            {
                AppendFieldMethod.Invoke(null, new object[] { builder, value });
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
        }

        private static void ExpectPayloadTooLarge(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                True(ex.Message.Contains("1 MiB", StringComparison.Ordinal));
                return;
            }
            throw new Exception("Expected TBQ payload ceiling rejection.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected condition to be true.");
        }
    }

    internal static class TbqProjectWorkspaceEncodingBoundSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => TbqProjectWorkspaceEncodingBoundSmoke.Run();
    }
}
