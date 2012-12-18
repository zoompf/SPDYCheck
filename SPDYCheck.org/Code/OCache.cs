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
using System.Text;


namespace Zoompf.General.Collections
{
    
    /// <summary>
    /// Generic Object cache. Items expire automatically, cache will never be larger than provided amount
    /// </summary>
    /// <typeparam name="K">Type of object cache will hold</typeparam>
    public class OCache<K>
    {

        //Objects will be cached for 5 minutes by default
        private const int DefaultObjectLifetime = 5 * 60;

        private Dictionary<string, Tupal<K, DateTime>> objects;
        private object locker;
        private int maxItems;

        public OCache() : this(100) { }

        public OCache(int maxItems)
        {
            this.objects = new Dictionary<string, Tupal<K, DateTime>>();
            this.maxItems = maxItems;
        }

        public K Get(String key)
        {
            Tupal<K, DateTime> i;
            if(!this.objects.TryGetValue(key, out i))
            {
                return default(K);
            }
            lock (locker)
            {
                //has item expired?
                if (DateTime.Now > i.two)
                {
                    this.objects.Remove(key);
                    return default(K);
                }
            }
            return i.one;
        }

        //enforce the limits
        public void Add(string key, K val)
        {
            Add(key, val, DefaultObjectLifetime);

        }

        public void Add(string key, K val, int secondsTilExpires)
        {
            lock (locker)
            {
                if (this.objects.Count > this.maxItems)
                {
                    this.objects.Clear();
                }
            }
            this.objects[key] = new Tupal<K, DateTime>(val, DateTime.Now.AddSeconds(secondsTilExpires));

        }


    }


}
