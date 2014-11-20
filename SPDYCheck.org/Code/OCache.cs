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
    /// thread-safe Object cache. Provides a generic dictionary, keyed with Strings, for objects of class K. Objects expire after fixed amount of time
    /// </summary>
    /// <typeparam name="K"></typeparam>
    public class OCache<K>
    {

        //Objects will be cached for 5 minutes by default
        private const int DefaultObjectLifetime = 5 * 60;

        /// <summary>
        /// what is the default maximum number of objects should hold. Helps limit size
        /// </summary>
        private const int DefaultMaxItems = 500;

        /// <summary>
        /// how many adds to we allow into the cache before triggering a sweep/reap of the cache for expired object.
        /// you can manually sweet/reap the cache for expired objects with the "CleanExpiredObjects()" function
        /// </summary>
        private const int AddsUntilReap = 100;

        //
        // Cache implementation
        //

        // Key-Value pairing
        private Dictionary<string, K> values;

        // Timeouts for keys
        private Dictionary<string, DateTime> expirations;


        private int maxItems;

        //object for locking
        private object locker;

        //keeps track of how many adds we have done
        private int addCounter;


        public OCache() : this(DefaultMaxItems) { }

        public OCache(int maxItems)
        {
            this.values = new Dictionary<string, K>();
            this.expirations = new Dictionary<string, DateTime>();
            this.maxItems = maxItems;
            this.locker = new object();
            this.addCounter = 0;
        }

        public K Get(String key)
        {
            // First check if the key has expired
            DateTime expiration;
            if (!this.expirations.TryGetValue(key, out expiration))
            {
                return default(K);
            }
            if (DateTime.Now > expiration)
            {
                // Yep, remove it and return default
                this.values.Remove(key);
                this.expirations.Remove(key);
                return default(K);
            }

            // Not expired, return the value
            K value;
            if (!this.values.TryGetValue(key, out value))
            {
                return default(K);
            }

            return value;
        }

        public bool ContainsKey(String key)
        {
            // Is the key even present?
            if (!this.expirations.ContainsKey(key))
                return false;

            // Has the key expired?
            if (this.expirations[key] < DateTime.Now)
            {
                Remove(key);
                return false;
            }

            // It's valid
            return true;
        }

        public bool ContainsValue(K value)
        {
            // Clear out any expired keys first
            CleanExpiredObjects();

            // Then check for value existance
            return this.values.ContainsValue(value);
        }

        public bool Remove(String key)
        {
            this.expirations.Remove(key);
            return this.values.Remove(key);
        }

        public void Add(string key, K val)
        {
            Add(key, val, DefaultObjectLifetime);

        }

        /// <summary>
        /// Refresh the expiration timestamp for the key passed in. Returns false if key not found.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="secondsTilExpires"></param>
        /// <returns></returns>
        public bool UpdateExpiration(string key, int secondsTilExpires)
        {
            if (!this.expirations.ContainsKey(key))
                return false;
            this.expirations[key] = DateTime.Now.AddSeconds(secondsTilExpires);
            return true;
        }

        /// <summary>
        /// Adds an object K into the cache for a key, and a given expiration time. If the key already exists, the object is updated
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <param name="secondsTilExpires"></param>
        public void Add(string key, K val, int secondsTilExpires)
        {

            lock (locker)
            {
                //are we too big already?
                if (this.values.Count >= this.maxItems)
                {
                    //first try to remove expired items
                    CleanExpiredObjects();

                    if (this.values.Count >= this.maxItems)
                    {
                        //if we got here, all our items are current, but we have too many.
                        //for now, just flush the entire cache
                        this.expirations.Clear();
                        this.values.Clear();
                    }
                }

                this.addCounter++;

                //is it time to do our sweep/reap? Only do it if we didn't clear
                if (this.addCounter >= AddsUntilReap)
                {
                    CleanExpiredObjects();
                    this.addCounter = 0;
                }

                this.expirations[key] = DateTime.Now.AddSeconds(secondsTilExpires);
                this.values[key] = val;
            }
        }

        /// <summary>
        /// If key is already present, replace it with new value. If not, add it.
        /// </summary>
        public void SafeUpdate(string key, K val, int secondsTilExpires)
        {
            Remove(key);
            Add(key, val, secondsTilExpires);
        }


        /// <summary>
        /// Sweep the list looking for expired objects
        /// </summary>
        public void CleanExpiredObjects()
        {
            lock (locker)
            {
                List<String> keysToDelete = new List<string>();
                //groan, no easy way to do this. We can't remove objects from the dictionary while in a foreach loop
                //so we make a note of which keys to sweep and then do it, all inside a lock for thread safety
                foreach (string key in this.expirations.Keys)
                {
                    //is it expired?
                    if (this.expirations[key] < DateTime.Now)
                    {
                        keysToDelete.Add(key);
                    }
                }
                foreach (String key in keysToDelete)
                {
                    this.expirations.Remove(key);
                    this.values.Remove(key);
                }
            }
        }


    }

}
