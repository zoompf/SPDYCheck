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
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Security.Authentication;

namespace Zoompf.SPDYAnalysis
{
    public class TlsHandshaker
    {

        static byte[] npnExtension = new byte[] { 0x33, 0x74, 0, 0 };

        public static ServerHello ExchangeHellos(string hostname, int port, SslProtocols protocol, int mSecTimeout = 8000)
        {
            
            TcpClient tcpClient = null;
            try
            {
                tcpClient = TimeOutSocket.Connect(hostname, port, mSecTimeout);
            }
            catch (Exception)
            {
                return null;
            }

            if (!tcpClient.Connected)
            {
                return null;
            }

            try {


                NetworkStream stream = tcpClient.GetStream();


                byte[] clientHelo = createTLSClientHello(hostname, protocol);

                //send it to the server
                stream.Write(clientHelo, 0, clientHelo.Length);
                stream.Flush();

                //read back the header
                byte[] tmp = new byte[5];

                stream.Read(tmp, 0, 5);
                
                int serverHelloLen = readAsInt(tmp, 3, 2);

                byte[] serverHelloBytes = new byte[serverHelloLen];

                stream.Read(serverHelloBytes, 0, serverHelloLen);

                stream.Close();

                return ServerHello.ParseServerHello(serverHelloBytes);

            } catch(Exception) {

                

            }

            return null;
        }



        private static byte[] createTLSClientHello(String hostname, SslProtocols protocol)
        {

            ByteBuffer buffer = new ByteBuffer();
            //build our byte array backwards

            buffer.Append(buildNPNExtension());
            buffer.Append(buildSNIRecord(hostname));
            //for now, now ALPN extension
            //buffer.Append(buildALPNExtension());

            //prepend the length of the extensions
            buffer.Prepend(toInt16(buffer.Size));

            //1 compression option, which is null compression
            buffer.PrependHex("0100");

            //our ciphers
            buffer.PrependHex("cc14cc13cc15c02bc02f009ec00ac0140039c009c0130033009c0035002f000a00ff");
            //cipher length
            buffer.Prepend(toInt16(34));

            //SessionID len = 0
            buffer.Prepend(0x00);

            //prepend random
            buffer.Prepend(buildRandomBytes());
            buffer.Prepend(buildDateTimeBytes());

            //TLS 1.2 marker
            if (protocol == SslProtocols.Tls12)
            {

                buffer.PrependHex("0303");

            }
            else if (protocol == SslProtocols.Tls11)
            {
                buffer.PrependHex("0302");

            }
            else if (protocol == SslProtocols.Tls)
            {
                buffer.PrependHex("0301");
            }
            else 
            {
                buffer.PrependHex("0300");

            }

            //length so far
            buffer.Prepend(toInt16(buffer.Size));
            //record type and 00 for the first byte of the size (stupid uint24s)
            buffer.PrependHex("0100");
            //size again

            buffer.Prepend(toInt16(buffer.Size));
            //outer record
            if (protocol == SslProtocols.Ssl3)
            {
                buffer.PrependHex(removeWhitespace("16 03 00"));
            }
            else
            {
                buffer.PrependHex(removeWhitespace("16 03 01"));
            }

            return buffer.ToByteArray();
 
        }


        //=========================== Utility functions go here!


        /// <summary>
        /// Gets the CTime bytes in proper endian-ness for use in random parameter of Client Hello
        /// </summary>
        /// <returns></returns>
        protected static byte[] buildDateTimeBytes()
        {

            byte[] ctime = BitConverter.GetBytes((int)DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
            Array.Reverse(ctime); //get endian-ness correct
            return ctime;
        }

        protected static byte[] buildRandomBytes()
        {
            byte[] random = new byte[28];
            Random r = new Random();
            r.NextBytes(random);
            return random;

        }

        /// <summary>
        /// Builds the SNI extension bytes for a hostname (SNI is the length of the hostname + 9 bytes)
        /// </summary>
        protected static byte[] buildSNIRecord(string hostname)
        {

            byte[] asciiHost = System.Text.Encoding.ASCII.GetBytes(hostname);

            ByteBuffer buffer = new ByteBuffer();
            //0x0000 to tell that is an SNI extension
            buffer.AppendHex(removeWhitespace("00 00"));
            buffer.Append(toInt16(asciiHost.Length + 5));
            buffer.Append(toInt16(asciiHost.Length + 3));
            buffer.AppendHex("00");
            buffer.Append(toInt16(asciiHost.Length));
            buffer.Append(asciiHost);

            return buffer.ToByteArray();

        }

        protected static byte[] buildNPNExtension()
        {
            return npnExtension;
        }


        protected static byte[] buildALPNExtension()
        {

            //From Chrome 
            //0000   00 10 00 1d 00 1b 08 68 74 74 70 2f 31 2e 31 08  .......http/1.1.
            //0010   73 70 64 79 2f 33 2e 31 05 68 32 2d 31 34 02 68  spdy/3.1.h2-14.h
            //0020   32                                               2

            return ByteBuffer.HexStringToBytes(removeWhitespace("00 10 00 1d 00 1b 08 68 74 74 70 2f 31 2e 31 08 73 70 64 79 2f 33 2e 31 05 68 32 2d 31 34 02 68 32"));
            
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


        protected static string removeWhitespace(string s)
        {
            StringBuilder sb = new StringBuilder(s.Length);
            foreach (char c in s)
            {
                if (!Char.IsWhiteSpace(c))
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        protected static byte[] toInt16(int num)
        {
            byte[] t = BitConverter.GetBytes((short)num);
            Array.Reverse(t);
            return t;
        }





    }
}
