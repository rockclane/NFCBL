using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using NFCBL.Droid.NFCServices;
using Android.Content;
using Android.Nfc;
using Android.Bluetooth;
using Android;
using Xamarin.Forms;
using Xamarin.Essentials;

namespace NFCBL.Droid
{
    [Activity(Label = "NFCBL", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true,LaunchMode =LaunchMode.SingleInstance, ScreenOrientation = ScreenOrientation.Portrait, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize )]
    [IntentFilter(new[] { NfcAdapter.ActionNdefDiscovered }, Categories = new[] { "android.intent.category.DEFAULT" }, DataMimeType = "*/*")]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        BroadcastReceiver mReceiver;
        BluetoothPairingReceiver mPairingRequestReceiver;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            CrossNFC.Init(this);

            IntentFilter filter1 = new IntentFilter(BluetoothDevice.ActionAclConnected);
            IntentFilter filter2 = new IntentFilter(BluetoothDevice.ActionAclDisconnectRequested);
            IntentFilter filter3 = new IntentFilter(BluetoothDevice.ActionAclDisconnected);
            IntentFilter filter4 = new IntentFilter(BluetoothDevice.ActionPairingRequest);
            IntentFilter filter5 = new IntentFilter(BluetoothDevice.ActionBondStateChanged);
            IntentFilter filter6 = new IntentFilter(BluetoothDevice.ActionClassChanged);
            IntentFilter filter7 = new IntentFilter(BluetoothDevice.ActionFound);
            IntentFilter filter8 = new IntentFilter(BluetoothDevice.ActionUuid);
            IntentFilter filter9 = new IntentFilter(BluetoothDevice.ActionAliasChanged);
            IntentFilter filter10 = new IntentFilter(BluetoothDevice.ActionNameChanged);
            IntentFilter filter11 = new IntentFilter(Intent.ActionPowerConnected);
            IntentFilter filter12 = new IntentFilter(Intent.ActionPowerDisconnected);
            IntentFilter filter13 = new IntentFilter(Intent.ActionBatteryChanged);






            mReceiver = new BTReceiver();
            this.RegisterReceiver(mReceiver, filter1);
            this.RegisterReceiver(mReceiver, filter2);
            this.RegisterReceiver(mReceiver, filter3);
            this.RegisterReceiver(mReceiver, filter4);
            this.RegisterReceiver(mReceiver, filter5);
            this.RegisterReceiver(mReceiver, filter6);
            this.RegisterReceiver(mReceiver, filter7);
            this.RegisterReceiver(mReceiver, filter8);
            this.RegisterReceiver(mReceiver, filter9);
            this.RegisterReceiver(mReceiver, filter10);
            this.RegisterReceiver(mReceiver, filter11);
            this.RegisterReceiver(mReceiver, filter12);
            this.RegisterReceiver(mReceiver, filter13);



            this.RequestPermissions(new string[] { Manifest.Permission.ReadExternalStorage }, 0);
            this.RequestPermissions(new string[] { Manifest.Permission.WriteExternalStorage }, 0);

            if (this.ApplicationContext.CheckSelfPermission("android.permission.ACCESS_COARSE_LOCATION") != Permission.Granted)
            {
                this.RequestPermissions(new string[] { Manifest.Permission.AccessCoarseLocation }, 11);

            }
            if (this.ApplicationContext.CheckSelfPermission("android.permission.ACCESS_COARSE_LOCATION") == Permission.Granted)
            {
                mReceiver = new BluetoothDeviceReceiver();
                this.RegisterReceiver(mReceiver, new IntentFilter(BluetoothAdapter.ActionDiscoveryStarted));
                this.RegisterReceiver(mReceiver, new IntentFilter(BluetoothAdapter.ActionRequestDiscoverable));
                this.RegisterReceiver(mReceiver, new IntentFilter(BluetoothDevice.ActionFound));
                this.RegisterReceiver(mReceiver, new IntentFilter(BluetoothAdapter.ActionDiscoveryFinished));
                this.RegisterReceiver(mReceiver, new IntentFilter(BluetoothAdapter.ActionConnectionStateChanged));
                this.RegisterReceiver(mReceiver, new IntentFilter(BluetoothAdapter.ActionScanModeChanged));
                this.RegisterReceiver(mReceiver, new IntentFilter(BluetoothAdapter.ActionStateChanged));


                //mPairingRequestReceiver = new BluetoothPairingReceiver();
                //this.RegisterReceiver(mPairingRequestReceiver, new IntentFilter(BluetoothDevice.ActionPairingRequest));
            }
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            LoadApplication(new App());
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
        protected override void OnResume()
        {
            base.OnResume();
            CrossNFC.OnResume();
        }

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            CrossNFC.OnNewIntent(intent);
        }
    }

    public class BTReceiver : BroadcastReceiver
    {

        public override void OnReceive(Context context, Intent intent)
        {
            String action = intent.Action;

            if (BluetoothDevice.ActionAclConnected.Equals(action))
            {
                BluetoothDevice device = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);
                var callback = new MyGattCallback();
                var gatt = device.ConnectGatt(Platform.CurrentActivity, false, callback);
                var initialRssi = gatt.ReadRemoteRssi();
            }
            else if (BluetoothDevice.ActionAclDisconnected.Equals(action))
            {
                //Do something if disconnected, like close the connection and update the printer connected status

            }
            else if (BluetoothDevice.ActionPairingRequest.Equals(action))
            {
                try
                {
                    BluetoothDevice device = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);
                    int pin = intent.GetIntExtra("android.bluetooth.device.extra.PAIRING_KEY", 1234);
                    //the pin in case you need to accept for an specific pin
                    // Log.d(TAG, "Start Auto Pairing. PIN = " + intent.getIntExtra("android.bluetooth.device.extra.PAIRING_KEY", 1234));
                    byte[] pinBytes;
                    pinBytes = BitConverter.GetBytes(pin);
                    device.SetPin(pinBytes);
                    //setPairing confirmation if neeeded
                    // device.SetPairingConfirmation(true);
                }
                catch (Exception e)
                {
                    throw e;
                }
                //else if...
            }
            else if (BluetoothDevice.ActionBondStateChanged.Equals(action))
            {
                try
                {
                    BluetoothDevice device = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);
                 
                    var bondState = device.BondState;
                    switch (bondState)
                    {
                        case Bond.Bonding:

                            break;
                        case Bond.Bonded:
                            MessagingCenter.Send<string, string>((string)this, "Bonded", device.Name);
                           
                            break;
                        case Bond.None:
                            MessagingCenter.Send<string, string>((string)this, "NotBondedEvent", device?.Name);
                            //the pin in case you need to accept for an specific pin
                            // Log.d(TAG, "Start Auto Pairing. PIN = " + intent.getIntExtra("android.bluetooth.device.extra.PAIRING_KEY", 1234));
                            //byte[] pinBytes;
                            //pinBytes = BitConverter.GetBytes(pin);

                            //setPairing confirmation if neeeded
                            // device.SetPairingConfirmation(true);

                            break;
                        default:
                            //device.SetPairingConfirmation(true);
                            break;
                    }
                }

                catch (Exception e)
                {
                    throw e;
                }
                //else if...
            }
            else if (Intent.ActionPowerDisconnected.Equals(action))
            {
                MessagingCenter.Send<string, string>((string)this, "PowerDisconnected", "Power is Disconnted Check ");
            }
            else if (Intent.ActionPowerConnected.Equals(action)) {
                MessagingCenter.Send<string, string>((string)this, "PowerConnected", "Power is Connected  Check ");
            }
        }
    }
     
    public class BluetoothDeviceReceiver : BroadcastReceiver
    {

        public override void OnReceive(Context context, Intent intent)
        {
           
            var action = intent.Action;
            var device = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);

            if (action == BluetoothDevice.ActionFound && !string.IsNullOrEmpty(device.Name) && device.Name.Contains("IP30"))
            {
                
            }
            else if (action == BluetoothAdapter.ActionDiscoveryFinished) { }
               

        }
    }

    [BroadcastReceiver]
    [IntentFilter(new[] { BluetoothDevice.ActionPairingRequest }, Priority = 1000)]
    public class BluetoothPairingReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            int pairingRequestType = 0;
            var action = intent.Action;
            var device = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);

            if (action == BluetoothDevice.ActionPairingRequest)
            {
                device = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);
                pairingRequestType = intent.GetIntExtra(BluetoothDevice.ExtraPairingVariant, BluetoothDevice.Error);
                switch (pairingRequestType)
                {
                    case BluetoothDevice.PairingVariantPasskeyConfirmation:
                        int pin1 = intent.GetIntExtra("android.bluetooth.device.extra.PAIRING_KEY", 1234);
                        // var method =  (device.GetType().GetMethod("CreateBond", null));
                        //  method.Invoke(device, null);
                       // InvokeAbortBroadcast();
                      //  device.SetPairingConfirmation(true);
                        break;
                    case BluetoothDevice.PairingVariantPin:
                        device.SetPin(new byte[] { 0x30, 0x30, 0x30, 0x30 });
                        break;
                    case 4:
                        int pin = intent.GetIntExtra("android.bluetooth.device.extra.PAIRING_KEY", 1234);
                        InvokeAbortBroadcast();
                        MessagingCenter.Send<string, int>((string)this, "PinDetected", pin);
                      
                        //the pin in case you need to accept for an specific pin
                        // Log.d(TAG, "Start Auto Pairing. PIN = " + intent.getIntExtra("android.bluetooth.device.extra.PAIRING_KEY", 1234));
                        //byte[] pinBytes;
                        //pinBytes = BitConverter.GetBytes(pin);

                        //setPairing confirmation if neeeded
                        // device.SetPairingConfirmation(true);

                        break;
                    default:
                        //device.SetPairingConfirmation(true);
                        break;
                }

            }
        }
    }

    public class MyGattCallback : BluetoothGattCallback
    {
        public override void OnConnectionStateChange(BluetoothGatt gatt, GattStatus status, ProfileState newState)
        {
            base.OnConnectionStateChange(gatt, status, newState);

            if (newState == ProfileState.Disconnected)
            {
                // disconnected
            }
        }

        public override void OnReadRemoteRssi(BluetoothGatt gatt, int rssi, [GeneratedEnum] GattStatus status)
        {
            base.OnReadRemoteRssi(gatt, rssi, status);
            MessagingCenter.Send<string, int>((string)this, "RssiValueChanged", rssi);
        }
    }

}
