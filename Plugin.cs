using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using S = Switcheroo;
using Wox.Infrastructure.Hotkey;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;

namespace Wox.Plugin.Switcheroo
{
    public class Plugin : IPlugin
    {
        protected PluginInitContext context;

        public void Init(PluginInitContext context)
        {
            this.context = context;
            S.CoreStuff.Initialize();
            context.API.GlobalKeyboardEvent += API_GlobalKeyboardEvent;
        }

        bool API_GlobalKeyboardEvent(int keyevent, int vkcode, SpecialKeyState state)
        {
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
                context.API.ShowApp();
                context.API.ChangeQuery(context.CurrentPluginMetadata.ActionKeyword + " ", true);
            });
        }

        public String IconImageDataUri(S.AppWindow self)
        {
            var key = "IconImageDataUri-" + self.HWnd;
            var iconImageDataUri = System.Runtime.Caching.MemoryCache.Default.Get(key) as String; ;
            if (iconImageDataUri == null)
            {
                var iconImage = self.IconImage;
                try
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        BitmapEncoder encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(iconImage));
                        encoder.Save(memoryStream);
                        var b64String = Convert.ToBase64String(memoryStream.ToArray());
                        iconImageDataUri = "data:image/png;base64," + b64String;
                        System.Runtime.Caching.MemoryCache.Default.Add(key, iconImageDataUri, DateTimeOffset.Now.AddHours(1));
                    }
                }
                catch
                {
                    return null;
                }
            }
            return iconImageDataUri;
        }

        public List<Result> Query(Query query)
        {
            var queryString = query.GetAllRemainingParameter();
            S.CoreStuff.WindowList.Clear();
            S.CoreStuff.GetWindows();

            List<S.FilterResult> filterResults = S.CoreStuff.FilterList(queryString).ToList();
            S.FilterResult woxResult = filterResults.FirstOrDefault(o => o.AppWindow.Title == "Wox");
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
                return new Result()
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
    }
}
