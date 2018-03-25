using NLog;
using Sorry.NET.Models;
using Sorry.NET.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace Sorry.NET.Service
{
    public class GifService
    {
        public IEnumerable<string> GetLast5Image()
        {
            var server = HttpContext.Current.Server;
            string tmpPath = server.MapPath("/Template/tmp");

            if (!Directory.Exists(tmpPath))
            {
                Directory.CreateDirectory(tmpPath);
            }

            var directory = new DirectoryInfo(tmpPath);

            var allImage = directory.GetFiles()
                                    .OrderByDescending(x => x.LastWriteTime);

            // 只保留當日的圖
            foreach (var item in allImage.Skip(5))
            {
                if (item.LastWriteTime < DateTime.Now.Date)
                {
                    File.Delete(item.FullName);
                }
            }

            IEnumerable<string> last5Image = allImage.Take(5)
                                                     .Select(x =>
                                                     {
                                                         int idx = x.FullName.IndexOf("Template");
                                                         return ("/" + x.FullName.Substring(idx).Replace(@"\", @"/"));
                                                     });

            return last5Image;
        }

        public bool RenderAss(string sourceAssTemplatePath, string guid, List<string> sentences)
        {
            try
            {
                var server = HttpContext.Current.Server;

                string serverSourceAssTemplatePath = server.MapPath(sourceAssTemplatePath);
                string serverExportAssTemplatePath = Path.Combine(Path.GetTempPath(), (guid + ".gif"));

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
                LogManager.GetCurrentClassLogger().Error(e);
                return false;
            }
        }

        public bool RenderGif(string sourceVideoTemplatePath, string guid)
        {
            try
            {
                var server = HttpContext.Current.Server;

                string serverSourceVideoTemplatePath = server.MapPath(sourceVideoTemplatePath);
                string serverAssTemplatePath = Path.Combine(Path.GetTempPath(), (guid + ".gif"));
                string serverGifPath = server.MapPath(string.Format("/Template/tmp/{0}.gif", guid));
                string serverProcessPath = server.MapPath("/App_Data/ffmpeg.exe");

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
                LogManager.GetCurrentClassLogger().Error(e);
                return false;
            }
            finally
            {
                GC.Collect();
            }
        }

        private void outputHandle(object sender, DataReceivedEventArgs args)
        {
            LogManager.GetCurrentClassLogger().Info(args.Data);
        }
    }
}