using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using ManagedWinapi.Windows;
using Switcheroo;
using Switcheroo.Core;
using Wox.Infrastructure;
using Wox.Infrastructure.Hotkey;
using Wox.Plugin.Features;
using Control = System.Windows.Controls.Control;

namespace Wox.Plugin.Switcheroo
{
    public class Plugin : IPlugin, ISettingProvider, IContextMenu
    {
        private bool altTabHooked;
        protected PluginInitContext context;
        public void Init(PluginInitContext context)
        {
            this.context = context;
            context.API.GlobalKeyboardEvent += API_GlobalKeyboardEvent;
        }

        public Control CreateSettingPanel()
        {
            return new SwitcherooSetting(this);
        }

        public List<Result> Query(Query query)
        {       
            var queryString = query.GetAllRemainingParameter();

            var windowContext = new WindowFilterContext<AppWindowViewModel>
            {
                Windows = new WindowFinder().GetWindows().Select(w => new AppWindowViewModel(w)),
                ForegroundWindowProcessTitle = new AppWindow(SystemWindow.ForegroundWindow.HWnd).ProcessTitle
            };

            var filterResults = new WindowFilterer().Filter(windowContext, queryString).Select(o=>o.AppWindow.AppWindow).ToList();


            return filterResults.Select(o =>
            {
                return new Result
                {
                    Title = o.Title,
                    SubTitle = o.ProcessTitle,
                    IcoPath = IconImageDataUri(o),
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
            if (!SwitcherooStorage.Instance.OverrideAltTab) return true;
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
            
            //return as soon as possible
            ThreadPool.QueueUserWorkItem(o =>
            {
                context.API.HideApp();
                context.API.ShowApp();
                context.API.ChangeQuery(context.CurrentPluginMetadata.ActionKeyword + " ", true);
            });
        }


        public string IconImageDataUri(AppWindow self)
        {
            var key = "IconImageDataUri-" + self.HWnd;
            var iconImageDataUri = MemoryCache.Default.Get(key) as string;
            ;
            if (iconImageDataUri == null)
            {
                try
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        BitmapEncoder encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(new IconToBitmapImageConverter().Convert(self.LargeWindowIcon)));
                        encoder.Save(memoryStream);
                        var b64String = Convert.ToBase64String(memoryStream.ToArray());
                        iconImageDataUri = "data:image/png;base64," + b64String;
                        MemoryCache.Default.Add(key, iconImageDataUri, DateTimeOffset.Now.AddHours(1));
                    }
                }
                catch
                {
                    return null;
                }
            }
            return iconImageDataUri;
        }

        public List<Result> LoadContextMenus(Result selectedResult)
        {
            AppWindow app = (AppWindow) selectedResult.ContextData;
            return new List<Result>
            {
                new Result
                {
                    Title = "Close " + app.Title,
                    IcoPath = IconImageDataUri(app),
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