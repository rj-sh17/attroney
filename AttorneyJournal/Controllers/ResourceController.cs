using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using AspNet.Security.OAuth.Validation;
using AttorneyJournal.Data;
using AttorneyJournal.Helpers;
using AttorneyJournal.Models;
using AttorneyJournal.Models.Domain.Storage;
using AttorneyJournal.Models.ResourceModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using OneSignal.CSharp.SDK.Core.Resources.Devices;

namespace AttorneyJournal.Controllers {
	[Route ("api")]
	public class ResourceController : Controller {
		//public IActionResult Index()
		//{
		//	return View();
		//}

		private static readonly FormOptions DefaultFormOptions = new FormOptions ();
		private readonly ILogger<ResourceController> _logger;
		private readonly UserManager<ApplicationUser> _userManager;

		private readonly IAmazonS3 _amazonS3;
		private readonly ApplicationDbContext _context;

		public ResourceController (
			UserManager<ApplicationUser> userManager,
			ILogger<ResourceController> logger,
			IAmazonS3 s3Client,
			ApplicationDbContext context) {
			_userManager = userManager;
			_logger = logger;
			_amazonS3 = s3Client;
			_context = context;
		}

		[ApiExplorerSettings (IgnoreApi = true)]
		[Authorize (ActiveAuthenticationSchemes = OAuthValidationDefaults.AuthenticationScheme)]
		[HttpGet ("message")]
		public async Task<IActionResult> GetMessage () {
			var user = await _userManager.GetUserAsync (User);
			if (user == null) {
				return BadRequest ();
			}

			return Content ($"{user.UserName} has been successfully authenticated.");
		}

		#region Upload

		/// <summary>
		/// Headers:
		/// "unique-upload-id": Generate a GUID in this form 4A569ACC-FF6B-4FE0-8EAA-7F97774E2E3D
		/// "upload-type": video | image | thumb
		/// </summary>
		/// <returns></returns>
		[HttpPost ("upload")]
		[DisableFormValueModelBinding]
		[Authorize (ActiveAuthenticationSchemes = OAuthValidationDefaults.AuthenticationScheme)]
		public async Task<IActionResult> Upload () {
			//StringValues UniqueUploadId = default(StringValues);
			//StringValues UploadType = default(StringValues);

			//Request.Headers.TryGetValue("unique-upload-id", out UniqueUploadId);
			//Request.Headers.TryGetValue("upload-type", out UploadType);
			Request.Headers.TryGetValue ("X-Upload-Title", out StringValues uploadTitle);
			Request.Headers.TryGetValue ("X-Date-Taken", out StringValues dateTaken);

			var user = await _userManager.GetUserAsync (User);
			if (user == null) return BadRequest ();

			if (!MultipartRequestHelper.IsMultipartContentType (Request.ContentType))
				return BadRequest ($"Expected a multipart request, but got {Request.ContentType}");

			// Used to accumulate all the form url encoded key value pairs in the 
			// request.
			var formAccumulator = new KeyValueAccumulator ();

			var objectKey = Guid.NewGuid ().ToString ();
			var thumbObjectKey = $"{Guid.NewGuid()}.jpg";
			FileStorage parent = null;

			var boundary = MultipartRequestHelper.GetBoundary (MediaTypeHeaderValue.Parse (Request.ContentType), DefaultFormOptions.MultipartBoundaryLengthLimit);
			var reader = new MultipartReader (boundary, HttpContext.Request.Body);

			var section = await reader.ReadNextSectionAsync ();
			while (section != null) {
				var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse (section.ContentDisposition, out ContentDispositionHeaderValue contentDisposition);

				if (hasContentDispositionHeader) {
					if (MultipartRequestHelper.HasFileContentDisposition (contentDisposition)) {
						var targetFilePath = Path.GetTempFileName ();
						using (var targetStream = System.IO.File.Create (targetFilePath)) {
							await section.Body.CopyToAsync (targetStream);
							_logger.LogInformation ($"Copied the uploaded file '{targetFilePath}'");
						}

						var partName = contentDisposition.Name;
						var strippedFileName = contentDisposition.FileName.Replace ("\"", string.Empty);
						var mimeType = MimeTypesHelper.GetMimeType (strippedFileName);
						var fileExtension = $".{strippedFileName.Split('.').LastOrDefault()}";
						var originalFileName = strippedFileName.Replace (fileExtension, string.Empty);

						var type = FileStorageType.Thumb;
						var isFile = partName.Replace ("\"", string.Empty) == "file";

						if (mimeType.Contains ("image") && isFile) type = FileStorageType.Image;
						else if (mimeType.Contains ("video") && isFile) type = FileStorageType.Video;

						if (isFile) objectKey = $"{objectKey}{fileExtension}";

						try {
							using (var amazonClient = new AmazonS3Helper (_amazonS3, "attorney-journal-dev")) {
								await amazonClient.UploadFileAsync (targetFilePath, isFile ? objectKey : thumbObjectKey);
							}

							var date = new DateTime ();
							try {
								date = DateTime.Parse (dateTaken);
							} catch (System.Exception) { }

							var newFile = new FileStorage {
								Parent = parent,
									AmazonObjectKey = isFile ? objectKey : thumbObjectKey,
									CreatedAt = DateTime.UtcNow,
									FileExtension = fileExtension,
									OriginalName = originalFileName,
									MimeType = mimeType,
									Owner = user,
									Type = type,
									Title = uploadTitle.FirstOrDefault () ?? string.Empty,
									DateTaken = date
							};

							_context.Files.Add (newFile);

							parent = isFile ? newFile : null;
						} catch (AmazonS3Exception s3Exception) {
							Console.WriteLine (s3Exception.Message, s3Exception.InnerException);
						}
					}
				}

				// Drains any remaining section body that has not been consumed and
				// reads the headers for the next section.
				section = await reader.ReadNextSectionAsync ();
			}

			// Bind form data to a model
			var formValueProvider = new FormValueProvider (BindingSource.Form, new FormCollection (formAccumulator.GetResults ()), CultureInfo.CurrentCulture);

			var bindingSuccessful = await TryUpdateModelAsync (new { }, string.Empty, formValueProvider);
			if (!bindingSuccessful || !ModelState.IsValid) return BadRequest (ModelState);

			await _context.SaveChangesAsync (CancellationToken.None);
			return Json (new { fileUrl = AmazonS3Helper.GenerateUrl (objectKey), thumbUrl = AmazonS3Helper.GenerateUrl (thumbObjectKey), result = true });

		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		[HttpPost ("addText")]
		[Authorize (ActiveAuthenticationSchemes = OAuthValidationDefaults.AuthenticationScheme)]
		public async Task<IActionResult> AddTextualContext (string text) {
			try {
				var user = await _userManager.GetUserAsync (User);
				if (user == null) return Unauthorized ();

				_context.Files.Add (new FileStorage {
					Content = text,
						Type = FileStorageType.Text,
						CreatedAt = DateTime.UtcNow,
						Owner = user,
						MimeType = "text/plain"
				});

				await _context.SaveChangesAsync ();
			} catch (Exception e) {
				Response.StatusCode = (int) HttpStatusCode.Unauthorized;
				return Json (new { Exception = e.Message, Result = false });
			}

			return Json (new { result = true });
		}

		#endregion

		#region Account

		[HttpGet ("userinfo"), Produces ("application/json")]
		[Authorize (ActiveAuthenticationSchemes = OAuthValidationDefaults.AuthenticationScheme)]
		public async Task<IActionResult> GetUserInfo () {
			var user = await _userManager.GetUserAsync (User);
			if (user == null) return Unauthorized ();
			var userDetail = await _context.Users.FirstOrDefaultAsync (x => x.Id == user.Id);

			return Json (new {
				userDetail?.Name,
					userDetail?.Surname,
					Email = user.Email.ToLowerInvariant (),
					user.DateOfAccident,
					ProfilePicture = AmazonS3Helper.GenerateUrl (user.ProfilePictureKey),
					IsAssigned = user.AssignedToAttorneyId.HasValue
			});
		}

		/// <summary>
		/// Needed to register device token.
		/// </summary>
		/// <param name="token"></param>
		/// <returns></returns>
		//[HttpPost("registerDeviceToken"), Produces("application/json")]
		//[Authorize(ActiveAuthenticationSchemes = OAuthValidationDefaults.AuthenticationScheme)]
		//public async Task<IActionResult> SetDeviceNotificationToken(string token)
		//{
		//	try
		//	{
		//		var user = await _userManager.GetUserAsync(User);
		//		if (user == null) return Unauthorized();
		//		var userDetail = await _context.RegistrationCodes.FirstOrDefaultAsync(x => x.UserId == user.Id);

		//		userDetail.NotificationToken = token;
		//		userDetail.LastUpdateNotificationToken = DateTime.UtcNow;

		//		var client = new OneSignal.CSharp.SDK.Core.OneSignalClient("NDQ1OTcxMjUtYjc1ZS00NzRhLTkyOTctNTY0NDRkZjg0Yjdk");
		//		client.Devices.Add(new DeviceAddOptions
		//		{
		//			AppId = Guid.Parse("c5c4e64b-0c5f-4fb3-9dcc-1dfc8b54dac8"),
		//			DeviceType = DeviceTypeEnum.iOS,
		//			Language = "en",
		//			Timezone = null,
		//			DeviceModel = "",
		//			DeviceOS = "",
		//			AdId = "", //identifierForVendor,
		//			TestType = TestTypeEnum.Development,

		//		});

		//		await _context.SaveChangesAsync();
		//	}
		//	catch (Exception e)
		//	{
		//		return Json(new { exception = e.Message, result = false });
		//	}

		//	return Json(new { result = true });
		//}

		#endregion

		#region Stream Secure File

		/// <summary>
		/// 
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		[HttpGet ("streamFile")]
		[Authorize (ActiveAuthenticationSchemes = OAuthValidationDefaults.AuthenticationScheme)]
		[Obsolete ("Non pi� usato...")]
		public async Task<FileResult> StreamFileFromS3 (string key = "9ad43609-8656-4b72-958e-5018472f2548") {
			var user = await _userManager.GetUserAsync (User);
			var file = await _context.Files.FirstOrDefaultAsync (x => x.AmazonObjectKey == key && x.Owner.Id == user.Id);

			if (file == null) {
				HttpContext.Response.StatusCode = (int) HttpStatusCode.Forbidden;
				return null;
			}

			//byte[] buffer = new byte[4096];
			//var getObjRequest = new GetObjectRequest { BucketName = "attorney-journal-dev", Key = key };

			//using (GetObjectResponse getObjRespone = await _amazonS3.GetObjectAsync(getObjRequest))

			//using (var amazonStream = (Stream)getObjRespone.ResponseStream)
			//{
			//	int bytesReaded = 0;
			//	HttpContext.Response.ContentLength = getObjRespone.ContentLength;

			//	while ((bytesReaded = amazonStream.Read(buffer, 0, buffer.Length)) > 0)
			//	{
			//		HttpContext.Response.ContentType = file.MimeType;
			//		HttpContext.Response
			//		Response.OutputStream.Write(buffer, 0, bytesReaded);
			//		Response.OutputStream.Flush();
			//		buffer = new byte[BUFFER_SIZE];
			//	}
			//}

			//var options = new Dictionary<string, object>();
			//var stream = await _amazonS3 .GetObjectStreamAsync("attorney-journal-dev", key, options);

			var tempFile = Path.GetTempFileName ();
			var getObjRequest = new GetObjectRequest { BucketName = "attorney-journal-dev", Key = key };

			using (var getObjectResponse = await _amazonS3.GetObjectAsync (getObjRequest)) {
				await getObjectResponse.WriteResponseStreamToFileAsync (tempFile, true, CancellationToken.None);
				HttpContext.Response.ContentType = file.MimeType;
				var result = new FileContentResult (System.IO.File.ReadAllBytes (tempFile), file.MimeType) {
					FileDownloadName = $"{file.OriginalName}.{file.FileExtension}"
				};
				return result;
			}
		}

		/// <summary>
		/// Get User line
		/// </summary>
		/// <returns></returns>
		[HttpGet ("getUserTimeline")]
		[Authorize (ActiveAuthenticationSchemes = OAuthValidationDefaults.AuthenticationScheme)]
		public async Task<UserTimelineModel> GetUserTimeline () {
			var user = await _userManager.GetUserAsync (User);
			if (user != null) {

				//var userTimeline =  new UserTimelineModel
				//{
				//	Items = await _context.Files.Where(x => x.Owner.Id == user.Id && x.Parent == null).Select(
				//		x => new TimelineItemModel
				//		{
				//			CreatedAt = x.CreatedAt,
				//			File = new ItemModel
				//			{
				//				Title = x.Title,
				//				Content = x.Content,
				//				FileExtension = x.FileExtension,
				//				MimeType = x.MimeType,
				//				ObjectKey = x.AmazonObjectKey,
				//				ObjectUrl = AmazonS3Helper.GenerateUrl(x.AmazonObjectKey, "attorney-journal-dev")
				//			},
				//			Thumb = _context.Files.Where(y => y.Parent == x).Select(y => new ItemModel
				//			{
				//				FileExtension = y.FileExtension,
				//				MimeType = y.MimeType,
				//				ObjectKey = y.AmazonObjectKey,
				//				ObjectUrl = AmazonS3Helper.GenerateUrl(y.AmazonObjectKey, "attorney-journal-dev")
				//			}).FirstOrDefault()
				//		}).OrderByDescending(x => x.CreatedAt).ToListAsync(CancellationToken.None)
				//};

				return await CustomerController.GetTimeLine (_context, user.Id);
			}

			Response.StatusCode = (int) HttpStatusCode.Unauthorized;
			return null;
		}

		//private void AddErrors(IdentityResult result)
		//{
		//	foreach (var error in result.Errors)
		//	{
		//		ModelState.AddModelError(string.Empty, error.Description);
		//	}
		//}

		#endregion

	}

}