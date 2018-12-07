using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Sorry.NET.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Sorry.NET.Service
{
    public class GifService
    {
        private readonly ILogger _logger = null;
        private readonly IConfiguration _configuration = null;
        private readonly IHostingEnvironment _hostingEnvironment;

        public GifService(ILogger<GifService> logger, IConfiguration configuration, IHostingEnvironment hostingEnvironment)
        {
            _logger = logger;
            _configuration = configuration;
            _hostingEnvironment = hostingEnvironment;
        }

        public IEnumerable<string> GetLast5Image()
        {
            string wwwrootDir = _hostingEnvironment.WebRootPath;
            string tmpPath = Path.Combine(wwwrootDir, "images", "tmp");

            if (!Directory.Exists(tmpPath))
            {
                Directory.CreateDirectory(tmpPath);
            }

            var directory = new DirectoryInfo(tmpPath);

            var allImage = directory.GetFiles()
                                    .OrderByDescending(x => x.LastWriteTime);

            // 只保留五張圖
            foreach (var item in allImage.Skip(5))
            {
                if (item.LastWriteTime < DateTime.Now.Date)
                {
                    File.Delete(item.FullName);
                }
            }

            return allImage.Select(x => $"/images/tmp/{x.Name}");
        }

        public bool RenderAss(string sourceAssTemplatePath, string guid, List<string> sentences)
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string serverSourceAssTemplatePath = Path.Combine(baseDir, Path.Combine(sourceAssTemplatePath.Split('/')));
                string serverExportAssTemplatePath = Path.Combine(Path.GetTempPath(), $"{guid}.ass");

                string sourceAssTemplateContent = File.ReadAllText(serverSourceAssTemplatePath);

                for (int i = 0; i < sentences.Count; i++)
                {
                    string key = string.Format("<%= sentences[{0}] %>", i);
                    sourceAssTemplateContent = sourceAssTemplateContent.Replace(key, sentences[i]);
                }

                File.WriteAllText(serverExportAssTemplatePath, sourceAssTemplateContent);
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return false;
            }
        }

        public bool RenderGif(string sourceVideoTemplatePath, string guid)
        {
            try
            {
                string wwwrootDir = _hostingEnvironment.WebRootPath;
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string serverSourceVideoTemplatePath = Path.Combine(baseDir, Path.Combine(sourceVideoTemplatePath.Split('/')));
                string serverAssTemplatePath = Path.Combine(Path.GetTempPath(), $"{guid}.ass");
                string serverGifPath = Path.Combine(wwwrootDir, "images", "tmp", $"{guid}.gif");
                string serverProcessPath = Path.Combine(baseDir, "App_Data", "ffmpeg.exe");

                string cmd = string.Format("-i \"{0}\" -r 6 -vf \"ass='{1}'\",scale=300:-1 -y \"{2}\"",
                                           serverSourceVideoTemplatePath,
                                           serverAssTemplatePath.Replace(@"\", @"\\").Replace(@":\", @"\:\"),
                                           serverGifPath);

                using (var p = new Process())
                {
                    p.StartInfo.FileName = serverProcessPath;
                    p.StartInfo.Arguments = cmd;

                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.RedirectStandardError = true;
                    p.StartInfo.RedirectStandardOutput = true;

                    p.OutputDataReceived += outputHandle;
                    p.ErrorDataReceived += outputHandle;

                    p.Start();
                    p.BeginOutputReadLine();
                    p.BeginErrorReadLine();
                    p.WaitForExit();
                }

                File.Delete(serverAssTemplatePath);
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return false;
            }
            finally
            {
                GC.Collect();
            }
        }

        public List<TemplateModel> GetAllTemplate()
        {
            return _configuration.GetSection("Templates").Get<List<TemplateModel>>();
        }

        private void outputHandle(object sender, DataReceivedEventArgs args)
        {
            _logger.LogInformation(args.Data);
        }
    }
}
