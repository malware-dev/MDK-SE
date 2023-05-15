using Sandbox.ModAPI;
using System;
using System.Runtime.CompilerServices;

namespace Malware.MDKWhitelistExtractor
{
    public static class GameThread
    {
        public static GameThreadSwitcherAwaitable SwitchToGameThread()
        {
            return new GameThreadSwitcherAwaitable();
        }

        public class GameThreadSwitcherAwaitable: INotifyCompletion
        {
            public bool IsCompleted => false;

            public void OnCompleted(Action continuation)
            {
                MyAPIGateway.Utilities.InvokeOnGameThread(continuation);
            }

            public GameThreadSwitcherAwaitable GetAwaiter()
            {
                return this;
            }

            public void GetResult()
            {
                // No result to get.
            }
        }
    }
}