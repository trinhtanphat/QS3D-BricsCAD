using System;

namespace QS3D.Core.Agent.Harness
{
    public enum HarnessState
    {
        Created,
        ContextResolving,
        Ready,
        Running,
        WaitingPermission,
        WaitingExternal,
        Completed,
        Blocked,
        Cancelled,
        Failed
    }

    public sealed class HarnessLifecycle
    {
        public HarnessState CurrentState { get; private set; } = HarnessState.Created;

        public void TransitionTo(HarnessState next)
        {
            if (!IsAllowed(CurrentState, next))
            {
                throw new InvalidOperationException(
                    "Illegal harness lifecycle transition: " + CurrentState + " -> " + next + ".");
            }

            CurrentState = next;
        }

        private static bool IsAllowed(HarnessState current, HarnessState next)
        {
            switch (current)
            {
                case HarnessState.Created:
                    return next == HarnessState.ContextResolving;
                case HarnessState.ContextResolving:
                    return next == HarnessState.Ready || next == HarnessState.Blocked || next == HarnessState.Cancelled || next == HarnessState.Failed;
                case HarnessState.Ready:
                    return next == HarnessState.Running || next == HarnessState.Cancelled || next == HarnessState.Failed;
                case HarnessState.Running:
                    return next == HarnessState.WaitingPermission
                        || next == HarnessState.WaitingExternal
                        || next == HarnessState.Completed
                        || next == HarnessState.Blocked
                        || next == HarnessState.Cancelled
                        || next == HarnessState.Failed;
                case HarnessState.WaitingPermission:
                case HarnessState.WaitingExternal:
                    return next == HarnessState.Running
                        || next == HarnessState.Blocked
                        || next == HarnessState.Cancelled
                        || next == HarnessState.Failed;
                case HarnessState.Completed:
                case HarnessState.Blocked:
                case HarnessState.Cancelled:
                case HarnessState.Failed:
                default:
                    return false;
            }
        }
    }
}
