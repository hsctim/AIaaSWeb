using Microsoft.Extensions.Configuration;

namespace AIaaS.Configuration
{
    public interface IAppConfigurationAccessor
    {
        IConfigurationRoot Configuration { get; }
    }
}
