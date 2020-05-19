﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Nop.Core.Data;
using Nop.Plugin.Reports.DotnetReport.Models;
using Nop.Services.Configuration;
using Nop.Services.Security;

namespace Nop.Plugin.Reports.DotnetReport.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ReportApiController : ControllerBase
    {
        private readonly DotNetReportConfigSettings _settings;
        private readonly IPermissionService _permissionService;
        private readonly ISettingService _settingService;

        public ReportApiController(
            DotNetReportConfigSettings settings,
            IPermissionService permissionService,
            ISettingService settingService)
        {
            _settings = settings;
            _permissionService = permissionService;
            _settingService = settingService;
        }

        public DotNetReportSettings GetSettings()
        {
            var settings = new DotNetReportSettings
            {
                ApiUrl = _settings.ApiUrl,
                AccountApiToken = _settings.AccountApiToken, // Your Account Api Token from your http://dotnetreport.com Account
                DataConnectApiToken = _settings.DataConnectApiToken // Your Data Connect Api Token from your http://dotnetreport.com Account
            };

            // Populate the values below using your Application Roles/Claims if applicable

            settings.ClientId = "";  // You can pass your multi-tenant client id here to track their reports and folders
            settings.UserId = ""; // You can pass your current authenticated user id here to track their reports and folders            
            settings.UserName = "";
            settings.CurrentUserRole = new List<string>(); // Populate your current authenticated user's roles

            settings.Users = new List<string>(); // Populate all your application's user, ex  { "Jane", "John" }
            settings.UserRoles = new List<string>(); // Populate all your application's user roles, ex  { "Admin", "Normal" }       
            settings.CanUseAdminMode = true; // Set to true only if current user can use Admin mode to setup reports and dashboard

            return settings;
        }

        [HttpPost]
        public IActionResult GetLookupList(dynamic model)
        {
            string lookupSql = model.lookupSql;
            string connectKey = model.connectKey;

            var sql = DotNetReportHelper.Decrypt(lookupSql, _settings.PrivateApiToken);

            // Uncomment if you want to restrict max records returned
            sql = sql.Replace("SELECT ", "SELECT TOP 500 ");

            var dt = new DataTable();
            var dataSettings = DataSettingsManager.LoadSettings();
            using (var conn = new SqlConnection(dataSettings.DataConnectionString))
            {
                conn.Open();
                var command = new SqlCommand(sql, conn);
                var adapter = new SqlDataAdapter(command);

                adapter.Fill(dt);
            }

            var data = new List<object>();
            foreach (DataRow dr in dt.Rows)
                data.Add(new { id = dr[0], text = dr[1] });

            return Ok(data);
        }

        [HttpPost]
        public async Task<IActionResult> RunReportApi(DotNetReportApiCall data)
        {
            return await CallReportApi(data.Method, JsonConvert.SerializeObject(data));
        }

        [HttpGet]
        public async Task<IActionResult> CallReportApi(string method, string model)
        {
            using (var client = new HttpClient())
            {
                var settings = GetSettings();
                var keyvalues = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("account", settings.AccountApiToken),
                    new KeyValuePair<string, string>("dataConnect", settings.DataConnectApiToken),
                    new KeyValuePair<string, string>("clientId", settings.ClientId),
                    new KeyValuePair<string, string>("userId", settings.UserId),
                    new KeyValuePair<string, string>("userRole", string.Join(",", settings.CurrentUserRole))
                };

                var data = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(model);
                foreach (var key in data.Keys)
                    if (key != "adminMode" || key == "adminMode" && settings.CanUseAdminMode)
                        keyvalues.Add(new KeyValuePair<string, string>(key, data[key].ToString()));

                
                var content = new FormUrlEncodedContent(keyvalues);
                var response = await client.PostAsync(new Uri(settings.ApiUrl + method), content);
                var stringContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject(stringContent);
                if (stringContent == "\"\"")
                    result = new { };
                return Ok(result);
            }

        }

        [HttpPost]
        public IActionResult RunReport(dynamic data)
        {
            string reportSql = data.reportSql;
            string connectKey = data.connectKey;
            string reportType = data.reportType;
            var pageNumber = 1;
            var pageSize = 50;
            string sortBy = data.sortBy;
            bool desc = data.desc;
            string reportSeries = data.reportSeries;

            var sql = "";

            try
            {
                if (string.IsNullOrEmpty(reportSql))
                {
                    throw new Exception("Query not found");
                }
                var allSqls = reportSql.Split(new string[] { "%2C" }, StringSplitOptions.RemoveEmptyEntries);
                var dt = new DataTable();
                var dtPaged = new DataTable();
                var dtCols = 0;

                List<string> fields = new List<string>();
                for (int i = 0; i < allSqls.Length; i++)
                {
                    sql = DotNetReportHelper.Decrypt(HttpUtility.HtmlDecode(allSqls[i]), _settings.PrivateApiToken);

                    var sqlSplit = sql.Substring(0, sql.IndexOf("FROM")).Replace("SELECT", "").Trim();
                    var sqlFields = Regex.Split(sqlSplit, "], (?![^\\(]*?\\))").Where(x => x != "CONVERT(VARCHAR(3)")
                        .Select(x => x.EndsWith("]") ? x : x + "]")
                        .ToList();

                    if (!String.IsNullOrEmpty(sortBy))
                    {
                        if (sortBy.StartsWith("DATENAME(MONTH, "))
                        {
                            sortBy = sortBy.Replace("DATENAME(MONTH, ", "MONTH(");
                        }
                        if (sortBy.StartsWith("MONTH(") && sortBy.Contains(")) +") && sql.Contains("Group By"))
                        {
                            sortBy = sortBy.Replace("MONTH(", "CONVERT(VARCHAR(3), DATENAME(MONTH, ");
                        }
                        if (!sql.Contains("ORDER BY"))
                        {
                            sql = sql + "ORDER BY " + sortBy + (desc ? " DESC" : "");
                        }
                        else
                        {
                            sql = sql.Substring(0, sql.IndexOf("ORDER BY")) + "ORDER BY " + sortBy + (desc ? " DESC" : "");
                        }
                    }

                    // Execute sql
                    var dtRun = new DataTable();
                    var dtPagedRun = new DataTable();
                    var dataSettings = DataSettingsManager.LoadSettings();
                    using (var conn = new SqlConnection(dataSettings.DataConnectionString))
                    {
                        conn.Open();
                        var command = new SqlCommand(sql, conn);
                        var adapter = new SqlDataAdapter(command);
                        adapter.Fill(dtRun);
                        dtPagedRun = (dtRun.Rows.Count > 0) ? dtPagedRun = dtRun.AsEnumerable().Skip((pageNumber - 1) * pageSize).Take(pageSize).CopyToDataTable() : dtRun;

                        string[] series = { };
                        if (i == 0)
                        {
                            dt = dtRun;
                            dtPaged = dtPagedRun;
                            dtCols = dtRun.Columns.Count;
                            fields.AddRange(sqlFields);
                        }
                        else if (i > 0)
                        {
                            // merge in to dt
                            if (!string.IsNullOrEmpty(reportSeries))
                                series = reportSeries.Split(new string[] { "%2C" }, StringSplitOptions.RemoveEmptyEntries);

                            var j = 1;
                            while (j < dtPagedRun.Columns.Count)
                            {
                                var col = dtPagedRun.Columns[j++];
                                dtPaged.Columns.Add($"{col.ColumnName} ({series[i - 1]})", col.DataType);
                                fields.Add(sqlFields[j - 1]);
                            }

                            foreach (DataRow dr in dtPaged.Rows)
                            {
                                DataRow match = null;
                                if (fields[0].ToUpper().StartsWith("CONVERT(VARCHAR(10)")) // group by day
                                {
                                    match = dtPagedRun.AsEnumerable().Where(r => !string.IsNullOrEmpty(r.Field<string>(0)) && !string.IsNullOrEmpty((string)dr[0]) && Convert.ToDateTime(r.Field<string>(0)).Day == Convert.ToDateTime((string)dr[0]).Day).FirstOrDefault();
                                }
                                else if (fields[0].ToUpper().StartsWith("CONVERT(VARCHAR(3)")) // group by month/year
                                {

                                }
                                else
                                {
                                    match = dtPagedRun.AsEnumerable().Where(r => r.Field<string>(0) == (string)dr[0]).FirstOrDefault();
                                }
                                if (match != null)
                                {
                                    j = 1;
                                    while (j < dtCols)
                                    {
                                        dr[j + i + dtCols - 2] = match[j];
                                        j++;
                                    }
                                }
                            }
                        }
                    }
                }

                var model = new DotNetReportResultModel
                {
                    ReportData = DataTableToDotNetReportDataModel(dtPaged, fields),
                    Warnings = GetWarnings(allSqls[0]),
                    ReportSql = allSqls[0],
                    ReportDebug = Request.Host.Host.Contains("localhost"),
                    Pager = new DotNetReportPagerModel
                    {
                        CurrentPage = pageNumber,
                        PageSize = pageSize,
                        TotalRecords = dt.Rows.Count,
                        TotalPages = (int)((dt.Rows.Count / pageSize) + 1)
                    }
                };

                return Ok(model);
            }

            catch (Exception ex)
            {
                var model = new DotNetReportResultModel
                {
                    ReportData = new DotNetReportDataModel(),
                    ReportSql = sql,
                    HasError = true,
                    Exception = ex.Message
                };

                return Ok(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboards(bool adminMode = false)
        {
            var model = await GetDashboardsData(adminMode);
            return Ok(model);
        }

        public async Task<dynamic> GetDashboardsData(bool adminMode = false)
        {
            var settings = GetSettings();

            using (var client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("account", settings.AccountApiToken),
                    new KeyValuePair<string, string>("dataConnect", settings.DataConnectApiToken),
                    new KeyValuePair<string, string>("clientId", settings.ClientId),
                    new KeyValuePair<string, string>("userId", settings.UserId),
                    new KeyValuePair<string, string>("userRole", string.Join(",", settings.CurrentUserRole)),
                    new KeyValuePair<string, string>("adminMode", adminMode.ToString()),
                });

                var response = await client.PostAsync(new Uri(settings.ApiUrl + $"/ReportApi/GetDashboards"), content);
                var stringContent = await response.Content.ReadAsStringAsync();

                var model = JsonConvert.DeserializeObject<dynamic>(stringContent);
                return model;
            }
        }

        [HttpGet]
        public IActionResult GetUsersAndRoles()
        {
            var settings = GetSettings();
            return Ok(new
            {
                noAccount = string.IsNullOrEmpty(settings.AccountApiToken) || settings.AccountApiToken == "Your Public Account Api Token",
                users = settings.CanUseAdminMode ? settings.Users : new List<string>(),
                userRoles = settings.CanUseAdminMode ? settings.UserRoles : new List<string>(),
                currentUserId = settings.UserId,
                currentUserRoles = settings.UserRoles,
                currentUserName = settings.UserName,
                allowAdminMode = settings.CanUseAdminMode
            });
        }

        private string GetWarnings(string sql)
        {
            var warning = "";
            if (sql.ToLower().Contains("cross join"))
                warning += "Some data used in this report have relations that are not setup properly, so data might duplicate incorrectly.<br/>";

            return warning;
        }

        public static bool IsNumericType(Type type)
        {

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    return true;

                case TypeCode.Boolean:
                case TypeCode.DateTime:
                case TypeCode.String:
                default:
                    return false;
            }
        }

        public static string GetLabelValue(DataColumn col, DataRow row)
        {
            switch (Type.GetTypeCode(col.DataType))
            {
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                    return row[col].ToString();

                case TypeCode.Double:
                case TypeCode.Decimal:
                    return @row[col].ToString();// "'" + (Convert.ToDouble(@row[col].ToString()).ToString("C")) + "'";

                case TypeCode.Boolean:
                    return Convert.ToBoolean(@row[col]) ? "Yes" : "No";

                case TypeCode.DateTime:
                    try
                    {
                        return "'" + @Convert.ToDateTime(@row[col]).ToShortDateString() + "'";
                    }
                    catch
                    {
                        return "'" + @row[col] + "'";
                    }

                case TypeCode.String:
                default:
                    return "'" + @row[col].ToString().Replace("'", "") + "'";
            }
        }

        public static string GetFormattedValue(DataColumn col, DataRow row)
        {
            if (@row[col] != null)
                switch (Type.GetTypeCode(col.DataType))
                {
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                    case TypeCode.Single:
                        return row[col].ToString();


                    case TypeCode.Double:
                    case TypeCode.Decimal:
                        return col.ColumnName.Contains("%")
                            ? (Convert.ToDouble(row[col].ToString()) / 100).ToString("P2")
                            : Convert.ToDouble(row[col].ToString()).ToString("C");


                    case TypeCode.Boolean:
                        return Convert.ToBoolean(row[col]) ? "Yes" : "No";


                    case TypeCode.DateTime:
                        try
                        {
                            return Convert.ToDateTime(row[col]).ToShortDateString();
                        }
                        catch
                        {
                            return row[col] != null ? row[col].ToString() : null;
                        }

                    case TypeCode.String:
                    default:
                        if (row[col].ToString() == "System.Byte[]")

                            return "<img src=\"data:image/png;base64," + Convert.ToBase64String((byte[])row[col], 0, ((byte[])row[col]).Length) + "\" style=\"max-width: 200px;\" />";
                        else
                            return row[col].ToString();

                }
            return "";
        }


        private DotNetReportDataModel DataTableToDotNetReportDataModel(DataTable dt, List<string> sqlFields)
        {
            var model = new DotNetReportDataModel
            {
                Columns = new List<DotNetReportDataColumnModel>(),
                Rows = new List<DotNetReportDataRowModel>()
            };

            int i = 0;
            foreach (DataColumn col in dt.Columns)
            {
                var sqlField = sqlFields[i++];
                model.Columns.Add(new DotNetReportDataColumnModel
                {
                    SqlField = sqlField.Substring(0, sqlField.IndexOf("AS")).Trim(),
                    ColumnName = col.ColumnName,
                    DataType = col.DataType.ToString(),
                    IsNumeric = IsNumericType(col.DataType)
                });

            }

            foreach (DataRow row in dt.Rows)
            {
                i = 0;
                var items = new List<DotNetReportDataRowItemModel>();

                foreach (DataColumn col in dt.Columns)
                {

                    items.Add(new DotNetReportDataRowItemModel
                    {
                        Column = model.Columns[i],
                        Value = row[col] != null ? row[col].ToString() : null,
                        FormattedValue = GetFormattedValue(col, row),
                        LabelValue = GetLabelValue(col, row)
                    });
                    i += 1;

                }

                model.Rows.Add(new DotNetReportDataRowModel
                {
                    Items = items.ToArray()
                });
            }

            return model;
        }

    }
}