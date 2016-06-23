using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management.Instrumentation;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ManagedWinapi.Windows;
using Switcheroo;
using Switcheroo.Core;
using Wox.Infrastructure.Hotkey;
using Wox.Infrastructure.Storage;
using Control = System.Windows.Controls.Control;
using System.Windows;
using Application = System.Windows.Application;
using ManagedWinapi;

namespace Wox.Plugin.Switcheroo
{
    public class Plugin : IPlugin, ISettingProvider, IContextMenu
    {
        private bool _altTabHooked;
        private SwitcherooSettings _settings;
        private PluginJsonStorage<SwitcherooSettings> _storage;
        private IntPtr _woxWindowHandle;
        protected PluginInitContext Context;


        public List<Result> LoadContextMenus(Result selectedResult)
        {
            var app = (AppWindow) selectedResult.ContextData;
            return new List<Result>
            {
                new Result
                {
                    Title = "Close " + app.Title,
                    IcoPath = app.ExecutablePath,
                    Action = e =>
                    {
                        Context.API.ChangeQuery(Context.CurrentPluginMetadata.ActionKeyword + " ", true);
                        app.PostClose();
                        return true;
                    }
                }
            };
        }

        public void Init(PluginInitContext context)
        {
            Context = context;
            _storage = new PluginJsonStorage<SwitcherooSettings>();
            _settings = _storage.Load();
            context.API.GlobalKeyboardEvent += API_GlobalKeyboardEvent;
        }

        public List<Result> Query(Query query)
        {
            var queryString = query.Search;

            var windowContext = new WindowFilterContext<AppWindowViewModel>
            {
                Windows = new WindowFinder().GetWindows().Select(w => new AppWindowViewModel(w)),
                ForegroundWindowProcessTitle = new AppWindow(SystemWindow.ForegroundWindow.HWnd).ProcessTitle
            };

            var filterResults =
                new WindowFilterer().Filter(windowContext, queryString).Select(o => o.AppWindow.AppWindow).ToList();


            return filterResults.Select(o =>
            {
                return new Result
                {
                    Title = o.Title,
                    SubTitle = o.ProcessTitle,
                    IcoPath = o.ExecutablePath,
                    ContextData = o,
                    Action = con =>
                    {
                        o.SwitchTo();
                        Context.API.HideApp();
                        return true;
                    }
                };
            }).ToList();
        }

        public Control CreateSettingPanel()
        {
            return new SwitcherooSetting(_settings, _storage);
        }

        private void ActivateWindow()
        {
            var altKey = new KeyboardKey(Keys.Alt);
            var altKeyPressed = false;

            if ((altKey.AsyncState & 0x8000) == 0)
            {
                altKey.Press();
                altKeyPressed = true;
            }

            Context.API.ShowApp();
            
            if (altKeyPressed)
            {
                altKey.Release();
            }
        }

        private bool API_GlobalKeyboardEvent(int keyevent, int vkcode, SpecialKeyState state)
        {
            if (!_settings.OverrideAltTab) return true;
            if (keyevent == (int) KeyEvent.WM_SYSKEYDOWN && vkcode == (int) Keys.Tab && state.AltPressed)
            {
                OnAltTabPressed();
                return false;
            }
            if (keyevent == (int) KeyEvent.WM_SYSKEYUP && vkcode == (int) Keys.Tab)
            {
                //prevent system alt+tab action
                return false;
            }
            return true;
        }

        private void OnAltTabPressed()
        {
            Context.API.ChangeQuery(Context.CurrentPluginMetadata.ActionKeyword + " ", true);
            ActivateWindow();
        }
    }
}