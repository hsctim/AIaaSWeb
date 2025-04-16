using System.Threading.Tasks;

namespace AIaaS.Net.Sms
{
    public interface ISmsSender
    {
        Task SendAsync(string number, string message);
    }
}