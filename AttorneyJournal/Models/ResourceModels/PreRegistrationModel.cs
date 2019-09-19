using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AttorneyJournal.Models.ResourceModels
{
	public class SuccessModel
	{
		public bool IsSuccess { get; set; }
	}

	/// <summary>
	/// 
	/// </summary>
	public class PreRegistrationModel : SuccessModel
	{
		/// <summary>
		/// 
		/// </summary>
		public string Code { get; set; }
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
		public string Mail { get; set; }
	}
}
