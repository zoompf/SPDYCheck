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

using System.Net;

namespace Zoompf.SPDYAnalysis
{
    public static class SPDYChecker
    {

        public static SPDYResult Test(string host, int port, int mSecTimeout)
        {

            SPDYResult result = new SPDYResult(host, port);

            SimpleRequestor requestor = new SimpleRequestor();

            SimpleResponse resp = null;

            //check #1, do they have SSL?
            SSLInspector inspector = new SSLInspector(host, port);

            inspector.Inspect(mSecTimeout);

            
            result.ConnectivitySSL = inspector.ConnectivityWorks;
            result.SpeaksSSL = inspector.SpeaksSSL;
            
       
            if(inspector.SpeaksSSL) {

                //gather info on the SSL cert errors
                result.CertErrors.AddRange(inspector.CertificateErrors);

                //probe the SSL port for SPDY support and compression info
                SSLHandshaker handshaker = new SSLHandshaker(host, port);

                handshaker.Check(mSecTimeout);
                result.HasNPNExtension = handshaker.HasNPNExtension;
                result.SPDYProtocols.AddRange(handshaker.SPDYProtocols);

                //lets check the HTTP headers of the SSL website, to get the server header and test for HSTS support.
                requestor.Timeout = 9000;
                resp = requestor.Head("https://" + host + ":" + port + "/");
                if (resp != null)
                {
                    result.SSLServerHeader = resp.GetHeaderValue("Server");
                    result.HstsHeader = resp.GetHeaderValue("Strict-Transport-Security");
                }
                
               
            }

            //check to see what a request to port 80 does
            requestor.Timeout = 9000;
            resp = requestor.Head("http://" + host + "/");
            if (resp != null)
            {
                result.ConnectivityHTTP = true;
             
                //if an SSL server exists
                if (result.SpeaksSSL)
                {
                    //does a request to 80 automatically redirect us?
                    if (resp.ResponseURL.Scheme == "https" && resp.ResponseURL.Host.ToLower().Contains(host))
                    {
                        result.RedirectsToSSL = true;
                    }
                }
                else
                {
                    //no SSL, so grab the Server header if its there to determine if the server might be able to support SPDY
                    result.HTTPServerHeader = resp.GetHeaderValue("Server");
                }
            }

            return result;

        }






    }
}
