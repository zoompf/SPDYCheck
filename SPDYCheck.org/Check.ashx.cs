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
using System.Linq;
using System.Web;
using System.Web.Caching;

using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

using Zoompf.SPDYAnalysis;
using Zoompf.General;
using Zoompf.General.Collections;

namespace SPDYCheck.org
{
    /// <summary>
    /// Summary description for Check
    /// </summary>
    public class Check : IHttpHandler
    {

        //require a . in the hostname, and an optional :port
        //capture group 1: the host name
        //capture group 2: optional port
        private static Regex hostRegex = new Regex(@"^([a-zA-Z0-9\-\.]+\.[a-zA-Z0-9]+)(:\d{3,5})?$", RegexOptions.Compiled);


        public void ProcessRequest(HttpContext context)
        {
            
            context.Response.ContentType = "text/plain";

            string host = String.Empty;
            int port = 443;

            string tmp = Normalize(context.Request.QueryString["host"]).ToLower();

            Match match = hostRegex.Match(tmp);
            if (match.Success)
            {
                host = match.Groups[1].Value;

                if (match.Groups.Count > 2)
                {
                    //got a port, skip the ":" at the start
                    port = Convert.ToInt32(match.Groups[2].Value.Substring(1));
                }

                //disallow localhost and private ips
                if (
                    host == "localhost" ||
                    host == "127.0.0.1" ||
                    host.StartsWith("192.") ||
                    host.StartsWith("172.") ||
                    host.StartsWith("10."))
                {
                    host = String.Empty;
                }
            }


            JObject resp = new JObject();

            if (String.IsNullOrEmpty(host))
            {
                resp["bad"] = true;

            }
            else
            {

                bool fromCache = false;

                SPDYResult result = SPDYChecker.Test(host, port, 8000);


                TestLog.Log(fromCache, result, context.Request.UserHostAddress);

                ////Hurray! Everything worked!

                JArray a;
                resp["Host"] = result.Hostname;
                resp["bad"] = false;
                resp["ConnectivityHTTP"] = result.ConnectivityHTTP;
                resp["HTTPServerHeader"] = result.HTTPServerHeader;

                resp["ConnectivitySSL"] = result.ConnectivitySSL;
                resp["SpeaksSSL"] = result.SpeaksSSL;
                resp["SupportSSLHTTPFallback"] = result.SupportSSLHTTPFallback;

                resp["RedirectsToSSL"] = result.RedirectsToSSL;

                resp["SSLCertificateValid"] = result.SSLCertificateValid;

                if (!result.SSLCertificateValid)
                {
                    a = new JArray();
                    foreach (SSLCertError s in result.CertErrors)
                    {
                        a.Add(s.ToString());
                    }
                    resp["CertErrors"] = a;
                }

                resp["SSLServerHeader"] = result.SSLServerHeader;
                resp["HasNPNExtension"] = result.HasNPNExtension;
                resp["SupportsSPDY"] = result.SupportsSPDY;
                if (result.SupportsSPDY)
                {


                    a = new JArray();
                    foreach (string s in result.SPDYProtocols)
                    {
                        a.Add(s);
                    }
                    resp["SPDYProtocols"] = a;
                }

                resp["SupportsHSTS"] = result.UsesStrictTransportSecurity;
                if (result.UsesStrictTransportSecurity)
                {
                    resp["HSTSHeader"] = result.HstsHeader;
                    resp["HSTSMaxAge"] = result.HstsMaxAge;
                }

            }

            context.Response.Write(resp.ToString());
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }


        //=========== Helper Methods
        public static String Normalize(object o)
        {
            return (o != null) ? o.ToString().Trim() : String.Empty;
        }

    }
}