using System;
using System.Collections.Generic;
using System.Text;
using Nop.Plugin.Reports.DotnetReport.Models;
using Nop.Services.Localization;
using Nop.Web.Framework.Validators;

namespace Nop.Plugin.Reports.DotnetReport.Validator
{
    public class DotNetReportUserRegisterValidator : BaseNopValidator<DotNetReportUserRegister>
    {
        //public DotNetReportUserRegisterValidator(ILocalizationService localizationService)
        //{
        //    RuleFor(x => x.Email).NotEmpty().WithMessage(localizationService.GetResource("Account.Fields.Email.Required"));
        //    RuleFor(x => x.Email).EmailAddress().WithMessage(localizationService.GetResource("Common.WrongEmail"));
        //}
    }
}
