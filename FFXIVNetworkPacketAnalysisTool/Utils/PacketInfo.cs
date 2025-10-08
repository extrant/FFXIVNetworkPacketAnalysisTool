using System;
using System.Runtime.InteropServices;

namespace FFXIVNetworkPacketAnalysisTool.Utils
{
    public enum PacketDirection
    {
        Send,    // 发包 UP
        Receive  // 收包 DOWN
    }

    public class PacketInfo
    {
        public long SessionId { get; set; }
        public DateTime Timestamp { get; set; }
        public PacketDirection Direction { get; set; }
        public ushort Opcode { get; set; }
        public string OpcodeName { get; set; } = "Unknown";
        public byte[] RawData { get; set; } = Array.Empty<byte>();
        public uint PacketLength { get; set; }
        public ushort Priority { get; set; } // 仅发包时使用
        public uint TargetID { get; set; }    // 仅收包时使用

        // 用于UI显示的时间字符串
        public string TimeString => Timestamp.ToString("HH:mm:ss.fff");

        // 用于UI显示的方向字符串
        public string DirectionString => Direction == PacketDirection.Send ? "发包 ↑" : "收包 ↓";

        // 格式化的十六进制数据
        public string GetHexDump()
        {
            if (RawData == null || RawData.Length == 0)
                return "No data";

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < RawData.Length; i += 16)
            {
                // 偏移地址
                sb.AppendFormat("{0:X4}: ", i);

                // 十六进制部分
                for (int j = 0; j < 16; j++)
                {
                    if (i + j < RawData.Length)
                        sb.AppendFormat("{0:X2} ", RawData[i + j]);
                    else
                        sb.Append("   ");

                    if (j == 7) sb.Append(" ");
                }

                sb.Append(" ");

                // ASCII部分
                for (int j = 0; j < 16 && i + j < RawData.Length; j++)
                {
                    byte b = RawData[i + j];
                    sb.Append(b >= 32 && b < 127 ? (char)b : '.');
                }

                sb.AppendLine();
            }
            return sb.ToString();
        }

        // 复制原始数据的副本
        public unsafe static byte[] CloneData(byte* ptr, int length)
        {
            if (ptr == null || length <= 0)
                return Array.Empty<byte>();

            byte[] data = new byte[length];
            Marshal.Copy((IntPtr)ptr, data, 0, length);
            return data;
        }
    }
}
