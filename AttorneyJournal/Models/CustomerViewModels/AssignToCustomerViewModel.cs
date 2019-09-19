using System.ComponentModel.DataAnnotations;

namespace AttorneyJournal.Models.CustomerViewModels
{
	public class AssignToCustomerViewModel
	{
		/// <summary>
		///     A valid mail.
		/// </summary>
		[Required]
		[EmailAddress]
		[Display(Name = "Email")]
		public string Email { get; set; }
	}
}