using System;
using System.ComponentModel.DataAnnotations;

namespace AttorneyJournal.Models.AccountViewModels
{
	/// <summary>
	/// Provide user information data for registration.
	/// </summary>
	public class RegisterViewModel
	{
		/// <summary>
		/// A valid mail.
		/// </summary>
		[Required]
		[EmailAddress]
		[Display(Name = "Email")]
		public string Email { get; set; }

		/// <summary>
		/// The Password must be at least 6 and at max 32 characters long.
		/// </summary>
		[Required]
		[StringLength(32, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
		[DataType(DataType.Password)]
		[Display(Name = "Password")]
		public string Password { get; set; }

		/// <summary>
		/// The confirmation password must match inserted password.
		/// </summary>
		[DataType(DataType.Password)]
		[Display(Name = "Confirm password")]
		[Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
		public string ConfirmPassword { get; set; }

		[Required]
		[Display(Name = nameof(Name))]
		public string Name { get; set; }

		[Required]
		[Display(Name = nameof(Surname))]
		public string Surname { get; set; }

		/// <summary>
		/// Registration Code provided by Attorney. This will bind user to Attorney.
		/// </summary>
		//[Required]
		//[MaxLength(8)]
		//public string RegistrationCode { get; set; }

		/// <summary>
		/// 
		/// </summary>
		[Required]
		public DateTime DateOfAccident { get; set; }

		/// <summary>
		/// Base64 image format JPEG.
		/// </summary>
		public string ProfileImage { get; set; }
	}
}