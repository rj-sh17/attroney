using System;
using System.ComponentModel.DataAnnotations;

namespace AttorneyJournal.Models.Domain
{
	public class BaseEntity
	{
		[Key]
		public Guid Id { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public DateTime? UpdatedAt { get; set; } = null;

		public bool IsValid { get; set; } = true;

		public BaseEntity()
		{
			Id = Guid.NewGuid();
		}
	}
}
