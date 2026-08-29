using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Mep;

namespace QS3D.Core.SmokeTests
{
    internal static class MepRecognitionCurrentIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            TokenLimitRejectsBeforeReadingOverrunCurrent();
        }

        private static void TokenLimitRejectsBeforeReadingOverrunCurrent()
        {
            var source = new TokenLimitProbe();
            var error = Capture<ArgumentException>(() => new MepRecognitionRule(
                "current-integrity-token-limit",
                1,
                MepRecognitionDiscipline.Structure,
                "Structure",
                source,
                MepRecognitionSource.LayerOrBlockName));

            Contains("at most 100", error.Message,
                "MEP token limit must retain the existing bounded-input diagnostic.");
            Equal(MepRecognitionLimits.MaxTokensPerRule + 1, source.MoveNextCalls,
                "MEP token limit must observe exactly the first disallowed MoveNext.");
            Equal(MepRecognitionLimits.MaxTokensPerRule, source.CurrentReads,
                "MEP token limit must reject element 101 before reading caller Current.");
        }

        private static TException Capture<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException error)
            {
                return error;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException(message + " Actual=" + actual + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class TokenLimitProbe : IEnumerable<string>
        {
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public IEnumerator<string> GetEnumerator() => new ProbeEnumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class ProbeEnumerator : IEnumerator<string>
            {
                private readonly TokenLimitProbe _owner;
                private int _index = -1;

                internal ProbeEnumerator(TokenLimitProbe owner)
                {
                    _owner = owner;
                }

                public string Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        return "DUPLICATE-TOKEN";
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return _index <= MepRecognitionLimits.MaxTokensPerRule;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
