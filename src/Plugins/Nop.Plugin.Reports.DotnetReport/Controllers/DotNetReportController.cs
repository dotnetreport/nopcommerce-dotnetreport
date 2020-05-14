using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nop.Plugin.Reports.DotnetReport.Models;
using Nop.Services.Configuration;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Reports.DotnetReport.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.Admin)]
    public class DotNetReportController : BasePluginController
    {
        private readonly DotNetReportConfigSettings _settings;
        private readonly IPermissionService _permissionService;
        private readonly ISettingService _settingService;

        public DotNetReportController(
            DotNetReportConfigSettings settings,
            IPermissionService permissionService,
            ISettingService settingService)
        {
            _settings = settings;
            _permissionService = permissionService;
            _settingService = settingService;
        }

        public IActionResult Index()
        {
            return View("~/Plugins/Reports.DotnetReport/Views/DotNetReport/Index.cshtml");
        }

        public IActionResult Report(int reportId, string reportName, string reportDescription, bool includeSubTotal, bool showUniqueRecords,
            bool aggregateReport, bool showDataWithGraph, string reportSql, string connectKey, string reportFilter, string reportType, int selectedFolder)
        {
            var model = new DotNetReportModel
            {
                ReportId = reportId,
                ReportType = reportType,
                ReportName = HttpUtility.UrlDecode(reportName),
                ReportDescription = HttpUtility.UrlDecode(reportDescription),
                ReportSql = reportSql,
                ConnectKey = connectKey,
                IncludeSubTotals = includeSubTotal,
                ShowUniqueRecords = showUniqueRecords,
                ShowDataWithGraph = showDataWithGraph,
                SelectedFolder = selectedFolder,
                ReportFilter = reportFilter // json data to setup filter correctly again
            };
            return View("~/Plugins/Reports.DotnetReport/Views/DotNetReport/Report.cshtml", model);
           
        }

        public async Task<IActionResult> Dashboard(int? id = null, bool adminMode = false)
        {
            var reportApi = new ReportApiController(_settings, _permissionService, _settingService);
            var model = new List<DotNetDasboardReportModel>();
            var settings = reportApi.GetSettings();

            var dashboards = (JArray)await reportApi.GetDashboardsData(adminMode);
            if (!id.HasValue && dashboards.Count > 0)
                id = ((dynamic)dashboards.First()).Id;

            using (var client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("account", settings.AccountApiToken),
                    new KeyValuePair<string, string>("dataConnect", settings.DataConnectApiToken),
                    new KeyValuePair<string, string>("clientId", settings.ClientId),
                    new KeyValuePair<string, string>("userId", settings.UserId),
                    new KeyValuePair<string, string>("userRole", string.Join(",", settings.CurrentUserRole)),
                    new KeyValuePair<string, string>("id", id.HasValue ? id.Value.ToString() : "0"),
                    new KeyValuePair<string, string>("adminMode", adminMode.ToString()),
                });

                var response = await client.PostAsync(new Uri(settings.ApiUrl + $"/ReportApi/LoadSavedDashboard"), content);
                var stringContent = await response.Content.ReadAsStringAsync();

                model = JsonConvert.DeserializeObject<List<DotNetDasboardReportModel>>(stringContent);
            }
            return View("~/Plugins/Reports.DotnetReport/Views/DotNetReport/Dashboard.cshtml", new DotNetDashboardModel
            {
                Dashboards = dashboards.Select(x => (dynamic)x).ToList(),
                Reports = model
            });
        }

        [HttpPost]
        public IActionResult DownloadExcel(string reportSql, string connectKey, string reportName)
        {
            var excel = DotNetReportHelper.GetExcelFile(reportSql, connectKey, reportName, _settings.PrivateApiToken);
            Response.Headers.Add("content-disposition", "attachment; filename=" + reportName + ".xlsx");
            Response.ContentType = "application/vnd.ms-excel";

            return File(excel, "application/vnd.ms-excel", reportName + ".xlsx");
        }

        [HttpPost]
        public IActionResult DownloadXml(string reportSql, string connectKey, string reportName)
        {
            var xml = DotNetReportHelper.GetXmlFile(reportSql, connectKey, reportName, _settings.PrivateApiToken);
            Response.Headers.Add("content-disposition", "attachment; filename=" + reportName + ".xml");
            Response.ContentType = "application/xml";

            return File(xml, "application/xml", reportName + ".xml");
        }

    }
}