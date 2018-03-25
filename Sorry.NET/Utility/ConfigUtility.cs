using Newtonsoft.Json;
using Sorry.NET.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace Sorry.NET.Utility
{
    public static class ConfigUtility
    {
        public static List<TemplateModel> Templates
        {
            get
            {
                var server = HttpContext.Current.Server;
                string json = File.ReadAllText(server.MapPath("~/Template/template.json"));
                List<TemplateModel> templates = JsonConvert.DeserializeObject<List<TemplateModel>>(json);
                return templates;
            }
        }
    }
}