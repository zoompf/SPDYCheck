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

            SPDYResult result = new SPDYResult(host);

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

                //if we don't support SPDY, grab the Server header for additional analysis
                if (!result.SupportsSPDY)
                {
                    //check port 80...
                    requestor.Timeout = 9000;
                    resp = requestor.Head("https://" + host + ":" + port + "/");
                    if (resp != null)
                    {
                        result.SSLServerHeader = resp.GetHeaderValue("Server");
                    }
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
