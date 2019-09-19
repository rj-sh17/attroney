using System;
using AttorneyJournal.Models.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace AttorneyJournal.Models
{
	// Add profile data for application users by adding properties to the ApplicationUser class
	public class ApplicationUser : IdentityUser
	{
		public Attorney AssignedToAttorney { get; set; }
		public Guid? AssignedToAttorneyId { get; set; }

		public DateTime? DateOfAccident { get; set; }
		public string ProfilePictureKey { get; set; }

		// Added from RegistrationCodeEntity
		public string Name { get; set; }
		public string Surname { get; set; }
	}
}
