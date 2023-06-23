using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NFCBL.Services
{
    public interface IBlBondedService
    {

        bool GetBluetoothAndBondDevice(string address);

        Task<string> PairAndConnectViaSecuredRFComm(string address, string Message);
    }
}
