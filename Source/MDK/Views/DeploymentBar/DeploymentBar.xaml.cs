using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using Malware.MDKServices;
using MDK.Resources;
using MDK.Views.BlueprintManager;
using MDK.VisualStudio;

namespace MDK.Views.DeploymentBar
{
    /// <summary>
    /// Interaction logic for DeploymentBar.xaml
    /// </summary>
    public partial class DeploymentBar : NotificationBar
    {
        MDKProjectProperties[] _deployedScripts;

        /// <summary>
        /// Creates a new instance of <see cref="DeploymentBar"/>
        /// </summary>
        public DeploymentBar() { InitializeComponent(); }

        /// <summary>
        /// A list of deployed scripts
        /// </summary>
        public MDKProjectProperties[] DeployedScripts
        {
            get => _deployedScripts;
            set
            {
                _deployedScripts = value;
                copyLink.IsVisible = _deployedScripts?.Length == 1;
            }
        }

        /// <summary>
        /// Whether the Show Me hyperlink should be available
        /// </summary>
        public bool CanShowMe { get => showMeLink.IsVisible; set => showMeLink.IsVisible = value; }

        string FormattedPath(string scriptOutputPath) => Path.GetFullPath(Environment.ExpandEnvironmentVariables(scriptOutputPath)).TrimEnd('\\').ToUpper();

        void ShowMeLink_OnClicked(object sender, EventArgs e)
        {
            Close();
            var distinctPaths = DeployedScripts.Select(script => FormattedPath(script.Paths.OutputPath)).Distinct().ToArray();
            if (distinctPaths.Length == 1)
            {
                var model = new BlueprintManagerDialogModel(Text.MDKPackage_Deploy_Description,
                    distinctPaths[0], DeployedScripts.Select(s => s.Name));
                BlueprintManagerDialog.ShowDialog(model);
            }
        }

        async void CopyLink_OnClicked(object sender, EventArgs e)
        {
            Close();
            var item = _deployedScripts.FirstOrDefault();
            var path = Path.Combine(FormattedPath(item.Paths.OutputPath), item.Name, "script.cs");
            if (File.Exists(path))
            {
                var script = File.ReadAllText(path, Encoding.UTF8);
                System.Windows.Clipboard.SetText(script, TextDataFormat.UnicodeText);
                var bar = new CopiedToClipboardBar();
                await bar.ShowAsync(ServiceProvider);
            }
        }
    }
}
