using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AttorneyJournal.Data;
using AttorneyJournal.Models;
using AttorneyJournal.Models.AttorneyViewModels;
using AttorneyJournal.Models.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Core;
using OpenIddict.Models;

namespace AttorneyJournal.Controllers
{
	[Authorize(Roles = CommonConstant.AdministratorRole)]
	public class AttorneyController : Controller
	{
		private readonly ApplicationDbContext _context;
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly OpenIddictApplicationManager<OpenIddictApplication> _appManager;

		public AttorneyController(
			UserManager<ApplicationUser> userManager,
			ApplicationDbContext context,
			OpenIddictApplicationManager<OpenIddictApplication> appManager)
		{
			_userManager = userManager;
			_context = context;
			_appManager = appManager;
		}

		// GET: Attorney
		[HttpGet]
		[ApiExplorerSettings(IgnoreApi = true)]
		[Route("Attorney/List/{page?}/{itemPerPage?}")]
		public async Task<IActionResult> Index(int page = 0, int itemPerPage = 10)
		{
			var total = await _context.Attorneys.CountAsync();
			var list = await _context.Attorneys
				.Select(x => new CreateAttorneyViewModel
				{
					Surname = x.Surname,
					Name = x.Name,
					CreatedAt = x.CreatedAt,
					UpdatedAt = x.UpdatedAt,
					Id = x.Id,
					IsValid = x.IsValid,
					UserSubscribed = x.Users.Count
				})
				.OrderByDescending(x => x.CreatedAt)
				.Skip(page * itemPerPage)
				.Take(itemPerPage)
				.ToListAsync();

			return View(new CustomerController.TablePagination<CreateAttorneyViewModel>
			{
				Total = total,
				Lists = list
			});
		}

		// GET: Attorney/Details/5
		public ActionResult Details(int id)
		{
			return View();
		}

		// GET: Attorney/Create
		public ActionResult Create()
		{
			return View(new CreateAttorneyViewModel { Password = Guid.NewGuid().ToString().Substring(1, 6) });
		}

		// POST: Attorney/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Create(CreateAttorneyViewModel model)
		{
			try
			{
				var userAttorney = await _userManager.FindByEmailAsync(model.Email);
				if (userAttorney != null)
				{
					ModelState.AddModelError(nameof(CreateAttorneyViewModel.Email), "Duplicated Email!");
					return View(model);
				}

				userAttorney = new ApplicationUser { UserName = model.Email, Email = model.Email, AssignedToAttorneyId = null };
				await _userManager.CreateAsync(userAttorney, CommonConstant.SuperAdminPassword);
				await _userManager.AddToRoleAsync(userAttorney, CommonConstant.AttorneyRole);
				await _userManager.AddClaimAsync(userAttorney, new Claim(CommonConstant.ClaimName, model.Name));
				await _userManager.AddClaimAsync(userAttorney, new Claim(CommonConstant.ClaimSurname, model.Surname));

				_context.Attorneys.Add(new Attorney
				{
					AttorneyUserId = new Guid(userAttorney.Id),
					Name = model.Name,
					Surname = model.Surname,
					CreatedAt = DateTime.UtcNow,
					IsValid = true
				});
				await _context.SaveChangesAsync();

				return RedirectToAction("Index");
			}
			catch
			{
				return View(model);
			}
		}

		// GET: Attorney/Edit/5
		public ActionResult Edit(int id)
		{
			return View();
		}

		// POST: Attorney/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Edit(int id, IFormCollection collection)
		{
			try
			{
				// TODO: Add update logic here

				return RedirectToAction("Index");
			}
			catch
			{
				return View();
			}
		}

		// GET: Attorney/Delete/5
		public ActionResult Delete(int id)
		{
			return View();
		}

		// POST: Attorney/Delete/5
		[HttpGet]
		[Route("Attorney/Delete/{id}")]
		public async Task<IActionResult> Delete(Guid id)
		{
			try
			{
				var found = await _context.Attorneys.FirstOrDefaultAsync(x => x.Id == id);
				if (found != null)
				{
					found.IsValid = !found.IsValid;

					var user = await _userManager.FindByIdAsync(found.AttorneyUserId.ToString());
					user.LockoutEnd = !found.IsValid ? DateTimeOffset.UtcNow.AddYears(1000) : new DateTimeOffset?();

					// Revoke all the token associated with.
					if (!found.IsValid) await _userManager.RemoveFromRoleAsync(user, CommonConstant.AttorneyRole);
					else await _userManager.AddToRoleAsync(user, CommonConstant.AttorneyRole);


					await _context.SaveChangesAsync();
				}
				return RedirectToAction("Index", "Attorney");
			}
			catch
			{
				return View("Error");
			}
		}
	}
}