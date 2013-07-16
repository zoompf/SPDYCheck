/*

 * SPDYChecker - Audits websites for SPDY support and troubleshooting problems
    Copyright (C) 2012  Zoompf Incorporated
    info@zoompf.com

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License along
    with this program; if not, write to the Free Software Foundation, Inc.,
    51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.

*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

using Zoompf.SPDYAnalysis;

namespace SPDYCheck.org
{
    public class TestLog
    {
        static String logfile = @"D:\ZoompfDeployed\Logs\ActivityLogs\spdy.csv";

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