using System;
using System.IO;

namespace CSGO_External_Overlay
{
    class MyUtil
    {
        /// <summary>
        /// 保存指定Log文件到本地
        /// </summary>
        /// <param name="fileName">文件名（部分）</param>
        /// <param name="logContent">保存内容</param>
        public static void SaveAppLogFile(string fileName, string logContent)
        {
            try
            {
                string path = AppDomain.CurrentDomain.BaseDirectory + "AppLog";
                Directory.CreateDirectory(path);
                path += $@"\{fileName}_#_{ DateTime.Now:yyyyMMdd_HH-mm-ss_ffff}.log";
                File.WriteAllText(path, logContent);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
