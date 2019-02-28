using System;
using System.Diagnostics;
using MDKListenerPlugin.Base;
using Pipes;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using VRage.Game.ModAPI;
using VRage.Plugins;

namespace MDKListenerPlugin
{
    public class Plugin : SpaceEngineersPlugin, IPlugin
    {
        static void Dispose<T>(ref T disposable) where T : class, IDisposable
        {
            disposable?.Dispose();
            disposable = null;
        }

        static object GetItemInControlPanel()
        {
            if (MyGuiScreenTerminal.GetCurrentScreen() == MyTerminalPageEnum.ControlPanel)
            {
                return MyGuiScreenTerminal.InteractedEntity;
            }

            return null;
        }

        Server _server;
        bool _available;
        MyTerminalPageEnum _currentScreen;

        protected override void OnSessionReady()
        {
            base.OnSessionReady();
            _available = true;
        }

        protected override void OnSessionUnloading()
        {
            _available = false;
            base.OnSessionUnloading();
        }

        protected override void OnUpdate()
        {
            if (_available)
            {
                var screen = MyGuiScreenTerminal.GetCurrentScreen();
                if (screen != _currentScreen)
                {
                    _currentScreen = screen;
                    OnScreenChanged(screen);
                }
            }

            base.OnUpdate();
        }

        void OnScreenChanged(MyTerminalPageEnum screen)
        {
            if (GetItemInControlPanel() is MyTerminalBlock entity)
                Debug.WriteLine(entity.CustomName);
            //switch (screen)
            //{
            //    case MyTerminalPageEnum.None:
            //        break;
            //    case MyTerminalPageEnum.Properties:
            //        break;
            //    case MyTerminalPageEnum.Inventory:
            //        break;
            //    case MyTerminalPageEnum.ControlPanel:
            //        break;
            //    case MyTerminalPageEnum.Production:
            //        break;
            //    case MyTerminalPageEnum.Info:
            //        break;
            //    case MyTerminalPageEnum.Factions:
            //        break;
            //    case MyTerminalPageEnum.Comms:
            //        break;
            //    case MyTerminalPageEnum.Gps:
            //        break;
            //    default:
            //        throw new ArgumentOutOfRangeException();
            //}
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                Dispose(ref _server);
            base.Dispose(disposing);
        }
    }
}