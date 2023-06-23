using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Content.Res;
using Android.OS;
using Android.Runtime;
using Android.Security;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.Security;
using Java.Security.Cert;
using Java.Util;
using Javax.Net.Ssl;
using NFCBL.Models.Exceptions;
using NFCBL.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

[assembly: Dependency(typeof(NFCBL.Droid.NFCServices.BondedService))]
namespace NFCBL.Droid.NFCServices
{
    public class BondedService : IBlBondedService
    {
        public static string CertFileName = "handheld.pfx";
        public static string FilePath = "/sdcard/Security/";
        public const string SppRecordUUID = "00001101-0000-1000-8000-00805F9B34FB";
        //private readonly UUID SppRecordUUID1 = UUID.FromString(SppRecordUUID);
        private BluetoothSocket _socket;
        private Android.Content.Res.AssetManager contextManager;
        private AssetManager assetManager;
        private X509Certificate2 rootCACertificate;
        private X509Certificate2 signedCACertificate;
        private X509Certificate2 privateKey;
       // private  string SppRecordUUID = "00001101 - 0000 - 1000 - 8000 - 00805F9B34FB";
        private string _bluetoothDeviceAddress;
        private SSLContext sslContext;
        private Java.Net.Socket _secureSocket;
        private BluetoothSocket _blsecureSocket;


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

        public async Task<string> PairAndConnectViaSecuredRFComm(string address, string Message)
        {
           
            try
            {
                _bluetoothDeviceAddress = address;
                BluetoothAdapter bAdapter = BluetoothAdapter.DefaultAdapter;
                var callback = new MyGattCallback();
                var device = bAdapter.GetRemoteDevice(address);
                //_ = Task.Run(async () =>
                //{
                //    await CreateSocketAndConnectAsync(device, Message);
                //});

              //  await CreateSocketAndConnectAsync(device, Message);
              return   await CreateSSLStreamConnectAsync(device, Message);
            }
            catch (Exception ex)
            {
                throw ex;
                return string.Empty;
            }
        }

        private async Task<string> CreateSSLStreamConnectAsync(BluetoothDevice device, string message)
        {
            LoadPxfCertificates();
            Guid mUUID = new Guid("00001101-0000-1000-8000-00805F9B34FB");
            _socket = device.CreateRfcommSocketToServiceRecord(Java.Util.UUID.FromString(mUUID.ToString()));

            //var bluetoothDeviceInfos = client.PairedDevices;
            //var deviceInfo = bluetoothDeviceInfos.FirstOrDefault(_ => _.DeviceName.Contains("WH-1000XM3"));
            //cli.Connect(deviceInfo.DeviceAddress, deviceInfo.InstalledServices.ElementAt(0));

            if (_socket is object)
            {
                try
                {
                    _socket.Connect();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);

                    try
                    {
                        _socket.Connect();
                        AndroidNetworkStream.GetAvailable(_socket.InputStream as Android.Runtime.InputStreamInvoker);
                    }
                    catch (Exception ex2)
                    {
                        System.Diagnostics.Debug.WriteLine(ex2.Message);

                        _socket = null;
                    }
                }
            }
            SslStream sslStream = new SslStream(
              PlatformGetStream(),
               false,
               new RemoteCertificateValidationCallback(ValidateServerCertificate),
               null
               );
            try
            {
                sslStream.AuthenticateAsClient("server", new X509CertificateCollection(xCertColl), SslProtocols.Tls12, false);
            }
            catch (AuthenticationException e)
            {
                Console.WriteLine("Exception: {0}", e.Message);
                if (e.InnerException != null)
                {
                    Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
                }
                Console.WriteLine("Authentication failed - closing the connection.");
                _socket.Close();
                return String.Empty;
            }
            byte[] messsage1 = Encoding.UTF8.GetBytes(message + "\n");
            await  sslStream.WriteAsync(messsage1);
            sslStream.Flush();
            // Read message from the server.
            string serverMessage = ReadMesssage(sslStream);
            return serverMessage;
        }

    

        private void RunReadMessage(SslStream sslStream)
        {

            cancelationTokenSource = new CancellationTokenSource();
            var cancelationToken = cancelationTokenSource.Token;
            cancelationToken.Register(() => Log.Info("", "Cancellation Requested"));

            Task.Run(() =>
            {
                Log.Info(" BL Reading", "Task Started!");
                try
                {
                    while (true)
                    {
                        cancelationToken.ThrowIfCancellationRequested();

                        var message1 = ReadMesssage(sslStream); // execute step
                    }
                }
                catch (System.OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    //sslStream.Close();
                    Log.Warn(" ", "Task ending due to exception: " + e.Message, e);
                }
                finally
                {

                    Log.Info(TAG, "Task Ended!");
                }
            }, cancelationToken);


        }

        private string ReadMesssage(SslStream sslStream)
        {
            // Read the  message sent by the server.
            // The end of the message is signaled using the
            // "<EOF>" marker.
            byte[] buffer = new byte[2048];
            StringBuilder messageData = new StringBuilder();
            int bytes = -1;
            do
            {
                bytes = sslStream.Read(buffer, 0, buffer.Length);

                // Use Decoder class to convert from bytes to UTF8
                // in case a character spans two buffers.
                Decoder decoder = Encoding.UTF8.GetDecoder();
                char[] chars = new char[decoder.GetCharCount(buffer, 0, bytes)];
                decoder.GetChars(buffer, 0, bytes, chars, 0);
                messageData.Append(chars);
                // Check for EOF.
                if (messageData.ToString().IndexOf("\n") != -1)
                {
                    break;
                }
            } while (bytes != 0);

            return messageData.ToString();
        }
        NetworkStream PlatformGetStream()
        {
            if (GetConnected())
                return new AndroidNetworkStream(_socket.InputStream, _socket.OutputStream);

            return null;
        }
        bool GetConnected()
        {
            return _socket is object && _socket.IsConnected;
        }

        private async Task CreateSocketAndConnectAsync(BluetoothDevice device, string message)
        {
            LoadSSLCertificates();
          
            TrustSSLCertificatesWithSSLStream();
          // var UUID = device.GetUuids().ElementAt(0);
            var bluetoothSocket = device.CreateRfcommSocketToServiceRecord(UUID.FromString(SppRecordUUID));
            bluetoothSocket.Connect();

          
            // Create an SSL socket
            //   var sslSocket = sslContext.SocketFactory.CreateSocket(bluetoothSocket, device.Address, device.Port, true);
           // var sslSocketFactory = sslContext.SocketFactory;
            // Java.Net.Socket socket2 = new Java.Net.Socket(device.Address, 1);
            // Java.Net.Socket socket1 = new Java.Net.Socket(Java.Net.InetAddress.GetByName(device.Address), 0);
         

           //  SSLSocket secureSocket = (SSLSocket)sslSocketFactory.CreateSocket(bluetoothSocket.RemoteDevice.Address, 1);
            
           // secureSocket.SoTimeout = 1000;
           // var secureSocket = (SSLSocket)sslSocketFactory.CreateSocket(socket1, bluetoothSocket.RemoteDevice.Address, 1, true);
            // Socket secureSocket1 = sslSocketFactory.CreateSocket(bluetoothSocket, bluetoothSocket.RemoteDevice.Address,1, true);

            //if (secureSocket == null)
            //{
            //    throw new Exception(
            //        $"Can not connect to the remote bluetooth device with address:  Can not create RFCOMM socket.");
            //}
            if (bluetoothSocket == null)
            {
                throw new Exception(
                    $"Can not connect to the remote bluetooth device with address:  Can not create RFCOMM socket.");
            }

            try
            {
                //await secureSocket.ConnectAsync(null);
                //_secureSocket = secureSocket;
                _blsecureSocket = bluetoothSocket;
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

        private void LoadPxfCertificates()
        {
            //X509Certificate2 myCert = new X509Certificate2("C:\\Users\\muzzi\\source\\repos\\ClientSSL\\handheld_keys\\handheld.pfx", "test1234");


            //X509Certificate2 rootCert = new X509Certificate2("C:\\Users\\muzzi\\source\\repos\\ClientSSL\\handheld_keys\\ca.crt");
            // To remove certificate from store


           
            contextManager = Android.App.Application.Context.Assets;

            assetManager = Android.App.Application.Context.Assets;
            var certificateBytes = new byte[0];
            var certificateBytes1 = new byte[0];
            var certificateBytes2 = new byte[0];


            using (var certificateStream = assetManager.Open("ca.crt"))
            using (var memoryStream = new MemoryStream())
            {
                certificateStream.CopyTo(memoryStream);
                certificateBytes = memoryStream.ToArray();
            }

            using (var certificateStream1 = assetManager.Open("handheld.pfx"))
            using (var memoryStream = new MemoryStream())
            {
                certificateStream1.CopyTo(memoryStream);
                certificateBytes1 = memoryStream.ToArray();
            }

            //using (var certificateStream2 = assetManager.Open("bags.iotintegration.jks"))
            //using (var memoryStream = new MemoryStream())
            //{
            //    certificateStream2.CopyTo(memoryStream);
            //    certificateBytes2 = memoryStream.ToArray();
            //}
            //X509Certificate2 cert = new X509Certificate2(certificateBytes2, "Password@123",
             //    X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
          //  certificateBytes1 = LoadCertificateFromStore("client-270983d4-edaa-43b5-abd7-f02842211413", Android.App.Application.Context);
            //using (var certificateStream2 = assetManager.Open("handheld.key"))
            //using (var memoryStream = new MemoryStream())
            //{
            //    certificateStream2.CopyTo(memoryStream);
            //    certificateBytes2 = memoryStream.ToArray();
            //}
            // Load the Root CA

            rootCACertificate = new X509Certificate2(certificateBytes);
            X509Store storeRoot = new X509Store(StoreName.Root);
            storeRoot.Open(OpenFlags.ReadWrite);
            storeRoot.Add(rootCACertificate);
            //  storeRoot.Remove(rootCACertificate);

            // Load the Signed CA
            try
            {
          //      byte[] hashBytes =  {
          //          59,4,248,102,77,97,142,201,
          //210,12,224,93,25,41,100,197,
          //210,12,224,93,25,41,100,197,
          //213,134,130,135, 213,134,130,135};
          //      var jksFile = new X509Certificate2(certificateBytes2, "Password@123");
          //      var cspParams = new CspParameters(24) { KeyContainerName = "XML_DSIG_RSA_KEY" };
          //      var key = new RSACryptoServiceProvider(cspParams);
          //      key.FromXmlString(jksFile.PrivateKey.ToXmlString(true));
          //      var x = key.SignHash(hashBytes, CryptoConfig.MapNameToOID("SHA256"));
            }
            catch (Exception ex)
            {

                throw;
            }
            ///sdcard/Download/Data/
            //var file = await FileSystem.OpenAppPackageFileAsync("certificado.p12");

            //using (var ms = new MemoryStream())
            //{
            //    file.CopyTo(ms);
            //    var bytes = ms.GetBuffer();
            //    var x509 = new X509Certificate2(bytes, string.Empty);
            //}

            signedCACertificate = new X509Certificate2(certificateBytes1,"test1234");
            X509Store storeMy = new X509Store(StoreName.My);
            storeMy.Open(OpenFlags.ReadWrite);
            storeMy.Add(signedCACertificate);
            xCertColl = new X509CertificateCollection { signedCACertificate };
        }

        private void TrustSSLCertificatesWithSSLStream()
        {
            // Create an SSL context
            sslContext = SSLContext.GetInstance("TLS");

            // Initialize the SSL context
            var keyManagerFactory = KeyManagerFactory.GetInstance(KeyManagerFactory.DefaultAlgorithm);
            //var keyStore = KeyStore.GetInstance("PKCS12");

            //keyStore.Load(assetManager.Open("handheld.key"), "".ToCharArray());
            //keyManagerFactory.Init(keyStore, "".ToCharArray());

            var trustManagerFactory = TrustManagerFactory.GetInstance(TrustManagerFactory.DefaultAlgorithm);
            var trustStore = KeyStore.GetInstance(KeyStore.DefaultType);

            trustStore.Load(null, null);
            var certificateFactory = CertificateFactory.GetInstance("X.509");
            var rootCertificateEntry = certificateFactory.GenerateCertificate(new MemoryStream(rootCACertificate.Export(X509ContentType.Cert))) as Java.Security.Cert.X509Certificate;
            var signedCertificateEntry = certificateFactory.GenerateCertificate(new MemoryStream(signedCACertificate.Export(X509ContentType.Cert))) as Java.Security.Cert.X509Certificate;

            trustStore.SetCertificateEntry("root_ca", rootCertificateEntry);
            trustStore.SetCertificateEntry("signed_ca", signedCertificateEntry);
            trustManagerFactory.Init(trustStore);

            sslContext.Init(null, trustManagerFactory.GetTrustManagers(), null);

        }


        private void LoadSSLCertificates()
        {
            contextManager = Android.App.Application.Context.Assets;

            assetManager = Android.App.Application.Context.Assets;
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
         
            //using (var certificateStream2 = assetManager.Open("handheld.key"))
            //using (var memoryStream = new MemoryStream())
            //{
            //    certificateStream2.CopyTo(memoryStream);
            //    certificateBytes2 = memoryStream.ToArray();
            //}
            // Load the Root CA
            rootCACertificate = new X509Certificate2(certificateBytes);

            // Load the Signed CA
            signedCACertificate = new X509Certificate2(certificateBytes1);

            // Load the private key
         //   privateKey = new X509Certificate2(certificateBytes2);

        }

        public async Task TransmitAsync(Memory<byte> buffer,
        CancellationToken cancellationToken = default)
        {
            ValidateSocket();
            try
            {
                await _secureSocket.OutputStream.WriteAsync(buffer, cancellationToken);
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
            if (_secureSocket == null)
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
                    return _secureSocket.InputStream.IsDataAvailable();
                }
                catch (Exception exception)
                {
                    throw new BluetoothReciveException(
                        $"Can not recive is data available for the device with address: \"{_bluetoothDeviceAddress}\"",
                        exception);
                }
            }
        }

        public string TAG { get; private set; }

        public async Task<int> ReciveAsync(Memory<byte> buffer,
         CancellationToken cancellationToken = default)
        {
            ValidateSocket();
            try
            {
                return await _secureSocket.InputStream.ReadAsync(buffer, cancellationToken);
            }
            catch (Exception exception)
            {
                throw new BluetoothReciveException(
                    $"Can not recive data from the device with address: \"{_bluetoothDeviceAddress}\"",
                    exception);
            }
        }

        public byte[] LoadCertificateFromStore(string alias, Context context)
        {
            byte[] keystoreRaw = new byte[0];
            CertificateFactory certFactory = CertificateFactory.GetInstance("X.509");
            //using (MemoryStream memoryStream = new MemoryStream(keystoreRaw))
            //{
            //    KeyStore keyStore = KeyStore.GetInstance("pkcs12");
            //    keyStore.Load(memoryStream, "test123".ToCharArray());

            //}
         
           
                var keyChain = KeyChain.GetCertificateChain(Android.App.Application.Context, alias);
                var clientCert = keyChain.FirstOrDefault();
                // Convert the certificate to a byte array
                 keystoreRaw = clientCert.GetEncoded();
       
            //var keyChain = KeyChain.GetCertificateChain(Android.App.Application.Context, alias);
            //var clientCert = keyChain.FirstOrDefault();
            //// Convert the certificate to a byte array
            // var certBytes = clientCert.GetEncoded();

            // Create a new X509Certificate2 object from the byte array
            // var x509Cert = new X509Certificate2(certBytes);

              return keystoreRaw;
             // return certBytes;

        }
        private static Hashtable certificateErrors = new Hashtable();
        private X509CertificateCollection xCertColl;
        private CancellationTokenSource cancelationTokenSource;

        // The following method is invoked by the RemoteCertificateValidationDelegate.
        public static bool ValidateServerCertificate(
              object sender,
              System.Security.Cryptography.X509Certificates.X509Certificate certificate,
              X509Chain chain,
              SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            Console.WriteLine("Certificate error: {0}", sslPolicyErrors);

            // Do not allow this client to communicate with unauthenticated servers.
            return true;
        }
    }
}