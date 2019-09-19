using System;
using System.Diagnostics;
using System.Threading.Tasks;
using AttorneyJournal.Models.ConfigurationModels;
using Microsoft.Extensions.Options;
using RestSharp;
using RestSharp.Authenticators;

namespace AttorneyJournal.Services
{
	// This class is used by the application to send Email and SMS
	// when you turn on two-factor authentication in ASP.NET Identity.
	// For more details see this link https://go.microsoft.com/fwlink/?LinkID=532713
	/// <summary>
	/// 
	/// </summary>
	public class AuthMessageSender : IEmailSender, ISmsSender
	{
		private readonly EmailSettings _emailSettings;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="emailSettings"></param>
		public AuthMessageSender(IOptions<EmailSettings> emailSettings)
		{
			_emailSettings = emailSettings.Value;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="email"></param>
		/// <param name="subject"></param>
		/// <param name="message"></param>
		/// <param name="name"></param>
		/// <param name="surname"></param>
		/// <returns></returns>
		public Task SendEmailAsync(string sign, string email, string subject, string message, string name = null, string surname = null)
		{
			return Task.Run(() => { SendMail(sign, subject, email, message, name, surname); });
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="number"></param>
		/// <param name="message"></param>
		/// <returns></returns>
		public Task SendSmsAsync(string number, string message)
		{
			// Plug in your SMS service here to send a text message.
			return Task.FromResult(0);
		}

		#region Mailgun

		/// <summary>
		/// 
		/// </summary>
		/// <param name="subject"></param>
		/// <param name="to"></param>
		/// <param name="body"></param>
		/// <param name="name"></param>
		/// <param name="surname"></param>
		private void SendMail(string sign, string subject, string to, string body, string name = null, string surname = null)
		{
			var template = System.IO.File.ReadAllText("wwwroot/template/generic.html");
			template = template.Replace("*|CONTENT|*", body);
			template = template.Replace("*|NAME|*", name ?? "Client");
			template = template.Replace("*|SURNAME|*", surname ?? string.Empty);
			template = template.Replace("*|ATTORNEY_SIGN|*", sign ?? "Visual Evidence Recorder TEAM");

			var client = new RestClient
			{
				BaseUrl = new Uri(_emailSettings.BaseUri),
				Authenticator = new HttpBasicAuthenticator("api", _emailSettings.ApiKey)
			};
			var request = new RestRequest {Resource = "{domain}/messages"};

			request.AddParameter("domain", "visualevidencerecorder.com", ParameterType.UrlSegment);
			request.AddParameter("from", $"Visual  Evidence Recorder <{_emailSettings.From}>");
			request.AddParameter("to", $"{name} {surname} <{to}>");
			request.AddParameter("subject", subject);
			request.AddParameter("html", template);
			request.Method = Method.POST;
			client.ExecuteAsync(request, r => { Debug.WriteLine($"Send results: {r.StatusCode}"); });
		}

		#endregion

	}
}
