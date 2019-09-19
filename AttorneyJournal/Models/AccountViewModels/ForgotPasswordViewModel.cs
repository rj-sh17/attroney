using System.ComponentModel.DataAnnotations;

namespace AttorneyJournal.Models.AccountViewModels
{
	public class ForgotPasswordViewModel
	{
		[Required]
		[EmailAddress]
		public string Email { get; set; }
	}
}