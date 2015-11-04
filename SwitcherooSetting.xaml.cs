using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Wox.Plugin.Switcheroo
{
    /// <summary>
    /// Interaction logic for SwitcherooSetting.xaml
    /// </summary>
    public partial class SwitcherooSetting : UserControl
    {
        private readonly Plugin _plugin;

        public SwitcherooSetting(Plugin plugin)
        {
            _plugin = plugin;
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            cbOverrideAltTab.Content += " (buggy. Wox shows up, but does not get focused)";
            cbOverrideAltTab.IsChecked = SwitcherooStorage.Instance.OverrideAltTab;

            cbOverrideAltTab.Checked += (o, args) =>
            {
                SwitcherooStorage.Instance.OverrideAltTab = true;
                SwitcherooStorage.Instance.Save();
            };
            cbOverrideAltTab.Unchecked += (o, args) =>
            {
                SwitcherooStorage.Instance.OverrideAltTab = false;
                SwitcherooStorage.Instance.Save();
            };
        }

        private void label_MouseUp(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/kvakulo/Switcheroo");
        }
    }
}
