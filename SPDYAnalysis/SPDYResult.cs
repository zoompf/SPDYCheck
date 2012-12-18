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


            //SPDY Stuff
            this.SPDYProtocols = new List<string>();
            
            


            
        }


    }
}
