using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace AttorneyJournal.Models
{
	/// <summary>
	/// 
	/// </summary>
	[DataContract]
	public class HttpResponseMessageWithError
	{
		/// <summary>
		/// 
		/// </summary>
		public HttpResponseMessageWithError() { }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="errors"></param>
		/// <param name="modelErrors"></param>
		public HttpResponseMessageWithError(List<string> errors = null, List<string> modelErrors = null)
		{
			Errors = errors;
			ModelErrors = modelErrors;
		}

		/// <summary>
		/// 
		/// </summary>
		[DataMember] public List<string> Errors { get; set; }
		/// <summary>
		/// 
		/// </summary>
		[DataMember] public List<string> ModelErrors { get; set; }



		//[DataMember] public HttpResponseMessage Message { get; set; }
	}

	/// <summary>
	/// 
	/// </summary>
	public static class HttpResponseMessageExtensions
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="controller"></param>
		/// <param name="errors"></param>
		/// <param name="statusCode"></param>
		/// <returns></returns>
		public static HttpResponseMessageWithError SetResponseAndReturn(
			this Controller controller,
			List<string> errors = null, 
			HttpStatusCode statusCode = HttpStatusCode.OK)
		{
			controller.Response.StatusCode = (int) statusCode;
			return new HttpResponseMessageWithError(errors, controller.ModelState.SelectMany(x => x.Value.Errors).Select(x => x.ErrorMessage).ToList());
		}
	}
}
