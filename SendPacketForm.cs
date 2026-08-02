//NCShark - By AlSch092 @ Github, thanks to @Diamondo25 for MapleShark
using System;
using PacketDotNet;
using System.Windows.Forms;
using System.Net.NetworkInformation;
using System.Net;
using SharpPcap.LibPcap;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Linq;

namespace NCShark
{
    public partial class SendPacketForm : Form
    {
        private LibPcapLiveDevice mDevice = null;
        public string sourceIp = null;
        public string destIp = null;
        public int sourcePort = 0;
        public int destPort = 0;

        public string sourceMAC = null;
        public string destinationMac = null;

        public ushort lastIdentificationNum = 0;
        public uint lastSequenceNumber = 0;
        public uint lastAcknowledgmentNumber = 0;
        public uint lastPacketSize = 0;

        // Session tracking for TCP sequence number consistency (Fase G)
        private SessionForm mSession = null;
        private bool mIsOutbound = false;

        // Replay reconstruction data, captured live from the selected packet.
        // Used to rebuild the 2-byte protocol header and re-encrypt at the CURRENT key position.
        private byte[] mReplayRawPayload = null;
        private ulong mReplayXorCount = 0;

        public SendPacketForm()
        {
            InitializeComponent();
        }

        public void SetDevice(LibPcapLiveDevice device, string filter)
        {
            mDevice = device;
            mDevice.Filter = filter;
        }

        static byte[] ConvertAsciiStringToBytes(string asciiString)
        {
            string[] asciiBytes = asciiString.Split(' ');
            byte[] byteArray = new byte[asciiBytes.Length];

            for (int i = 0; i < asciiBytes.Length; i++)
            {
                byteArray[i] = byte.Parse(asciiBytes[i], System.Globalization.NumberStyles.HexNumber);
            }

            return byteArray;
        }

        void sendPshAck(byte[] plaintextPayload) //packet contains TCP data -> Wrap Ethernet into TCP packet
        {
            try
            {
                if (mDevice == null)
                {
                    SafeUiMessage("Device was NULL!");
                    return;
                }

                // No-op when the capture device is already open (it normally is).
                mDevice.Open();

                // Refresh sequence numbers from the tracked session before sending (Fase G)
                RefreshSequenceFromSession();
                Console.WriteLine("Sending to: {0}:{1} from {2}:{3} LastId: {4}", destIp, destPort, sourceIp, sourcePort, lastIdentificationNum);

                // Resolve real MACs (source = capture adapter, destination = ARP).
                ResolveMacAddresses();

                // Build the full on-wire payload:
                //  - Replay of a captured packet: recover the 2-byte header from the captured
                //    encrypted bytes and re-encrypt [header + data] at the CURRENT key position,
                //    so the server's current cipher state accepts it. The live capture cipher
                //    state (Cipher.xor_out/xor_in) is never mutated by a replay.
                //  - Manual payload: encrypt the typed bytes from the current key position as-is.
                ulong liveKey = mIsOutbound ? Cipher.xor_out.count : Cipher.xor_in.count;
                byte[] wirePayload;
                if (mReplayRawPayload != null && mReplayRawPayload.Length >= 2)
                {
                    wirePayload = Cipher.BuildReplayPayload(mReplayRawPayload, mReplayXorCount, plaintextPayload, liveKey);
                }
                else
                {
                    wirePayload = (byte[])plaintextPayload.Clone();
                    var replayKey = new XorKeyLookup { count = liveKey };
                    Cipher.XorBytes(replayKey, wirePayload, wirePayload.Length, false);
                }

                //Create a new Ethernet packet wrapped in the TCP packet
                var ethPacket = new EthernetPacket(
                    PhysicalAddress.Parse(sourceMAC ?? "00-00-00-00-00-00"),    // Source MAC address
                    PhysicalAddress.Parse(destinationMac ?? "00-00-00-00-00-01"),   // Destination MAC address (gateway)
                    EthernetPacketType.IpV4                   // Ethernet type (IPv4)
                );

                // Create a new IPv4 packet
                var ipPacket = new IPv4Packet(
                    IPAddress.Parse(sourceIp),   // Source IP address
                    IPAddress.Parse(destIp)    // Destination IP address
                );

                ipPacket.Version = (IpVersion)4;
                ipPacket.TimeToLive = 128;
                // Set the "Don't Fragment" (DF) bit
                ipPacket.FragmentFlags = (ushort)(1 << 1); // Shift 1 bit to the left to set the DF bit
                ipPacket.FragmentOffset = 0;
                ipPacket.Id = (ushort)(lastIdentificationNum + 1);
                lastIdentificationNum = ipPacket.Id;    // persist for next send


                // Create a new TCP packet
                var tcpPacket = new TcpPacket((ushort)sourcePort, (ushort)destPort)
                {
                    Psh = true,
                    Ack = true,
                    WindowSize = 65535,    // TCP window size (max reasonable default)
                    SequenceNumber = lastSequenceNumber,
                    AcknowledgmentNumber = lastAcknowledgmentNumber,
                };

                // Set the payload for the TCP packet
                tcpPacket.PayloadData = wirePayload;

                // Add the TCP packet to the IPv4 packet
                ipPacket.PayloadPacket = tcpPacket;

                ethPacket.PayloadPacket = ipPacket;

                // Recalculate checksums now that the full packet hierarchy is assembled
                tcpPacket.UpdateTCPChecksum();    // computes TCP checksum over payload + pseudo-header
                ipPacket.UpdateIPChecksum();      // recomputes IP header checksum

                lock (mDevice)
                {
                    mDevice.SendPacket(ethPacket);
                }

                // Update TCP sequence tracking after successful send (Fase G)
                lastSequenceNumber += (uint)wirePayload.Length;
                lastPacketSize = (uint)wirePayload.Length;

                // Keep session sequence numbers in sync
                if (mSession != null)
                {
                    mSession.NotifyPacketSent(mIsOutbound, wirePayload.Length);
                }

                // Update UI display with new sequence values (marshaled to the UI thread)
                SafeUi(() =>
                {
                    txtSequence.Text = lastSequenceNumber.ToString();
                    txtAck.Text = lastAcknowledgmentNumber.ToString();
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("[NCShark] Send failed: " + ex.ToString());
                SafeUiMessage("Send failed: " + ex.Message);
            }
        }

        private void button_SendPacket_Click(object sender, EventArgs e)
        {
            // Parse the payload on the UI thread to avoid cross-thread TextBox access.
            string textBytesPayload = textBox_Send.Text;
            if (textBytesPayload == null || textBytesPayload.Length <= 2)
            {
                MessageBox.Show("Error parsing input packet, check spaces");
                return;
            }

            byte[] plaintextPayload;
            try
            {
                plaintextPayload = ConvertAsciiStringToBytes(textBytesPayload);
            }
            catch
            {
                MessageBox.Show("Error parsing input packet, check spaces and hex format");
                return;
            }

            Thread t = new Thread(() => sendPshAck(plaintextPayload));
            t.IsBackground = true;
            t.Start();
        }

        private void SendPacketForm_Load(object sender, EventArgs e)
        {

        }

        public void LoadFromPacket(NCPacket packet, SessionForm session)
        {
            if (packet == null || session == null) return;

            mSession = session;
            mIsOutbound = packet.Outbound;

            // Parse endpoint strings (format: "IP:Port" or just "IP")
            string localIp;
            int localPort;
            ParseEndpoint(session.mLocalEndpoint, out localIp, out localPort);
            string remoteIp;
            int remotePort;
            ParseEndpoint(session.mRemoteEndpoint, out remoteIp, out remotePort);

            // Set the public fields used by sendPshAck()
            sourceIp = localIp;
            destIp = remoteIp;
            sourcePort = localPort;
            destPort = remotePort;

            // Fill UI textboxes (read-only display)
            txtSourceIp.Text = localIp;
            txtSourcePort.Text = localPort > 0 ? localPort.ToString() : "";
            txtDestIp.Text = remoteIp;
            txtDestPort.Text = remotePort > 0 ? remotePort.ToString() : "";
            txtOpcode.Text = "0x" + packet.Opcode.ToString("X4");

            // Fill hex data display with the packet's raw bytes
            txtHexData.Text = BitConverter.ToString(packet.Buffer).Replace("-", " ");

            // Pre-fill the send payload with the selected packet's data
            textBox_Send.Text = BitConverter.ToString(packet.Buffer).Replace("-", " ");

            // Populate TCP sequence tracking from session (Fase G)
            RefreshSequenceFromSession();
            lastIdentificationNum = 0;   // fresh IP ID for this send form instance

            // Update UI with sequence numbers
            txtSequence.Text = lastSequenceNumber.ToString();
            txtAck.Text = lastAcknowledgmentNumber.ToString();

            // Remember the captured encrypted payload and key index so the send can
            // rebuild the protocol header and re-encrypt at the CURRENT key position.
            // The live capture cipher state (Cipher.xor_out / Cipher.xor_in) is intentionally
            // NOT touched here — a replay must never desynchronize live decryption.
            mReplayRawPayload = packet.RawPayload;
            mReplayXorCount = packet.XorCount;
        }

        /// <summary>
        /// Refreshes TCP sequence/ack numbers from the tracked session.
        /// Called when loading from session and before each send.
        /// </summary>
        private void RefreshSequenceFromSession()
        {
            if (mSession == null) return;

            if (mIsOutbound)
            {
                lastSequenceNumber = mSession.OutboundSequence;
                lastAcknowledgmentNumber = mSession.InboundSequence;
            }
            else
            {
                lastSequenceNumber = mSession.InboundSequence;
                lastAcknowledgmentNumber = mSession.OutboundSequence;
            }

            lastPacketSize = 0;
        }

        /// <summary>Runs an action on the UI thread (marshals from the send worker thread).</summary>
        private void SafeUi(Action action)
        {
            if (IsDisposed) return;
            if (InvokeRequired) BeginInvoke(action);
            else action();
        }

        /// <summary>Shows a MessageBox on the UI thread.</summary>
        private void SafeUiMessage(string message)
        {
            SafeUi(() => MessageBox.Show(this, message, "NCShark", MessageBoxButtons.OK, MessageBoxIcon.Information));
        }

        /// <summary>
        /// Resolves the real MAC addresses: source = capture adapter, destination = ARP of the
        /// target (same subnet) or of the default gateway (remote destinations). Falls back to
        /// placeholder MACs if resolution fails so the rest of the send can still be attempted.
        /// </summary>
        private void ResolveMacAddresses()
        {
            try
            {
                if (mDevice != null && mDevice.Interface != null && mDevice.Interface.MacAddress != null)
                {
                    byte[] bytes = mDevice.Interface.MacAddress.GetAddressBytes();
                    if (bytes.Length == 6)
                        sourceMAC = string.Join("-", bytes.Select(b => b.ToString("X2")));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[NCShark] Failed to resolve source MAC: " + ex.Message);
            }

            try
            {
                if (string.IsNullOrEmpty(destIp) || mDevice == null || mDevice.Interface == null) return;

                IPAddress dest = IPAddress.Parse(destIp);
                IPAddress target = dest;
                bool sameSubnet = false;

                foreach (var addr in mDevice.Interface.Addresses)
                {
                    if (addr == null || addr.Addr == null || addr.Addr.ipAddress == null) continue;
                    if (addr.Addr.ipAddress.AddressFamily != AddressFamily.InterNetwork) continue;
                    IPAddress netmask = addr.Netmask != null ? addr.Netmask.ipAddress : null;
                    if (netmask == null) continue;

                    byte[] a = addr.Addr.ipAddress.GetAddressBytes();
                    byte[] m = netmask.GetAddressBytes();
                    byte[] d = dest.GetAddressBytes();
                    sameSubnet = true;
                    for (int i = 0; i < 4; i++)
                        if ((a[i] & m[i]) != (d[i] & m[i])) { sameSubnet = false; break; }
                    if (sameSubnet) break;
                }

                if (!sameSubnet && mDevice.Interface.GatewayAddress != null)
                    target = mDevice.Interface.GatewayAddress;

                string mac = ArpResolve(target);
                if (mac != null)
                    destinationMac = mac;
                else
                    Console.WriteLine("[NCShark] ARP failed for {0} - packet may not be delivered", target);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[NCShark] Failed to resolve destination MAC: " + ex.Message);
            }
        }

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern int SendARP(uint DestIP, uint SrcIP, byte[] pMacAddr, ref uint PhyAddrLen);

        private static string ArpResolve(IPAddress ip)
        {
            byte[] addr = ip.GetAddressBytes();
            uint dest = (uint)(addr[0] | (addr[1] << 8) | (addr[2] << 16) | (addr[3] << 24));
            byte[] mac = new byte[6];
            uint len = (uint)mac.Length;
            if (SendARP(dest, 0, mac, ref len) == 0 && len == 6)
                return string.Join("-", mac.Select(b => b.ToString("X2")));
            return null;
        }

        /// <summary>
        /// Refreshes the payload hex text from an NCPacket's buffer.
        /// Called by DataForm.btnApplyChanges after editing bytes in the HexBox.
        /// </summary>
        public void RefreshPacketData(NCPacket packet)
        {
            if (packet == null) return;
            txtHexData.Text = BitConverter.ToString(packet.Buffer).Replace("-", " ");
            textBox_Send.Text = BitConverter.ToString(packet.Buffer).Replace("-", " ");
        }

        private void ParseEndpoint(string endpoint, out string ip, out int port)
        {
            ip = endpoint ?? "";
            port = 0;

            if (string.IsNullOrEmpty(endpoint)) return;

            int lastColon = endpoint.LastIndexOf(':');
            if (lastColon > 0 && int.TryParse(endpoint.Substring(lastColon + 1), out port))
            {
                ip = endpoint.Substring(0, lastColon);
            }
        }
    }
}
