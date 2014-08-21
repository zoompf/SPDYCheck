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
using System.Text;

namespace Zoompf.SPDYAnalysis
{

    public class SPDYResult
    {

        public string Hostname = String.Empty;
        
        
        
        //SSL Stuff
        
        public bool ConnectivitySSL = false;
        public bool SpeaksSSL = false;
        public List<SSLCertError> CertErrors;
        public bool HasNPNExtension = false;


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
                string[] parts = this.HstsHeader.Split('=');
                if (parts.Length != 2)
                {
                    return 0;
                }
                try
                {
                    return Convert.ToInt32(parts[1]);
                }
                catch (Exception)
                {

                }
                return 0;
            }
        }


        public SPDYResult(string hostname)
        {
            this.Hostname = hostname;

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
            
            


            
        }


    }
}
