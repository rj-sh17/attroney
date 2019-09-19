namespace AttorneyJournal.Models.ResourceModels
{
	using System;
	using System.Runtime.Serialization;
	using Domain.Storage;

	[DataContract]
	public class TimelineItemModel
	{
		[DataMember] public DateTime CreatedAt { get; set; }
		[DataMember] public ItemModel File { get; set; }
		[DataMember] public ItemModel Thumb { get; set; }
	}

	[DataContract]
	public class ItemModel
	{
		[DataMember] public string Title { get; set; }
		[DataMember] public string Content { get; set; }

		[DataMember] public string ObjectKey { get; set; }
		[DataMember] public string ObjectUrl { get; set; }
		[DataMember] public string MimeType { get; set; }
		[DataMember] public string FileExtension { get; set; }

		[DataMember] public Guid Id { get; set; }
		[DataMember] public FileStorageType Type { get; set; }
		public bool Viewed { get; set; }

		[DataMember] public DateTime? DateTaken { get; set; }
	}
}
