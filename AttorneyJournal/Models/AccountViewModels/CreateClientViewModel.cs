using System;
using System.ComponentModel.DataAnnotations;

namespace AttorneyJournal.Models.AccountViewModels
{
	/// <summary>
	/// 
	/// </summary>
	public class CreateClientViewModel
	{
		public string Id { get; set; }
		public DateTime? CreatedAt { get; set; }
		public string AssignedToAttorney { get; set; }
		//public string DeviceToken { get; set; }

		/// <summary>
		/// 
		/// </summary>
		[Required]
		[Display(Name = "Name")]
		public string Name { get; set; }

		/// <summary>
		/// 
		/// </summary>
		[Required]
		[Display(Name = "Surname")]
		public string Surname { get; set; }

		/// <summary>
		/// A valid mail.
		/// </summary>
		[Required]
		[EmailAddress]
		[Display(Name = "Email")]
		public string Email { get; set; }


		public string ImageURL { get; set; }

		/// <summary>
		/// Registration Code provided by Attorney. This will bind user to Attorney.
		/// </summary>
		//[Required]
		//[MaxLength(8)]
		//public string RegistrationCode { get; set; } = Guid.NewGuid().ToString().ToUpperInvariant().Substring(0, 8);
	}
}