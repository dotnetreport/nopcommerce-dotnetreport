using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Data.OleDb;
using Newtonsoft.Json;
using System.Data;
using Nop.Plugin.Reports.DotnetReport.Models;
using Nop.Web.Framework.Controllers;
using Nop.Services.Security;
using Nop.Services.Configuration;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Core.Data;
using Nop.Web.Framework;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Data;
using System.Data.SqlClient;

namespace Nop.Plugin.Reports.DotnetReport.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.Admin)]
    public class DotNetSetupController : BasePluginController
    {
        private readonly DotNetReportConfigSettings _settings;
        private readonly IPermissionService _permissionService;
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;
        private readonly IDbContext _dbContext;
        public DotNetSetupController(
            DotNetReportConfigSettings settings,
            IPermissionService permissionService,
            ISettingService settingService, ILocalizationService localizationService,
            INotificationService notificationService, IDbContext dbContext)
        {
            _settings = settings;
            _permissionService = permissionService;
            _settingService = settingService;
            _localizationService = localizationService;
            _notificationService = notificationService;
            _dbContext = dbContext;
        }

        public IActionResult Configure()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.AccessAdminPanel))
                return AccessDeniedView();
            
            var model = new DotNetReportConfig
            {
                ApiUrl = _settings.ApiUrl,
                AccountApiToken= _settings.AccountApiToken,
                DataConnectApiToken = _settings.DataConnectApiToken,
                PrivateApiToken = _settings.PrivateApiToken
            };
            
            return View("~/Plugins/Reports.DotnetReport/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAntiForgery]
        public IActionResult Configure(DotNetReportConfig model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.AccessAdminPanel))
                return Content("Access denied");

            if (!ModelState.IsValid)
                return Configure();
            //save settings
            _settings.ApiUrl = model.ApiUrl;
            _settings.AccountApiToken = model.AccountApiToken;
            _settings.DataConnectApiToken = model.DataConnectApiToken;
            _settings.PrivateApiToken = model.PrivateApiToken;
            _settingService.SaveSetting(_settings);
            _notificationService.SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
           // return Json(new { Result = true });
        }


        public async Task<IActionResult> Index(string databaseApiKey = "")
        {
            var connect = GetConnection(databaseApiKey);
            var tables = new List<TableViewModel>();

            tables.AddRange(await GetTables("BASE TABLE", connect.AccountApiKey, connect.DatabaseApiKey));
            tables.AddRange(await GetTables("VIEW", connect.AccountApiKey, connect.DatabaseApiKey));

           // tables = tables.Take(10).ToList();

            var model = new ManageViewModel
            {
                ApiUrl = connect.ApiUrl,
                AccountApiKey = connect.AccountApiKey,
                DatabaseApiKey = connect.DatabaseApiKey,
                Tables = tables
            };

            return View("~/Plugins/Reports.DotnetReport/Views/DotNetSetup/Index.cshtml", model);
        }

        #region "Private Methods"

        private ConnectViewModel GetConnection(string databaseApiKey)
        {
            return new ConnectViewModel
            {
                ApiUrl = _settings.ApiUrl,
                AccountApiKey = _settings.AccountApiToken,
                DatabaseApiKey = string.IsNullOrEmpty(databaseApiKey) ? _settings.DataConnectApiToken : databaseApiKey
            };
        }

        private string GetConnectionString(ConnectViewModel connect)
        {
            using (var client = new HttpClient())
            {                
                var dataSettings = DataSettingsManager.LoadSettings();
                var connString = dataSettings.DataConnectionString;
                connString = connString.Replace("Trusted_Connection=True", "");

                return connString;
            }

        }

        //private FieldTypes ConvertToJetDataType(int oleDbDataType)
        //{
        //    switch ((OleDbType)oleDbDataType)
        //    {
        //        case OleDbType.LongVarChar:
        //            return FieldTypes.Varchar; // "varchar";
        //        case OleDbType.BigInt:
        //            return FieldTypes.Int; // "int";       // In Jet this is 32 bit while bigint is 64 bits
        //        case OleDbType.Binary:
        //        case OleDbType.LongVarBinary:
        //            return FieldTypes.Varchar; // "binary";
        //        case OleDbType.Boolean:
        //            return FieldTypes.Boolean; // "bit";
        //        case OleDbType.Char:
        //            return FieldTypes.Varchar; // "char";
        //        case OleDbType.Currency:
        //            return FieldTypes.Money; // "decimal";
        //        case OleDbType.DBDate:
        //        case OleDbType.Date:
        //        case OleDbType.DBTimeStamp:
        //            return FieldTypes.DateTime; // "datetime";
        //        case OleDbType.Decimal:
        //        case OleDbType.Numeric:
        //            return FieldTypes.Double; // "decimal";
        //        case OleDbType.Double:
        //            return FieldTypes.Double; // "double";
        //        case OleDbType.Integer:
        //            return FieldTypes.Int; // "int";
        //        case OleDbType.Single:
        //            return FieldTypes.Int; // "single";
        //        case OleDbType.SmallInt:
        //            return FieldTypes.Int; // "smallint";
        //        case OleDbType.TinyInt:
        //            return FieldTypes.Int; // "smallint";  // Signed byte not handled by jet so we need 16 bits
        //        case OleDbType.UnsignedTinyInt:
        //            return FieldTypes.Int; // "byte";
        //        case OleDbType.VarBinary:
        //            return FieldTypes.Varchar; // "varbinary";
        //        case OleDbType.VarChar:
        //            return FieldTypes.Varchar; // "varchar";
        //        case OleDbType.BSTR:
        //        case OleDbType.Variant:
        //        case OleDbType.VarWChar:
        //        case OleDbType.VarNumeric:
        //        case OleDbType.Error:
        //        case OleDbType.WChar:
        //        case OleDbType.DBTime:
        //        case OleDbType.Empty:
        //        case OleDbType.Filetime:
        //        case OleDbType.Guid:
        //        case OleDbType.IDispatch:
        //        case OleDbType.IUnknown:
        //        case OleDbType.UnsignedBigInt:
        //        case OleDbType.UnsignedInt:
        //        case OleDbType.UnsignedSmallInt:
        //        case OleDbType.PropVariant:
        //        default:
        //            return FieldTypes.Varchar; // 
        //            //throw new ArgumentException(string.Format("The data type {0} is not handled by Jet. Did you retrieve this from Jet?", ((OleDbType)oleDbDataType)));
        //    }
        //}

        private async Task<List<TableViewModel>> GetApiTables(string accountKey, string dataConnectKey)
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(string.Format("{0}/ReportApi/GetTables?account={1}&dataConnect={2}&clientId=", _settings.ApiUrl, accountKey, dataConnectKey));

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();

                dynamic values = JsonConvert.DeserializeObject<dynamic>(content);

                var tables = new List<TableViewModel>();
                foreach (var item in values)
                    tables.Add(new TableViewModel
                    {
                        Id = item.tableId,
                        TableName = item.tableDbName,
                        DisplayName = item.tableName,
                        AllowedRoles = item.tableRoles.ToObject<List<string>>()
                    });

                return tables;
            }
        }

        private async Task<List<ColumnViewModel>> GetApiFields(string accountKey, string dataConnectKey, int tableId)
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(string.Format("{0}/ReportApi/GetFields?account={1}&dataConnect={2}&clientId={3}&tableId={4}&includeDoNotDisplay=true", _settings.ApiUrl, accountKey, dataConnectKey, "", tableId));

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();

                dynamic values = JsonConvert.DeserializeObject<dynamic>(content);

                var columns = new List<ColumnViewModel>();
                foreach (var item in values)
                {
                    var column = new ColumnViewModel
                    {
                        Id = item.fieldId,
                        ColumnName = item.fieldDbName,
                        DisplayName = item.fieldName,
                        FieldType = item.fieldType,
                        PrimaryKey = item.isPrimary,
                        ForeignKey = item.hasForeignKey,
                        DisplayOrder = item.fieldOrder,
                        ForeignKeyField = item.foreignKey,
                        ForeignValueField = item.foreignValue,
                        ForeignTable = item.foreignTable,
                        DoNotDisplay = item.doNotDisplay,
                        AllowedRoles = item.columnRoles.ToObject<List<string>>()
                    };

                    JoinTypes join;
                    Enum.TryParse((string)item.foreignJoin, out join);
                    column.ForeignJoin = join;

                    columns.Add(column);
                }

                return columns;
            }
        }

        private async Task<List<TableViewModel>> GetTables(string type = "BASE TABLE", string accountKey = null, string dataConnectKey = null)
        {
            var tables = new List<TableViewModel>();

            var currentTables = new List<TableViewModel>();

            if (!string.IsNullOrEmpty(accountKey) && !string.IsNullOrEmpty(dataConnectKey))
                currentTables = await GetApiTables(accountKey, dataConnectKey);

            var connString = GetConnectionString(GetConnection(dataConnectKey));
            using (var conn = new SqlConnection(connString))
            {
                // open the connection to the database 
                conn.Open();

                // Get the Tables
                var schemaTable = conn.GetSchema("Tables", new string[] { null, null, null, type });

                // Store the table names in the class scoped array list of table names
                for (var i = 0; i < schemaTable.Rows.Count; i++)
                {
                    var tableName = schemaTable.Rows[i].ItemArray[2].ToString();

                    // see if this table is already in database
                    var matchTable = currentTables.FirstOrDefault(x => x.TableName.ToLower() == tableName.ToLower());
                    if (matchTable != null)
                        matchTable.Columns = await GetApiFields(accountKey, dataConnectKey, matchTable.Id);
                   
                    var table = new TableViewModel
                    {
                        Id = matchTable != null ? matchTable.Id : 0,
                        TableName = matchTable != null ? matchTable.TableName : tableName,
                        DisplayName = matchTable != null ? matchTable.DisplayName : tableName,
                        IsView = type == "VIEW",
                        Selected = matchTable != null,
                        Columns = new List<ColumnViewModel>(),
                        AllowedRoles = matchTable != null ? matchTable.AllowedRoles : new List<string>()
                    };

                  //  var dtField = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Columns, new object[] { null, null, tableName });
                    var idx = 0;
                    string[] restrictionsColumns = new string[4];
                    restrictionsColumns[2] = tableName;
                    DataTable schemaColumns = conn.GetSchema("Columns", restrictionsColumns);
                    DataTable schemaPrimaryKey = conn.GetSchema("IndexColumns", restrictionsColumns);
                    DataTable schemaForeignKeys = conn.GetSchema("ForeignKeys", restrictionsColumns);
                    foreach (DataRow rowColumn in schemaColumns.Rows)
                        {
                        string columnName = rowColumn[3].ToString();
                        
                        var matchColumn = matchTable != null ? matchTable.Columns.FirstOrDefault(x => x.ColumnName.ToLower() == columnName.ToLower()) : null;
                            var column = new ColumnViewModel
                            {
                                ColumnName = matchColumn != null ? matchColumn.ColumnName : columnName.ToString(),
                                DisplayName = matchColumn != null ? matchColumn.DisplayName : columnName.ToString(),
                               // PrimaryKey = matchColumn != null ? matchColumn.PrimaryKey : rowColumn[6].ToString().ToLower().EndsWith("id") && idx == 0,
                                DisplayOrder = matchColumn != null ? matchColumn.DisplayOrder : idx++,
                                FieldType = matchColumn != null ? matchColumn.FieldType : rowColumn[7].ToString(),
                                AllowedRoles = matchColumn != null ? matchColumn.AllowedRoles : new List<string>()
                            };

                            if (matchColumn != null)
                            {
                                column.ForeignKey = matchColumn.ForeignKey;
                                column.ForeignJoin = matchColumn.ForeignJoin;
                                column.ForeignTable = matchColumn.ForeignTable;
                                column.ForeignKeyField = matchColumn.ForeignKeyField;
                                column.ForeignValueField = matchColumn.ForeignValueField;
                                column.Id = matchColumn.Id;
                                column.DoNotDisplay = matchColumn.DoNotDisplay;
                                column.DisplayOrder = matchColumn.DisplayOrder;

                                column.Selected = true;
                            }

                        if (!table.Columns.Any(x=>x.PrimaryKey = true))
                        {
                            foreach (System.Data.DataRow rowPrimaryKey in schemaPrimaryKey.Rows)
                            {
                                string indexName = rowPrimaryKey[2].ToString();

                                if (indexName.IndexOf("PK_") != -1)
                                    column.PrimaryKey = true;
                            }
                        }
                        if (!table.Columns.Any(x => x.ForeignKey = true))
                        {
                            foreach (System.Data.DataRow rowFK in schemaForeignKeys.Rows)
                            {
                                column.ForeignKey = true;
                                column.ForeignKeyField = rowFK[2].ToString();
                            }
                        }
                        table.Columns.Add(column);
                        }
                    table.Columns = table.Columns.OrderBy(x => x.DisplayOrder).ToList();
                    tables.Add(table);
                }

                conn.Close();
                conn.Dispose();
            }


            return tables;
        }

        #endregion
    }
}