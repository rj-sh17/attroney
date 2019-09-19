namespace AttorneyJournal.Controllers {
    using System;
    using System.Threading.Tasks;
    using AttorneyJournal.Data;
    using AttorneyJournal.Models;
    using AttorneyJournal.Models.HomeViewModels;
    using AttorneyJournal.Services;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;

    public class HomeController : BaseController {
		private readonly ApplicationDbContext _context;
		private readonly IEmailSender _emailSender;

		public HomeController (
			UserManager<ApplicationUser> userManager,
			SignInManager<ApplicationUser> signInManager,
			ApplicationDbContext context,
			IEmailSender emailSender) : base (userManager, signInManager) {
			_context = context;
			_emailSender = emailSender;
		}

		[Authorize]
		public IActionResult Index () {
			return RedirectToAction ("Index", "Customer");
		}

		public IActionResult Error () {
			return View ();
		}

		[HttpGet]
		[ApiExplorerSettings (IgnoreApi = true)]
		[Route ("SendFeedback/")]
		public IActionResult SendFeedback () {
			return View ();
		}

		[HttpPost]
		[Route ("SendFeedback/")]
		public async Task<IActionResult> SendFeedback (SendFeedbackViewModel model) {
			if (!ModelState.IsValid) return View (model);

			var user = await _userManager.GetUserAsync (User);
			if (user == null) return RedirectToAction ("SendFeedback", "Home");
			var userId = new Guid (user.Id);

			var attorney = await _context.Attorneys.FirstOrDefaultAsync (x => x.AttorneyUserId == userId);
			if (attorney == null) return RedirectToAction ("SendFeedback", "Home");

			await _emailSender.SendEmailAsync (
				null,
				"services@aires.io",
				"Visual Evidence Recorder Feedback",
				"Message from: " + attorney.Name + " " + attorney.Surname + "<br><br>" + model.Feedback,
				"Administrator",
				"");

			return RedirectToAction ("Index", "Customer");
		}
	}
}