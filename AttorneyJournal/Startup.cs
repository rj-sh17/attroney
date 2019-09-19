using System.Security.Claims;
using System.Text;
using Amazon.S3;
using AspNet.Security.OpenIdConnect.Primitives;
using AttorneyJournal.Models.ConfigurationModels;
using AttorneyJournal.Models.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace AttorneyJournal
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Data;
    using Models;
    using Services;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Identity;
    using System;
    using System.Threading;
    using OpenIddict.Core;
    using OpenIddict.Models;

    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            if (env.IsDevelopment())
            {
                // For more details on using the user secret store see https://go.microsoft.com/fwlink/?LinkID=532709
                builder.AddUserSecrets<Startup>();
            }

            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public static IConfigurationRoot Configuration { get; private set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<RenderViewService>();

            // Add framework services.
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                // Configure the context to use Microsoft SQL Server.
                //options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"));
                options.UseMySql(@"Server=remotemysql.com;database=c5kNUJoA5x;uid=c5kNUJoA5x;pwd=w4No4w1EWT;Charset=utf8;", builder => { builder.MigrationsHistoryTable("__migrations"); });

                //.ReplaceService<SqlServerHistoryRepository, MyHistoryRepository>();
                // Register the entity sets needed by OpenIddict.
                // Note: use the generic overload if you need
                // to replace the default OpenIddict entities.
                options.UseOpenIddict();
            });

            //services.AddDbContext<StorageDbContext>(options =>
            //{
            //	options.UseSqlServer(Configuration.GetConnectionString("StorageConnection"));
            //});

            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
                {
                    options.Password.RequireDigit = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequiredLength = 6;

                    options.User.RequireUniqueEmail = true;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.AddAntiforgery(options => options.HeaderName = "X-XSRF-TOKEN");

            services.Configure<FormOptions>(x =>
            {
                x.ValueLengthLimit = int.MaxValue;
                x.MultipartBodyLengthLimit = int.MaxValue;
                x.MultipartHeadersLengthLimit = int.MaxValue;
            });

            services.AddMvc();

            #region Amazon S3
            Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", "AKIAI4JMKE3SYJ3M25JA");
            Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", "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");
            Environment.SetEnvironmentVariable("AWS_REGION", "us-west-2");

            services.AddDefaultAWSOptions(Configuration.GetAWSOptions());
            services.AddAWSService<IAmazonS3>();

            #endregion

            // Configure Identity to use the same JWT claims as OpenIddict instead
            // of the legacy WS-Federation claims it uses by default (ClaimTypes),
            // which saves you from doing the mapping in your authorization controller.
            services.Configure<IdentityOptions>(options =>
            {
                options.ClaimsIdentity.UserNameClaimType = OpenIdConnectConstants.Claims.Name;
                options.ClaimsIdentity.UserIdClaimType = OpenIdConnectConstants.Claims.Subject;
                options.ClaimsIdentity.RoleClaimType = OpenIdConnectConstants.Claims.Role;
            });

            // Register the OpenIddict services.
            // Note: use the generic overload if you need
            // to replace the default OpenIddict entities.
            services.AddOpenIddict(options =>
            {
                // Register the Entity Framework stores.
                options.AddEntityFrameworkCoreStores<ApplicationDbContext>();

                // Register the ASP.NET Core MVC binder used by OpenIddict.
                // Note: if you don't call this method, you won't be able to
                // bind OpenIdConnectRequest or OpenIdConnectResponse parameters.
                options.AddMvcBinders();

                // Enable the authorization, logout, token and userinfo endpoints.
                options.EnableAuthorizationEndpoint("/connect/authorize")
                       .EnableLogoutEndpoint("/connect/logout")
                       .EnableTokenEndpoint("/connect/token")
                       .EnableUserinfoEndpoint("/api/userinfo");

                // Note: the Mvc.Client sample only uses the code flow and the password flow, but you
                // can enable the other flows if you need to support implicit or client credentials.
                options.AllowAuthorizationCodeFlow()
                       .AllowPasswordFlow()
                       .AllowRefreshTokenFlow();

                // Make the "client_id" parameter mandatory when sending a token request.
                options.RequireClientIdentification();

                // When request caching is enabled, authorization and logout requests
                // are stored in the distributed cache by OpenIddict and the user agent
                // is redirected to the same page with a single parameter (request_id).
                // This allows flowing large OpenID Connect requests even when using
                // an external authentication provider like Google, Facebook or Twitter.
                options.EnableRequestCaching();

                // During development, you can disable the HTTPS requirement.
                options.DisableHttpsRequirement();

#if DEBUG
                options.SetAccessTokenLifetime(TimeSpan.FromMinutes(30));
#else
				options.SetAccessTokenLifetime(TimeSpan.FromHours(8));
#endif
                // Note: to use JWT access tokens instead of the default
                // encrypted format, the following lines are required:
                //
                //options.UseJsonWebTokens();
                //options.AddEphemeralSigningKey();
            });

            // Add application services.
            services.AddTransient<IEmailSender, AuthMessageSender>();
            services.AddTransient<ISmsSender, AuthMessageSender>();

            services.Configure<EmailSettings>(Configuration.GetSection("EmailSettings"));
            services.AddScoped<IHttpContextAccessor, HttpContextAccessor>();

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        }

        //FormatXmlCommentProperties.cs
        //public class FormatXmlCommentProperties : IOperationFilter
        //{
        //    private string Formatted(string text)
        //    {
        //        if (text == null) return null;
        //        var stringBuilder = new StringBuilder(text);

        //        return stringBuilder
        //            .Replace("<para>", "<p>")
        //            .Replace("</para>", "</p>")
        //            .ToString();
        //    }

        //    public void Apply(Operation operation, OperationFilterContext context)
        //    {
        //        operation.Description = Formatted(operation.Description);
        //        operation.Summary = Formatted(operation.Summary);
        //    }
        //}

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, RoleManager<IdentityRole> roleManager)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseWhen(context => context.Request.Path.StartsWithSegments("/api"), branch =>
            {
                // Add a middleware used to validate access
                // tokens and protect the API endpoints.
                branch.UseOAuthValidation(options => {
                    options.SaveToken = true;
                    options.AutomaticAuthenticate = true;
                });

                // If you prefer using JWT, don't forget to disable the automatic
                // JWT -> WS-Federation claims mapping used by the JWT middleware:
                //
                // JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
                // JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.Clear();
                //
                //branch.UseJwtBearerAuthentication(new JwtBearerOptions
                //{
                //	Authority = "http://localhost:54540/",
                //	Audience = "resource_server",
                //	RequireHttpsMetadata = false,
                //	TokenValidationParameters = new TokenValidationParameters
                //	{
                //		NameClaimType = OpenIdConnectConstants.Claims.Subject,
                //		RoleClaimType = OpenIdConnectConstants.Claims.Role
                //	}
                //});

                // Alternatively, you can also use the introspection middleware.
                // Using it is recommended if your resource server is in a
                // different application/separated from the authorization server.
                //
                //branch.UseOAuthIntrospection(options =>
                //{
                //	options.Authority = new Uri("http://localhost:54540/");
                //	options.Audiences.Add("resource_server");
                //	options.ClientId = "resource_server";
                //	options.ClientSecret = "875sqd4s5d748z78z7ds1ff8zz8814ff88ed8ea4z4zzd";
                //	options.RequireHttpsMetadata = false;
                //});
            });

            app.UseWhen(context => !context.Request.Path.StartsWithSegments("/api"), branch =>
            {
                branch.UseStatusCodePagesWithReExecute("/error");

                //branch.UseGoogleAuthentication(new GoogleOptions
                //{
                //	ClientId = "560027070069-37ldt4kfuohhu3m495hk2j4pjp92d382.apps.googleusercontent.com",
                //	ClientSecret = "n2Q-GEw9RQjzcRbU3qhfTj8f"
                //});

                //branch.UseTwitterAuthentication(new TwitterOptions
                //{
                //	ConsumerKey = "6XaCTaLbMqfj6ww3zvZ5g",
                //	ConsumerSecret = "Il2eFzGIrYhz6BWjYhVXBPQSfZuS4xoHpSSyD9PI"
                //});
            });

            app.UseIdentity();
            app.UseOAuthValidation();
            app.UseOpenIddict();

            InitializeRoles(roleManager).GetAwaiter().GetResult();

            // Add external authentication middleware below. To configure them please see https://go.microsoft.com/fwlink/?LinkID=532715

            app.UseMvcWithDefaultRoute();


            InitializeAsync(app.ApplicationServices, CancellationToken.None).GetAwaiter().GetResult();
        }

        #region Roles

        private readonly string[] _roles = { CommonConstant.AdministratorRole, CommonConstant.AttorneyRole, CommonConstant.UserRole };

        private async Task InitializeRoles(RoleManager<IdentityRole> roleManager)
        {
            foreach (var role in _roles)
            {
                if (await roleManager.RoleExistsAsync(role)) continue;
                var newRole = new IdentityRole(role);
                await roleManager.CreateAsync(newRole);
                await roleManager.AddClaimAsync(newRole, new Claim(ClaimTypes.Role, role));
            }
        }

        #endregion

        private async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken)
        {
            // Create a new service scope to ensure the database context is correctly disposed when this methods returns.
            using (var scope = services.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                //await context.Database.EnsureCreatedAsync(CancellationToken.None);
                //await context.Database.MigrateAsync(CancellationToken.None);

                //#region Seed 

                //var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                //#region SuperAdmin

                //var superAdmin = await userManager.FindByEmailAsync(CommonConstant.SuperAdminUserMail);
                //if (superAdmin == null)
                //{
                //	superAdmin = new ApplicationUser { UserName = CommonConstant.SuperAdminUserMail, Email = CommonConstant.SuperAdminUserMail, AssignedToAttorneyId = null };
                //	await userManager.CreateAsync(superAdmin, CommonConstant.SuperAdminPassword);
                //	await userManager.AddToRoleAsync(superAdmin, CommonConstant.AdministratorRole);
                //	await userManager.AddClaimAsync(superAdmin, new Claim(CommonConstant.ClaimName, CommonConstant.SuperAdminName));
                //	await userManager.AddClaimAsync(superAdmin, new Claim(CommonConstant.ClaimSurname, CommonConstant.SuperAdminSurname));
                //}

                //#endregion

                //#region Attorney

                //var defaultAttorney = await context.Attorneys.FindAsync(CommonConstant.DefaultAttorneyId);
                //if (defaultAttorney == null)
                //{
                //	defaultAttorney = new Attorney
                //	{
                //		Id = CommonConstant.DefaultAttorneyId,
                //		Name = CommonConstant.DefaultAttorneyName,
                //		Surname = CommonConstant.DefaultAttorneySurname,
                //		CreatedAt = DateTime.UtcNow
                //	};
                //	context.Attorneys.Add(defaultAttorney);

                //	var attorney = await userManager.FindByEmailAsync(CommonConstant.DefaultAttorneyUserMail);
                //	if (attorney == null)
                //	{
                //		attorney = new ApplicationUser { UserName = CommonConstant.DefaultAttorneyUserMail, Email = CommonConstant.DefaultAttorneyUserMail, AssignedToAttorneyId = null };
                //		await userManager.CreateAsync(attorney, CommonConstant.DefaultAttorneyPassword);
                //		await userManager.AddToRoleAsync(attorney, CommonConstant.AttorneyRole);
                //		defaultAttorney.AttorneyUserId = new Guid(attorney.Id);
                //	}
                //	await context.SaveChangesAsync(cancellationToken);
                //}

                //#endregion



                //#endregion

                //var manager = scope.ServiceProvider.GetRequiredService<OpenIddictApplicationManager<OpenIddictApplication>>();

                //if (await manager.FindByClientIdAsync("mvc", cancellationToken) == null)
                //{
                //	var application = new OpenIddictApplication
                //	{
                //		ClientId = "mvc",
                //		DisplayName = "MVC client application"//,
                //		//LogoutRedirectUri = "http://localhost:53507/",
                //		//RedirectUri = "http://localhost:53507/signin-oidc"
                //	};

                //	await manager.CreateAsync(application, "901564A5-E7FE-42CB-B10D-61EF6A8F3654", cancellationToken);
                //}
                //if (await manager.FindByClientIdAsync("visual_evidence_recorder", cancellationToken) == null)
                //{
                //	var application = new OpenIddictApplication
                //	{
                //		ClientId = "visual_evidence_recorder",
                //		DisplayName = "Visual Evidence Recorder"
                //	};

                //	await manager.CreateAsync(application, "4A0B0FEA-18DE-464D-B6A1-FC1BC1FD2FE9", cancellationToken);
                //}

                //// To test this sample with Postman, use the following settings:
                ////
                //// * Authorization URL: http://localhost:54540/connect/authorize
                //// * Access token URL: http://localhost:54540/connect/token
                //// * Client ID: postman
                //// * Client secret: [blank] (not used with public clients)
                //// * Scope: openid email profile roles
                //// * Grant type: authorization code
                //// * Request access token locally: yes
                //if (await manager.FindByClientIdAsync("postman", cancellationToken) == null)
                //{
                //	var application = new OpenIddictApplication
                //	{
                //		ClientId = "postman",
                //		DisplayName = "Postman"//,
                //		//RedirectUri = "https://www.getpostman.com/oauth2/callback"
                //	};
                //	await manager.CreateAsync(application, cancellationToken);
                //}
            }
        }


    }
}
