using System;
using System.Threading;
using System.Threading.Tasks;
using Sandbox;

namespace MDKListenerPlugin.Base
{
    public class SpaceEngineersSynchronizationContext : SynchronizationContext
    {
        readonly string _invocationId;

        public SpaceEngineersSynchronizationContext(string invocationId)
        {
            _invocationId = invocationId;
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            void invoke() => d(state);
            MySandboxGame.Static.Invoke(invoke, _invocationId);
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            var tcs = new TaskCompletionSource<object>();

            void invoke()
            {
                try
                {
                    d(state);
                    tcs.SetResult(null);
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            }

            MySandboxGame.Static.Invoke(invoke, _invocationId);
            tcs.Task.Wait();
        }
    }
}