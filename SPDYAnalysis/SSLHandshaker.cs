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
using System.IO;
using System.Net.Sockets;
using System.Net;

namespace Zoompf.SPDYAnalysis
{

    /// <summary>
    /// Do the SSL handshake with the server, collecting information about the server hello
    /// </summary>
    public class SSLHandshaker
    {

        public List<String> SPDYProtocols { get; private set; }

        public bool HasNPNExtension;

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

  
        private string hostname;
        private int port;


        public SSLHandshaker(string hostname, int port)
        {
            this.hostname = hostname;
            this.port = port;
            
            this.SPDYProtocols = new List<string>();
            this.HasNPNExtension = false;
        }




        private static byte[] ArraySlice(byte[] array, int offset, int len)
        {
            if (array == null)
            {
                return null;
            }
            if (offset + len > array.Length)
            {
                return null;
            }
            byte[] tmp = new byte[len];
            Array.Copy(array, offset, tmp, 0, len);
            return tmp;
        }

        /// <summary>
        /// Extracts out the list of protocols listed in the NPN extension
        /// </summary>
        private static List<String> readNPNProtocols(byte[] data, int offset, int len)
        {
            List<String> ret = new List<string>();
            int curr = offset;
            while (curr < offset + len)
            {
                //read length
                int stringLen = (int)data[curr];
                ret.Add(System.Text.Encoding.ASCII.GetString(data, curr + 1, stringLen));
                curr += 1 + stringLen;
            }

            return ret;
        }

        private static int readAsInt(byte[] array, int offset, int len)
        {
            if (array == null)
            {
                return -1;
            }
            if (offset + len > array.Length)
            {
                return -1;
            }

            byte[] tmp = new byte[len];
            Array.Copy(array, offset, tmp, 0, len);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(tmp);
            }
            if (len == 2)
            {
                return BitConverter.ToUInt16(tmp, 0);
            }

            return -1;

        }


        public void Check(int mSecTimeout)
        {
            TcpClient tcp = null;
            try
            {
                tcp = TimeOutSocket.Connect(hostname, port, mSecTimeout);
            }
            catch (Exception)
            {
                return;
            }

            if (!tcp.Connected)
            {
                return;
            }


            NetworkStream stream = tcp.GetStream();

            ByteBuffer buffer = new ByteBuffer();

            byte[] clientHelo;

            clientHelo = SSLClientHello.BuildMessage(hostname);

            stream.Write(clientHelo, 0, clientHelo.Length);
            stream.Flush();

            byte[] tmp = new byte[5];

            stream.Read(tmp, 0, 5);
            //TODO: Sanity checking here
            int len = readAsInt(tmp, 3, 2);

            buffer.Append(stream, len);

            stream.Close();

            tmp = buffer.ToByteArray();

            int offsetToCompress = 41;
            //at [38] is our SessionID length field. Add its value to get the offset to the compression field
            offsetToCompress += (int)tmp[38];

            int workingOffset = offsetToCompress + 1;


            int extLengh = readAsInt(tmp, workingOffset, 2);
            //look for our extensions
            if (extLengh > 0)
            {
                //Console.WriteLine("Length of extentions: " + extLengh);
                //skip past length, get to 1st ext.
                workingOffset += 2;
                while (workingOffset < tmp.Length)
                {
                    byte[] extensionHeader = ArraySlice(tmp, workingOffset, 4);
                    if (extensionHeader == null)
                    {
                        //Console.WriteLine("extension was null!");
                        break;
                    }

                    int extDataLen = readAsInt(extensionHeader, 2, 2);

                    //found our NPN extension
                    if (extensionHeader[0] == 0x33 && extensionHeader[1] == 0x74)
                    {
                        this.HasNPNExtension = true;
                        SPDYProtocols = readNPNProtocols(tmp, workingOffset + 4, extDataLen);
                    }


                    workingOffset += 4 + extDataLen;
                }



            }




        }



    }
}
