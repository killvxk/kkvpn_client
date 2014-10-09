using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace kkvpn_client.Engine
{
    class DriverConnector
    {
        const uint EVENT_ALL_ACCESS = (0x000F0000 | 0x00100000 | 0x00000003);
        const uint IOCTL_REGISTER = ((0x12) << 16) | ((0x1) << 2);
        const uint IOCTL_RESTART = ((0x12) << 16) | ((0x2) << 2);
        const uint INFINITE = 0xFFFFFFFF;
        const int BUFFER_SIZE = 0x4000;

        #region Structures

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct KKDRV_FILTER_DATA
        {
            public uint low;
            public uint high;
            public uint local;
        }

        //[StructLayout(LayoutKind.Sequential, Pack = 1)]
        //struct KKDRV_NET_BUFFER_FLAT
        //{
        //    public UInt32 length;
        //    public byte[] buffer;
        //}

        #endregion Structures

        #region Unmanaged code imports

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern SafeFileHandle CreateFile(
            string fileName,
            [MarshalAs(UnmanagedType.U4)] FileAccess fileAccess,
            [MarshalAs(UnmanagedType.U4)] FileShare fileShare,
            IntPtr securityAttributes,
            [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
            //[MarshalAs(UnmanagedType.U4)] FileAttributes flagsAndAttributes, //
            uint flagsAndAttributes,
            IntPtr template
            );

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int CloseHandle(
            SafeFileHandle hObject
            );

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool DeviceIoControl(
            SafeFileHandle hDevice,
            uint dwIoControlCode,
            ref KKDRV_FILTER_DATA InBuffer,
            int nInBufferSize,
            IntPtr OutBuffer,
            uint nOutBufferSize,
            IntPtr pBytesReturned,
            IntPtr lpOverlapped
            );

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool DeviceIoControl(
            SafeFileHandle hDevice,
            uint dwIoControlCode,
            IntPtr InBuffer,
            int nInBufferSize,
            IntPtr OutBuffer,
            uint nOutBufferSize,
            IntPtr pBytesReturned,
            IntPtr lpOverlapped
            );

        [DllImport("kernel32.dll", SetLastError = true)]
        unsafe static extern bool WriteFile(
            SafeFileHandle hFile, 
            byte[] lpBuffer,
            uint nNumberOfBytesToWrite, 
            ref uint lpNumberOfBytesWritten,
            NativeOverlapped* lpOverlapped
            );

        [DllImport("kernel32.dll", SetLastError = true)]
        unsafe static extern bool ReadFile(
            SafeFileHandle hFile, 
            IntPtr lpBuffer,
            int nNumberOfBytesToRead,
            IntPtr lpNumberOfBytesRead, 
            NativeOverlapped* lpOverlapped
            );

        [DllImport("kernel32.dll")]
        static extern bool CancelIo(SafeFileHandle hFile);

        #endregion Unmanaged code imports

        public delegate void ProcessData(byte[] data);
        private ProcessData Processor;
        private bool isStopping;

        private SafeFileHandle Device;
        private const string DeviceName = "\\\\.\\kkdrv";
        private const string DriverService = "kkVPNDriver";
        private const string DriverServiceDisplay = "kkVPN Driver";
        private const string DriverFilename = "C:\\DriverTest\\Drivers\\kkdrv.sys";

        private IntPtr ReadBuffer;

        private ulong RxBytes;
        private ulong RxPackets;

        private ulong TxBytes;
        private ulong TxPackets;

        public DriverConnector() {}

        public ulong GetRxBytes() { return RxBytes; }
        public ulong GetRxPackets() { return RxPackets; }
        public ulong GetTxBytes() { return TxBytes; }
        public ulong GetTxPackets() { return TxPackets; }

        public void ResetStats()
        {
            RxBytes = 0;
            RxPackets = 0;
            TxBytes = 0;
            TxPackets = 0;
        }

        public void InitializeDevice()
        {
            if (!DriverSystemCheck.CheckStatus(@"Root\kkdrv"))
            {
                throw new InvalidOperationException("Nie znaleziono urządzenia!");
            }

            Device = CreateFile(
                fileName: DeviceName,
                fileAccess: FileAccess.ReadWrite,
                fileShare: FileShare.None,
                securityAttributes: IntPtr.Zero,
                creationDisposition: FileMode.Open,
                flagsAndAttributes: 0x40000000,
                template: IntPtr.Zero
                );

            if (Device.IsInvalid || Device.IsClosed)
            {
                throw new Win32ErrorException(
                    "CreateFile",
                    Marshal.GetLastWin32Error(),
                    "Otwarcie uchwytu urządzenia zakończyło się niepowodzeniem!"
                    );
            }

            if(!ThreadPool.BindHandle(Device))
            {
                throw new Win32ErrorException(
                    "BindHandle",
                    Marshal.GetLastWin32Error(),
                    "Przypisanie uchwytu do puli wątków zakończyło się niepowodzeniem!"
                    );
            }
        }

        public void SetFilter(uint Subnetwork, uint Mask, uint Local)
        {
            if (Device.IsClosed || Device.IsInvalid)
            {
                throw new DeviceHandleInvalidException(
                    "Nieotwarty uchwyt sterownika!"
                    );
            }

            KKDRV_FILTER_DATA FilterData;

            FilterData.low = Subnetwork + 1;
            FilterData.high = Subnetwork + (~Mask) - 1;
            FilterData.local = Local;

            if (!DeviceIoControl(
                    hDevice: Device,
                    dwIoControlCode: IOCTL_REGISTER,
                    InBuffer: ref FilterData,
                    nInBufferSize: Marshal.SizeOf(FilterData),
                    OutBuffer: IntPtr.Zero,
                    nOutBufferSize: 0,
                    pBytesReturned: IntPtr.Zero,
                    lpOverlapped: IntPtr.Zero
                    ))
            {
                throw new Win32ErrorException(
                    "DeviceIoControl", 
                    Marshal.GetLastWin32Error(), 
                    "Przesłanie danych do sterownika zakończyło się niepowodzeniem!"
                    );
            }

            ResetStats();
        }

        public void ResetFilter()
        {
            if (Device.IsClosed || Device.IsInvalid)
            {
                throw new DeviceHandleInvalidException(
                    "Nieotwarty uchwyt sterownika!"
                    );
            }

            if (!DeviceIoControl(
                    hDevice: Device,
                    dwIoControlCode: IOCTL_RESTART,
                    InBuffer: IntPtr.Zero,
                    nInBufferSize: 0,
                    OutBuffer: IntPtr.Zero,
                    nOutBufferSize: 0,
                    pBytesReturned: IntPtr.Zero,
                    lpOverlapped: IntPtr.Zero
                    ))
            {
                throw new Win32ErrorException(
                    "DeviceIoControl",
                    Marshal.GetLastWin32Error(),
                    "Przesłanie danych do sterownika zakończyło się niepowodzeniem!"
                    );
            }
        }

        public void CloseDevice()
        {
            ResetFilter();

            if (ReadBuffer == IntPtr.Zero)
            {
                Marshal.FreeHGlobal(ReadBuffer);
            }

            if (Device != null)
            {
                CancelIo(Device);
                CloseHandle(Device);
            }
        }

        unsafe public void StartReading(ProcessData processor)
        {
            isStopping = false;
            this.Processor = processor;

            if (Device.IsClosed || Device.IsInvalid)
            {
                throw new DeviceHandleInvalidException(
                    "Nieotwarty uchwyt sterownika!"
                    );
            }

            if (ReadBuffer == IntPtr.Zero)
            {
                ReadBuffer = Marshal.AllocHGlobal(BUFFER_SIZE);
            }
            Overlapped overlapped = new Overlapped();

            ReadFile(Device, ReadBuffer, BUFFER_SIZE, IntPtr.Zero, overlapped.Pack(ReadCompletionCallback, null));
        }

        public void StopReading()
        {
            isStopping = true;
        }

        unsafe public void ReadData()
        {
            if (Device.IsClosed || Device.IsInvalid)
            {
                throw new DeviceHandleInvalidException(
                    "Nieotwarty uchwyt sterownika!"
                    );
            }
            Overlapped overlapped = new Overlapped();

            ReadFile(
                Device, 
                ReadBuffer, 
                BUFFER_SIZE, 
                IntPtr.Zero, 
                overlapped.Pack(ReadCompletionCallback, null)
                );
        }

        unsafe void ReadCompletionCallback(uint errorCode, uint bytesRead, NativeOverlapped* nativeOverlapped)
        {
            try 
            {
                if (errorCode == 0 && Processor != null)
                {
                    byte[] Result = new byte[bytesRead];
                    Marshal.Copy(ReadBuffer, Result, 0, (int)bytesRead);
                    Processor(Result);

                    TxBytes += (ulong)bytesRead;
                    TxPackets++;
                }

                if (!isStopping)
                {
                    ReadData();
                }
            }
            finally
            {
                System.Threading.Overlapped.Unpack(nativeOverlapped);
                System.Threading.Overlapped.Free(nativeOverlapped);
            }
        }

        unsafe public void WriteData(byte[] Data)
        {
            const int PacketLengthHeaderOffset = 0x2;

            if (Device != null)
            {
                uint BytesWritten = 0;
                uint TotalLength = (UInt32)Data.Length;
                int Offset = 0;
                ushort PacketLength = BitConverter.ToUInt16(Data, PacketLengthHeaderOffset).InvertBytes();

                while(true)
                {
                    byte[] Temp = new byte[PacketLength];
                    Buffer.BlockCopy(Data, Offset, Temp, 0, PacketLength);

                    Overlapped overlapped = new Overlapped();

                    WriteFile(
                        Device,
                        Temp,
                        (uint)PacketLength,
                        ref BytesWritten,
                        overlapped.Pack(WriteCompletionCallback, null)
                        );

                    Offset += PacketLength;

                    if (Offset + PacketLength < TotalLength)
                    {
                        PacketLength = BitConverter.ToUInt16(Data, Offset + PacketLengthHeaderOffset).InvertBytes();
                    }
                    else
                    {
                        break;
                    }      
                }

                RxBytes += (UInt64)TotalLength;
                RxPackets++;
            }
        }

        unsafe void WriteCompletionCallback(uint errorCode, uint bytesWritten, NativeOverlapped* nativeOverlapped)
        {
            try
            {
                
            }
            finally
            {
                System.Threading.Overlapped.Unpack(nativeOverlapped);
                System.Threading.Overlapped.Free(nativeOverlapped);
            }
        }
    }

    public class Win32ErrorException : Exception
    {
        public string Function;
        public int ErrorCode;

        public Win32ErrorException(string function, int errorCode, string message)
            : base(message)
        {
            this.Function = function;
            this.ErrorCode = errorCode;
        }
    }

    public class DeviceHandleInvalidException : Exception
    {
        public DeviceHandleInvalidException(string message)
            : base(message)
        {

        }
    }
}
