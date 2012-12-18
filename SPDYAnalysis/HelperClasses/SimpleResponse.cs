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
