using Android.App;
using Android.Widget;
using Android.OS;
using Android.Bluetooth;
using Android.Util;
using Android.Content;
using Android.Runtime;
using System.Collections.Generic;
using System;
using System.Diagnostics.CodeAnalysis;

namespace XamBlue.Droid
{
	[Activity(Label = "XamBlue", MainLauncher = true, Icon = "@mipmap/icon")]
	public class MainActivity : Activity, BluetoothDeviceBroadcastReceiver.ICallback
	{
		const string TAG = "MainActivity";
		const int REQUEST_ENABLE_BT = 100;
		const string PAIRING_DEVICE1_NAME = "MotoG3";
		const string PAIRING_DEVICE1_ADDR = "A4:70:D6:88:D3:75";

		int count = 1;
		BluetoothAdapter bluetoothAdapter;
		BluetoothDeviceBroadcastReceiver broadcastReceiver;



		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			// Set our view from the "main" layout resource
			SetContentView(Resource.Layout.Main);

			// Get our button from the layout resource,
			// and attach an event to it
			Button button = FindViewById<Button>(Resource.Id.myButton);

			button.Click += delegate { button.Text = $"{count++} clicks!"; };

			bluetoothAdapter = BluetoothAdapter.DefaultAdapter;
			if (bluetoothAdapter == null)
			{
				Log.Debug(TAG, "Device doesn't support Bluetooth");
			}
		}

		protected override void OnStart()
		{
			base.OnStart();

			if (bluetoothAdapter == null)
				return;

			if (!bluetoothAdapter.IsEnabled)
			{
				Intent enableBtIntent = new Intent(BluetoothAdapter.ActionRequestEnable);
				StartActivityForResult(enableBtIntent, REQUEST_ENABLE_BT);
				return;
			}

			// bluetooth is enabled
			ListBondedDevices();
		}

		protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
		{
			base.OnActivityResult(requestCode, resultCode, data);

			if (requestCode == REQUEST_ENABLE_BT && resultCode == Result.Ok)
			{
				// Find devices
				ListBondedDevices();
			}
		}

		private void ListBondedDevices()
		{
			ICollection<BluetoothDevice> pairedDevices = bluetoothAdapter.BondedDevices;

			if (pairedDevices.Count > 0)
			{
				bool foundDevice = false;
				// There are paired devices. Get the name and address of each paired device.
				foreach (BluetoothDevice device in pairedDevices)
				{
					string deviceName = device.Name;
					string deviceHardwareAddress = device.Address; // MAC address
					Log.Debug(TAG, $"(Bonded device) {deviceName} {deviceHardwareAddress}");
					if (deviceName == PAIRING_DEVICE1_NAME && deviceHardwareAddress == PAIRING_DEVICE1_ADDR)
					{
						foundDevice = true;
						break;
					}
				}

				if (foundDevice)
				{
					// connect
				}
				else
				{
					StartDiscovery();
				}
				return;
			}
			
			Log.Debug(TAG, "No bonded devices.");
			StartDiscovery();
		}

		private void StartDiscovery()
		{
			// Register for broadcasts when a device is discovered.
			IntentFilter filter = new IntentFilter(BluetoothDevice.ActionFound);
			broadcastReceiver = new BluetoothDeviceBroadcastReceiver(this);
			RegisterReceiver(broadcastReceiver, filter);

			Log.Debug(TAG, "Starting discovery");
			bluetoothAdapter.StartDiscovery();
		}

		public void FoundDevice(string deviceName, string deviceHardwareAddress)
		{
			Log.Debug(TAG, $"Found device {deviceName} {deviceHardwareAddress}");
			if (deviceName == PAIRING_DEVICE1_NAME && deviceHardwareAddress == PAIRING_DEVICE1_ADDR)
			{
				// connect
				Log.Debug(TAG, "Stopping discovery");
				UnregisterReceiver(broadcastReceiver);
				broadcastReceiver = null;
				bluetoothAdapter.CancelDiscovery();
			}
		}

		protected override void OnDestroy()
		{
			if (broadcastReceiver != null)
				UnregisterReceiver(broadcastReceiver);

			base.OnDestroy();
		}
	}

	[BroadcastReceiver (Enabled = true, Exported = true)]
	sealed public class BluetoothDeviceBroadcastReceiver : BroadcastReceiver
	{
		public interface ICallback
		{
			void FoundDevice(string name, string addr);
		}

		WeakReference<ICallback> callbackRef;

		public BluetoothDeviceBroadcastReceiver() : base()
		{ }

		public BluetoothDeviceBroadcastReceiver([NotNull] ICallback callback) : base()
		{
			callbackRef = new WeakReference<ICallback>(callback);
		}

		public override void OnReceive(Context context, Intent intent)
		{
			if (intent.Action == BluetoothDevice.ActionFound)
			{
				// Discovery has found a device. Get the BluetoothDevice
				// object and its info from the Intent.
				if (!(intent.GetParcelableExtra(BluetoothDevice.ExtraDevice) is BluetoothDevice device))
					return;
				if (callbackRef.TryGetTarget(out ICallback callback))
					callback.FoundDevice(device.Name, device.Address);
			}
		}
	}
}

