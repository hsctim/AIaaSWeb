using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using AIaaS.MultiTenancy.Accounting.Dto;

namespace AIaaS.MultiTenancy.Accounting
{
    public interface IInvoiceAppService
    {
        Task<InvoiceDto> GetInvoiceInfo(EntityDto<long> input);

        Task CreateInvoice(CreateInvoiceDto input);
    }
}
