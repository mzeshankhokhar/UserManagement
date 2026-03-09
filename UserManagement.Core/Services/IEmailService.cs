using System.Threading.Tasks;

namespace UserManagement.Core.Services
{
    public interface IEmailService
    {
        Task SendAsync(string toEmail, string subject, string htmlBody);
    }
}
