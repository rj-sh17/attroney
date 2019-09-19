using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace AttorneyJournal.Models.AttorneyViewModels
{
	public class BaseViewModel
	{
		public Guid Id { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }
		public bool IsValid { get; set; }
	}

    public class CreateAttorneyViewModel : BaseViewModel
	{
	    /// <summary>
	    /// 
	    /// </summary>
	    [Required]
	    [Display(Name = nameof(Name))]
	    public string Name { get; set; }

	    /// <summary>
	    /// 
	    /// </summary>
	    [Required]
	    [Display(Name = nameof(Surname))]
	    public string Surname { get; set; }

	    /// <summary>
	    /// A valid mail.
	    /// </summary>
	    [Required]
	    [EmailAddress]
	    [Display(Name = nameof(Email))]
	    public string Email { get; set; }

		[Required]
		[Display(Name = nameof(Password))]
		public string Password { get; set; }

		public int UserSubscribed { get; set; }
	}
}
