//NCShark - By AlSch092 @ Github, thanks to @Diamondo25 for MapleShark
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace NCShark
{
    public sealed class NCPacket : ListViewItem
    {

        public DateTime Timestamp { get; private set; }
        public bool Outbound { get; private set; }
        public ushort Build { get; private set; }
        public ushort Locale { get; private set; }
        public ushort Opcode { get; private set; }
        public new string Name { set { SubItems[4].Text = value; } }

        public byte[] Buffer { get; set; }
        public int Cursor { get; private set; }
        public int Length { get { return Buffer.Length; } }
        public int Remaining { get { return Length - Cursor; } }
        public uint PreDecodeIV { get; private set; }
        public uint PostDecodeIV { get; private set; }
        /// <summary>
        /// Stores the XOR key-table index at which this packet's payload data begins.
        /// Captured during live sniffing; used by SendPacketForm to re-encrypt before resend.
        /// Value of 0 means unknown/unset (fallback: use isFirstSend=true).
        /// </summary>
        public ulong XorCount { get; set; }

        /// <summary>
        /// The full TCP payload exactly as captured on the wire (still XOR-encrypted,
        /// including the 2-byte protocol header). Stored so a replay can recover the
        /// header bytes that the logging pipeline strips. Null for packets that were
        /// never captured live (e.g. old .msb files without the field).
        /// </summary>
        public byte[] RawPayload { get; set; }

        private bool _edited = false;
        public bool Edited
        {
            get { return _edited; }
            set
            {
                _edited = value;
                if (value)
                    this.BackColor = Color.LightYellow;
                else
                    this.BackColor = SystemColors.Window;
            }
        }

        public NCPacket(DateTime pTimestamp, bool pOutbound, ushort pOpcode, string pName, byte[] pBuffer)
            : base(new string[] {
                pTimestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                pOutbound ? "Outbound" : "Inbound",
                pBuffer.Length.ToString(),
                "0x" + pOpcode.ToString("X4"),
                pName })
        {
            Timestamp = pTimestamp;
            Outbound = pOutbound;

            Opcode = pOpcode;
            Buffer = pBuffer;

        }

        public NCPacket(DateTime pTimestamp, ushort pOpcode, string pName, byte[] pBuffer)
            : base(new string[] {
                pTimestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                pBuffer.Length.ToString(),
                "0x" + pOpcode.ToString("X4"),
                pName })
        {
            Timestamp = pTimestamp;
            Opcode = pOpcode;
            Buffer = pBuffer;
        }

        public void Rewind() { Cursor = 0; }

        public bool ReadByte(out byte pValue)
        {
            pValue = 0;
            if (Cursor + 1 > Length) return false;
            pValue = Buffer[Cursor++];
            return true;
        }
        public bool ReadSByte(out sbyte pValue)
        {
            pValue = 0;
            if (Cursor + 1 > Length) return false;
            pValue = (sbyte)Buffer[Cursor++];
            return true;
        }
        public bool ReadUShort(out ushort pValue)
        {
            pValue = 0;
            if (Cursor + 2 > Length) return false;
            pValue = (ushort)(Buffer[Cursor++] |
                              Buffer[Cursor++] << 8);
            return true;
        }
        public bool ReadShort(out short pValue)
        {
            pValue = 0;
            if (Cursor + 2 > Length) return false;
            pValue = (short)(Buffer[Cursor++] |
                             Buffer[Cursor++] << 8);
            return true;
        }
        public bool ReadUInt(out uint pValue)
        {
            pValue = 0;
            if (Cursor + 4 > Length) return false;
            pValue = (uint)(Buffer[Cursor++] |
                            Buffer[Cursor++] << 8 |
                            Buffer[Cursor++] << 16 |
                            Buffer[Cursor++] << 24);
            return true;
        }
        public bool ReadInt(out int pValue)
        {
            pValue = 0;
            if (Cursor + 4 > Length) return false;
            pValue = (int)(Buffer[Cursor++] |
                           Buffer[Cursor++] << 8 |
                           Buffer[Cursor++] << 16 |
                           Buffer[Cursor++] << 24);
            return true;
        }
        public bool ReadFloat(out float pValue)
        {
            pValue = 0;
            if (Cursor + 4 > Length) return false;
            pValue = BitConverter.ToSingle(Buffer, Cursor);
            Cursor += 4;
            return true;
        }
        public bool ReadULong(out ulong pValue)
        {
            pValue = 0;
            if (Cursor + 8 > Length) return false;
            pValue = (ulong)(Buffer[Cursor++] |
                             Buffer[Cursor++] << 8 |
                             Buffer[Cursor++] << 16 |
                             Buffer[Cursor++] << 24 |
                             Buffer[Cursor++] << 32 |
                             Buffer[Cursor++] << 40 |
                             Buffer[Cursor++] << 48 |
                             Buffer[Cursor++] << 56);
            return true;
        }
        public bool ReadLong(out long pValue)
        {
            pValue = 0;
            if (Cursor + 8 > Length) return false;
            pValue = (long)(Buffer[Cursor++] |
                            Buffer[Cursor++] << 8 |
                            Buffer[Cursor++] << 16 |
                            Buffer[Cursor++] << 24 |
                            Buffer[Cursor++] << 32 |
                            Buffer[Cursor++] << 40 |
                            Buffer[Cursor++] << 48 |
                            Buffer[Cursor++] << 56);
            return true;
        }
        public bool ReadFlippedLong(out long pValue) // 5 6 7 8 1 2 3 4
        {
            pValue = 0;
            if (Cursor + 8 > Length) return false;
            pValue = (long)(
                            Buffer[Cursor++] << 32 |
                            Buffer[Cursor++] << 40 |
                            Buffer[Cursor++] << 48 |
                            Buffer[Cursor++] << 56 |
                            Buffer[Cursor++] |
                            Buffer[Cursor++] << 8 |
                            Buffer[Cursor++] << 16 |
                            Buffer[Cursor++] << 24);
            return true;
        }
        public bool ReadDouble(out double pValue)
        {
            pValue = 0;
            if (Cursor + 8 > Length) return false;
            pValue = BitConverter.ToDouble(Buffer, Cursor);
            Cursor += 8;
            return true;
        }
        public bool ReadBytes(byte[] pBytes) { return ReadBytes(pBytes, 0, pBytes.Length); }
        public bool ReadBytes(byte[] pBytes, int pStart, int pLength)
        {
            if (Cursor + pLength > Length) return false;
            
            System.Buffer.BlockCopy(Buffer, Cursor, pBytes, pStart, pLength);
            Cursor += pLength;
            return true;
        }

        public bool ReadPaddedString(out string pValue, int pLength)
        {
            pValue = "";
            if (Cursor + pLength > Length) return false;
            int length = 0;
            while (length < pLength && Buffer[Cursor + length] != 0x00) ++length;
            if (length > 0) pValue = Encoding.ASCII.GetString(Buffer, Cursor, length);
            Cursor += pLength;
            return true;
        }
    }
}
