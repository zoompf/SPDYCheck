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
using System.Security.Authentication;
using System.Globalization;

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
        private static Regex hostRegex = new Regex(@"^([a-zA-Z0-9\-\.]+\.[a-zA-Z0-9]+)(:\d{2,5})?$", RegexOptions.Compiled);

        private static Regex punyHostRegex = new Regex(@"^([a-zA-Z0-9\-\.]+\.[a-zA-Z0-9\-]+)(:\d{2,5})?$", RegexOptions.Compiled);

        // Cache results of common entries
        private static OCache<String> cachedResults = new OCache<string>();
        protected const int cacheResultsExpiration = 600;  // 10 minutes

        // Prevent hacking/abuse
        private static OCache<int> userTracker = new OCache<int>();
        protected const int userTrackerExpiration = 3600;    // 1 hour
        protected const int maxAttempts = 15;                // max # attempts allowed in that time

        public void ProcessRequest(HttpContext context)
        {
            JObject resp = new JObject();
            resp["bad"] = false;
            resp["hiterror"] = false;
            resp["excessive"] = false;
            
            context.Response.ContentType = "text/plain";

            IdnMapping mapper = new IdnMapping();
            

            string host = String.Empty;
            int port = 443;

            String clientIPAddress = getClientIP(context);

            // If excessive attempts from this IP, return now
            if (clientHitMaxAttempts(clientIPAddress))
            {
                resp["excessive"] = true;
                context.Response.Write(resp.ToString());
                return;
            }

            string tmp = Normalize(context.Request.QueryString["host"]).ToLower();
            string unpuny = mapper.GetAscii(tmp);
            
            Match match = null;
            
            //was this a puny domain name?
            if (tmp != unpuny)
            {
                match = punyHostRegex.Match(unpuny);
            }
            else
            {
                match = hostRegex.Match(tmp);
            }
            if (match.Success)
            {
                host = match.Groups[1].Value;

                if (match.Groups[2].Value.Length > 1)
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

            // If bad host, return now
            if (String.IsNullOrEmpty(host))
            {
                resp["bad"] = true;
                context.Response.Write(resp.ToString());
                return;
            }

            // If this host is in our cache, just return that.
            if (cachedResults.ContainsKey(host))
            {
                String log = String.Format("{0}\tReturning cached result of {1} for {2}", DateTime.Now, host, clientIPAddress);
                TestLog.Log(log);
                context.Response.Write(cachedResults.Get(host));
                return;
            }

            SPDYResult result;
            try 
            {
                result=SPDYChecker.Test(host, port, 8000, clientIPAddress);
            }
            catch(Exception e) 
            {
                resp["hiterror"] = true;
                context.Response.Write(resp.ToString());
                String log = String.Format("{0}Error encountered for {1}: {2}", DateTime.Now, host, e.Message);
                TestLog.Log(log);
                return;
            }

            TestLog.Log(false, result, clientIPAddress);

            ////Hurray! Everything worked!

            JArray a;
            resp["Host"] = result.Hostname;
            resp["Port"] = result.Port;
            resp["ConnectivityHTTP"] = result.ConnectivityHTTP;
            resp["HTTPServerHeader"] = result.HTTPServerHeader;

            resp["ConnectivitySSL"] = result.ConnectivitySSL;
            resp["SpeaksSSL"] = result.SpeaksSSL;
            resp["Protocol"] = protoString(result.Protocol);
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

            resp["HasALPNExtension"] = result.HasALPNExtension;
            resp["SupportsHTTP2"] = result.SupportsHTTP2;
            if (result.SupportsHTTP2 && result.ALPNProtocols.Count > 0)
            {
                resp["HTTP2Protocol"] = result.ALPNProtocols[0];
            }
            else
            {
                resp["HTTP2Protocol"] = "";
            }



            resp["SupportsHSTS"] = result.UsesStrictTransportSecurity;
            if (result.UsesStrictTransportSecurity)
            {
                resp["HSTSHeader"] = result.HstsHeader;
                resp["HSTSMaxAge"] = result.HstsMaxAge;
            }


            String response = resp.ToString();
            context.Response.Write(response);

            // Cache for future use (perform secondary lookup in case another thread added this during execution)
            if (!cachedResults.ContainsKey(host))
            {
                cachedResults.Add(host, response, cacheResultsExpiration);
            }
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }

        private String protoString(SslProtocols protocols)
        {

            switch (protocols)
            {
                case SslProtocols.Ssl2:
                    return "SSL v2";

                case SslProtocols.Ssl3:
                    return "SSL v3";

                case SslProtocols.Tls:
                    return "TLS v1.0";

                case SslProtocols.Tls11:
                    return "TLS v1.1";

                case SslProtocols.Tls12:
                    return "TLS v1.2";

                default:
                    return "TLS";
            }

        }

        // Find the client's IP address from CloudFlare, look for CF-Connecting-IP header.
        // See https://support.cloudflare.com/hc/en-us/articles/200170986-How-does-CloudFlare-handle-HTTP-Request-headers-
        protected String getClientIP(HttpContext context)
        {
            System.Collections.Specialized.NameValueCollection headers = context.Request.Headers;
            for (int index = 0; index < headers.Count; ++index)
            {
                String key = headers.GetKey(index);
                if (key == "CF-Connecting-IP")
                {
                    String[] vals = headers.GetValues(index);
                    if (vals.Length > 0)
                    {
                        // Should always be first entry
                        return vals[0];
                    }
                }
            }
            return String.Empty;
        }

        /// <summary>
        /// Has this client IP address hit us too many times? Returns true if they have.
        /// </summary>
        /// <param name="clientIPAddress"></param>
        /// <returns></returns>
        protected bool clientHitMaxAttempts(String clientIPAddress) 
        {
            // Need an IP address
            if (String.IsNullOrWhiteSpace(clientIPAddress))
            {
                return false;
            }

            // First visit in awhile
            if (!userTracker.ContainsKey(clientIPAddress))
            {
                userTracker.Add(clientIPAddress, 1, userTrackerExpiration);
                return false;
            }

            // We've seen this person before. Update their count
            int currentAttempts = userTracker.Get(clientIPAddress);
            ++currentAttempts;
            userTracker.SafeUpdate(clientIPAddress,currentAttempts,userTrackerExpiration);

            if(currentAttempts>maxAttempts) 
            {
                // Exceeded count
                String log = String.Format("{0}\tBlocking excessive request {1} from {2}", DateTime.Now, clientIPAddress, currentAttempts);
                TestLog.Log(log);
                return true;
            }
            else 
            {
                // Still okay
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