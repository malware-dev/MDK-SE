using System;
using System.Diagnostics;
using Sandbox.Game.World;
using SpaceEngineers.Game;

namespace MDKListenerPlugin.Base
{
    public abstract class SpaceEngineersPlugin
    {
        bool _isInitialized;
        bool _isConnectedToSession;

        public SpaceEngineersGame Game { get; private set; }

        ~SpaceEngineersPlugin()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (MySession.Static != null)
                    MySession.Static.OnReady -= OnSessionReady;
                MySession.OnLoading -= OnSessionLoading;
                MySession.AfterLoading -= OnSessionAfterLoading;
                MySession.OnUnloading -= OnSessionUnloading;
                MySession.OnUnloaded -= OnSessionUnloaded;
            }
        }

        public void Init(object gameInstance)
        {
            Debugger.Launch();
            Game = (SpaceEngineersGame)gameInstance;
            OnStarting();
        }

        protected virtual void OnStarting()
        { }

        protected virtual void OnSessionUnloaded()
        { }

        protected virtual void OnSessionUnloading()
        { }

        protected virtual void OnSessionAfterLoading()
        { }

        protected virtual void OnSessionLoading()
        {
            if (!_isConnectedToSession)
            {
                _isConnectedToSession = true;
                MySession.Static.OnReady += OnSessionReady;
            }
        }

        protected virtual void OnSessionReady()
        { }

        public void Update()
        {
            if (!_isInitialized)
            {
                _isInitialized = true;
                OnInitialize();
            }

            if (MySession.Static == null)
                return;
            OnUpdate();
        }

        protected virtual void OnInitialize()
        {
            MySession.OnLoading += OnSessionLoading;
            MySession.AfterLoading += OnSessionAfterLoading;
            MySession.OnUnloading += OnSessionUnloading;
            MySession.OnUnloaded += OnSessionUnloaded;
        }

        protected virtual void OnUpdate()
        { }
    }
}