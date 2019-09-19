using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AttorneyJournal.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AttorneyJournal.Controllers
{
	public class BaseController : Controller
	{
		protected readonly UserManager<ApplicationUser> _userManager;
		protected readonly SignInManager<ApplicationUser> _signInManager;

		public BaseController(
			UserManager<ApplicationUser> userManager,
			SignInManager<ApplicationUser> signInManager
		)
		{
			_userManager = userManager;
			_signInManager = signInManager;
		}

		public override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			var user = _userManager.GetUserAsync(User).GetAwaiter().GetResult();
			if (_userManager.IsLockedOutAsync(user).GetAwaiter().GetResult())
			{
				_signInManager.SignOutAsync().GetAwaiter().GetResult();
				filterContext.Result = RedirectToAction("Login", "Account");
			}
		}
	}
}