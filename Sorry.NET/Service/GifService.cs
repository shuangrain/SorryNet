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

            IEnumerable<string> allImage = directory.GetFiles()
                                                    .OrderByDescending(x => x.LastWriteTime)
                                                    .Select(x => x.FullName);
            IEnumerable<string> last5Image = allImage.Take((allImage.Count() > 5) ? 5 : allImage.Count())
                                                     .Select(x => "/" + x.Substring(x.IndexOf("Template")).Replace(@"\", @"/"));

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

                    p.StartInfo.UseShellExecute = false; //輸出資訊重定向
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.RedirectStandardError = true;
                    p.StartInfo.RedirectStandardOutput = true;

                    p.OutputDataReceived += outputHandle;
                    p.ErrorDataReceived += outputHandle;

                    p.Start(); //啟動執行緒
                    p.BeginOutputReadLine();
                    p.BeginErrorReadLine();
                    p.WaitForExit(); //等待進程結束
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
            NLog.LogManager.GetCurrentClassLogger().Info(args.Data);
            Debug.WriteLine(args.Data);
        }
    }
}