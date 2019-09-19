using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using System.Text;

namespace AttorneyJournal.Services
{
	/// <summary>
	/// https://gist.github.com/ahmad-moussawi/1643d703c11699a6a4046e57247b4d09
	/// </summary>
	public class RenderViewService
	{
		private IRazorViewEngine _viewEngine;
		private ITempDataProvider _tempDataProvider;
		private IServiceProvider _serviceProvider;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="viewEngine"></param>
		/// <param name="tempDataProvider"></param>
		/// <param name="serviceProvider"></param>
		public RenderViewService(
			IRazorViewEngine viewEngine,
			ITempDataProvider tempDataProvider,
			IServiceProvider serviceProvider)
		{
			_viewEngine = viewEngine;
			_tempDataProvider = tempDataProvider;
			_serviceProvider = serviceProvider;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TModel"></typeparam>
		/// <param name="name"></param>
		/// <param name="model"></param>
		/// <returns></returns>
		public string Render<TModel>(string name, TModel model)
		{
			var actionContext = GetActionContext();

			var viewEngineResult = _viewEngine.FindView(actionContext, name, false);

			if (!viewEngineResult.Success)
			{
				throw new InvalidOperationException(string.Format("Couldn't find view '{0}'", name));
			}

			var view = viewEngineResult.View;

			using (var output = new StringWriter())
			{
				var viewContext = new ViewContext(
					actionContext,
					view,
					new ViewDataDictionary<TModel>(
						metadataProvider: new EmptyModelMetadataProvider(),
						modelState: new ModelStateDictionary())
					{
						Model = model
					},
					new TempDataDictionary(
						actionContext.HttpContext,
						_tempDataProvider),
					output,
					new HtmlHelperOptions());

				view.RenderAsync(viewContext).GetAwaiter().GetResult();

				return output.ToString();
			}
		}

		public async Task<string> RenderViewToStringAsync<TModel>(string name, TModel model)
		{
			var actionContext = GetActionContext();

			var viewEngineResult = _viewEngine.FindView(actionContext, name, true);

			if (!viewEngineResult.Success)
			{
				throw new InvalidOperationException(string.Format("Couldn't find view '{0}'", name));
			}

			var view = viewEngineResult.View;

			using (var output = new StringWriter())
			{
				var viewContext = new ViewContext(
					actionContext,
					view,
					new ViewDataDictionary<TModel>(
						metadataProvider: new EmptyModelMetadataProvider(),
						modelState: new ModelStateDictionary())
					{
						Model = model
					},
					new TempDataDictionary(
						actionContext.HttpContext,
						_tempDataProvider),
					output,
					new HtmlHelperOptions());

				await view.RenderAsync(viewContext);

				return output.ToString();
			}
		}

		private ActionContext GetActionContext()
		{
			var httpContext = new DefaultHttpContext()
			{
				RequestServices = _serviceProvider
			};
			return new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
		}
	}
}
