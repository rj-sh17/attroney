using System.IO;

namespace AttorneyJournal {
	using Microsoft.AspNetCore.Hosting;
	using Microsoft.Extensions.Configuration;
	using Microsoft.Extensions.Logging;

	public class Program {
		public static void Main (string[] args) {
			var configuration = new ConfigurationBuilder ()
				.AddEnvironmentVariables (prefix: "ASPNETCORE_")
				.AddEnvironmentVariables (prefix: "AMAZON_")
				//.AddCommandLine(args)
				.Build ();

			var host = new WebHostBuilder ()
				.ConfigureLogging (options => options.AddConsole ())
				.ConfigureLogging (options => options.AddDebug ())
				.UseConfiguration (configuration)
				.UseSetting ("detailedErrors", "true")
				.UseContentRoot (Directory.GetCurrentDirectory ())
				.UseIISIntegration()
				.UseKestrel ()
				.UseStartup<Startup> ()
				//.UseApplicationInsights() // Removed, not compatible with swagger.
				.Build ();

			host.Run ();
		}
	}
}