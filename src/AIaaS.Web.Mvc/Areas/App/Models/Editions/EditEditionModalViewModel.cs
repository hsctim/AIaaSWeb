using System.Collections.Generic;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using AIaaS.Editions.Dto;
using AIaaS.Web.Areas.App.Models.Common;

namespace AIaaS.Web.Areas.App.Models.Editions
{
    [AutoMapFrom(typeof(GetEditionEditOutput))]
    public class EditEditionModalViewModel : GetEditionEditOutput, IFeatureEditViewModel
    {
        public bool IsEditMode => Edition.Id.HasValue;

        public IReadOnlyList<ComboboxItemDto> EditionItems { get; set; }

        public IReadOnlyList<ComboboxItemDto> FreeEditionItems { get; set; }
    }
}