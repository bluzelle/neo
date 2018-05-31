using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bluzelle.NEO.Bridge.Core
{
    public interface ISwarm
    {
        bool Create(byte[] uuid, byte[] key, byte[] value);
        byte[] Read(byte[] uuid, byte[] key);

        bool Update(byte[] uuid, byte[] key, byte[] value);
        bool Remove(byte[] uuid, byte[] key);
    }
}
