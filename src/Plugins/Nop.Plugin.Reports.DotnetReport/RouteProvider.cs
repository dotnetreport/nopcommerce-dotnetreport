using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Reports.DotnetReport
{
    public partial class RouteProvider : IRouteProvider
    {
      
        /// <summary>
        /// Register routes
        /// </summary>
        /// <param name="routeBuilder">Route builder</param>
        public void RegisterRoutes(IRouteBuilder routeBuilder)
        {
            routeBuilder.MapRoute(
                              name: "Plugin.Reports.DotnetReport.Dashboard",
                              template: "Admin/DotNetReport/Dashboard",
                              defaults: new { controller = "DotNetReport", action = "Dashboard" },
                              dataTokens: new { area = "admin" },
                              constraints: new { }
                );
            routeBuilder.MapRoute(
                             name: "Plugin.Reports.DotnetReport.Index",
                            template: "Admin/DotNetReport/Index",
                            defaults: new { controller = "DotNetReport", action = "Index" },
                            dataTokens: new { area = "admin" },
                            constraints: new { }
              );
            
        }

        /// <summary>
        /// Gets a priority of route provider
        /// </summary>
        public int Priority
        {
            get { return 0; }
        }
    }
}
