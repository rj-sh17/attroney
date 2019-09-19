using System;
using System.Linq;

namespace AttorneyJournal.Models.ResourceModels {
	using System.Collections.Generic;
	using System.Runtime.Serialization;

	/// <summary>
	/// 
	/// </summary>
	[DataContract]
	public class UserTimelineModel {
		/// <summary>
		/// 
		/// </summary>
		[DataMember] public List<TimelineItemModel> Items { get; set; }
		/// <summary>
		/// 
		/// </summary>
		[DataMember] public string Name { get; set; }
		/// <summary>
		/// 
		/// </summary>
		[DataMember] public string Surname { get; set; }
		/// <summary>
		/// 
		/// </summary>
		[DataMember] public string UserId { get; set; }

		[DataMember] public DateTime DateOfAccident { get; set; }
		/// <summary>
		/// 
		/// </summary>
		public DateTime? LastUploadedItem => Items?.FirstOrDefault ()?.CreatedAt;

		public string PhotoURL { get; set; }
	}
}