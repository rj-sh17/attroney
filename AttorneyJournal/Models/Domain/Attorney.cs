using System;
using System.Collections;
using System.Collections.Generic;

namespace AttorneyJournal.Models.Domain
{
	/// <summary>
	/// 
	/// </summary>
	public class Attorney : BaseEntity
	{
		/// <summary>
		/// 
		/// </summary>
		public string Name { get; set; }
		/// <summary>
		/// 
		/// </summary>
		public string Surname { get; set; }
		/// <summary>
		/// 
		/// </summary>
		
		public Guid AttorneyUserId { get; set; }
		
		public ICollection<ApplicationUser> Users { get; set; }
	}
}