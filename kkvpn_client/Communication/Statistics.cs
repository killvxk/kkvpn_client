using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kkvpn_client
{
    class Statistics
    {
        public double DLSpeed = 0;
        public double ULSpeed = 0;
        public ulong DLPackets = 0;
        public ulong DLBytes = 0;
        public ulong ULPackets = 0;
        public ulong ULBytes = 0;

        private int LastSpeedCheck;
        private ulong LastSpeedCheckDL = 0;
        private ulong LastSpeedCheckUL = 0;

        public Statistics()
        {
            LastSpeedCheck = Environment.TickCount;
        }

        public Statistics UpdateStats()
        {
            double time = Environment.TickCount - LastSpeedCheck;
            LastSpeedCheck = Environment.TickCount;

            this.DLSpeed = (double)(this.DLBytes - LastSpeedCheckDL) / time;
            this.ULSpeed = (double)(this.ULBytes - LastSpeedCheckUL) / time;

            LastSpeedCheckDL = this.DLBytes;
            LastSpeedCheckUL = this.ULBytes;

            return this;
        }

        public void Clear()
        {
            DLSpeed = 0;
            ULSpeed = 0;
            DLPackets = 0;
            DLBytes = 0;
            ULPackets = 0;
            ULBytes = 0;

            LastSpeedCheckDL = 0;
            LastSpeedCheckUL = 0;

            LastSpeedCheck = Environment.TickCount;
        }
    }
}
