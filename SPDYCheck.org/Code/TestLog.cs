using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

using Zoompf.SPDYAnalysis;

namespace SPDYCheck.org
{
    public class TestLog
    {
        static String logfile = @"C:\ZoompfDeployed\Logs\ActivityLogs\spdy.csv";

        public static void Log(bool wasCached, SPDYResult result, String ip)
        {

            try
            {
                System.IO.File.AppendAllText(logfile, MakeCSV(
                                                                DateTime.Now,
                                                                ip,
                                                                result.Hostname,
                                                                (wasCached) ? "=CACHED=" : "-",
                                                                result.ConnectivityHTTP,
                                                                result.ConnectivitySSL,
                                                                result.SpeaksSSL,
                                                                result.SSLCertificateValid,
                                                                result.HasNPNExtension,
                                                                result.SupportsSPDY,
                                                                result.RedirectsToSSL) + Environment.NewLine);
            }
            catch (Exception)
            {

            }
        }

        private static String MakeCSV(params object[] args)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < args.Length; i++)
            {
                String arg = String.Empty;
                if (args[i] != null)
                {
                    arg = args[i].ToString();
                }

                if (arg.Contains(","))
                {
                    sb.Append('"');
                    sb.Append(arg);
                    sb.Append('"');
                }
                else
                {
                    sb.Append(arg);
                }
                if (i < args.Length - 1)
                {
                    sb.Append(", ");
                }
            }
            return sb.ToString();
        }



    }
}