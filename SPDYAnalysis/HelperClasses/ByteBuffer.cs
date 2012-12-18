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
using System.IO;
using System.Text;


namespace Zoompf.SPDYAnalysis
{
    /// <summary>
    /// Byte Buffer. Handy class that auto resizes as you add bytes and lets you get a byte array out of it
    /// </summary>
    internal class ByteBuffer
    {
        private byte[] buffer;
        private int offset;
        private int initialCap;

        //current number of the bytes stored in the buffer
        public long Size
        {
            get
            {
                return this.offset;
            }
        }

        /// <summary>
        /// Creates a ByteBuffer
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the buffer</param>

        //start wit a 32K buffer by default;
        public ByteBuffer() : this(32768)
        {

        }

        public  ByteBuffer(int initialCapacity)
        {
            this.initialCap = initialCapacity;
            Clear();
        }

        public void Clear()
        {
            this.buffer = new byte[this.initialCap];
            this.offset = 0;
        }

        public void Append(Stream ms, int size)
        {
            byte [] tmp = new byte[size];
            ms.Read(tmp, 0, size);
            Append(tmp, size);
        }

        /// <summary>
        /// Appends a string of raw Hex digits (i.e. "0xFFAB")
        /// </summary>
        /// <param name="hexString">string of hex digits, with optional leading 0x</param>

        public void AppendHex(String hexString)
        {
            Append(HexStringToBytes(hexString));
        }

        public void Append(byte b)
        {
            byte[] array = new byte[1];
            array[0] = b;
            Append(array,1);
        }
        public void Append(byte[] array)
        {
            Append(array, array.Length);
        }

        //for when the array is not completely full of data
        public void Append(byte[] array, int size)
        {
            int existingCap = this.buffer.Length - offset;

            if (existingCap < size)
            {
                resizeByAtLeast(size);
            }
           
            Array.Copy(array, 0, this.buffer, this.offset, size);

            this.offset += size;
        }

        public byte[] ToByteArray()
        {
            //if offset was zero, then nowthing was added into the buffer
            if (offset == 0)
            {
                return null;
            }
            // Buffer is now too big. Shrink it.
            byte[] ret = new byte[offset];
            Array.Copy(this.buffer, ret, offset);
            return ret;
        }

        private void resizeByAtLeast(int size)
        {

            int increaseBy = 1024 * 1024; //1 M increase
            if (increaseBy < size)
            {
                increaseBy = size;
            }
            
            byte[] newBuffer = new byte[this.buffer.Length + increaseBy];
            
            Array.Copy(this.buffer,newBuffer, this.buffer.Length);
            this.buffer = newBuffer;

        }

        /// <summary>
        /// Converts a string of hex digits like "0xFFAB" or "00FFEE" to bytes
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        private static byte[] HexStringToBytes(string hex)
        {
            //strips the leading 0x off a hex literal
            if (hex.StartsWith("0x") && hex.Length > 2)
            {
                hex = hex.Substring(2, hex.Length - 2);
            }

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
            {
                bytes[(i / 2)] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }



    }
}
