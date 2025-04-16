using System.Threading.Tasks;
using Abp.Dependency;

namespace AIaaS.MultiTenancy.Accounting
{
    public interface IInvoiceNumberGenerator : ITransientDependency
    {
        Task<string> GetNewInvoiceNumber();
    }
}