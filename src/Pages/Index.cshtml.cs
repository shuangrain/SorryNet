using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sorry.NET.Models;
using Sorry.NET.Service;

namespace Sorry.NET.Pages
{
    public class IndexModel : PageModel
    {
        private readonly GifService _gifService = null;

        public IndexModel(GifService gifService)
        {
            _gifService = gifService;
        }

        public void OnGet()
        {
            ViewData["Templates"] = _gifService.GetAllTemplate();
            ViewData["Last5Image"] = _gifService.GetLast5Image();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult OnPost(ExportGifModel model)
        {
            TemplateModel template = _gifService.GetAllTemplate().FirstOrDefault(x => x.Name == model.Name);
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

            string gifPath = string.Format("/images/tmp/{0}.gif", guid);

            return alertAndRedirect(gifPath, "成功");
        }

        [NonAction]
        private IActionResult alertAndRedirect(string url, string content)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("<script>");
            sb.AppendFormat("alert('{0}');", content);
            sb.AppendFormat("window.location.href = '{0}';", url);
            sb.Append("</script>");

            byte[] data = Encoding.UTF8.GetBytes(sb.ToString());
            Response.ContentType = "text/html; charset=utf-8";
            Response.Body.Write(data, 0, data.Length);

            return Content(string.Empty);
        }

        [NonAction]
        private IEnumerable<string> getModelErrors()
        {
            return ModelState.Values.SelectMany(v => v.Errors).Select(x => x.ErrorMessage);
        }
    }
}
