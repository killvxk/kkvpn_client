using kkvpn_client.Misc;
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
    unsafe class DriverConnector
    {
        const uint EVENT_ALL_ACCESS = (0x000F0000 | 0x00100000 | 0x00000003);
        const uint IOCTL_REGISTER = ((0x12) << 16) | ((0x1) << 2);
        const uint IOCTL_RESTART = ((0x12) << 16) | ((0x2) << 2);
        const uint INFINITE = 0xFFFFFFFF;
        const int BUFFER_SIZE = 0x1000;

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

        //[DllImport("kernel32.dll", SetLastError = true)]
        //private static extern int CloseHandle(
        //    SafeFileHandle hObject
        //    );

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
        unsafe static extern int CancelIoEx(
            SafeFileHandle hFile,
            NativeOverlapped* lpOverlapped
            );

        #endregion Unmanaged code imports

        public delegate void ProcessData(byte[] data);
        private ProcessData DriverDataExternalProcessor;
        private bool IsStopping;

        private uint Local;

        private SafeFileHandle Device;
        private const string DeviceName = "\\\\.\\kkdrv";

        private IntPtr ReadBuffer;
        private NativeOverlapped* ReadOverlapped;
        private NativeOverlapped* WriteOverlapped;

        public DriverConnector() { }

        public void InitializeDevice()
        {
            //if (!DriverSystemCheckAndStart.CheckStatus(@"Root\kkdrv"))
            //{
            //    throw new InvalidOperationException("Nie znaleziono urządzenia!");
            //}
            //bool res = DriverSystemCheckAndStart.StartDriver(@"kkdrv");

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

            if (!ThreadPool.BindHandle(Device))
            {
                throw new Win32ErrorException(
                    "BindHandle",
                    Marshal.GetLastWin32Error(),
                    "Przypisanie uchwytu do puli wątków zakończyło się niepowodzeniem!"
                    );
            }
        }

        public void SetFilter(uint subnetwork, uint mask, uint local)
        {
            if (Device.IsClosed || Device.IsInvalid)
            {
                throw new DeviceHandleInvalidException(
                    "Nieotwarty uchwyt sterownika!"
                    );
            }

            ResetFilter();
            KKDRV_FILTER_DATA FilterData;

            FilterData.low = (subnetwork + 1);
            FilterData.high = (subnetwork + (~mask) - 1);
            FilterData.local = 0;
            Local = local.InvertBytes();

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
            if (ReadBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(ReadBuffer);
            }

            if (!Device.IsClosed && !Device.IsInvalid)
            {
                StopReading();
                Device.Close();
            }
        }

        unsafe public void StartReading(ProcessData processor)
        {
            IsStopping = false;
            this.DriverDataExternalProcessor = processor;

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
            ReadOverlapped = overlapped.Pack(ReadCompletionCallback, null);

            ReadFile(Device, ReadBuffer, BUFFER_SIZE, IntPtr.Zero, ReadOverlapped);
        }

        public void StopReading()
        {
            IsStopping = true;
            ResetFilter();
            CancelIoEx(Device, null);
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
            ReadOverlapped = overlapped.Pack(ReadCompletionCallback, null);

            ReadFile(
                Device,
                ReadBuffer,
                BUFFER_SIZE,
                IntPtr.Zero,
                ReadOverlapped
                );
        }

        unsafe void ReadCompletionCallback(uint errorCode, uint bytesRead, NativeOverlapped* nativeOverlapped)
        {
            const int packetDestinationHostOffset = 0x10;

            try
            {
                if (errorCode == 0 && DriverDataExternalProcessor != null && bytesRead >= 20)
                {
                    byte[] temp = new byte[bytesRead];
                    Marshal.Copy(ReadBuffer, temp, 0, (int)bytesRead);

                    if (BitConverter.ToUInt32(temp, packetDestinationHostOffset) == Local)
                    {
                        WriteData(temp);
                    }
                    else
                    {
                        DriverDataExternalProcessor(temp);
                    }
                }

                if (!IsStopping)
                {
                    ReadData();
                }
            }
            finally
            {
                //System.Threading.Overlapped.Unpack(nativeOverlapped);
                System.Threading.Overlapped.Free(nativeOverlapped);
            }
        }

        public void WriteData(byte[] data)
        {
            if (Device != null)
            {
                uint bytesWritten = 0;
                Overlapped overlapped = new Overlapped();
                WriteOverlapped = overlapped.Pack(WriteDeviceIOCompletionCallback, null);

                WriteFile(
                    Device,
                    data,
                    (uint)data.Length,
                    ref bytesWritten,
                    WriteOverlapped
                    );
            }
        }

        unsafe void WriteDeviceIOCompletionCallback(uint errorCode, uint bytesWritten, NativeOverlapped* nativeOverlapped)
        {
            //System.Threading.Overlapped.Unpack(nativeOverlapped);
            System.Threading.Overlapped.Free(nativeOverlapped);
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