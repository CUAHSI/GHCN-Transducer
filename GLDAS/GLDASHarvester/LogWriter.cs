using System;
using System.IO;
using System.Reflection;

namespace GldasHarvester
{

    /// <summary>
    /// Helper class for writing messages to log file
    /// see example at https://stackoverflow.com/questions/20185015/how-to-write-log-file-in-c
    /// </summary>
    public class LogWriter
    {
        private string _logFile = string.Empty;

        public LogWriter()
        {
            var exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _logFile = exePath + "\\log_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt";
        }

        public void LogWrite(string logMessage)
        {
            var m_exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            try
            {
                using (StreamWriter w = File.AppendText(_logFile))
                {
                    Log(logMessage, w);
                }
            }
            catch (Exception ex)
            {
            }
        }

        public static void Log(string logMessage, TextWriter txtWriter)
        {
            try
            {
                txtWriter.Write("\r\nLog Entry : ");
                txtWriter.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(),
                    DateTime.Now.ToLongDateString());
                txtWriter.WriteLine("  :");
                txtWriter.WriteLine("  :{0}", logMessage);
                txtWriter.WriteLine("-------------------------------");
            }
            catch (Exception ex)
            {
            }
        }
    }
}
