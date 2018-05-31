using Neo.Lux.Core;
using Neo.Lux.Cryptography;
using Neo.Lux.Debugger;
using Neo.Lux.Utils;
using Neo.Lux.VM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bluzelle.NEO.Bridge.Core
{
    public class BridgeManager : IBlockchainProvider
    {
        private NeoAPI neo_api;
        private byte[] contract_bytecode;
        private UInt160 bluzelle_contract_hash;
        private bool running;

        private SnapshotVM listenerVM;
        private ISwarm swarm;

        private KeyPair owner_keys;

        private Dictionary<UInt256, Transaction> transactions = new Dictionary<UInt256, Transaction>();

        public BridgeManager(NeoAPI api, ISwarm swarm, string ownerWIF, string contractPath)
        {
            this.neo_api = api;
            this.swarm = swarm;
            this.owner_keys = KeyPair.FromWIF(ownerWIF);

            if (File.Exists(contractPath))
            {
                this.contract_bytecode = File.ReadAllBytes(contractPath);
                this.bluzelle_contract_hash = contract_bytecode.ToScriptHash();
            }
            else
            {
                throw new Exception($"Could not find contract avm at location {contractPath}");
            }

            this.listenerVM = new SnapshotVM(this);
        }

        public void Stop()
        {
            if (running)
            {
                running = false;
            }
        }

        public void Run()
        {
            if (running)
            {
                return;
            }

            this.running = true;

            // TODO: The last block should persistent between multiple sessions, in order to not miss any block
            var lastBlock = neo_api.GetBlockHeight() - 1;

            do
            {
                var currentBlock = neo_api.GetBlockHeight();
                if (currentBlock > lastBlock)
                {
                    ProcessIncomingBlock(currentBlock);
                    lastBlock = currentBlock;
                }

                // sleeps 10 seconds in order to wait some time until next block is generated
                Thread.Sleep(10 * 1000);
            } while (running);
        }

        private void ProcessIncomingBlock(uint height)
        {
            var block = neo_api.GetBlock(height);

            if (block == null)
            {
                throw new Exception($"API failure, could not fetch block #{height}");
            }

            foreach (var tx in block.transactions)
            {
                if (tx.type != TransactionType.InvocationTransaction)
                {
                    continue;
                }

                List<AVMInstruction> ops;

                try
                {
                    ops = NeoTools.Disassemble(tx.script);
                }
                catch
                {
                    continue;
                }

                for (int i = 0; i < ops.Count; i++)
                {
                    var op = ops[i];

                    // opcode data must contain the script hash to the Bluzelle contract, otherwise ignore it
                    if (op.opcode == OpCode.APPCALL && op.data != null && op.data.Length == 20)
                    {
                        var scriptHash = new UInt160(op.data);

                        if (scriptHash != bluzelle_contract_hash)
                        {
                            continue;
                        }

                        var engine = new ExecutionEngine(tx, listenerVM, listenerVM);
                        engine.LoadScript(tx.script);

                        engine.Execute(null
                            /*x =>
                            {
                                debugger.Step(x);
                            }*/
                            );

                        ProcessNotifications(tx);
                    }
                }

            }

        }

        /// <summary>
        /// Catches and processes all notifications triggered in a Neo transaction
        /// </summary>
        /// <param name="tx"></param>
        private void ProcessNotifications(Transaction tx)
        {
            // add the transaction to the cache
            transactions[tx.Hash] = tx;

            var notifications = listenerVM.GetNotifications(tx);
            if (notifications == null)
            {
                return;
            }

            foreach (var entry in notifications)
            {
                switch (entry.Name)
                {
                    case "blz_create":{
                            if (entry.Args.Length != 3)
                            {
                                throw new Exception($"Swarm.Create expects 3 arguments");
                            }

                            var uuid = (byte[])entry.Args[0];
                            var key = (byte[])entry.Args[1];
                            var value = (byte[])entry.Args[2];

                            this.swarm.Create(uuid, key, value);
                            break;
                    }

                    case "blz_read":
                        {
                            if (entry.Args.Length != 2)
                            {
                                throw new Exception($"Swarm.Read expects 2 arguments");
                            }

                            var uuid = (byte[])entry.Args[0];
                            var key = (byte[])entry.Args[1];

                            var value = this.swarm.Read(uuid, key);

                            var push_tx = neo_api.CallContract(owner_keys, bluzelle_contract_hash, "api_push", new object[] {uuid, key, value });

                            neo_api.WaitForTransaction(owner_keys, push_tx);
                            break;
                        }

                    case "blz_update":
                        {
                            if (entry.Args.Length != 3)
                            {
                                throw new Exception($"Swarm.Update expects 3 arguments");
                            }

                            var uuid = (byte[])entry.Args[0];
                            var key = (byte[])entry.Args[1];
                            var value = (byte[])entry.Args[2];

                            this.swarm.Update(uuid, key, value);
                         
                            //  public static event Action<byte[], byte[], byte[]> OnUpdate;
                            break;
                        }

                    case "blz_delete":
                        {
                            if (entry.Args.Length != 2)
                            {
                                throw new Exception($"Swarm.Delete expects 2 arguments");
                            }

                            var uuid = (byte[])entry.Args[0];
                            var key = (byte[])entry.Args[1];

                            this.swarm.Remove(uuid, key);

                            break;
                        }

                    }
            }
        }

        /// <summary>
        /// Fetches a transaction from local catch. If not found, will try fetching it from a NEO blockchain node
        /// </summary>
        /// <param name="hash">Hash of the transaction</param>
        /// <returns></returns>
        public Transaction GetTransaction(UInt256 hash)
        {
            return transactions.ContainsKey(hash) ? transactions[hash] : neo_api.GetTransaction(hash);
        }
    }
}
