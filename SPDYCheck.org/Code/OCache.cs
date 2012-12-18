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
        private int maxAge;
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
            //has item expired?
            if (DateTime.Now > i.two)
            {
                this.objects.Remove(key);
                return default(K);
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
            if (this.objects.Count > this.maxItems)
            {
                this.objects.Clear();
            }
            this.objects[key] = new Tupal<K, DateTime>(val, DateTime.Now.AddSeconds(secondsTilExpires));

        }


    }


}
