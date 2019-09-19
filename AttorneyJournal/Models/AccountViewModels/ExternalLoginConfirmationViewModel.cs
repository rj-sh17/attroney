using System.ComponentModel.DataAnnotations;

namespace AttorneyJournal.Models.AccountViewModels
{
	public class ExternalLoginConfirmationViewModel
	{
		[Required]
		[EmailAddress]
		public string Email { get; set; }
	}
}
