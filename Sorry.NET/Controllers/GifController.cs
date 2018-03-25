using Newtonsoft.Json;
using Sorry.NET.Models;
using Sorry.NET.Service;
using Sorry.NET.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace Sorry.NET.Controllers
{
    public class GifController : Controller
    {
        private readonly GifService _gifService = null;

        public GifController()
        {
            _gifService = new GifService();
        }

        // GET: GifFactory
        public ActionResult Index()
        {
            ViewBag.Templates = ConfigUtility.Templates;
            ViewBag.Last5Image = _gifService.GetLast5Image();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(ExportGifModel model)
        {
            TemplateModel template = ConfigUtility.Templates.FirstOrDefault(x => x.Name == model.Name);
            if (template == null)
            {
                ModelState.AddModelError("", "找不到此樣板");
            }

            if (!ModelState.IsValid)
            {
                return alertAndRedirect(Url.Action("Index"), getModelErrors().First());
            }

            string guid = Guid.NewGuid().ToString();

            _gifService.RenderAss(template.Ass, guid, model.Sentences);
            _gifService.RenderGif(template.Video, guid);

            string gifPath = string.Format("/Template/tmp/{0}.gif", guid);

            return alertAndRedirect(gifPath, "成功");
        }

        [NonAction]
        private ActionResult alertAndRedirect(string url, string content)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("<script>");
            sb.AppendFormat("alert('{0}');", content);
            sb.AppendFormat("window.location.href = '{0}';", url);
            sb.Append("</script>");

            return Content(sb.ToString());
        }

        [NonAction]
        private IEnumerable<string> getModelErrors()
        {
            return ModelState.Values.SelectMany(v => v.Errors).Select(x => x.ErrorMessage);
        }
    }
}