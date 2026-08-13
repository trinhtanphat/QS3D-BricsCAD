using System;
using QS3D.Core.Measurement;

namespace QS3D.Core.Takeoff
{
    public sealed class TakeoffResultWithTrace
    {
        internal TakeoffResultWithTrace(TakeoffResult result, MeasurementTrace trace)
        {
            Result = result ?? throw new ArgumentNullException(nameof(result));
            Trace = trace ?? throw new ArgumentNullException(nameof(trace));
        }

        public TakeoffResult Result { get; }
        public MeasurementTrace Trace { get; }
    }
}
