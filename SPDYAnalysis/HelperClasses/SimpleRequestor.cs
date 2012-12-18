using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.IO.Compression;


namespace Zoompf.SPDYAnalysis
{
    /// <summary>
    /// Very basic, blocking, Http requestor. Only support HEAD
    /// </summary>
    internal class SimpleRequestor
    {
        public string UserAgent;

        public int Timeout;

        public SimpleRequestor()
        {
            this.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.11 (KHTML, like Gecko) Chrome/23.0.1271.95 Safari/537.11";
            this.Timeout = 60000;
        }

        //============

        
        private HttpWebRequest CreateRequest(Uri url, string method)
        {
            // Create a request for the URL.        
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
            request.AllowAutoRedirect = true;
            request.Method = method;
            //force HTTP/1.1

            request.ProtocolVersion = HttpVersion.Version11;
            request.UserAgent = this.UserAgent;
            request.Timeout = this.Timeout;
            request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");
            request.Accept = "*/*";
            return request;
        }

        protected HttpWebResponse GetResponse(HttpWebRequest request)
        {

            HttpWebResponse webResponse = null;

            try
            {
                webResponse = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException webException)
            {

                if (webException.Status == WebExceptionStatus.NameResolutionFailure)
                {
                    return null;
                }
                else if (webException.Status == WebExceptionStatus.Timeout)
                {
                    return null;
                }
                //WebExceptions are thrown for many reasons (404s, etc). We may still
                //have a valuable response
                webResponse = (HttpWebResponse)webException.Response;

                if (webResponse == null)
                {
                    return null;
                }

            }
            catch (Exception)
            {
                return null;
            }
            return webResponse;
        }


        //===============


        public SimpleResponse Head(string url)
        {
            Uri uri = null;
            try
            {
                uri = new Uri(url);
            }
            catch (Exception)
            {
                return null;
            }
            return Head(uri);

        }


        public SimpleResponse Head(Uri url)
        {
            HttpWebRequest request = CreateRequest(url, "HEAD");
            HttpWebResponse response = GetResponse(request);
            return parseResponse(response);            
        }
      
        //=====================

        protected SimpleResponse parseResponse(HttpWebResponse response)
        {


            if (response == null)
            {
                return null;
            }

            SimpleResponse ret = new SimpleResponse(response.ResponseUri);

            byte[] bytes = ReadFully(response);

            ret.setHeaders(response.Headers);

            ret.statusCode = (int) response.StatusCode;
            ret.BodyBytes = bytes;

            return ret;

        }

        /// <summary>
        /// Reads data from a web stream, unwrapping any compression that exists
        /// </summary>
        private static byte[] ReadFully(HttpWebResponse response)
        {

            Stream stream = response.GetResponseStream();
            if (response.ContentEncoding.ToLower().Contains("gzip"))
            {
                stream = new GZipStream(stream, CompressionMode.Decompress);
            }
            else if (response.ContentEncoding.ToLower().Contains("deflate"))
            {
                stream = new DeflateStream(stream, CompressionMode.Decompress);
            }

            return ReadAllBytes(stream);
        }
        
        /// <summary>
        /// Read all the bytes from a stream
        /// </summary>
        private static byte[] ReadAllBytes(Stream stream)
        {
            ByteBuffer byteBuffer = new ByteBuffer();
            try
            {

                byte[] buffer = new byte[8192];

                int bytesRead = 0;

                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    //append them to our byte buffer
                    byteBuffer.Append(buffer, bytesRead);
                }
            }
            catch (Exception)
            {
                byteBuffer = null;
            }

            return (byteBuffer != null) ? byteBuffer.ToByteArray() : null;
        }


    }

}
