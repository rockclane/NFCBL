using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Xamarin.Essentials;


namespace NFCBL.Services
{
    public  class SSLtream
    {
        // Load the digital certificates for this device and the other device
    //    var thisDeviceCert = await Certificate.GetClientCertificateAsync();
    //    var otherDeviceCert = await Certificate.GetIntermediateCertificateAsync();

    //    // Connect to the other Bluetooth device
    //    var device = await Bluetooth.GetPairedDevicesAsync();
    //    var bluetoothDevice = device.FirstOrDefault(d => d.Name == "Other Device");
    //    var serviceUuid = new Guid("00001101-0000-1000-8000-00805F9B34FB"); // Bluetooth serial port service UUID
    //    var service = await bluetoothDevice.GetServiceAsync(serviceUuid);
    //    var characteristic = await service.GetCharacteristicAsync(serviceUuid);

    //    // Create an SslStream with the Bluetooth input and output streams and authenticate the other device's digital certificate
    //    var sslStream = new SslStream(characteristic.InputStream.AsStream(), false, ValidateCertificate);
    //    sslStream.AuthenticateAsClient("other-device.com", new X509Certificate2Collection(otherDeviceCert), SslProtocols.Tls12, false);

    //// Send data to the other device over the secure connection
    //var data = System.Text.Encoding.UTF8.GetBytes("Hello, other device!");
    //    sslStream.Write(data);
    //sslStream.Flush();

    //// Receive data from the other device over the secure connection
    //var buffer = new byte[4096];
    //    var bytesRead = sslStream.Read(buffer, 0, buffer.Length);
    //    var receivedData = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);

    //    // Close the connection
    //    sslStream.Close();
    //characteristic.Close();
        
// Callback function to validate the other device's digital certificate
private static bool ValidateCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                // The other device's digital certificate is valid
                return true;
            }
            else
            {
                // The other device's digital certificate is not valid
                return false;
            }
        }
    }
}