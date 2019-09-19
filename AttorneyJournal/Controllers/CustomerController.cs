using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using AttorneyJournal.Data;
using AttorneyJournal.Helpers;
using AttorneyJournal.Models;
using AttorneyJournal.Models.AccountViewModels;
using AttorneyJournal.Models.CustomerViewModels;
using AttorneyJournal.Models.Domain;
using AttorneyJournal.Models.Domain.Storage;
using AttorneyJournal.Models.ResourceModels;
using AttorneyJournal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneSignal.CSharp.SDK.Core.Resources.Notifications;

namespace AttorneyJournal.Controllers {
    /// <summary>
    /// 
    /// </summary>
    [Authorize (Roles = "Administrator, Attorney")]
    public class CustomerController : BaseController {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly IAmazonS3 _amazonS3;

        public CustomerController (
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            IEmailSender emailSender,
            IAmazonS3 amazonS3) : base (userManager, signInManager) {
            _context = context;
            _emailSender = emailSender;
            _amazonS3 = amazonS3;
        }

        public class TablePagination<T>
            where T : class {
            public int Total { get; set; }
            public List<T> Lists { get; set; }
        }

        [HttpGet]
        [ApiExplorerSettings (IgnoreApi = true)]
        [Route ("Customer")]
        public async Task<IActionResult> Index (int page = 0, int itemPerPage = 10) {
            Predicate<ApplicationUser> conditionalResults = x => true;

            var user = await _userManager.GetUserAsync (User);
            if (User.IsInRole ("Attorney")) {
                var userId = new Guid (user.Id);
                var attorney = await _context.Attorneys.FirstOrDefaultAsync (x => x.AttorneyUserId == userId);
                conditionalResults = x => x.AssignedToAttorneyId == attorney.Id;
            }

            var total = await _context.Users.CountAsync ();
            var list = await _context.Users
                .Include (x => x.AssignedToAttorney)
                .Where (x => conditionalResults (x))
                .Select (x => new CreateClientViewModel {
                    //RegistrationCode = x.Code,
                    Surname = x.Surname,
                    Name = x.Name,
                    Email = x.UserName,
                    Id = x.Id,
                    CreatedAt = x.DateOfAccident,
                    AssignedToAttorney = x.AssignedToAttorneyId.HasValue ? (x.AssignedToAttorney.Name + " " + x.AssignedToAttorney.Surname) : "Not Assigned",
                    ImageURL = AmazonS3Helper.GenerateUrl (x.ProfilePictureKey, "attorney-journal-dev")
                })
                .OrderByDescending (x => x.CreatedAt)
                // .Skip (page * itemPerPage)
                // .Take (itemPerPage)
                .ToListAsync ();

            return View (new TablePagination<CreateClientViewModel> {
                Total = total,
                Lists = list
            });
        }

        /// <summary>
        /// 
        /// </summary>
        public class PushNotificationModel {
            public string UserId { get; set; }
            public string Message { get; set; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [ApiExplorerSettings (IgnoreApi = true)]
        [Route ("Customer/SendPushNotification")]
        public async Task<IActionResult> SendPushNotification (PushNotificationModel model) {
            var user = await _userManager.FindByIdAsync (model.UserId);
            if (user == null) return Json (new { result = false });

            NotificationCreateResult pushResult;

            try {
                pushResult = PushNotificationHelper.SendPushOneSignal (user.Email.ToLowerInvariant (), model.Message); // .SendAPNSNotification(model.DeviceToken, model.Message);

                _context.Files.Add (new FileStorage {
                    Content = $"{model.Message}",
                    Type = FileStorageType.PushNotification,
                    CreatedAt = DateTime.UtcNow,
                    MimeType = "text/plain",
                    Title = "Push Message",
                    Owner = user,
                    Viewed = true
                });
                await _context.SaveChangesAsync ();
            } catch (Exception e) {
                return Json (new { exception = e.Message, result = false });
            }

            return Json (new { result = pushResult.Recipients > 0 });
        }

        public static async Task<UserTimelineModel> GetTimeLine (ApplicationDbContext context, string userId, List<Guid> listIds = null, bool showDummy=false) {
            var userDetail = await context.Users.FirstOrDefaultAsync (x => x.Id == userId);
            if (userDetail == null) return null;

            var userTimeline = new UserTimelineModel ();
            IQueryable<FileStorage> files = context.Files;
            if (listIds != null && listIds.Any ()) files = context.Files.Where (x => listIds.Contains (x.Id));

            userTimeline.Name = userDetail.Name;
            userTimeline.Surname = userDetail.Surname;
            userTimeline.UserId = userDetail.Id;
            userTimeline.DateOfAccident = userDetail.DateOfAccident.GetValueOrDefault (DateTime.UtcNow);
            userTimeline.PhotoURL = AmazonS3Helper.GenerateUrl (userDetail.ProfilePictureKey, "attorney-journal-dev");

            userTimeline.Items = await files.Where (x => x.Owner.Id == userId && x.Parent == null).Select (
                x => new TimelineItemModel {
                    CreatedAt = x.CreatedAt,
                    File = new ItemModel {
                        Id = x.Id,
                        Title = x.Title,
                        Content = x.Content,
                        FileExtension = x.FileExtension,
                        MimeType = x.MimeType,
                        ObjectKey = x.AmazonObjectKey,
                        ObjectUrl = AmazonS3Helper.GenerateUrl (x.AmazonObjectKey, "attorney-journal-dev"),
                        Type = x.Type,
                        Viewed = x.Viewed,
                        DateTaken = x.DateTaken
                    },
                    Thumb = context.Files.Where (y => y.Parent == x).Select (y => new ItemModel {
                        FileExtension = y.FileExtension,
                        MimeType = y.MimeType,
                        ObjectKey = y.AmazonObjectKey,
                        ObjectUrl = GetImageAsBase64Url(AmazonS3Helper.GenerateUrl(y.AmazonObjectKey, "attorney-journal-dev")),
                        DateTaken = x.DateTaken
                    }).FirstOrDefault ()
                }).OrderByDescending (x => x.CreatedAt).ToListAsync (CancellationToken.None);

            if (showDummy) { 
                userTimeline.Items.Add(new TimelineItemModel
                {
                    CreatedAt = DateTime.Now,
                    File = new ItemModel
                    {
                        Id = Guid.NewGuid(),
                        Title = "test image",
                        Content = "this is test image",
                        Type = FileStorageType.Image,
                        FileExtension = "jpg",
                        MimeType = "img/jpg",
                        ObjectUrl = "https://appwrk.com/wp-content/themes/appwrk_theme/images/testi_1.jpg",
                        Viewed = true,
                        DateTaken = DateTime.Now
                    },
                    Thumb = new ItemModel
                    {
                        FileExtension = "jpg",
                        MimeType = "img/jpg",
                        ObjectUrl = GetImageAsBase64Url("https://appwrk.com/wp-content/themes/appwrk_theme/images/testi_1.jpg"),
                        DateTaken = DateTime.Now
                    }
                });

                userTimeline.Items.Add(new TimelineItemModel
                {
                    CreatedAt = DateTime.Now,
                    File = new ItemModel
                    {
                        Id = Guid.NewGuid(),
                        Title = "test video",
                        Content = "this is test video",
                        Type = FileStorageType.Video,
                        ObjectUrl = "https://www.youtube.com/watch?v=e_04ZrNroTo",
                        Viewed = true,
                        DateTaken = DateTime.Now
                    },
                    Thumb = new ItemModel
                    {
                        ObjectUrl = GetImageAsBase64Url("http://i3.ytimg.com/vi/e_04ZrNroTo/hqdefault.jpg"),
                        DateTaken = DateTime.Now
                    }
                });

            }

            return userTimeline;
        }

        #region Encode Images

        public static string GetImageAsBase64Url(string imageUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(imageUrl)) return null;
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(imageUrl);

                WebResponse webResponse =(webRequest.GetResponseAsync()).Result;

                System.IO.Stream stream = webResponse.GetResponseStream();
                return "data:image/jpeg;base64," + Convert.ToBase64String(ReadFully(stream));
            }
            catch (Exception)
            {
                return imageUrl;
            }
            
        }

        public static byte[] ReadFully(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }
       
        #endregion

        [HttpGet]
        [ApiExplorerSettings (IgnoreApi = true)]
        [Route ("Customer/ViewTimeline/{id?}")]
        public async Task<IActionResult> ViewTimeline (string id,bool showDummy=false) {
            var model = await GetTimeLine (_context, id,showDummy:showDummy);
            if (model == null) return NotFound ();
            return View (model);
        }

        public class RequestTimelinePayload {
            public string Id { get; set; }
            public List<Guid> ListIds { get; set; } = null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        [HttpPost]
        [ApiExplorerSettings (IgnoreApi = true)]
        [Route ("Customer/DownloadTimeline")]
        public async Task<IActionResult> DownloadTimeline (RequestTimelinePayload payload) {
            using (var amazonClient = new AmazonS3Helper (_amazonS3)) {
                var timeline = await GetTimeLine (_context, payload.Id, payload.ListIds);
                if (timeline == null) return NotFound ();
                return Json (new { url = await amazonClient.PrepareZip (timeline) });
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet]
        [ApiExplorerSettings (IgnoreApi = true)]
        [Route ("Customer/MarkContentAsViewed/{id}/{userId}")]
        public async Task<IActionResult> MarkContentAsViewed (Guid id, string userId) {
            var found = await _context.Files.FindAsync (id);
            found.Viewed = !found.Viewed;
            await _context.SaveChangesAsync ();

            return RedirectToAction ("ViewTimeline", new { id = userId });
        }

        [HttpGet]
        [ApiExplorerSettings (IgnoreApi = true)]
        [Route ("Customer/RegistrationCode/")]
        public IActionResult RegistrationCode () {
            return View (new CreateClientViewModel ());
        }

        [HttpGet]
        [ApiExplorerSettings (IgnoreApi = true)]
        [Route ("Customer/AssignCustomerToMe/")]
        public IActionResult AssignCustomerToMe () {
            return View ();
        }

        [HttpPost]
        [Route ("Customer/AssignCustomerToMe/")]
        public async Task<IActionResult> AssignCustomerToMe (AssignToCustomerViewModel model) {
            if (!ModelState.IsValid) return View (model);

            var user = await _userManager.GetUserAsync (User);
            if (user == null) return Unauthorized ();
            var userId = new Guid (user.Id);

            var attorney = await _context.Attorneys.FirstOrDefaultAsync (x => x.AttorneyUserId == userId);
            if (attorney == null) return Unauthorized ();

            var userToAssign = await _userManager.FindByEmailAsync (model.Email);
            if (userToAssign == null) return NotFound ("User not found");
            if (userToAssign.AssignedToAttorneyId.HasValue) return BadRequest ("User is assigned to another Attorney");

            userToAssign.AssignedToAttorney = attorney;
            userToAssign.AssignedToAttorneyId = attorney.Id;

            await _context.SaveChangesAsync ();

            return RedirectToAction ("Index", "Customer");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        //[HttpPost]
        //[ApiExplorerSettings(IgnoreApi = true)]
        //public async Task<IActionResult> AddRegistrationCode(CreateClientViewModel model)
        //{
        //	if (ModelState.IsValid)
        //	{
        //		var user = await _userManager.GetUserAsync(User);
        //		if (await _context.RegistrationCodes.AnyAsync(x => x.Mail.ToLower() == model.Email.ToLower()))
        //		{
        //			ModelState.AddModelError(nameof(model.Email), "Duplicated Email!");
        //			return View(nameof(RegistrationCode), model);
        //		}
        //		var attorney = await _context.Attorneys.FirstOrDefaultAsync(x => x.AttorneyUserId == new Guid(user.Id));
        //		_context.RegistrationCodes.Add(new RegistrationCodeEntity
        //		{
        //			Attorney = attorney,
        //			Code = model.RegistrationCode,
        //			CreatedAt = DateTime.UtcNow,
        //			HasBeenUsed = false,
        //			Name = model.Name,
        //			Surname = model.Surname,
        //			Mail = model.Email
        //		});

        //		//TODO: update store link.
        //		const string storeLink = "https://itunes.apple.com/us/app/evidence-recorder/id1250598244?ls=1&mt=8";

        //		await _context.SaveChangesAsync();
        //		await _emailSender.SendEmailAsync(
        //			$"{attorney.Name} {attorney.Surname}",
        //			model.Email,
        //			$"{attorney.Name} {attorney.Surname} has invited you to Visual Evidence Recorder",
        //			$"{attorney.Name} {attorney.Surname} has invited you to use the Visual Evidence Recorder app.<br /><br />" +
        //			$"To begin, please download the Visual Recorder App on Apple's App Store by clicking this link: <a href=\"{storeLink}\" target=\"_blank_\">Visual Evidence Recorder App</a>." +
        //			" Once you have downloaded the app,<br /> please open it and select SIGN UP WITH YOUR PERSONAL CODE.<br /><br />" +
        //			$"Your Personal Code is: {model.RegistrationCode}", model.Name, model.Surname);
        //		return RedirectToAction("Index");
        //	}
        //	return View(nameof(RegistrationCode), model);
        //}
    }
}