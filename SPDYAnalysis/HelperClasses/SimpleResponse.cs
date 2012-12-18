using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;


namespace Zoompf.SPDYAnalysis
{
    /// <summary>
    /// Very simple web response
    /// </summary>
    public class SimpleResponse
    {
        public Uri ResponseURL;

        public int statusCode;

        public NameValueCollection Headers;
        public byte[] BodyBytes;

        public String HeadersAsString
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (String header in Headers.Keys)
                {
                    sb.AppendLine(header + ": " + Headers[header]);
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// poor-mans way to look at the response bytes. Assumes ASCII, which is already for
        /// the basic HEAD and TRACE/TRACK verbs we may use here
        /// </summary>
        public String BodyAsTest
        {
            get
            {
                //not explicitly set, so guess
                if (this.BodyBytes != null)
                {
                    return System.Text.Encoding.ASCII.GetString(BodyBytes);
                }

                return String.Empty;
            }
        }




        public SimpleResponse()
            : this(null)
        {

        }

        public SimpleResponse(Uri finalUrl)
        {
            this.ResponseURL = finalUrl;
            this.BodyBytes = null;
            this.Headers = new NameValueCollection();
        }
        
        /// <summary>
        /// Normalizes the header names
        /// </summary>
        /// <param name="headers"></param>
        public void setHeaders(NameValueCollection headers)
        {
            foreach (string header in headers.Keys)
            {
                if (!String.IsNullOrEmpty(headers[header].Trim()))
                {
                    this.Headers.Add(header.ToLower(), headers[header]);
                }
            }
        }

        public String GetHeaderValue(String header)
        {
            foreach (String key in Headers.Keys)
            {
                if (key.ToLower() == header.ToLower())
                {
                    return this.Headers[key];
                }
            }
            return String.Empty;
        }

    }
}
