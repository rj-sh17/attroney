﻿/*
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0)
 * See https://github.com/openiddict/openiddict-core for more information concerning
 * the license and the contributors participating to this project.
 */

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AspNet.Security.OpenIdConnect.Extensions;
using AspNet.Security.OpenIdConnect.Primitives;
using AspNet.Security.OpenIdConnect.Server;
using AttorneyJournal.Helpers;
using AttorneyJournal.Models;
using AttorneyJournal.ViewModels.Authorization;
using AttorneyJournal.ViewModels.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Mvc.Server.ViewModels.Authorization;
using OpenIddict.Core;
using OpenIddict.Models;

namespace AttorneyJournal.Controllers
{
	/// <summary>
	/// 
	/// </summary>
	public class AuthorizationController : Controller
	{
		private readonly OpenIddictApplicationManager<OpenIddictApplication> _applicationManager;
		private readonly IOptions<IdentityOptions> _identityOptions;
		private readonly SignInManager<ApplicationUser> _signInManager;
		private readonly UserManager<ApplicationUser> _userManager;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="applicationManager"></param>
		/// <param name="identityOptions"></param>
		/// <param name="signInManager"></param>
		/// <param name="userManager"></param>
		public AuthorizationController(
			OpenIddictApplicationManager<OpenIddictApplication> applicationManager,
			IOptions<IdentityOptions> identityOptions,
			SignInManager<ApplicationUser> signInManager,
			UserManager<ApplicationUser> userManager)
		{
			_applicationManager = applicationManager;
			_identityOptions = identityOptions;
			_signInManager = signInManager;
			_userManager = userManager;
		}

		#region Authorization code, implicit and implicit flows
		// Note: to support interactive flows like the code flow,
		// you must provide your own authorization endpoint action:

		[ApiExplorerSettings(IgnoreApi = true)]
		[Authorize, HttpGet("~/connect/authorize")]
		public async Task<IActionResult> Authorize(OpenIdConnectRequest request)
		{
			Debug.Assert(request.IsAuthorizationRequest(),
				"The OpenIddict binder for ASP.NET Core MVC is not registered. " +
				"Make sure services.AddOpenIddict().AddMvcBinders() is correctly called.");

			// Retrieve the application details from the database.
			var application = await _applicationManager.FindByClientIdAsync(request.ClientId, HttpContext.RequestAborted);
			if (application == null)
			{
				return View("Error", new ErrorViewModel
				{
					Error = OpenIdConnectConstants.Errors.InvalidClient,
					ErrorDescription = "Details concerning the calling client application cannot be found in the database"
				});
			}

			// Flow the request_id to allow OpenIddict to restore
			// the original authorization request from the cache.
			return View(new AuthorizeViewModel
			{
				ApplicationName = application.DisplayName,
				RequestId = request.RequestId,
				Scope = request.Scope
			});
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		[ApiExplorerSettings(IgnoreApi = true)]
		[Authorize, FormValueRequired("submit.Accept")]
		[HttpPost("~/connect/authorize"), ValidateAntiForgeryToken]
		public async Task<IActionResult> Accept(OpenIdConnectRequest request)
		{
			Debug.Assert(request.IsAuthorizationRequest(),
				"The OpenIddict binder for ASP.NET Core MVC is not registered. " +
				"Make sure services.AddOpenIddict().AddMvcBinders() is correctly called.");

			// Retrieve the profile of the logged in user.
			var user = await _userManager.GetUserAsync(User);
			if (user == null)
			{
				return View("Error", new ErrorViewModel
				{
					Error = OpenIdConnectConstants.Errors.ServerError,
					ErrorDescription = "An internal error has occurred"
				});
			}

			// Create a new authentication ticket.
			var ticket = await CreateTicketAsync(_signInManager, _identityOptions, request, user);

			// Returning a SignInResult will ask OpenIddict to issue the appropriate access/identity tokens.
			return SignIn(ticket.Principal, ticket.Properties, ticket.AuthenticationScheme);
		}

		[ApiExplorerSettings(IgnoreApi = true)]
		[Authorize, FormValueRequired("submit.Deny")]
		[HttpPost("~/connect/authorize"), ValidateAntiForgeryToken]
		public IActionResult Deny()
		{
			// Notify OpenIddict that the authorization grant has been denied by the resource owner
			// to redirect the user agent to the client application using the appropriate response_mode.
			return Forbid(OpenIdConnectServerDefaults.AuthenticationScheme);
		}

		// Note: the logout action is only useful when implementing interactive
		// flows like the authorization code flow or the implicit flow.

		[ApiExplorerSettings(IgnoreApi = true)]
		[HttpGet("~/connect/logout")]
		public IActionResult Logout(OpenIdConnectRequest request)
		{
			Debug.Assert(request.IsLogoutRequest(),
				"The OpenIddict binder for ASP.NET Core MVC is not registered. " +
				"Make sure services.AddOpenIddict().AddMvcBinders() is correctly called.");

			// Flow the request_id to allow OpenIddict to restore
			// the original logout request from the distributed cache.
			return View(new LogoutViewModel
			{
				RequestId = request.RequestId
			});
		}

		[ApiExplorerSettings(IgnoreApi = true)]
		[HttpPost("~/connect/logout"), ValidateAntiForgeryToken]
		public async Task<IActionResult> Logout()
		{
			// Ask ASP.NET Core Identity to delete the local and external cookies created
			// when the user agent is redirected from the external identity provider
			// after a successful authentication flow (e.g Google or Facebook).
			await _signInManager.SignOutAsync();

			// Returning a SignOutResult will ask OpenIddict to redirect the user agent
			// to the post_logout_redirect_uri specified by the client application.
			return SignOut(OpenIdConnectServerDefaults.AuthenticationScheme);
		}
		#endregion

		#region Password, authorization code and refresh token flows
		// Note: to support non-interactive flows like password,
		// you must provide your own token endpoint action:

		/// <summary>
		/// 
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		[HttpPost("~/connect/token"), Produces("application/json")]
		public async Task<IActionResult> Exchange(OpenIdConnectRequest request)
		{
			Debug.Assert(request.IsTokenRequest(), "The OpenIddict binder for ASP.NET Core MVC is not registered. Make sure services.AddOpenIddict().AddMvcBinders() is correctly called.");

			if (request.IsPasswordGrantType())
			{
				var user = await _userManager.FindByNameAsync(request.Username);
				if (user == null)
				{
					return BadRequest(new OpenIdConnectResponse
					{
						Error = OpenIdConnectConstants.Errors.InvalidGrant,
						ErrorDescription = "The username/password couple is invalid."
					});
				}

				// Ensure the user is allowed to sign in.
				if (!await _signInManager.CanSignInAsync(user))
				{
					return BadRequest(new OpenIdConnectResponse
					{
						Error = OpenIdConnectConstants.Errors.InvalidGrant,
						ErrorDescription = "The specified user is not allowed to sign in."
					});
				}

				// Reject the token request if two-factor authentication has been enabled by the user.
				if (_userManager.SupportsUserTwoFactor && await _userManager.GetTwoFactorEnabledAsync(user))
				{
					return BadRequest(new OpenIdConnectResponse
					{
						Error = OpenIdConnectConstants.Errors.InvalidGrant,
						ErrorDescription = "The specified user is not allowed to sign in."
					});
				}

				// Ensure the user is not already locked out.
				if (_userManager.SupportsUserLockout && await _userManager.IsLockedOutAsync(user))
				{
					return BadRequest(new OpenIdConnectResponse
					{
						Error = OpenIdConnectConstants.Errors.InvalidGrant,
						ErrorDescription = "The username/password couple is invalid."
					});
				}

				#region Rimossa conferma mail
				// Ensure email confirmation
				//if (!await _userManager.IsEmailConfirmedAsync(user))
				//{
				//	return BadRequest(new OpenIdConnectResponse
				//	{
				//		Error = OpenIdConnectConstants.Errors.AccessDenied,
				//		ErrorDescription = "You must confirm email address before login."
				//	});
				//}
				#endregion

				// Ensure the password is valid.
				if (!await _userManager.CheckPasswordAsync(user, request.Password))
				{
					if (_userManager.SupportsUserLockout)
					{
						await _userManager.AccessFailedAsync(user);
					}

					return BadRequest(new OpenIdConnectResponse
					{
						Error = OpenIdConnectConstants.Errors.InvalidGrant,
						ErrorDescription = "The username/password couple is invalid."
					});
				}

				if (_userManager.SupportsUserLockout)
				{
					await _userManager.ResetAccessFailedCountAsync(user);
				}

				// Create a new authentication ticket.
				var ticket = await CreateTicketAsync(_signInManager, _identityOptions, request, user);
				ticket.SetScopes(new[]
				{
					OpenIdConnectConstants.Scopes.OpenId,
					OpenIdConnectConstants.Scopes.Email,
					OpenIdConnectConstants.Scopes.Profile,
					OpenIdConnectConstants.Scopes.OfflineAccess,
					OpenIddictConstants.Scopes.Roles
				}.Intersect(request.GetScopes()));

				return SignIn(ticket.Principal, ticket.Properties, ticket.AuthenticationScheme);
			}

			else if (request.IsAuthorizationCodeGrantType() || request.IsRefreshTokenGrantType())
			{
				// Retrieve the claims principal stored in the authorization code/refresh token.
				var info = await HttpContext.Authentication.GetAuthenticateInfoAsync(
					OpenIdConnectServerDefaults.AuthenticationScheme);

				// Retrieve the user profile corresponding to the authorization code/refresh token.
				// Note: if you want to automatically invalidate the authorization code/refresh token
				// when the user password/roles change, use the following line instead:
			    var user = await _signInManager.ValidateSecurityStampAsync(info.Principal);
				//var user = await _userManager.GetUserAsync(info.Principal);
				if (user == null)
				{
					return BadRequest(new OpenIdConnectResponse
					{
						Error = OpenIdConnectConstants.Errors.LoginRequired,
						ErrorDescription = "The token is no longer valid."
					});
				}

				// Ensure the user is still allowed to sign in.
				if (!await _signInManager.CanSignInAsync(user))
				{
					return BadRequest(new OpenIdConnectResponse
					{
						Error = OpenIdConnectConstants.Errors.AccessDenied,
						ErrorDescription = "The user is no longer allowed to sign in."
					});
				}

				// Create a new authentication ticket, but reuse the properties stored in the
				// authorization code/refresh token, including the scopes originally granted.
				var ticket = await CreateTicketAsync(_signInManager, _identityOptions, request, user, info.Properties);

				return SignIn(ticket.Principal, ticket.Properties, ticket.AuthenticationScheme);
			}

			return BadRequest(new OpenIdConnectResponse
			{
				Error = OpenIdConnectConstants.Errors.UnsupportedGrantType,
				ErrorDescription = "The specified grant type is not supported."
			});
		}
		#endregion

		/// <summary>
		/// 
		/// </summary>
		/// <param name="signInManager"></param>
		/// <param name="identityOptions"></param>
		/// <param name="request"></param>
		/// <param name="user"></param>
		/// <param name="properties"></param>
		/// <returns></returns>
		public static async Task<AuthenticationTicket> CreateTicketAsync(
			SignInManager<ApplicationUser> signInManager,
			IOptions<IdentityOptions> identityOptions,
			OpenIdConnectRequest request,
			ApplicationUser user,
			AuthenticationProperties properties = null)
		{
			// Create a new ClaimsPrincipal containing the claims that
			// will be used to create an id_token, a token or a code.
			var principal = await signInManager.CreateUserPrincipalAsync(user);

			// Create a new authentication ticket holding the user identity.
			var ticket = new AuthenticationTicket(principal, properties,
				OpenIdConnectServerDefaults.AuthenticationScheme);

			if (!request.IsAuthorizationCodeGrantType() && !request.IsRefreshTokenGrantType())
			{
				// Set the list of scopes granted to the client application.
				// Note: the offline_access scope must be granted
				// to allow OpenIddict to return a refresh token.
				ticket.SetScopes(new[]
				{
					OpenIdConnectConstants.Scopes.OpenId,
					OpenIdConnectConstants.Scopes.Email,
					OpenIdConnectConstants.Scopes.Profile,
					OpenIdConnectConstants.Scopes.OfflineAccess,
					OpenIddictConstants.Scopes.Roles
				}.Intersect(request.GetScopes()));
			}

			ticket.SetResources("resource_server");

			// Note: by default, claims are NOT automatically included in the access and identity tokens.
			// To allow OpenIddict to serialize them, you must attach them a destination, that specifies
			// whether they should be included in access tokens, in identity tokens or in both.

			foreach (var claim in ticket.Principal.Claims)
			{
				// Never include the security stamp in the access and identity tokens, as it's a secret value.
				if (claim.Type == identityOptions.Value.ClaimsIdentity.SecurityStampClaimType)
				{
					continue;
				}

				var destinations = new List<string>
				{
					OpenIdConnectConstants.Destinations.AccessToken
				};

				// Only add the iterated claim to the id_token if the corresponding scope was granted to the client application.
				// The other claims will only be added to the access_token, which is encrypted when using the default format.
				if ((claim.Type == OpenIdConnectConstants.Claims.Name && ticket.HasScope(OpenIdConnectConstants.Scopes.Profile)) ||
					(claim.Type == OpenIdConnectConstants.Claims.Email && ticket.HasScope(OpenIdConnectConstants.Scopes.Email)) ||
					(claim.Type == OpenIdConnectConstants.Claims.Role && ticket.HasScope(OpenIddictConstants.Claims.Roles)))
				{
					destinations.Add(OpenIdConnectConstants.Destinations.IdentityToken);
				}

				claim.SetDestinations(destinations);
			}

			return ticket;
		}
	}
}