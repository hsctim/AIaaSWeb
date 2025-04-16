using AIaaS.Nlp.Dtos;

using Abp.Extensions;

namespace AIaaS.Web.Areas.App.Models.NlpTokens
{
    public class CreateOrEditNlpTokenModalViewModel
    {
        public CreateOrEditNlpTokenDto NlpToken { get; set; }

        public bool IsEditMode => NlpToken.Id.HasValue;
    }
}