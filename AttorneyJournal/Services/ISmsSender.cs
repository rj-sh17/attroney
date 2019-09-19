using System.Threading.Tasks;

namespace AttorneyJournal.Services
{
    public interface ISmsSender
    {
        Task SendSmsAsync(string number, string message);
    }
}
