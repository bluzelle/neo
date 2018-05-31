using System;
using System.IO;

using NUnit.Framework;
using System.Linq;
using Neo.Lux.Core;
using Neo.Lux.Utils;
using Neo.Lux.Cryptography;
using Neo.SmartContract;
using System.Diagnostics;
using System.Numerics;
using Neo.Lux.Debugger;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bluzelle.NEO.Contract;

namespace Bluzelle.NEO.Tests
{
    public class TestEnviroment
    {
        public readonly Emulator api;
        public readonly KeyPair owner_keys;
        public readonly KeyPair admin_keys;
        public readonly DebugClient debugger;

        public TestEnviroment()
        {
            debugger = new DebugClient();

            // this is the key for the NEO "issuer" in the virtual chain used for testing
            owner_keys = KeyPair.GenerateAddress();

            this.api = new Emulator(owner_keys);

            this.api.SetLogger(x => {
                if (api.Chain.HasDebugger)
                {
                    debugger.WriteLine(x);
                }
                Debug.WriteLine(x);
            });

            Transaction tx;

            // create a random key for the team
            admin_keys = KeyPair.GenerateAddress();

            // since the real admin address is hardcoded in the contract, use BypassKey to give same permissions to this key
            this.api.Chain.BypassKey(new UInt160(BluzelleContract.Admin_Address), new UInt160(admin_keys.address.AddressToScriptHash()));

            tx = api.SendAsset(owner_keys, admin_keys.address, "GAS", 800);
            Assert.IsNotNull(tx);

            var balances = api.GetAssetBalancesOf(admin_keys.address);
            Assert.IsTrue(balances.ContainsKey("GAS"));
            Assert.IsTrue(balances["GAS"] == 800);

            tx = api.DeployContract(admin_keys, ContractTests.contract_script_bytes, "0710".HexToBytes(), 5, ContractPropertyState.HasStorage, "Bluzelle", "1.0", "http://bluzelle.com", "info@bluzelle.com", "Bluzelle Smart Contract");
            Assert.IsNotNull(tx);
        }
    }

    [TestFixture]
    public class ContractTests
    {
        public static byte[] contract_script_bytes { get; set; }
        public static UInt160 contract_script_hash { get; set; }

        private string contract_folder;

        [OneTimeSetUp]
        public void FixtureSetUp()
        {
            var temp = TestContext.CurrentContext.TestDirectory.Split(new char[] { '\\', '/' }).ToList();

            for (int i = 0; i < 3; i++)
            {
                temp.RemoveAt(temp.Count - 1);
            }

            temp.Add("BluzelleContract");
            temp.Add("bin");
            temp.Add("Debug");

            contract_folder = String.Join("\\", temp.ToArray());

            contract_script_bytes = File.ReadAllBytes(contract_folder + "/BluzelleContract.avm");
            contract_script_hash = contract_script_bytes.ToScriptHash();

            Assert.IsNotNull(contract_script_bytes);
        }

        [Test]
        public void TestCore()
        {
            var env = new TestEnviroment();

            //Assert.IsTrue(TODO);
        }
    }
}
