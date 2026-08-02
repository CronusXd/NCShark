//NCShark - By AlSch092 @ Github, thanks to @Diamondo25 for MapleShark
using System;
using System.Runtime.InteropServices;

namespace NCShark
{
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public class XorKeyLookup
    {
        public ulong ResetCounter { get; set; }
        public uint Unk2 { get; set; } 
        public ulong count { get; set; } 
        public ulong Referenced1 { get; set; } 
        public ulong Unk3 { get; set; }
    };

    public class Cipher
    {
        public static readonly byte[] NCXorkeyTable = { 0xDE, 0x90, 0xC3, 0xA6, 0xE2, 0xF6, 0xDC, 0xE8, 0x0A, 0x6F, 0xAA, 0xE6, 0xA6, 0xA8, 0xE5, 0x6B, 0x44, 0xB5, 0xCB, 0x9F, 0x0A, 0x36, 0x09, 0x46, 0xA0, 0x6D, 0x30, 0xED, 0x3E, 0x15, 0x38, 0x07, 0x44, 0xB5, 0xCB, 0x9F, 0x9A, 0x82, 0x37, 0xD3, 0x90, 0xB4, 0x87, 0xFC, 0xCE, 0xB7, 0xC5, 0x54, 0xAF, 0xC2, 0x3E, 0x3E, 0xAB, 0xE1, 0x5A, 0xAA, 0x3E, 0x3B, 0x6A, 0xA5, 0xAD, 0x52, 0xE2, 0xDD, 0x80, 0x8F, 0xC6, 0xA6, 0x31, 0x02, 0x00 };

        public static XorKeyLookup xor_out = new XorKeyLookup();
        public static XorKeyLookup xor_in = new XorKeyLookup();

        /// <summary>Number of header bytes that precede the opcode in a decrypted packet.</summary>
        public const int HeaderSize = 2;

        /// <summary>
        /// Returns the XOR key byte at the given stream index (the counter wraps at 0x40).
        /// </summary>
        public static byte KeyByte(ulong index)
        {
            return NCXorkeyTable[index % 0x40];
        }

        /// <summary>
        /// Rebuilds the full on-wire payload for a packet replay.
        ///
        /// The captured payload was decrypted by XORing the whole stream (including the
        /// 2-byte header) sequentially from a historical key index. This method recovers
        /// the plaintext header bytes from the original encrypted bytes, re-attaches the
        /// caller-provided plaintext data, and re-encrypts everything from <paramref name="startKey"/>.
        ///
        /// A dedicated local XorKeyLookup is used so the live capture cipher state
        /// (Cipher.xor_out / Cipher.xor_in) is never mutated by a replay.
        /// </summary>
        /// <param name="rawPayload">Full encrypted payload exactly as captured on the wire.</param>
        /// <param name="xorCount">Key index where the logged (plaintext) data begins in the capture.</param>
        /// <param name="plaintextData">Plaintext opcode+data bytes (may have been edited).</param>
        /// <param name="startKey">Key index the server currently expects for the next client packet.</param>
        public static byte[] BuildReplayPayload(byte[] rawPayload, ulong xorCount, byte[] plaintextData, ulong startKey)
        {
            byte[] plain = new byte[HeaderSize + plaintextData.Length];

            // Recover the plaintext header from the captured bytes: encrypted ^ historical key.
            ulong headerStartHist = (xorCount + 0x40 - HeaderSize) % 0x40;
            for (int i = 0; i < HeaderSize && i < rawPayload.Length; i++)
                plain[i] = (byte)(rawPayload[i] ^ KeyByte((headerStartHist + (ulong)i) % 0x40));

            Buffer.BlockCopy(plaintextData, 0, plain, HeaderSize, plaintextData.Length);

            // Encrypt the whole plaintext payload from the current key position.
            var replayKey = new XorKeyLookup { count = startKey };
            XorBytes(replayKey, plain, plain.Length, false);
            return plain;
        }

        public unsafe static void XorBytes(XorKeyLookup keytable, byte[] buffer, int length, bool firstSend)
        {
            if (buffer == null || length == 0)
                return;

            if (firstSend)
            {
                keytable.count = 0;
                keytable.Referenced1 = 0;
                keytable.ResetCounter = 0x4000000040000000;
                keytable.Unk2 = 0;
                keytable.Unk3 = 0x200000;
            }

            uint r12 = 0;
            ulong r9 = 0x40;

            for (int i = 0; i < length; i++)
            {
                byte cl = NCXorkeyTable[keytable.count];
                keytable.count++;

                buffer[i] = (byte)(buffer[i] ^ cl);

                ulong eax = keytable.count;
                if (eax == r9)
                    eax = r12;

                keytable.count = eax;
            }
        }
    }
    
}