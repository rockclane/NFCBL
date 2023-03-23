using System;
using System.Runtime.Serialization;

namespace NFCBL.Models
{
    public class Item
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public string Description { get; set; }
    }
    public abstract class DataExchangeEventArgsBase
    {
        public DataExchangeEventArgsBase(Memory<byte> buffer)
        {
            Buffer = buffer;
        }

        public Memory<byte> Buffer { get; }
    }
    public class RecivedEventArgs : DataExchangeEventArgsBase
    {
        public RecivedEventArgs(Memory<byte> buffer) : base(buffer)
        {
        }
    }
    public class TransmittedEventArgs : DataExchangeEventArgsBase
    {
        public TransmittedEventArgs(Memory<byte> buffer) : base(buffer)
        {
        }
    }
    public class StateChangedEventArgs : EventArgs
    {
        public StateChangedEventArgs(ConnectionState connectionState)
        {
            ConnectionState = connectionState;
        }

        public ConnectionState ConnectionState { get; }
    }

    public enum ConnectionType
    {
        Transmitter,
        Reciver,
        Transiver
    }

    public enum ConnectionState
    {
        [EnumMember(Value = "Created...")]
        Created,
        [EnumMember(Value = "Initializing...")]
        Initializing,
        [EnumMember(Value = "Connecting...")]
        Connecting,
        [EnumMember(Value = "Connected")]
        Connected,
        [EnumMember(Value = "ErrorOccured")]
        ErrorOccured,
        [EnumMember(Value = "Reconnecting...")]
        Reconnecting,
        [EnumMember(Value = "Disconnecting...")]
        Disconnecting,
        [EnumMember(Value = "Disconnected")]
        Disconnected,
        [EnumMember(Value = "Disposing...")]
        Disposing,
        [EnumMember(Value = "Disposed")]
        Disposed
    }
}