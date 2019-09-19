namespace Mvc.Server.ViewModels.Authorization
{
	using Microsoft.AspNetCore.Mvc.ModelBinding;

	public class LogoutViewModel
	{
		[BindNever] public string RequestId { get; set; }
	}
}