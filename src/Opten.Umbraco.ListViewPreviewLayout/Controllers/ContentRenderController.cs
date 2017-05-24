using Opten.Umbraco.ListViewPreviewLayout.Models;
using System;
using System.IO;
using System.Web.Mvc;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web.Models;
using Umbraco.Web.Mvc;

namespace Opten.Umbraco.ListViewPreviewLayout.Controllers
{
	// TODO: Authorize
	[PluginController("ListViewPreviewLayout")]
	public class ContentRenderController : SurfaceController
	{
		public ActionResult Index(int id)
		{
			var content = Services.ContentService.GetById(id);

			var publishedContent = content.ToPublishedContent();

			var templateId = publishedContent.TemplateId;

			var typedModel = PublishedContentModelFactoryResolver.Current.Factory.CreateModel(publishedContent);

			var renderModel = new RenderModel(typedModel, System.Threading.Thread.CurrentThread.CurrentUICulture);

			RouteData.DataTokens["umbraco"] = renderModel;
			ViewData.Model = renderModel;

			if (templateId > 0)
			{
				var templateModel = Services.FileService.GetTemplate(templateId);
				if (templateModel == null)
					throw new InvalidOperationException("The template with Id " + templateId + " does not exist, the page cannot render");
				
				var template = templateModel.Alias.Split('.')[0].ToSafeAlias();

				var result = ViewEngines.Engines.FindView(ControllerContext, template, null);
				if (result.View == null)
				{
					throw new Exception("No physical template file was found for template " + template);
				}

				using (var stringWriter = new StringWriter()) {
					var viewCxt = new ViewContext(ControllerContext, result.View, ViewData, TempData, stringWriter);
					result.View.Render(viewCxt, stringWriter);
					result.ViewEngine.ReleaseView(ControllerContext, result.View);

					var viewHtml = stringWriter.GetStringBuilder().ToString();
					System.Text.RegularExpressions.Regex headRegex = new System.Text.RegularExpressions.Regex(@"<head[^>]*>[\s\S]*?</head>");
					System.Text.RegularExpressions.Regex styleRegex = new System.Text.RegularExpressions.Regex(@"<style[^>]*>[\s\S]*?</style>");
					System.Text.RegularExpressions.Regex scriptRegex = new System.Text.RegularExpressions.Regex(@"<script[^>]*>[\s\S]*?</script>");
					System.Text.RegularExpressions.Regex htmlBeginRegex = new System.Text.RegularExpressions.Regex(@"<html[^>]*>");
					System.Text.RegularExpressions.Regex htmlEndRegex = new System.Text.RegularExpressions.Regex(@"</html>");
					System.Text.RegularExpressions.Regex bodyBeginRegex = new System.Text.RegularExpressions.Regex(@"<body[^>]*>");
					System.Text.RegularExpressions.Regex bodyEndRegex = new System.Text.RegularExpressions.Regex(@"</body>");
					System.Text.RegularExpressions.Regex doctypeRegex = new System.Text.RegularExpressions.Regex(@"<!DOCTYPE html>");	
					
					viewHtml = headRegex.Replace(viewHtml, "");
					viewHtml = styleRegex.Replace(viewHtml, "");
					viewHtml = scriptRegex.Replace(viewHtml, "");
					viewHtml = htmlBeginRegex.Replace(viewHtml, "");
					viewHtml = htmlEndRegex.Replace(viewHtml, "");
					viewHtml = bodyBeginRegex.Replace(viewHtml, "");
					viewHtml = bodyEndRegex.Replace(viewHtml, "");
					viewHtml = doctypeRegex.Replace(viewHtml, "");

					return Content(viewHtml);
				}
			}
			else
			{
				LogHelper.Warn<ContentRenderController>("No specified template for content with id" + publishedContent.Id);
				return Content(publishedContent.Name + " has no template. can't render.");
			}
		}
	}
}
