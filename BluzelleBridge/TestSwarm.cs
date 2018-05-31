using Bluzelle.NEO.Bridge.Core;
using System;
using System.Collections.Generic;

namespace Bluzelle.NEO.Bridge
{
    public class TestSwarm : ISwarm
    {
        private Dictionary<byte[], byte[]> storage = new Dictionary<byte[], byte[]>(new ByteArrayComparer());

        public bool Create(byte[] uuid, byte[] key, byte[] value)
        {
            if (storage.ContainsKey(key))
            {
                return false;
            }

            storage[key] = value;
            return true;
        }

        public byte[] Read(byte[] uuid, byte[] key)
        {
            return storage.ContainsKey(key) ? storage[key] : null;
        }

        public bool Remove(byte[] uuid, byte[] key)
        {
            if (storage.ContainsKey(key))
            {
                storage.Remove(key);
                return true;
            }

            return false;
        }

        public bool Update(byte[] uuid, byte[] key, byte[] value)
        {
            if (storage.ContainsKey(key))
            {
                storage[key] = value;
                return true;
            }

            return false;
        }
    }
}
