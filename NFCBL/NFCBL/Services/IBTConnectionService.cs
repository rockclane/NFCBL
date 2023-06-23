using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NFCBL.Services
{
    public interface IBTConnectionService
    {
        /// <summary>
        /// Opens an connection to the bluetooth printer.
        /// </summary>
        /// <param name="address">the printer blutooth address.</param>
        string OpenConnection(string address);

        string PairWithDevice(string address);

        /// <summary>
        /// Closes the connection to bluetooth printer.
        /// </summary>
        void CloseConnection();


        bool IsBluetoothEnabled();

        /// <summary>
        /// Enables the bluetooth.
        /// </summary>
        void EnableBluetooth();

        /// <summary>
        /// Disables the bluetooth.
        /// </summary>
        void DisableBluetooth();
        Task PairAndConnectViaRFComm(string address,string message);
        bool GetBluetoothAndBondDevice(string address);
        Task<string> ReadBluetoothMessage();
     
        bool IsConnected { get; }
        void ReadBluetoothStregnth();
    }
}
