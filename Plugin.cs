using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using Wox.Infrastructure.Hotkey;
using Control = System.Windows.Controls.Control;
using S = Switcheroo;

namespace Wox.Plugin.Switcheroo
{
    public class Plugin : IPlugin, ISettingProvider
    {
        private bool altTabHooked;
        protected PluginInitContext context;

        public void Init(PluginInitContext context)
        {
            this.context = context;
            S.CoreStuff.Initialize();
            S.CoreStuff.GetWindows();
            AltTabHookCheck();
        }

        public Control CreateSettingPanel()
        {
            return new SwitcherooSetting(this);
        }

        public List<Result> Query(Query query)
        {
            AltTabHookCheck();

            var queryString = query.GetAllRemainingParameter();
            S.CoreStuff.WindowList.Clear();
            S.CoreStuff.GetWindows();

            var filterResults = S.CoreStuff.FilterList(queryString).ToList();
            var woxResult = filterResults.FirstOrDefault(o => o.AppWindow.Title == "Wox");
            if (woxResult != null)
            {
                filterResults.Remove(woxResult);
            }

            //swap first and second position
            if (filterResults.Count > 1)
            {
                var swap = filterResults[0];
                filterResults[0] = filterResults[1];
                filterResults[1] = swap;
            }

            return filterResults.Select(o =>
            {
                return new Result
                {
                    Title = o.AppWindow.Title,
                    SubTitle = o.AppWindow.ProcessTitle,
                    IcoPath = IconImageDataUri(o.AppWindow),
                    Action = con =>
                    {
                        o.AppWindow.SwitchTo();
                        context.API.HideApp();
                        return true;
                    }
                };
            }).ToList();
        }

        

        public void AltTabHookCheck()
        {
            if (SwitcherooStorage.Instance.OverrideAltTab && !altTabHooked)
            {
                context.API.GlobalKeyboardEvent += API_GlobalKeyboardEvent;
                altTabHooked = true;
            }
            else if (!SwitcherooStorage.Instance.OverrideAltTab && altTabHooked)
            {
                context.API.GlobalKeyboardEvent -= API_GlobalKeyboardEvent;
                altTabHooked = false;
            }
        }


        private bool API_GlobalKeyboardEvent(int keyevent, int vkcode, SpecialKeyState state)
        {
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
            //return as soon as possible
            ThreadPool.QueueUserWorkItem(o =>
            {
                context.API.ShowApp();
                context.API.ChangeQuery(context.CurrentPluginMetadata.ActionKeyword + " ", true);
            });
        }

        public string IconImageDataUri(S.AppWindow self)
        {
            var key = "IconImageDataUri-" + self.HWnd;
            var iconImageDataUri = MemoryCache.Default.Get(key) as string;
            ;
            if (iconImageDataUri == null)
            {
                var iconImage = self.IconImage;
                try
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        BitmapEncoder encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(iconImage));
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
    }
}