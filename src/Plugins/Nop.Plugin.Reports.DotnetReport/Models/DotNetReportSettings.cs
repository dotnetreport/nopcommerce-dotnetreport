using System;
using System.Collections.Generic;
using System.Text;
using Nop.Core.Configuration;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Reports.DotnetReport.Models
{
    public class DotNetReportConfigSettings : ISettings
    {
        [NopResourceDisplayName("Plugins.Reports.DotnetReport.Fields.ApiUrl")]
        public string ApiUrl { get; set; } = "https://www.dotnetreport.com/api";

        /// <summary>
        /// Your dotnet Report Account Key
        /// </summary>
        [NopResourceDisplayName("Plugins.Reports.DotnetReport.Fields.AccountApiToken")]
        public string AccountApiToken { get; set; }

        /// <summary>
        /// Your dotnet Report Data Connection Key
        /// </summary>
        [NopResourceDisplayName("Plugins.Reports.DotnetReport.Fields.DataConnectApiToken")]
        public string DataConnectApiToken { get; set; }
        [NopResourceDisplayName("Plugins.Reports.DotnetReport.Fields.PrivateApiToken")]
        public string PrivateApiToken { get; set; }
    }

}
