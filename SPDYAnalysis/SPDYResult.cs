/*

 * SPDYChecker - Audits websites for SPDY support and troubleshooting problems
    Copyright (C) 2015  Zoompf Incorporated
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
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Authentication;

namespace Zoompf.SPDYAnalysis
{

    public class SPDYResult
    {

        private static Regex hstsMaxAge = new Regex(@"max-age=(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);


        public string Hostname = String.Empty;
        public int Port = 0;
        
        
        
        //SSL Stuff
        
        public bool ConnectivitySSL = false;
        public bool SpeaksSSL = false;
        public SslProtocols Protocol = SslProtocols.None;
        public List<SSLCertError> CertErrors;
        public bool HasNPNExtension = false;
        public bool HasALPNExtension = false;

        public List<String> ALPNProtocols;
        public List<String> SPDYProtocols;
        public String SSLServerHeader;
        
        
        public bool ConnectivityHTTP = false;

        public String HTTPServerHeader;


        public bool RedirectsToSSL = false;
        public String HstsHeader = String.Empty;

        public bool SSLCertificateValid
        {
            get
            {
                return this.CertErrors.Count == 0;
            }
        }

        public bool SupportsHTTP2
        {
            get
            {
                foreach (String s in this.ALPNProtocols)
                {
                    if (s.Contains("h2"))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public bool SupportsSPDY
        {
            get
            {
                foreach (String s in this.SPDYProtocols)
                {
                    if (s.Contains("spdy"))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public bool SupportSSLHTTPFallback
        {
            get
            {
                foreach (String s in this.SPDYProtocols)
                {
                    if (s.ToLower().Contains("http"))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public bool UsesStrictTransportSecurity
        { 
            get { return !String.IsNullOrEmpty(this.HstsHeader); }
        }

        /// <summary>
        /// returns the max-age that the HSTS directive is cached for
        /// </summary>
        public int HstsMaxAge
        {
            get
            {
                
                if (!this.UsesStrictTransportSecurity)
                {
                    return 0;
                }

                Match match = hstsMaxAge.Match(this.HstsHeader);
                if (match.Success)
                {
                    return Convert.ToInt32(match.Groups[1].Value);
                }
                return 0;
                
            }
        }


        public SPDYResult(string hostname, int port)
        {
            this.Hostname = hostname;
            this.Port = port;
            if(this.Port != 443)
            {
                this.Hostname += ":" + this.Port;
            }

            //HTTP Stuff
            this.ConnectivityHTTP = false;
            this.RedirectsToSSL = false;
            this.HTTPServerHeader = String.Empty;

            
            //SSL Stuff
            this.ConnectivitySSL = false;
            this.SpeaksSSL = false;
            this.CertErrors = new List<SSLCertError>();
            this.SSLServerHeader = String.Empty;

            this.HstsHeader = String.Empty;

            //SPDY Stuff
            this.SPDYProtocols = new List<string>();
            this.ALPNProtocols = new List<string>();
            
            


            
        }


    }
}
