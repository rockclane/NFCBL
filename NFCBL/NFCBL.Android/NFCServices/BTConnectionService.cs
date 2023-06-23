using System.Text;
using System.Text.RegularExpressions;
using Android.Bluetooth;
using System;
using NFCBL.Services;
using Zebra.Sdk.Comm;
using Xamarin.Forms;
using System.Threading.Tasks;
using System.Threading;
using NFCBL.Models.Exceptions;
using System.IO;
using Android.Util;
using Android.OS;
using Xamarin.Essentials;
using Java.Util;
using System.Security.Cryptography.X509Certificates;
using Android.Content;
using Android.Content.Res;
using Javax.Net.Ssl;
using Java.Security;

[assembly: Dependency(typeof(NFCBL.Droid.NFCServices.BTConnectionService))]
namespace NFCBL.Droid.NFCServices
{
    public class BTConnectionService : IBTConnectionService
    {
        private BluetoothAdapter bluetoothAdapter;
        private BluetoothConnectionInsecure connection;
        private BluetoothConnection connection1;
        private BluetoothSocket _socket;
        private string _bluetoothDeviceAddress;
        private Context context;
        private AssetManager contextManager;
        private X509Certificate2 rootCACertificate;
        private X509Certificate2 signedCACertificate;
        private X509Certificate2 privateKey;
        public const string SppRecordUUID = "00001101-0000-1000-8000-00805F9B34FB";
        public BTConnectionService()
        {
            bluetoothAdapter = BluetoothAdapter.DefaultAdapter;
        }

        public void CloseConnection()
        {
            if ((connection != null) && (connection.Connected))
            {
                connection.Close();
            }
            connection = null;
        }
        public string OpenConnection(string address)
        {
            string result = string.Empty;
            try
            {
                if ((connection == null) || (!connection.Connected))
                {
                    connection = new BluetoothConnectionInsecure(address);
                    connection.Open();

                    result = connection.FriendlyName;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }





        //this method to check bluetooth is enable or not: true if enable, false is not enable
        public bool IsBluetoothEnabled()
        {
            bool isEnabled = false;
            BluetoothAdapter bAdapter = BluetoothAdapter.DefaultAdapter;
            if (bAdapter.IsEnabled)
            {
                // Bluetooth is not enable :)
                isEnabled = true;
            }
            return isEnabled;

        }

        //method to enable bluetooth
        public void EnableBluetooth()
        {
            BluetoothAdapter bAdapter = BluetoothAdapter.DefaultAdapter;
            if (!bAdapter.IsEnabled)
            {
                bAdapter.Enable();
            }
        }

        //method to disable bluetooth
        public void DisableBluetooth()
        {
            BluetoothAdapter bAdapter = BluetoothAdapter.DefaultAdapter;
            if (bAdapter.IsEnabled)
            {
                bAdapter.Disable();
            }
        }

        public string PairWithDevice(string address)
        {
            string result = string.Empty;
            try
            {
                if ((connection1 == null) || (!connection1.Connected))
                {
                    connection1 = new BluetoothConnection(address);
                    connection1.Open();

                    result = connection1.SimpleConnectionName + connection1.Manufacturer;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }

        public bool GetBluetoothAndBondDevice(string address)
        {
            bool result = false;
            try
            {
                BluetoothAdapter bAdapter = BluetoothAdapter.DefaultAdapter;
                _bluetoothDeviceAddress = address;
                var device = bAdapter.GetRemoteDevice(address);
                if (device != null)
                {
                    var state = device.CreateBond();
                    var callback = new MyGattCallback();
                    result = state;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }
        public async Task PairAndConnectViaRFComm(string address, string Message)
        {

            try
            {
                _bluetoothDeviceAddress = address;
                BluetoothAdapter bAdapter = BluetoothAdapter.DefaultAdapter;
                var callback = new MyGattCallback();
                var device = bAdapter.GetRemoteDevice(address);
                await CreateSocketAndConnectAsync(device, Message);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private async Task CreateSocketAndConnectAsync(BluetoothDevice device, string message)
        {
           // LoadSSLCertificates();

           // TrustSSLCertificatesWithSSLStream();

            var socket = device.CreateRfcommSocketToServiceRecord(UUID.FromString(SppRecordUUID));

            if (socket == null)
            {
                throw new Exception(
                    $"Can not connect to the remote bluetooth device with address:  Can not create RFCOMM socket.");
            }

            try
            {
                await socket.ConnectAsync();
                _socket = socket;
                byte[] bytes = Encoding.ASCII.GetBytes(message);
                await TransmitAsync(bytes);
                if (DataAvailable)
                {
                    await ReciveAsync(bytes);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Can not connect to the remote bluetooth device with address: \"{ex.Message}\". Can not connect to the RFCOMM socket.");
            }
        }

        //private void TrustSSLCertificatesWithSSLStream()
        //{
        //    // Create an SSL context
        //    var sslContext = SSLContext.GetInstance("TLS");

        //    // Initialize the SSL context
        //    var keyManagerFactory = KeyManagerFactory.GetInstance(KeyManagerFactory.DefaultAlgorithm);
        //    var keyStore = KeyStore.GetInstance("PKCS12");

        //    keyStore.Load(context.Resources.Assets.Open("privateKey.pfx"), "password".ToCharArray());
        //    keyManagerFactory.Init(keyStore, "password".ToCharArray());

        //    var trustManagerFactory = TrustManagerFactory.GetInstance(TrustManagerFactory.DefaultAlgorithm);
        //    var trustStore = KeyStore.GetInstance(KeyStore.DefaultType);
            
        //    trustStore.Load(null, null);
        //    trustStore.SetCertificateEntry("root_ca", rootCACertificate);
        //    trustStore.SetCertificateEntry("signed_ca", signedCACertificate);
        //    trustManagerFactory.Init(trustStore);

        //    sslContext.Init(keyManagerFactory.GetKeyManagers(), trustManagerFactory.GetTrustManagers(), null);

        //}

        private void LoadSSLCertificates()
        {
             contextManager = Android.App.Application.Context.Assets;

            var assetManager = Android.App.Application.Context.Assets;
            var certificateBytes = new byte[0];
            var certificateBytes1 = new byte[0];
            var certificateBytes2 = new byte[0];


            using (var certificateStream = assetManager.Open("ca.crt"))
            using (var memoryStream = new MemoryStream())
            {
                certificateStream.CopyTo(memoryStream);
                certificateBytes = memoryStream.ToArray();
            }

            using (var certificateStream1 = assetManager.Open("handheld.crt"))
            using (var memoryStream = new MemoryStream())
            {
                certificateStream1.CopyTo(memoryStream);
                certificateBytes1 = memoryStream.ToArray();
            }

            using (var certificateStream2 = assetManager.Open("handheld.key"))
            using (var memoryStream = new MemoryStream())
            {
                certificateStream2.CopyTo(memoryStream);
                certificateBytes2 = memoryStream.ToArray();
            }
            // Load the Root CA
            rootCACertificate = new X509Certificate2(certificateBytes);

            // Load the Signed CA
           signedCACertificate = new X509Certificate2(certificateBytes1);

            // Load the private key
            privateKey = new X509Certificate2(certificateBytes2);

        }

        public bool IsConnected
        {
            get => (connection != null && connection.Connected);
        }

        public async Task TransmitAsync(Memory<byte> buffer,
          CancellationToken cancellationToken = default)
        {
            ValidateSocket();
            try
            {
                await _socket.OutputStream.WriteAsync(buffer, cancellationToken);
            }
            catch (Exception exception)
            {
                throw new BluetoothTransmitException(
                    $"Can not transmit data to the device with address: \"{_bluetoothDeviceAddress}\"",
                    exception);
            }
        }

        private void ValidateSocket()
        {
            if (_socket == null)
            {
                throw new BluetoothConnectionException("Can not transmit/recive data because connection is not opened. Plase, use \"Task ConnectAsync()\" method before.");
            }
        }

        public bool DataAvailable
        {
            get
            {
                ValidateSocket();
                try
                {
                    return _socket.InputStream.IsDataAvailable();
                }
                catch (Exception exception)
                {
                    throw new BluetoothReciveException(
                        $"Can not recive is data available for the device with address: \"{_bluetoothDeviceAddress}\"",
                        exception);
                }
            }
        }

        public async Task<int> ReciveAsync(Memory<byte> buffer,
         CancellationToken cancellationToken = default)
        {
            ValidateSocket();
            try
            {
                return await _socket.InputStream.ReadAsync(buffer, cancellationToken);
            }
            catch (Exception exception)
            {
                throw new BluetoothReciveException(
                    $"Can not recive data from the device with address: \"{_bluetoothDeviceAddress}\"",
                    exception);
            }
        }
        public void Dispose()
        {
            try
            {
                if (_socket != null)
                {
                    _socket.Close();
                    _socket = null;
                }
            }
            catch (Exception exception)
            {
                Log.Warn("Dispose::Exception", exception.Message);
            }
        }

        public async Task<string> ReadBluetoothMessage()
        {
            var buffer = new byte[1064];
            var numberRead = await _socket.InputStream.ReadAsync(buffer);
            System.Array.Resize(ref buffer, numberRead);
            var message = Encoding.UTF8.GetString(buffer);
            return message;
        }
        public void ReadBluetoothStregnth()
        {
            BluetoothDevice device = null;
            BluetoothAdapter bAdapter = BluetoothAdapter.DefaultAdapter;
            var devices = bAdapter.BondedDevices;
            foreach (var pairedDevice in devices)
            {
                if (pairedDevice.Address == _bluetoothDeviceAddress)
                {
                    device = pairedDevice;
                }
                Console.WriteLine(
                    $"Found device with name: {pairedDevice.Name} and MAC address: {pairedDevice.Address}");
            }
            var callback = new MyGattCallback();

            var gatt = device?.ConnectGatt(Platform.CurrentActivity, false, callback);

            var initialRssi = gatt.ReadRemoteRssi();

        }
        private double CalculateDistance(int rssi)
        {
            int txPower = -59; // The transmit power of the device in dBm
            double ratio = rssi * 1.0 / txPower;
            if (ratio < 1.0)
            {
                return Math.Pow(ratio, 10);
            }
            else
            {
                double distance = 0.89976 * Math.Pow(ratio, 7.7095) + 0.111;
                return distance;
            }

        }
    }
}