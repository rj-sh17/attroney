using System;
using System.Collections.Generic;
using OneSignal.CSharp.SDK.Core.Resources.Notifications;

namespace AttorneyJournal.Helpers
{
	public static class PushNotificationHelper
	{
		private const string ApiKey = "MDM2M2VlYzEtOWMwOS00YTkzLThhODQtNmRkMzg0M2FjZTM0";
		private static readonly Guid AppId = new Guid("7917ae5f-574e-4840-be55-6239e795e82f");

		public static NotificationCreateResult SendPushOneSignal(string mail, string message)
		{
			var resource = new NotificationsResource(ApiKey, "https://onesignal.com/api/v1");
			var result = resource.Create(new NotificationCreateOptions
			{
				AppId = AppId,
				IncludedSegments = new List<string> { "All" },
				Filters = new List<object> { new { field = "tag", key = "email", relation = "=", value = mail } },
				Contents = new Dictionary<string, string> { { "en", message } },
				Headings = new Dictionary<string, string> { { "en", "Message from Evidence Recorder App" } }
			});
			return result;
		}
	}
}