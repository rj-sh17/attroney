using System;
using System.Collections.Async;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using AttorneyJournal.Models.Domain.Storage;
using AttorneyJournal.Models.ResourceModels;
using Microsoft.EntityFrameworkCore.ValueGeneration.Internal;

namespace AttorneyJournal.Helpers {
	/// <summary>
	/// 
	/// </summary>
	public class AmazonS3Helper : IDisposable {
		private readonly string _bucketName;
		private readonly TransferUtility _transferUtility;
		private readonly IAmazonS3 _amazonS3;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="amazonS3"></param>
		/// <param name="bucketName"></param>
		public AmazonS3Helper (IAmazonS3 amazonS3, string bucketName = "attorney-journal-dev") {
			_bucketName = bucketName;
			_amazonS3 = amazonS3;
			_transferUtility = new TransferUtility (_amazonS3);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="filePath"></param>
		/// <param name="key"></param>
		/// <param name="bucketName"></param>
		/// <param name="storageClass"></param>
		/// <returns></returns>
		public async Task UploadFileAsync (string filePath, string key, string bucketName = null, S3StorageClass storageClass = null) {
			var uploadRequest = new TransferUtilityUploadRequest {
				FilePath = filePath,
					BucketName = bucketName ?? _bucketName,
					Key = key,
					CannedACL = S3CannedACL.PublicRead,
					StorageClass = storageClass ?? S3StorageClass.Standard
			};
			await _transferUtility.UploadAsync (uploadRequest, CancellationToken.None);
			await _amazonS3.MakeObjectPublicAsync (bucketName ?? _bucketName, key, true);
		}

		public async Task CreateDirectory (string folderName) {
			//var result = await _amazonS3.ListObjectsAsync(new ListObjectsRequest {BucketName = _bucketName});

			var request = new PutObjectRequest {
				BucketName = _bucketName,
					StorageClass = S3StorageClass.Standard,
					ServerSideEncryptionMethod = ServerSideEncryptionMethod.None,
					Key = $"{folderName}/",
					ContentBody = string.Empty
			};

			var response = await _amazonS3.PutObjectAsync (request);
			//_amazonS3.CopyObjectAsync()
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="objectKey"></param>
		/// <param name="targetFilePath"></param>
		/// <param name="bucketName"></param>
		/// <returns></returns>
		public async Task<string> DownloadFileAsync (string objectKey, string targetFilePath, string bucketName = null) {
			var getObjRequest = new GetObjectRequest { BucketName = bucketName ?? _bucketName, Key = objectKey };
			using (var getObjectResponse = await _amazonS3.GetObjectAsync (getObjRequest))
			await getObjectResponse.WriteResponseStreamToFileAsync (targetFilePath, true, CancellationToken.None);
			return targetFilePath;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="timelineModel"></param>
		/// <returns></returns>
		public async Task<string> PrepareZip (UserTimelineModel timelineModel) {
			List<string> objectKeys = new List<string> ();
			timelineModel.Items = timelineModel.Items.OrderBy (x => x.CreatedAt).ToList ();

			#region Path
			const string exportBucketName = "ver-exports";
			var fileZipName = $"{timelineModel.Name.ToLowerInvariant()}_{timelineModel.Surname.ToLowerInvariant()}_export_{DateTime.UtcNow:yyyyMMddHHmmss}.zip";
			var tempPath = Path.Combine (Path.GetTempPath (), Guid.NewGuid ().ToString ());
			var fileZipPath = Path.Combine (Path.GetTempPath (), fileZipName);
			#endregion

			try {
				var files = new Dictionary<string, string> (objectKeys.Count);
				if (!Directory.Exists (tempPath)) Directory.CreateDirectory (tempPath);
				if (!Directory.Exists (Path.Combine (tempPath, "files"))) Directory.CreateDirectory (Path.Combine (tempPath, "files"));

				#region Config

				Directory.CreateDirectory (Path.Combine (tempPath, "config"));
				File.Copy ("wwwroot/template/export_template/config/bootstrap.css", Path.Combine (tempPath, "config", "bootstrap.css"));
				File.Copy ("wwwroot/template/export_template/config/bootstrap.js", Path.Combine (tempPath, "config", "bootstrap.js"));
				File.Copy ("wwwroot/template/export_template/config/jquery.js", Path.Combine (tempPath, "config", "jquery.js"));
				File.Copy ("wwwroot/template/export_template/config/site.css", Path.Combine (tempPath, "config", "site.css"));
				File.Copy ("wwwroot/template/export_template/config/site.js", Path.Combine (tempPath, "config", "site.js"));

				Directory.CreateDirectory (Path.Combine (tempPath, "config/fonts"));
				File.Copy ("wwwroot/template/export_template/config/fonts/glyphicons-halflings-regular.eot", Path.Combine (tempPath, "config/fonts", "glyphicons-halflings-regular.eot"));
				File.Copy ("wwwroot/template/export_template/config/fonts/glyphicons-halflings-regular.svg", Path.Combine (tempPath, "config/fonts", "glyphicons-halflings-regular.svg"));
				File.Copy ("wwwroot/template/export_template/config/fonts/glyphicons-halflings-regular.ttf", Path.Combine (tempPath, "config/fonts", "glyphicons-halflings-regular.ttf"));
				File.Copy ("wwwroot/template/export_template/config/fonts/glyphicons-halflings-regular.woff", Path.Combine (tempPath, "config/fonts", "glyphicons-halflings-regular.woff"));
				File.Copy ("wwwroot/template/export_template/config/fonts/glyphicons-halflings-regular.woff2", Path.Combine (tempPath, "config/fonts", "glyphicons-halflings-regular.woff2"));

				#endregion

				string templateToFill = File.ReadAllText ("wwwroot/template/export_template/openME.html");
				templateToFill = templateToFill.Replace ("*|PHOTO-FILENAME|*", timelineModel.PhotoURL);
				templateToFill = templateToFill.Replace ("*|NAME|*", timelineModel.Name);
				templateToFill = templateToFill.Replace ("*|SURNAME|*", timelineModel.Surname);
				templateToFill = templateToFill.Replace ("*|DATEACCIDENT|*", timelineModel.DateOfAccident.ToLocalTime ().ToString (CultureInfo.InvariantCulture));

				if (timelineModel.PhotoURL != null) {
					objectKeys.Add (timelineModel.PhotoURL);
				}

				string templateContents = string.Empty;

				foreach (var item in timelineModel.Items) {
					switch (item.File.Type) {
						case FileStorageType.Image:
							var imageTemplate = File.ReadAllText ("wwwroot/template/export_template/image.html");
							if (item.File.DateTaken.HasValue) {
								imageTemplate = imageTemplate.Replace ("*|DATE|*", "Created: " + item.File.DateTaken.Value.ToLocalTime ().ToString (CultureInfo.InvariantCulture) + " Uploaded: " + item.CreatedAt.ToLocalTime ().ToString (CultureInfo.InvariantCulture));
							} else {
								imageTemplate = imageTemplate.Replace ("*|DATE|*", "Uploaded: " + item.CreatedAt.ToLocalTime ().ToString (CultureInfo.InvariantCulture));
							}
							imageTemplate = imageTemplate.Replace ("*|FILENAME|*", item.File.ObjectKey);
							imageTemplate = imageTemplate.Replace ("*|TITLE|*", string.IsNullOrWhiteSpace (item.File.Title) ? item.File.Title : "A photo has been uploaded");
							templateContents += imageTemplate;

							objectKeys.Add (item.File.ObjectKey);

							break;

						case FileStorageType.Video:
							var videoTemplate = File.ReadAllText ("wwwroot/template/export_template/video.html");
							if (item.File.DateTaken.HasValue) {
								videoTemplate = videoTemplate.Replace ("*|DATE|*", "Created: " + item.File.DateTaken.Value.ToLocalTime ().ToString (CultureInfo.InvariantCulture) + " Uploaded: " + item.CreatedAt.ToLocalTime ().ToString (CultureInfo.InvariantCulture));
							} else {
								videoTemplate = videoTemplate.Replace ("*|DATE|*", "Uploaded: " + item.CreatedAt.ToLocalTime ().ToString (CultureInfo.InvariantCulture));
							}
							videoTemplate = videoTemplate.Replace ("*|FILENAME|*", item.File.ObjectKey);
							videoTemplate = videoTemplate.Replace ("*|THUMBNAME|*", item.Thumb.ObjectKey);
							videoTemplate = videoTemplate.Replace ("*|TITLE|*", string.IsNullOrWhiteSpace (item.File.Title) ? item.File.Title : "A video has been uploaded");
							templateContents += videoTemplate;

							objectKeys.Add (item.File.ObjectKey);
							objectKeys.Add (item.Thumb.ObjectKey);

							break;

						case FileStorageType.Text:
							var textTemplate = File.ReadAllText ("wwwroot/template/export_template/text.html");
							textTemplate = textTemplate.Replace ("*|TITLE|*", item.File.Title ?? "Text message");
							textTemplate = textTemplate.Replace ("*|DATE|*", item.CreatedAt.ToLocalTime ().ToString (CultureInfo.InvariantCulture));
							textTemplate = textTemplate.Replace ("*|CONTENT|*", item.File.Content);
							templateContents += textTemplate;

							break;

						case FileStorageType.PushNotification:
							var pushTemplate = File.ReadAllText ("wwwroot/template/export_template/push.html");
							pushTemplate = pushTemplate.Replace ("*|TITLE|*", item.File.Title ?? "Push Message");
							pushTemplate = pushTemplate.Replace ("*|DATE|*", item.CreatedAt.ToLocalTime ().ToString (CultureInfo.InvariantCulture));
							pushTemplate = pushTemplate.Replace ("*|CONTENT|*", item.File.Content);
							templateContents += pushTemplate;

							break;
						case FileStorageType.Thumb:
							break;
						default:
							throw new ArgumentOutOfRangeException ();
					}
				}

				templateToFill = templateToFill.Replace ("*|CONTENT|*", templateContents);
				File.WriteAllText (Path.Combine (tempPath, "openME.html"), templateToFill);

				await objectKeys.ParallelForEachAsync (async key => { files.Add (key, await DownloadFileAsync (key, Path.Combine (tempPath, "files", key))); }, 8);

				ZipFile.CreateFromDirectory (tempPath, fileZipPath);

				await UploadFileAsync (fileZipPath, fileZipName, exportBucketName, S3StorageClass.ReducedRedundancy);
			} catch (Exception e) {
				return $"Exception: {e.Message}";
			} finally {
				if (File.Exists (fileZipPath)) File.Delete (fileZipPath);
				if (Directory.Exists (tempPath)) Directory.Delete (tempPath, true);
			}

			return GenerateUrl (fileZipName, "ver-exports");
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="key"></param>
		/// <param name="bucketName"></param>
		/// <returns></returns>
		public static string GenerateUrl (string key, string bucketName = "attorney-journal-dev") {
			return string.IsNullOrWhiteSpace (key) ? null : $"https://s3-us-west-2.amazonaws.com/{bucketName}/{key}";
		}

		/// <summary>
		/// 
		/// </summary>
		public void Dispose () {
			_transferUtility?.Dispose ();
			GC.SuppressFinalize (this);
		}
	}
}