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

namespace Zoompf.SPDYAnalysis
{
    /// <summary>
    /// Constructs a raw SSL Client Hello message.
    ///     -Includes current DateTime in standard Unix CTIME style format
    ///     -Includes SNI extension with proper hostname
    ///     -Includes NPN/SPDY support extension
    /// </summary>
    public class SSLClientHello
    {

        /// <summary>
        /// Builds our SSL Client Hello byte array
        /// </summary>
        /// <param name="hostname"></param>
        /// <returns></returns>
        public static byte [] BuildMessage(string hostname)
        {

            ByteBuffer buffer = new ByteBuffer();


            byte[] sniRecord = BuildSNI(hostname);

            //ORiginal
            
//            buffer.Append(prepare(@"
//             16 03 01 00 B5 01 00 00 B1 03 01 4F CE BB 9C 3C
//            58 4B 33 D8 00 F3 7D A2 B1 53 2D 4C D2 72 92 F0
//            60 28 8E 2D 86 BF 7F E7 0D 8F BF 00 00 48 C0 0A
//            C0 14 00 88 00 87 00 39 00 38 C0 0F C0 05 00 84
//            00 35 C0 07 C0 09 C0 11 C0 13 00 45 00 44 00 66
//            00 33 00 32 C0 0C C0 0E C0 02 C0 04 00 96 00 41
//            00 05 00 04 00 2F C0 08 C0 12 00 16 00 13 C0 0D
//            C0 03 FE FF 00 0A 02 01 00 00 3F 00 00 00 13 00
//            11 00 00 0E 77 77 77 2E 67 6F 6F 67 6C 65 2E 63
//            6F 6D FF 01 00 01 00 00 0A 00 08 00 06 00 17 00
//            18 00 19 00 0B 00 02 01 00 00 23 00 00 33 74 00
//            00 00 05 00 05 01 00 00 00 00
//            "));

            
            

            //no extensions besides SPDY
            //had to shorten 3 lengths (2 handshake lengths and the extensions length)
            //includes accurate unix CTIME as part of random value
                
            
            //SNI record adds 9 + hostname.Length to size.
            //122 is standard length now (7A)
            //118

            buffer.AppendHex(prepare(@"16 03 01"));
            buffer.Append(toInt16(122 + sniRecord.Length));
            buffer.AppendHex(prepare("01 00"));
            buffer.Append(toInt16(118 + sniRecord.Length));
            buffer.AppendHex(prepare("03 01"));
            
            byte [] ctime = BitConverter.GetBytes((int) DateTime.Now.Subtract(new DateTime(1970,1,1)).TotalSeconds);
            Array.Reverse(ctime); //get endian-ness correct
            buffer.Append(ctime);
            buffer.AppendHex(prepare(@"3C
            58 4B 33 D8 00 F3 7D A2 B1 53 2D 4C D2 72 92 F0
            60 28 8E 2D 86 BF 7F E7 0D 8F BF 00 00 48 C0 0A
            C0 14 00 88 00 87 00 39 00 38 C0 0F C0 05 00 84
            00 35 C0 07 C0 09 C0 11 C0 13 00 45 00 44 00 66
            00 33 00 32 C0 0C C0 0E C0 02 C0 04 00 96 00 41
            00 05 00 04 00 2F C0 08 C0 12 00 16 00 13 C0 0D
            C0 03 FE FF 00 0A 02 01 00"));

            //now the extension length
            //this is the size of the SNI extension plus 4 for the NPN extension

            buffer.Append(toInt16(sniRecord.Length + 4));
            //append in our sni
            buffer.Append(sniRecord);
            //NPN tickler
            buffer.AppendHex(prepare("33 74 00 00"));


            return buffer.ToByteArray();
        }

        private static string prepare(string s)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in s)
            {
                if (!Char.IsWhiteSpace(c))
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private static byte[] toInt16(int num)
        {
            byte [] t = BitConverter.GetBytes((short)num);
            Array.Reverse(t);
            return t;
        }


        /// <summary>
        /// Builds the SNI extension bytes for a hostname (SNI is the length of the hostname + 9 bytes)
        /// </summary>
        private static byte[] BuildSNI(string hostname)
        {

            byte[] asciiHost = System.Text.Encoding.ASCII.GetBytes(hostname);

            ByteBuffer buffer = new ByteBuffer();
            //0x0000 to tell that is an SNI extension
            buffer.AppendHex(prepare("00 00"));
            buffer.Append(toInt16(asciiHost.Length + 5));
            buffer.Append(toInt16(asciiHost.Length + 3));
            buffer.AppendHex(prepare("00"));
            buffer.Append(toInt16(asciiHost.Length));
            buffer.Append(asciiHost);

            return buffer.ToByteArray();


        }




    }
}
