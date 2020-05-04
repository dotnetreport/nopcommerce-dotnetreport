
using System;
using Nop.Core;
using Nop.Services.DotnetReports;
using Nop.Services.Plugins;

namespace Nop.Plugin.Reports.DotnetReport
{
    public class DotnetReportPlugin : BasePlugin, IDotnetReports
    {
        #region Fields

        private readonly IWebHelper _webHelper;

        #endregion
        #region Ctor

        public DotnetReportPlugin(
            IWebHelper webHelper)
        {
            _webHelper = webHelper;
        }
        #endregion
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/Report/Index";
        }

        /// <summary>
        /// Install the plugin
        /// </summary>
        public override void Install()
        {
            base.Install();
        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        public override void Uninstall()
        {

            base.Uninstall();
        }
    }
}

//using System;
//using Nop.Services.Plugins;

//namespace Nop.Plugin.Reports.DotnetReport
//{
//    public class DotnetReportPlugin : BasePlugin
//    {

//    }
//}
