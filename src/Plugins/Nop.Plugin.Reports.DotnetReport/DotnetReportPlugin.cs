
using System;
using System.Linq;
using Microsoft.AspNetCore.Routing;
using Nop.Core;
using Nop.Services.Common;
//using Nop.Services.DotnetReports;
using Nop.Services.Plugins;
using Nop.Web.Framework;
using Nop.Web.Framework.Menu;

namespace Nop.Plugin.Reports.DotnetReport
{
    public class DotnetReportPlugin : BasePlugin, IMiscPlugin, IAdminMenuPlugin
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
            return $"{_webHelper.GetStoreLocation()}Admin/Setup/Configure";
        }

        /// <summary>
        /// Install the plugin
        /// </summary>
        public override void Install()
        {
            base.Install();
        }

        public void ManageSiteMap(SiteMapNode rootNode)
        {
            var CustomDashboard = new SiteMapNode()
            {
                SystemName = "Reports.DotnetReport",
                Title = "Custom Dashboard",
                ControllerName = "Report",
                ActionName = "Dashboard",
                Visible = true,
               IconClass = "fa-dot-circle-o",
                RouteValues = new RouteValueDictionary() { { "area", AreaNames.Admin } },
            };
            var CustomReport = new SiteMapNode()
            {
                SystemName = "Reports.DotnetReport",
                Title = "Custom Report",
                ControllerName = "Report",
                ActionName = "Index",
                Visible = true,
                IconClass = "fa-dot-circle-o",
                RouteValues = new RouteValueDictionary() { { "area", AreaNames.Admin } },
            };
            var reportsNode = rootNode.ChildNodes.FirstOrDefault(x => x.SystemName == "Reports");
            
          //  var Nodes = rootNode.ChildNodes;
          //  var pluginNode = Nodes.FirstOrDefault(x => x.SystemName == "Third party plugins");
            if (reportsNode != null)
                reportsNode.ChildNodes.Add(CustomDashboard);
               reportsNode.ChildNodes.Add(CustomReport);

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
