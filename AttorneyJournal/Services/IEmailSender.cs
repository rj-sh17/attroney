using System.Threading.Tasks;

namespace AttorneyJournal.Services
{
	public interface IEmailSender
	{
		Task SendEmailAsync(string sign, string email, string subject, string message, string name = null, string surname = null);
	}
}