using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Reports.DotnetReport.Models
{
    public class DotNetReportUserRegister
    {
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }
        public string Name { get; set; }
        public string BusinessName { get; set; }
        public string Phone { get; set; }
    }
}
