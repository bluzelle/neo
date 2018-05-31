using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.ComponentModel;
using System.Numerics;

namespace Bluzelle.NEO.Contract
{
    public class BluzelleContract : SmartContract
    {
        public static readonly byte[] Admin_Address = "AHKPx5dZYnwAweQUJQH3UefoswKm6beEz2".ToScriptHash();

        [DisplayName("blz_create")]
        public static event Action<byte[], byte[], byte[]> OnCreate;

        [DisplayName("blz_read")]
        public static event Action<byte[], byte[]> OnRead;

        [DisplayName("blz_update")]
        public static event Action<byte[], byte[], byte[]> OnUpdate;

        [DisplayName("blz_delete")]
        public static event Action<byte[], byte[]> OnDelete;

        public static bool Main(string operation, object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)
            {
                if (Runtime.CheckWitness(Admin_Address))
                {
                    return true;
                }

                return false;
            }
            else
            {
                if (operation == "create")
                {
                    if (args.Length != 3) return false;
                    byte[] address = (byte[])args[0];
                    byte[] key = (byte[])args[0];
                    byte[] value = (byte[])args[0];
                    return API_Create(address, key, value);
                }

                if (operation == "read")
                {
                    if (args.Length != 2) return false;
                    byte[] address = (byte[])args[0];
                    byte[] key = (byte[])args[0];
                    return API_Read(address, key);
                }

                if (operation == "update")
                {
                    if (args.Length != 3) return false;
                    byte[] address = (byte[])args[0];
                    byte[] key = (byte[])args[0];
                    byte[] value = (byte[])args[0];
                    return API_Update(address, key, value);
                }

                if (operation == "delete")
                {
                    if (args.Length != 2) return false;
                    byte[] address = (byte[])args[0];
                    byte[] key = (byte[])args[0];
                    return API_Delete(address, key);
                }

                if (operation == "push")
                {
                    if (args.Length != 2) return false;
                    byte[] key = (byte[])args[0];
                    byte[] value = (byte[])args[0];
                    return API_Push(key, value);
                }

                if (operation == "pull")
                {
                    if (args.Length != 2) return false;
                    byte[] address = (byte[])args[0];
                    byte[] key = (byte[])args[0];
                    return API_Pull(address, key);
                }

                return false;
            }
        }

        private static bool API_Create(byte[] scripthash, byte[] key, byte[] value)
        {
            if (Runtime.CheckWitness(scripthash))
            {
                return false;
            }

            OnCreate(scripthash, key, value);

            return true;
        }

        private static bool API_Read(byte[] scripthash, byte[] key)
        {
            if (Runtime.CheckWitness(scripthash))
            {
                return false;
            }

            OnRead(scripthash, key);

            return true;
        }

        private static bool API_Update(byte[] scripthash, byte[] key, byte[] value)
        {
            if (Runtime.CheckWitness(scripthash))
            {
                return false;
            }

            OnUpdate(scripthash, key, value);

            return true;
        }

        private static bool API_Delete(byte[] scripthash, byte[] key)
        {
            if (Runtime.CheckWitness(scripthash))
            {
                return false;
            }

            OnDelete(scripthash, key);

            return true;
        }

        private static bool API_Push(byte[] key, byte[] value)
        {
            if (Runtime.CheckWitness(Admin_Address))
            {
                return false;
            }

            return true;
        }

        private static bool API_Pull(byte[] scripthash, byte[] key)
        {
            if (Runtime.CheckWitness(scripthash))
            {
                return false;
            }

            OnDelete(scripthash, key);

            return true;
        }
    }
}
