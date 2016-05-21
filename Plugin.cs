using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ManagedWinapi.Windows;
using Switcheroo;
using Switcheroo.Core;
using Wox.Infrastructure.Hotkey;
using Wox.Infrastructure.Storage;
using Control = System.Windows.Controls.Control;

namespace Wox.Plugin.Switcheroo
{
    public class Plugin : IPlugin, ISettingProvider, IContextMenu
    {
        private bool altTabHooked;
        protected PluginInitContext context;
        private SwitcherooSettings settings;
        private PluginJsonStorage<SwitcherooSettings> storage;
        public void Init(PluginInitContext context)
        {
            this.context = context;
            storage = new PluginJsonStorage<SwitcherooSettings>();
            settings = storage.Load();
            context.API.GlobalKeyboardEvent += API_GlobalKeyboardEvent;
        }

        public Control CreateSettingPanel()
        {
            return new SwitcherooSetting(settings, storage);
        }

        public List<Result> Query(Query query)
        {
            var queryString = query.Search;

            var windowContext = new WindowFilterContext<AppWindowViewModel>
            {
                Windows = new WindowFinder().GetWindows().Select(w => new AppWindowViewModel(w)),
                ForegroundWindowProcessTitle = new AppWindow(SystemWindow.ForegroundWindow.HWnd).ProcessTitle
            };

            var filterResults = new WindowFilterer().Filter(windowContext, queryString).Select(o => o.AppWindow.AppWindow).ToList();


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
                        context.API.HideApp();
                        return true;
                    }
                };
            }).ToList();
        }


        private bool API_GlobalKeyboardEvent(int keyevent, int vkcode, SpecialKeyState state)
        {
            if (!settings.OverrideAltTab) return true;
            if (keyevent == (int)KeyEvent.WM_SYSKEYDOWN && vkcode == (int)Keys.Tab && state.AltPressed)
            {
                OnAltTabPressed();
                return false;
            }
            if (keyevent == (int)KeyEvent.WM_SYSKEYUP && vkcode == (int)Keys.Tab)
            {
                //prevent system alt+tab action
                return false;
            }
            return true;
        }

        private void OnAltTabPressed()
        {
            context.API.ShowApp();
            context.API.ChangeQuery(context.CurrentPluginMetadata.ActionKeyword + " ", true);
        }


        public List<Result> LoadContextMenus(Result selectedResult)
        {
            AppWindow app = (AppWindow)selectedResult.ContextData;
            return new List<Result>
            {
                new Result
                {
                    Title = "Close " + app.Title,
                    IcoPath = app.ExecutablePath,
                    Action = e =>
                    {
                        context.API.ChangeQuery(context.CurrentPluginMetadata.ActionKeyword + " ", true);
                        app.PostClose();
                        return true;
                    }
                }
            };
        }
    }
}