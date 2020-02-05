using System;
using System.Collections.Generic;
using System.Text;
using Nop.Core.Configuration;
using Nop.Web.Framework.Models;

namespace Nop.Plugin.Reports.DotnetReport.Models
{
    public class DotNetReportConfigSettings : ISettings
    {
        public string ApiUrl { get; set; } = "https://www.dotnetreport.com/api";

        /// <summary>
        /// Your dotnet Report Account Key
        /// </summary>
        public string AccountApiToken { get; set; }

        /// <summary>
        /// Your dotnet Report Data Connection Key
        /// </summary>
        public string DataConnectApiToken { get; set; }

        public string PrivateApiToken { get; set; }
    }

}
