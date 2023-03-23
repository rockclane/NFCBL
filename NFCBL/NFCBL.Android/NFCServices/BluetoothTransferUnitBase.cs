using Android.Bluetooth;
using NFCBL.Models;
using Plugin.BluetoothClassic.Abstractions;
using System;
using System.Threading;
using Xamarin.Forms.Internals;
using ConnectionState = NFCBL.Models.ConnectionState;

namespace NFCBL.Droid.NFCServices
{
    internal abstract class BluetoothDataTransferUnitBase
    {
        protected const int BufferSizeDefault = 16;
        protected const int BufferOffsetZero = 0;
        protected const int BufferOffsetDefault = BufferOffsetZero;

        protected ConcurrentValue<Models.ConnectionState> _connectionStateWrapper;
        protected ConcurrentValue<BluetoothSocket> _bluetoothSocketWrapper;
        protected int _bufferSize;
        protected CancellationToken _cancellationToken;

        public BluetoothDataTransferUnitBase(
            ConcurrentValue<ConnectionState> connectionStateWrapper,
            ConcurrentValue<BluetoothSocket> bluetoothSocketWrapper,
            CancellationToken cancellationToken = default,
            int bufferSize = BufferSizeDefault)
        {
            _connectionStateWrapper = connectionStateWrapper;
            _bluetoothSocketWrapper = bluetoothSocketWrapper;
            _bufferSize = bufferSize;
            _cancellationToken = cancellationToken;

            StartUnitThread();
        }

        protected abstract void StartUnitThread();

        protected Models.ConnectionState ConnectionState => _connectionStateWrapper.Value;

        protected BluetoothSocket BluetoothSocket => _bluetoothSocketWrapper.Value;

        protected void HandleException(string category, Exception exception)
        {
            LogWarning(category, exception);
           // RaiseErrorEvent(exception);
        }

        //public event Error OnError;

        //private void RaiseErrorEvent(Exception exception)
        //{
        //    OnError?.Invoke(this, new ThreadExceptionEventArgs(exception));
        //}

        protected static void LogWarning(string category, Exception exception)
        {
            string message = exception.Message;
            LogWarning(category, message);
        }

        protected static void LogWarning(string category, string message)
        {
            Log.Warning(category, message);
        }


    }
}