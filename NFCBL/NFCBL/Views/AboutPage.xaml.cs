using NFCBL.Services;
using Plugin.BluetoothClassic.Abstractions;
using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace NFCBL.Views
{
    public partial class AboutPage : ContentPage
    {
		public const string ALERT_TITLE = "NFC";
		public const string MIME_TYPE = "application/vnd.bluetooth.ep.oob";
		public const string MIME_TYPE_Text = "text/plain";
		public const string MIME_TYPE_Uri = "text/plain";
        public string Pin { get; set; }
        private readonly IBluetoothAdapter _bluetoothAdapter;

		string macaddress;
		NFCNdefTypeFormat _type;
		bool _makeReadOnly = false;
		bool _eventsAlreadySubscribed = false;
		bool _isDeviceiOS = false;
		private byte[] _btDeviceAddress;
		private byte[] BtOobData { get; set; }
		NFCNdefRecord record;
		NFCNdefRecord record1;
		NFCNdefRecord record2;
		bool NFcTagDetected;


		/// <summary>
		/// Property that tracks whether the Android device is still listening,
		/// so it can indicate that to the user.
		/// </summary>
		public bool DeviceIsListening
		{
			get => _deviceIsListening;
			set
			{
				_deviceIsListening = value;
				OnPropertyChanged(nameof(DeviceIsListening));
			}
		}
		private bool _deviceIsListening;

		public bool IsMacAddressFound
		{
			get => _IsMacAddressFound;
			set
			{
				_IsMacAddressFound = value;
				OnPropertyChanged(nameof(IsMacAddressFound));
			}
		}
		private bool _IsMacAddressFound;

		private bool _nfcIsEnabled;
        private IBluetoothConnection connection;

        public bool NfcIsEnabled
		{
			get => _nfcIsEnabled;
			set
			{
				_nfcIsEnabled = value;
				OnPropertyChanged(nameof(NfcIsEnabled));
				OnPropertyChanged(nameof(NfcIsDisabled));
			}
		}

		public bool NfcIsDisabled => !NfcIsEnabled;

        public bool IsBluetoothFound { get; private set; }

        public AboutPage()
        {
			_bluetoothAdapter = DependencyService.Resolve<IBluetoothAdapter>();
			InitializeComponent();
			ReadButton.IsEnabled = false;
			WriteButton.IsEnabled = false;
		}
		protected override bool OnBackButtonPressed()
		{
			UnsubscribeEvents();
			CrossNFC.Current.StopListening();
			return base.OnBackButtonPressed();
		}
		/// <summary>
		/// Subscribe to the NFC events
		/// </summary>
		void SubscribeEvents()
		{
			if (_eventsAlreadySubscribed)
				return;

			_eventsAlreadySubscribed = true;

			CrossNFC.Current.OnMessageReceived += Current_OnMessageReceived;
			CrossNFC.Current.OnMessagePublished += Current_OnMessagePublished;
			CrossNFC.Current.OnTagDiscovered += Current_OnTagDiscovered;
			CrossNFC.Current.OnNfcStatusChanged += Current_OnNfcStatusChanged;
			CrossNFC.Current.OnTagListeningStatusChanged += Current_OnTagListeningStatusChanged;
			MessagingCenter.Unsubscribe<string, string>(this, "Bonded");
			MessagingCenter.Subscribe<string, string>(this, "Bonded", async (sender, arg) =>
			{
				
				if (!WriteButton.IsEnabled)
				     DisplayAlert("Bluetooth Device is Paired and Communication Button is enabled to use", "Name of the Device is " + arg, "OK");
				WriteButton.IsEnabled = true;
				ReadButton.IsEnabled = true;
				BlConnect.IsEnabled = true;
			});
			MessagingCenter.Unsubscribe<string, int>(this, "PinDetected");
			MessagingCenter.Subscribe<string, int>(this, "PinDetected", async (sender, arg) =>
			{
				string fmt = "000000.##";
				if (Pin != null) Pin = null;
				if(arg.ToString().Length == 6)
				   Pin = arg.ToString();
				else if(arg.ToString().Length < 6)
                {
					Pin = arg.ToString(fmt);
                    
                }
				await Publish(NFCNdefTypeFormat.External);
			});
			MessagingCenter.Unsubscribe<string, string>(this, "PowerDisconnected");
			MessagingCenter.Subscribe<string, string>(this, "PowerDisconnected", async (sender, arg) =>
			{
				
					if (connection != null)
						connection.Dispose();
					if (NFcTagDetected)
					{
						NFcTagDetected = false;
						await DisplayAlert("Removed From Cradle", arg, "OK");
					}
				
				
			});
			MessagingCenter.Unsubscribe<string, string>(this, "PowerConnected");
			MessagingCenter.Subscribe<string, string>(this, "PowerConnected", async (sender, arg) =>
			{
				if (CrossNFC.Current.IsConnectedNFC)
				{
					await ShowAlert("NFC tag Detected");
					NFcTagDetected = true;
				}

			});
			MessagingCenter.Subscribe<string, string>(this, "NotBondedEvent", async (sender, arg) =>
			{
				ReadButton.IsEnabled = false;
				WriteButton.IsEnabled = false;
				BlConnect.IsEnabled = true;
				//await DisplayAlert("Bluetooth Device is Paired and Communication Button is enabled to use", "Name of the Device is " + arg, "OK");
			});
			MessagingCenter.Unsubscribe<string, int>(this, "RssiValueChanged");

			MessagingCenter.Subscribe<string, int>(this, "RssiValueChanged", async (sender,arg) =>
            {
				Device.BeginInvokeOnMainThread(() =>
			  {
				  RssiValue.Text = arg.ToString().Trim();
			  });

			});
			if (_isDeviceiOS)
				CrossNFC.Current.OniOSReadingSessionCancelled += Current_OniOSReadingSessionCancelled;
		}

		/// <summary>
		/// Unsubscribe from the NFC events
		/// </summary>
		void UnsubscribeEvents()
		{
			CrossNFC.Current.OnMessageReceived -= Current_OnMessageReceived;
			CrossNFC.Current.OnMessagePublished -= Current_OnMessagePublished;
			CrossNFC.Current.OnTagDiscovered -= Current_OnTagDiscovered;
			CrossNFC.Current.OnNfcStatusChanged -= Current_OnNfcStatusChanged;
			CrossNFC.Current.OnTagListeningStatusChanged -= Current_OnTagListeningStatusChanged;
			MessagingCenter.Unsubscribe<string, string>(this, "Bonded");
			MessagingCenter.Unsubscribe<string, int>(this, "PinDetected");
			MessagingCenter.Unsubscribe<string, string>(this, "PowerDisconnected");
			MessagingCenter.Unsubscribe<string, string>(this, "PowerConnected");
			MessagingCenter.Unsubscribe<string, int>(this, "RssiValueChanged");


			if (_isDeviceiOS)
				CrossNFC.Current.OniOSReadingSessionCancelled -= Current_OniOSReadingSessionCancelled;
		}
		protected async override void OnAppearing()
		{
			base.OnAppearing();
			
			// In order to support Mifare Classic 1K tags (read/write), you must set legacy mode to true.
			CrossNFC.Legacy = false;

			if (CrossNFC.IsSupported)
			{
				if (!CrossNFC.Current.IsAvailable)
					await ShowAlert("NFC is not available");

				NfcIsEnabled = CrossNFC.Current.IsEnabled;
				if (!NfcIsEnabled)
					await ShowAlert("NFC is disabled");

				if (Device.RuntimePlatform == Device.iOS)
					_isDeviceiOS = true;
				
				
				SubscribeEvents();
				
				//await StartListeningIfNotiOS();
			}
		}
		/// <summary>
		/// Event raised when Listener Status has changed
		/// </summary>
		/// <param name="isListening"></param>
		void Current_OnTagListeningStatusChanged(bool isListening) => DeviceIsListening = isListening;

		/// <summary>
		/// Event raised when NFC Status has changed
		/// </summary>
		/// <param name="isEnabled">NFC status</param>
		async void Current_OnNfcStatusChanged(bool isEnabled)
		{
			NfcIsEnabled = !isEnabled;
			await ShowAlert($"NFC has been {(isEnabled ? "enabled" : "disabled")}");
		}

		/// <summary>
		/// Event raised when a NDEF message is received
		/// </summary>
		/// <param name="tagInfo">Received <see cref="ITagInfo"/></param>
		async void Current_OnMessageReceived(ITagInfo tagInfo)
		{
			NFcTagDetected = true;
			TagReader.Text = String.Empty;
			if (tagInfo == null)
			{
				await ShowAlert("No tag found");
				TagReader.Text = String.Empty;
				return;
			}

			// Customized serial number
			var identifier = tagInfo.Identifier;
			var serialNumber = NFCUtils.ByteArrayToHexString(identifier, ":");
			var title = !string.IsNullOrWhiteSpace(serialNumber) ? $"Tag [{serialNumber}]" : "Tag Info";

			if (!tagInfo.IsSupported)
			{
				await ShowAlert("Unsupported tag (app)", title);
				TagReader.Text = String.Empty;
				NFcTagDetected = false;
			}
			else if (tagInfo.IsEmpty)
			{
				await ShowAlert("Empty tag", title);
				TagReader.Text = String.Empty;
				NFcTagDetected = false;
			}
			else
			{
				if(tagInfo.Records.Count() == 1) {
					var first = tagInfo.Records[0];
					await ShowAlert(GetMessage(first), title);
				}
                else
                {
					var i = 0;
                    foreach (var item in tagInfo.Records)
                    {
						++i;
						TagReader.Text += "Record" + i + Environment.NewLine + GetMessage(item) + Environment.NewLine;
					}
                }
			}
		}

		/// <summary>
		/// Event raised when user cancelled NFC session on iOS 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void Current_OniOSReadingSessionCancelled(object sender, EventArgs e) => Debug("iOS NFC Session has been cancelled");

		/// <summary>
		/// Event raised when data has been published on the tag
		/// </summary>
		/// <param name="tagInfo">Published <see cref="ITagInfo"/></param>
		async void Current_OnMessagePublished(ITagInfo tagInfo)
		{
			try
			{
				ChkReadOnly.IsChecked = false;
				CrossNFC.Current.StopPublishing();
				//if (tagInfo.IsEmpty)
				//	await ShowAlert("Formatting tag operation successful");
				//else
				//	await ShowAlert("Writing tag operation successful");
			}
			catch (Exception ex)
			{
				await ShowAlert(ex.Message);
			}
		}

		/// <summary>
		/// Event raised when a NFC Tag is discovered
		/// </summary>
		/// <param name="tagInfo"><see cref="ITagInfo"/> to be published</param>
		/// <param name="format">Format the tag</param>
		async void Current_OnTagDiscovered(ITagInfo tagInfo, bool format)
		{
			if (!CrossNFC.Current.IsWritingTagSupported)
			{
				await ShowAlert("Writing tag is not supported on this device");
				return;
			}

			try
            {
                if (!format && (record == null && record1 == null && record2 == null))
                    throw new Exception("Record can't be null.");
				if(tagInfo != null)
                    AddTagInfoRecords(tagInfo);
				if (format)
				{
					record = null;
					record1 = null;
					record2 = null;
					CrossNFC.Current.ClearMessage(tagInfo);
				}
				else
				{
					CrossNFC.Current.PublishMessage(tagInfo, _makeReadOnly);
				}
            }
            catch (Exception ex)
			{
				await ShowAlert(ex.Message);
			}
		}

        private void AddTagInfoRecords(ITagInfo tagInfo)
        {
            if (record != null && record1 == null & record2 == null)
                tagInfo.Records = new[] { record };
            else if (record != null && record1 != null & record2 == null)
                tagInfo.Records = new[] { record, record1 };
			else if (record == null && record1 != null & record2 == null)
				tagInfo.Records = new[] {  record1 };
			else if (record != null && record1 != null & record2 != null)
                tagInfo.Records = new[] { record, record1, record2 };
        }

        /// <summary>
        /// Start listening for NFC Tags when "READ TAG" button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        async void Button_Clicked_StartListening(object sender, System.EventArgs e) => await BeginListening();

		async void Button_Clicked_Connected(object sender, System.EventArgs e) => await CheckNFCOrNot();

      

        /// <summary>
        /// Stop listening for NFC tags
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        async void Button_Clicked_StopListening(object sender, System.EventArgs e) => await StopListening();

		/// <summary>
		/// Start publish operation to write the tag (TEXT) when <see cref="Current_OnTagDiscovered(ITagInfo, bool)"/> event will be raised
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		async void Button_Clicked_StartWriting(object sender, System.EventArgs e) => await Publish(NFCNdefTypeFormat.WellKnown);

		/// <summary>
		/// Start publish operation to write the tag (URI) when <see cref="Current_OnTagDiscovered(ITagInfo, bool)"/> event will be raised
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		async void Button_Clicked_StartWriting_Uri(object sender, System.EventArgs e) => await Publish(NFCNdefTypeFormat.Bluetooth);

		/// <summary>
		/// Start publish operation to write the tag (CUSTOM) when <see cref="Current_OnTagDiscovered(ITagInfo, bool)"/> event will be raised
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		async void Button_Clicked_StartWriting_Custom(object sender, System.EventArgs e) => await Publish(NFCNdefTypeFormat.Mime);

		/// <summary>
		/// Start publish operation to format the tag when <see cref="Current_OnTagDiscovered(ITagInfo, bool)"/> event will be raised
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		async void Button_Clicked_FormatTag(object sender, System.EventArgs e) => await Publish();

		/// <summary>
		/// Task to publish data to the tag
		/// </summary>
		/// <param name="type"><see cref="NFCNdefTypeFormat"/></param>
		/// <returns>The task to be performed</returns>
		async Task Publish(NFCNdefTypeFormat? type = null)
		{
			//await StartListeningIfNotiOS();
			try
			{
				
				if (ChkReadOnly.IsChecked)
				{
					if (!await DisplayAlert("Warning", "Make a Tag read-only operation is permanent and can't be undone. Are you sure you wish to continue?", "Yes", "No"))
					{
						ChkReadOnly.IsChecked = false;
						return;
					}
					_makeReadOnly = true;
				}
				else
					_makeReadOnly = false;

				switch (type)
				{
					case NFCNdefTypeFormat.WellKnown:
							string result = await DisplayPromptAsync("Enter Pin Code", "Random Pin Number", initialValue: "Pass=1234", maxLength: 9, keyboard: Keyboard.Numeric);
						record = new NFCNdefRecord
						{
							TypeFormat = NFCNdefTypeFormat.WellKnown,
							MimeType = MIME_TYPE_Text,
							  Payload = NFCUtils.EncodeToByteArray(result),
							LanguageCode = "en"
						};
						break;
					case NFCNdefTypeFormat.Bluetooth:
						string result1 = await DisplayPromptAsync("Enter Bluetooth Mac Address", "Random Pin Number", initialValue: "Mac=F4:B8:98:04:19:87", maxLength: 21, keyboard: Keyboard.Default);
						record1 = new NFCNdefRecord
						{
							TypeFormat = NFCNdefTypeFormat.WellKnown,
							MimeType = MIME_TYPE_Text,
							Payload = NFCUtils.EncodeToByteArray(result1),
							LanguageCode = "en"
						};
						break;
					case NFCNdefTypeFormat.Mime:
						string result2 = await DisplayPromptAsync("Add ANy Text", "Anything", maxLength: 18, keyboard: Keyboard.Default);
						record2 = new NFCNdefRecord
						{
							TypeFormat = NFCNdefTypeFormat.Mime,
							MimeType = MIME_TYPE,
							Payload = NFCUtils.EncodeToByteArray(result2)
						};
						break;
					case NFCNdefTypeFormat.External:
						if (record != null) record = null;
						record = new NFCNdefRecord
						{
							TypeFormat = NFCNdefTypeFormat.WellKnown,
							MimeType = MIME_TYPE_Text,
							Payload = NFCUtils.EncodeToByteArray("Pass=" + Pin.ToString()),
							LanguageCode = "en"
						};
						break;
					default:
						break;
				}

				if (type.HasValue) _type = type.Value;
				CrossNFC.Current.StartPublishing(!type.HasValue);

			}
			catch (Exception ex)
			{
				await ShowAlert(ex.Message);
			}
		}

		
		async Task CheckNFCOrNot()
		{
			try
			{
				CrossNFC.Current.StartListening();
				if (CrossNFC.Current.IsConnectedNFC)
				{
					await ShowAlert("Connected");

				}
                else
                {
					await ShowAlert("Removed");

				}

			}
			catch (Exception ex)
			{
				await ShowAlert(ex.Message);
			}
		}


		/// <summary>
		/// Returns the tag information from NDEF record
		/// </summary>
		/// <param name="record"><see cref="NFCNdefRecord"/></param>
		/// <returns>The tag information</returns>
		string GetMessage(NFCNdefRecord record)
		{
			var message = $"RawMessage: {record?.Message}";
			if (message.Contains(':'))
			{
				//macaddress = ReadMacAddress(record.Payload);
				macaddress = record?.Message.Remove(0, 4).Trim();
				IsMacAddressFound = true;
				MacAddressData.Text =macaddress.Trim();
				message += Environment.NewLine;
				message += $"MacAddress: {record?.Message.Remove(0, 4)}";
				//message += $"Andriod Bluetooth Mac Address: {NFCUtils.ByteArrayToHexString(record.Payload, ":")}";
				//message += Environment.NewLine;
				//message += $"Bluetooth Mac Address: {macaddress}";
				//message += Environment.NewLine;
				//message += $"Generic Bluetooth Mac Address: {ParsePayloadToData(record.Payload)}";
				message += Environment.NewLine;
				message += $"Type: {record?.TypeFormat}";

				if (!string.IsNullOrWhiteSpace(record?.MimeType))
				{
					message += Environment.NewLine;
					message += $"MimeType: {record?.MimeType}";
				}
			}
            else
            {
				message += Environment.NewLine;
				message += $"Actual String : {record?.Message.Remove(0,5)}";
			}
			return message;
		}

		private string ReadMacAddress(byte[] record)
        {
			var MACs = new string[12];
			int start = 0;
			string payload = NFCUtils.ByteArrayToHexString(record);
			string MAC;
			int mac_cnt = 0;
			 MAC = new string(payload.Remove(0, 4).ToArray());
			var lenght =MAC.Length;
            MACs[mac_cnt] = MAC.Substring(10, 2) + ":";
            MACs[mac_cnt] += MAC.Substring(8, 2) + ":";
            MACs[mac_cnt] += MAC.Substring(6, 2) + ":";
            MACs[mac_cnt] += MAC.Substring(4, 2) + ":";
            MACs[mac_cnt] += MAC.Substring(2, 2) + ":";
            MACs[mac_cnt] += MAC.Substring(0, 2);
            return MACs[mac_cnt];
        }


		private string ParsePayloadToData(byte[] payload)
		{
			if (payload == null || payload.Length < 8)
			{
				throw new NullReferenceException();
			}

			// OOB Data length (2 bytes) - little endian order
			var oobLength = (payload[1] << 8) | payload[0];

			if (oobLength != payload.Length)
			{
				// Don't be strict if the encoded length does not match the payload length.
				// According to the NFC Forum Bluetooth Secure Simple Pairing Using NFC specification,
				// there was an inconsistency in the Bluetooth definitions as to what the 
				// length contains (whether to include mandatory fields). This has only
				// been cleared up with Bluetooth 4.0.
				
			}

			// Bluetooth Device address (6 bytes)
			 _btDeviceAddress = new byte[6];
			Array.Copy(payload, 2, _btDeviceAddress, 0, 6);

			// OOB Optional data (xx bytes)
			 BtOobData = new byte[payload.Length - 8];
			Array.Copy(payload, 8, BtOobData, 0, payload.Length - 8);

			return NFCUtils.ByteArrayToHexString(_btDeviceAddress, ":");
		}
		public enum OobDataTypes : byte
		{
			IncompleteList16BitServiceClassUuids = (byte)0x02,
			CompleteList16BitServiceClassUuids = (byte)0x03,
			IncompleteList32BitServiceClassUuids = (byte)0x04,
			CompleteList32BitServiceClassUuids = (byte)0x05,
			IncompleteList128BitServiceClassUuids = (byte)0x06,
			CompleteList128BitServiceClassUuids = (byte)0x07,
			ShortenedLocalName = (byte)0x08,
			CompleteLocalName = (byte)0x09,
			ClassOfDevice = (byte)0x0D,
			SimplePairingHashC = (byte)0x0E,
			SimplePairingRandomizerR = (byte)0x0F
		}


		/// <summary>
		/// Write a debug message in the debug console
		/// </summary>
		/// <param name="message">The message to be displayed</param>
		void Debug(string message) => System.Diagnostics.Debug.WriteLine(message);

		/// <summary>
		/// Display an alert
		/// </summary>
		/// <param name="message">Message to be displayed</param>
		/// <param name="title">Alert title</param>
		/// <returns>The task to be performed</returns>
		Task ShowAlert(string message, string title = null) => DisplayAlert(string.IsNullOrWhiteSpace(title) ? ALERT_TITLE : title, message, "Cancel");

		/// <summary>
		/// Task to start listening for NFC tags if the user's device platform is not iOS
		/// </summary>
		/// <returns>The task to be performed</returns>
		async Task StartListeningIfNotiOS()
		{
			if (_isDeviceiOS)
				return;
			await BeginListening();
		}

		/// <summary>
		/// Task to safely start listening for NFC Tags
		/// </summary>
		/// <returns>The task to be performed</returns>
		async Task BeginListening()
		{
			try
			{
				CrossNFC.Current.StartListening();
				if (CrossNFC.Current.IsConnected)
                {
					CrossNFC.Current.StartMessagePublished();

				}
				
			}
			catch (Exception ex)
			{
				await ShowAlert(ex.Message);
			}
		}

		/// <summary>
		/// Task to safely stop listening for NFC tags
		/// </summary>
		/// <returns>The task to be performed</returns>
		async Task StopListening()
		{
			try
			{
				CrossNFC.Current.StopListening();
			}
			catch (Exception ex)
			{
				await ShowAlert(ex.Message);
			}
		}

        private async void Button_Clicked_BluetoothConnect(object sender, EventArgs e)
        {
			if(macaddress != null)
            {
                try
                {
					//var connection = DependencyService.Get<IBTConnectionService>().OpenConnection(macaddress);

					//if (!string.IsNullOrEmpty(connection))
					//{
					//    await ShowAlert("Bluettoth Connection is susccesfully Established with the device name", connection);
					//}

					//var connection = DependencyService.Get<IBTConnectionService>().PairWithDevice(macaddress);

					//if (!string.IsNullOrEmpty(connection))
					//{
					//    await ShowAlert("Bluettoth Connection is susccesfully Established with the device name", connection);
					IsBluetoothFound = false;
					ReadButton.IsEnabled = false;
					WriteButton.IsEnabled = false;
					BlConnect.IsEnabled = false;
					//}
					var list = _bluetoothAdapter.BondedDevices;
					var deviceToPair = list.Where(x => x.Address == macaddress).FirstOrDefault();
					if (deviceToPair == null)
					{
						var connection = DependencyService.Get<IBTConnectionService>().GetBluetoothAndBondDevice(macaddress);
					}
                    else
                    {
						BlConnect.IsEnabled = true;
						IsBluetoothFound = true;
						ReadButton.IsEnabled = true;
						WriteButton.IsEnabled = true;
						 connection = _bluetoothAdapter.CreateConnection(deviceToPair);
						await connection.ConnectAsync();
					}
					//if (connection)
					//{
					//    await ShowAlert("Bluettoth Connection is susccesfully Established with the device name", connection.ToString());
					//}
					//var connection = DependencyService.Get<IBTConnectionService>().PairAndConnectViaRFComm(macaddress);


					//    await ShowAlert("Bluettoth Connection is susccesfully Established with the device name", connection.ToString());


					//var connection = _bluetoothAdapter.CreateManagedConnection(deviceToPair);

					//if (connection.ConnectionState == ConnectionState.Connected)
					//{
					//	await ShowAlert("Bluettoth Connection is susccesfully Established with the device name");
					//}
				}
                catch (Exception ex)
                {
					await ShowAlert(ex.Message);
					BlConnect.IsEnabled = true;
					ReadButton.IsEnabled = false;
					WriteButton.IsEnabled = false;
				}
            }
        }

        private async void Button_Clicked_BlueetoothMessage(object sender, EventArgs e)
        {
            try
            {
				string result1 = await DisplayPromptAsync("Enter Some message you want to Send to BL", "Random Message", initialValue: "Hello Delta", maxLength: 21, keyboard: Keyboard.Default);
				if (IsBluetoothFound)
				{
					if (connection != null)
					{
						byte[] buffer = new byte[1056];
						buffer = Encoding.ASCII.GetBytes(result1);
						try
						{
							await connection.TransmitAsync(buffer, 0, buffer.Length);
						}
						catch (Exception exception)
						{
							await DisplayAlert("Exception", exception.Message, "Close");
						}
					}
							
				}
				else
				{

				
					var connection = DependencyService.Get<IBTConnectionService>().PairAndConnectViaRFComm(macaddress, result1);
				}

			}
            catch (Exception ex)
            {

				await ShowAlert(ex.Message);
			}
			


		}

		private async void Button_Clicked_BlueetoothReadMessage(object sender, EventArgs e)
		{
            try
            {
				if (IsBluetoothFound)
				{
					
						if (await connection.RetryConnectAsync())
						{
							byte[] buffer = new byte[1064];
							try
							{
								var count = await connection.ReciveAsync(buffer, 0, buffer.Length);
								if (count > 0)
								{
								System.Array.Resize(ref buffer, count);
								var message = Encoding.ASCII.GetString(buffer);
								await DisplayAlert("Read Message",message,"Ok");
							}
							}
							catch (Exception exception)
							{
								await DisplayAlert("Exception", exception.Message, "Close");
							}
						}
						else
						{
							await DisplayAlert("Exception", "Can not connect.", "Close");
						}
					
				}
				else
				{
					var connection = DependencyService.Get<IBTConnectionService>().ReadBluetoothMessage();
					if (!string.IsNullOrEmpty(connection.Result))
					{
						await ShowAlert("SuccessFully Read the Message which is ", connection.Result);
					}
				}
			}
            catch (Exception ex)
            {

				await ShowAlert(ex.Message);
			}
			

        }

        private async void TapGestureRecognizer_Tapped(object sender, EventArgs e)
        {
			try
			{
				
					 DependencyService.Get<IBTConnectionService>().ReadBluetoothStregnth();
					
				
			}
			catch (Exception ex)
			{

				await ShowAlert(ex.Message);
			}
		}
    }


}