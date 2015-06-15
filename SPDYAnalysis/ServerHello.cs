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

namespace Zoompf.SPDYAnalysis
{
    public class ServerHello
    {

        public int VersionMajor { get; private set; }
        public int VersionMinor { get; private set; }


        /// <summary>
        /// Session Resumption ID
        /// </summary>
        public String SessionID { get; private set; }


        public List<String> NPNProtocols { get; private set; }

        public bool HasNPNExtension { get; private set; }

        public List<String> ALPNProtocols { get; private set; }

        public bool HasALPNExtension { get; private set; }


        public bool SupportsSPDY
        {
            get
            {
                foreach (String s in this.NPNProtocols)
                {
                    if (s.Contains("spdy"))
                    {
                        return true;
                    }
                }
                return false;
            }
        }



        private ServerHello()
        {
            this.SessionID = String.Empty;
            this.HasALPNExtension = false;
            this.HasNPNExtension = false;
            this.NPNProtocols = new List<string>();
            this.ALPNProtocols = new List<string>();

        }



        public static ServerHello ParseServerHello(byte[] serverHello)
        {

            ServerHello ret = new ServerHello();

            if (serverHello[0] != SERVERHELLOTYPE)
            {
                throw new ArgumentException("Invalid TLS Record type. This is not a Hello message");
            }

            ret.VersionMajor = serverHello[4];
            ret.VersionMinor = serverHello[5];

            int sessionIDLen = serverHello[38];

            if (sessionIDLen > 0)
            {
                ret.SessionID = BitConverter.ToString(serverHello,39, sessionIDLen).Replace("-", "");
            }


            //38 is offset for SessionID Length

            int offsetToExtensions = 42;
            //at [38] is our SessionID length field. Add its value 
            offsetToExtensions += (int)serverHello[38];

            int workingOffset = offsetToExtensions;

            int extLengh = readAsInt(serverHello, workingOffset, 2);
            //look for our extensions
            if (extLengh > 0)
            {
                //Console.WriteLine("Length of extentions: " + extLengh);
                //skip past length, get to 1st ext.
                workingOffset += 2;
                while (workingOffset < serverHello.Length)
                {

                    byte eb1 = serverHello[workingOffset];
                    byte eb2 = serverHello[workingOffset + 1];

                    int extDataLen = readAsInt(serverHello,workingOffset + 2, 2);

                    //found our NPN extension
                    if (eb1 == 0x33 && eb2 == 0x74)
                    {
                        ret.HasNPNExtension = true;
                        ret.NPNProtocols = parseExtensionProtocolList(serverHello, workingOffset + 4, extDataLen);
                    }

                    else if (eb1 == 0x00 && eb2 == 0x10)
                    {
                        if (extDataLen >= 2)
                        {
                            ret.HasALPNExtension = true;
                            //APLN's extension is very similar to NPN, it has an extra length for some reason after the extension length, so just skip it
                            ret.ALPNProtocols = parseExtensionProtocolList(serverHello, workingOffset + 6, extDataLen - 2);
                        }
                    }

                    workingOffset += 4 + extDataLen;
                }



            }



            return ret;
        }

        const byte SERVERHELLOTYPE = 2;


        protected static byte[] ArraySlice(byte[] array, int offset, int len)
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


        protected static int readAsInt(byte[] array, int offset, int len)
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

        /// <summary>
        /// Extracts out the list of protocols listed in the NPN extension
        /// </summary>
        private static List<String> parseExtensionProtocolList(byte[] data, int offset, int len)
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


    }



}
