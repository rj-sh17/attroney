using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace AttorneyJournal.Models.Domain.Storage {
	public class FileStorage : BaseEntity {
		public FileStorage Parent { get; set; }

		public string Content { get; set; }
		public string Title { get; set; }
		public string OriginalName { get; set; }
		public string FileExtension { get; set; }
		public string AmazonObjectKey { get; set; }
		public string MimeType { get; set; }
		public ApplicationUser Owner { get; set; }
		public FileStorageType Type { get; set; }
		public bool Viewed { get; set; }
		public DateTime? DateTaken { get; set; } = null;
	}

	public enum FileStorageType {
		Image = 0,
		Video = 1,
		Text = 2,
		PushNotification = 3,
		Thumb = 10
	}
}