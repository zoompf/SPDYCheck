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
using System.Text;
using System.Text.RegularExpressions;

using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;
using System.Threading;

namespace Zoompf.SPDYAnalysis
{


    public enum SSLCertError
    {
        None,
        NotYetActive,
        Expired,
        Generic,
        IncorrectHost,
        SelfSigned,
    }



    /// <summary>
    /// Determines if a server is reachable, if it speaks SSL, and if the X.509 cert is valid
    /// </summary>
    public class SSLInspector
    {
        

        public bool ConnectivityWorks { get; private set; }

        public bool SpeaksSSL { get; private set; }

        public SslProtocols ProtocolUsed { get; private set; }

        public List<SSLCertError> CertificateErrors { get; private set; }

        //working set to avoid eventing issues
        private List<SSLCertError> working;

        public bool HasCertificateErrors
        {
            get
            {
                return this.CertificateErrors.Count > 0;
            }
        }

        private static Regex cnNameExtractor = new Regex("\\bCN=([a-zA-Z0-9\\-\\.]+)\\b", RegexOptions.Compiled);
        private object locker;
        private string host;
        private int port;


        public SSLInspector(string host, int port)
        {
            this.locker = new object();
            this.host = host;
            this.port = port;
            this.ConnectivityWorks = false;
            this.SpeaksSSL = false;
            this.CertificateErrors = new List<SSLCertError>();
            this.working = new List<SSLCertError>();
        }

        private string getNormalizedCN(string id)
        {
            Match match = cnNameExtractor.Match(id);
            if (!match.Success)
            {
                return String.Empty;
            }
            return match.Groups[1].Value.ToLower();
        }

        public void Inspect(int mSecTimout)
        {
            this.ProtocolUsed = SslProtocols.None;
            this.ConnectivityWorks = false;
            this.CertificateErrors = new List<SSLCertError>();
            this.SpeaksSSL = false;

            TcpClient client = null;
            try {
                
                client = TimeOutSocket.Connect(host, port, mSecTimout);
            } catch(Exception) {
                
                return;
            }
            this.ConnectivityWorks = client.Connected ;

            if (!this.ConnectivityWorks)
            {
                return;
            }

            RemoteCertificateValidationCallback callback = new RemoteCertificateValidationCallback(OnCertificateValidation);
            SslStream stream = new SslStream(client.GetStream(), false, callback);
             
            try{

                    stream.AuthenticateAsClient(host); //blocks
                    System.Threading.Thread.Sleep(100); //wait for good measure
            }
            catch (System.Net.Sockets.SocketException)
            {
                this.ConnectivityWorks = false;
                return;
            }
            catch (Exception)
            {
                //connection open, but not valid SSL
                this.ConnectivityWorks = true;
                return;
            }

            SpeaksSSL = true;
            this.ProtocolUsed = stream.SslProtocol;
            stream.Close();
            

            lock(locker) {
                try
                {
                    //there are weird conditions where the OnVertificate validation event has not fired yet, so we could get the errors collection modified
                    //if we are not careful. Wrap it to be safe
                    foreach (SSLCertError e in this.working)
                    {
                        this.CertificateErrors.Add(e);
                    }
                }
                catch (Exception)
                {


                }
            }
            
     

        }


        private bool OnCertificateValidation(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {

            if (errors != SslPolicyErrors.None)
            {

                lock (locker)
                {

                    //It is issued for the host we are on?
                    if (getNormalizedCN(certificate.Subject) != this.host.ToLower())
                    {
                        //names are mismatched!
                        this.working.Add(SSLCertError.IncorrectHost);
                    }

                    //self signed?
                    if (getNormalizedCN(certificate.Subject) == getNormalizedCN(certificate.Issuer))
                    {
                        this.working.Add(SSLCertError.SelfSigned);
                    }

                    //expired or not yet active?
                    DateTime startDate = DateTime.Parse(certificate.GetEffectiveDateString());
                    DateTime endDate = DateTime.Parse(certificate.GetExpirationDateString());
                    if (DateTime.Now < startDate)
                    {
                        this.working.Add(SSLCertError.NotYetActive);
                    }
                    else if (DateTime.Now > endDate)
                    {
                        this.working.Add(SSLCertError.Expired);
                    }

                    //if we have gotten here but not yet added an error, its generic
                    if (this.working.Count == 0)
                    {
                        this.working.Add(SSLCertError.Generic);
                    }
                }
            }
            //always return true to avoid throwing an exception
            return true;
        }



    }

}
