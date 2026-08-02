//NCShark - By AlSch092 @ Github, thanks to @Diamondo25 for MapleShark
using SharpPcap;
using SharpPcap.LibPcap;
using PacketDotNet;
using PacketDotNet.Utils;
using PacketDotNet.LLDP;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using System.IO;
using System.Text.RegularExpressions;

namespace NCShark
{
    public partial class MainForm : Form
    {
        private bool mClosed = false;
        private LibPcapLiveDevice mDevice = null;
        private SearchForm mSearchForm = new SearchForm();
        private DataForm mDataForm = new DataForm();
        private StructureForm mStructureForm = new StructureForm();
        private PropertyForm mPropertyForm = new PropertyForm();
        private long mTotalPacketsSeen = 0;          // total packets read from device (including filtered)
        private long mTotalPacketsCaptured = 0;      // packets that matched the filter
        private long mTotalSynPackets = 0;           // SYN packets matching game port
        private long mTotalSessionsCreated = 0;      // sessions actually created
        private long mTotalPacketsLogged = 0;        // packets actually added to session list
        private System.Windows.Forms.ToolStripStatusLabel mStatusLabel = null;
        private int mDebugPort1 = 0, mDebugPort2 = 0; // track most recent ports for debugging
        private string[] _startupArguments = null;

        public MainForm(string[] startupArguments)
        {
            InitializeComponent();
            Text = "NCShark " + Program.AssemblyVersion + "By AlSch092 [Github] with special thanks to Diamondo25";

            _startupArguments = startupArguments;
        }

        public SearchForm SearchForm { get { return mSearchForm; } }
        public DataForm DataForm { get { return mDataForm; } }
        public StructureForm StructureForm { get { return mStructureForm; } }
        public PropertyForm PropertyForm { get { return mPropertyForm; } }
        public LibPcapLiveDevice CurrentDevice { get { return mDevice; } }
        public SessionForm ActiveSession { get { return mDockPanel.ActiveDocument as SessionForm; } }
        public byte Locale { get { return (mDockPanel.ActiveDocument as SessionForm).Locale; } }

        PcapDevice device;

        private SessionForm NewSession()
        {
            SessionForm session = new SessionForm();
            return session;
        }

        public void CloseSession(SessionForm form)
        {
            mDockPanel.Contents.Remove(form);
        }

        public void CopyPacketHex(KeyEventArgs pArgs)
        {
            if (mDataForm.HexBox.SelectionLength > 0 && pArgs.Modifiers == Keys.Control && pArgs.KeyCode == Keys.C)
            {
                Clipboard.SetText(BitConverter.ToString((mDataForm.HexBox.ByteProvider as DynamicByteProvider).Bytes.ToArray(), (int)mDataForm.HexBox.SelectionStart, (int)mDataForm.HexBox.SelectionLength).Replace("-", " "));
                pArgs.SuppressKeyPress = true;
            }
            else if (mDataForm.HexBox.SelectionLength > 0 && pArgs.Control && pArgs.Shift && pArgs.KeyCode == Keys.C)
            {
                byte[] buffer = new byte[mDataForm.HexBox.SelectionLength];
                Buffer.BlockCopy((mDataForm.HexBox.ByteProvider as DynamicByteProvider).Bytes.ToArray(), (int)mDataForm.HexBox.SelectionStart, buffer, 0, (int)mDataForm.HexBox.SelectionLength);
                mSearchForm.HexBox.ByteProvider.DeleteBytes(0, mSearchForm.HexBox.ByteProvider.Length);
                mSearchForm.HexBox.ByteProvider.InsertBytes(0, buffer);
                pArgs.SuppressKeyPress = true;
            }
        }

        private DialogResult ShowSetupForm()
        {
            return new SetupForm().ShowDialog(this);
        }

        private void SetupAdapter()
        {
            if (mDevice != null)
            {
                mDevice.Close();
            }

            foreach (LibPcapLiveDevice device in LibPcapLiveDeviceList.Instance)
            {
                if (device.Interface.FriendlyName == Config.Instance.Interface)
                {
                    mDevice = device;
                    break;
                }
            }

            if (mDevice == null)
            {
                MessageBox.Show("Invalid configuration. Please re-setup your NCShark configuration.", "NCShark", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (ShowSetupForm() != DialogResult.OK)
                {
                    Close();
                    return;
                }
                SetupAdapter();
            }

            try
            {
                mDevice.Open(DeviceMode.Promiscuous, 2);
            }
            catch
            {
                MessageBox.Show("Failed to set the device in Promiscuous mode!");
                mDevice.Open();
            }

            mDevice.Filter = string.Format("tcp portrange {0}-{1}", Config.Instance.LowPort, Config.Instance.HighPort);
        }

        private void MainForm_Load(object pSender, EventArgs pArgs)
        {
            if (!Config.Instance.LoadedFromFile)
            {
                if (ShowSetupForm() != DialogResult.OK)
                {
                    Close();
                    return;
                }
            }

            SetupAdapter();

            // Create status bar for diagnostics
            StatusStrip statusStrip = new StatusStrip();
            mStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            mStatusLabel.Text = "Initializing...";
            statusStrip.Items.Add(mStatusLabel);
            this.Controls.Add(statusStrip);
            UpdateDiagnosticLabel();

            mTimer.Enabled = true;

            mSearchForm.Show(mDockPanel);
            mDataForm.Show(mDockPanel);
            mStructureForm.Show(mDockPanel);
            mPropertyForm.Show(mDockPanel);
            DockPane rightPane1 = new DockPane(mStructureForm, DockState.DockRight, true);
            DockPane rightPane2 = new DockPane(mPropertyForm, DockState.DockRight, true);
            rightPane1.Show();
            rightPane2.Show();


            foreach (string arg in _startupArguments)
            {
                SessionForm session = NewSession();
                session.OpenReadOnly(arg);
                session.Show(mDockPanel, DockState.Document);
            }
        }

        private void MainForm_FormClosed(object pSender, FormClosedEventArgs pArgs)
        {
            mTimer.Enabled = false;
            if (mDevice != null) mDevice.Close();
            mClosed = true;
        }

        private void mDockPanel_ActiveDocumentChanged(object pSender, EventArgs pArgs)
        {
            if (!mClosed)
            {
                SessionForm session = mDockPanel.ActiveDocument as SessionForm;
                mSearchForm.ComboBox.Items.Clear();
                if (session != null)
                {
                 //   session.RefreshPackets();

                    mSearchForm.RefreshOpcodes(false);
                    session.ReselectPacket();
                }
                else
                {
                    if (mDataForm.HexBox.ByteProvider != null) mDataForm.HexBox.ByteProvider.DeleteBytes(0, mDataForm.HexBox.ByteProvider.Length);
                    mStructureForm.Tree.Nodes.Clear();
                    mPropertyForm.Properties.SelectedObject = null;
                }
            }
        }

        private void mFileImportMenu_Click(object pSender, EventArgs pArgs)
        {
            if (mImportDialog.ShowDialog(this) != DialogResult.OK) return;
            device = new CaptureFileReaderDevice(mImportDialog.FileName);
            device.Open();
            new Thread(ParseImportedFile).Start();
        }

        void ParseImportedFile()
        {
            RawCapture packet = null;
            SessionForm session = null;

            this.Invoke((MethodInvoker)delegate
            {
                while ((packet = device.GetNextPacket()) != null)
                {
                    if (!started)
                        continue;

                    TcpPacket tcpPacket = TcpPacket.GetEncapsulated(Packet.ParsePacket(packet.LinkLayerType, packet.Data));
                    if (tcpPacket == null)
                        continue;

                    if ((tcpPacket.SourcePort < Config.Instance.LowPort || tcpPacket.SourcePort > Config.Instance.HighPort) &&
                        (tcpPacket.DestinationPort < Config.Instance.LowPort || tcpPacket.DestinationPort > Config.Instance.HighPort))
                        continue;
                    try
                    {
                        if (tcpPacket.Syn && !tcpPacket.Ack)
                        {
                            session = NewSession();
                            var res = session.BufferTCPPacket(tcpPacket, packet.Timeval.Date, started);
                            if (res == SessionForm.Results.Continue)
                                session.Show(mDockPanel, DockState.Document);
                        }
                        else if (session != null && session.MatchTCPPacket(tcpPacket))
                        {
                            var res = session.BufferTCPPacket(tcpPacket, packet.Timeval.Date, started);
                            if (res == SessionForm.Results.CloseMe)
                            {
                                session.Close();
                            }
                        }

                    }
                    catch (Exception)
                    {
                        session.Close();
                        session = null;
                    }
                }

                mSearchForm.RefreshOpcodes(false);
            });
        }

        private void mFileOpenMenu_Click(object pSender, EventArgs pArgs)
        {
            if (mOpenDialog.ShowDialog(this) == DialogResult.OK)
            {
                foreach (string path in mOpenDialog.FileNames)
                {
                    SessionForm session = NewSession();
                    session.OpenReadOnly(path);
                    session.Show(mDockPanel, DockState.Document);
                }
                mSearchForm.RefreshOpcodes(false);
            }
        }

        private void mFileQuit_Click(object pSender, EventArgs pArgs)
        {
            Close();
        }

        private void mViewMenu_DropDownOpening(object pSender, EventArgs pArgs)
        {
            mViewSearchMenu.Checked = mSearchForm.Visible;
            mViewDataMenu.Checked = mDataForm.Visible;
            mViewStructureMenu.Checked = mStructureForm.Visible;
            mViewPropertiesMenu.Checked = mPropertyForm.Visible;
        }

        private void mViewSearchMenu_CheckedChanged(object pSender, EventArgs pArgs)
        {
            if (mViewSearchMenu.Checked) mSearchForm.Show();
            else mSearchForm.Hide();

        }

        private void mViewDataMenu_CheckedChanged(object pSender, EventArgs pArgs)
        {
            if (mViewDataMenu.Checked) mDataForm.Show();
            else mDataForm.Hide();
        }

        private void mViewStructureMenu_CheckedChanged(object pSender, EventArgs pArgs)
        {
            if (mViewStructureMenu.Checked) mStructureForm.Show();
            else mStructureForm.Hide();
        }

        private void mViewPropertiesMenu_CheckedChanged(object pSender, EventArgs pArgs)
        {
            if (mViewPropertiesMenu.Checked) mPropertyForm.Show();
            else mPropertyForm.Hide();
        }

        private void mSendPacketMenu_Click(object sender, EventArgs e)
        {
            SessionForm session = mDockPanel.ActiveDocument as SessionForm;
            if (session == null)
            {
                MessageBox.Show("No active session.", "NCShark", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (session.ListView.SelectedItems.Count == 0)
            {
                MessageBox.Show("No packet selected. Please select a packet first.", "NCShark", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            NCPacket packet = session.ListView.SelectedItems[0] as NCPacket;
            if (packet == null) return;

            SendPacketForm sendForm = new SendPacketForm();
            string filter = string.Format("tcp portrange {0}-{1}", Config.Instance.LowPort, Config.Instance.HighPort);
            sendForm.SetDevice(mDevice, filter);
            sendForm.LoadFromPacket(packet, session);
            sendForm.Show();
        }

        List<SessionForm> closes = new List<SessionForm>();

        private void UpdateDiagnosticLabel()
        {
            if (mStatusLabel == null) return;
            string deviceName = (mDevice != null && mDevice.Interface != null) ? mDevice.Interface.FriendlyName : "NONE";
            string deviceStatus = (mDevice != null && mDevice.Opened) ? "OPEN" : "CLOSED";
            string captureStatus = started ? "RUNNING" : "STOPPED";
            mStatusLabel.Text = string.Format("Dev: {0} [{1}] | Ports {2}-{3} | {4} | Pkt: {5} tot, {6} match | SYN: {7} | Ses: {8} | Ex: {9}:{10}",
                deviceName, deviceStatus, Config.Instance.LowPort, Config.Instance.HighPort,
                captureStatus, mTotalPacketsSeen, mTotalPacketsCaptured,
                mTotalSynPackets, mTotalSessionsCreated,
                mDebugPort1, mDebugPort2);
        }

        /// <summary>
        /// Helper to find a matching SessionForm in the DockPanel contents (not MdiChildren).
        /// MdiChildren does NOT contain DockContent sessions managed by WeifenLuo DockPanel.
        /// </summary>
        private SessionForm FindSessionByPacket(TcpPacket pTCPPacket)
        {
            foreach (var content in mDockPanel.Contents)
            {
                SessionForm ses = content as SessionForm;
                if (ses != null && ses.MatchTCPPacket(pTCPPacket))
                    return ses;
            }
            return null;
        }

        /// <summary>
        /// Gets all SessionForm objects currently managed by the DockPanel.
        /// </summary>
        private List<SessionForm> GetSessions()
        {
            List<SessionForm> sessions = new List<SessionForm>();
            foreach (var content in mDockPanel.Contents)
            {
                SessionForm ses = content as SessionForm;
                if (ses != null)
                    sessions.Add(ses);
            }
            return sessions;
        }

        private void mTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                RawCapture packet = null;
                mTimer.Enabled = false;

                DateTime now = DateTime.Now;

                // Use DockPanel contents instead of MdiChildren (DockContent != MDI child)
                List<SessionForm> activeSessions = GetSessions();
                foreach (SessionForm ses in activeSessions)
                {
                    if (ses.CloseMe(now))
                        closes.Add(ses);
                }
                closes.ForEach((a) => { a.Close(); });
                closes.Clear();

                if (mDevice == null || !mDevice.Opened)
                {
                    System.Diagnostics.Debug.WriteLine("[NCShark] Device not ready, skipping tick.");
                    mTimer.Enabled = true;
                    return;
                }

                while ((packet = mDevice.GetNextPacket()) != null) //get packet from device driver
                {
                    if (packet == null) continue;
                    mTotalPacketsSeen++;

                    TcpPacket tcpPacket = TcpPacket.GetEncapsulated(Packet.ParsePacket(packet.LinkLayerType, packet.Data));
                    if (tcpPacket == null) continue;

                    // Track first few unique ports for debugging
                    if (mTotalPacketsSeen <= 20 || mTotalPacketsCaptured == 0)
                    {
                        mDebugPort1 = tcpPacket.SourcePort;
                        mDebugPort2 = tcpPacket.DestinationPort;
                    }

                    // Check if this packet is on the game port range
                    bool matchesPort = (tcpPacket.SourcePort >= Config.Instance.LowPort && tcpPacket.SourcePort <= Config.Instance.HighPort) ||
                                       (tcpPacket.DestinationPort >= Config.Instance.LowPort && tcpPacket.DestinationPort <= Config.Instance.HighPort);
                    if (!matchesPort)
                    {
                        // For first 50 packets, count non-matching too for diagnostics
                        if (mTotalPacketsSeen <= 50)
                        {
                            System.Diagnostics.Debug.WriteLine("[NCShark] Non-game packet: " + tcpPacket.SourcePort + " -> " + tcpPacket.DestinationPort);
                        }
                        continue;
                    }

                    mTotalPacketsCaptured++;

                    SessionForm session = null;
                    bool isNewSession = false;

                    try
                    {
                        // Try to find an existing session for this packet
                        session = FindSessionByPacket(tcpPacket);

                        // No session found — this could be:
                        // 1. A new SYN packet (new connection)
                        // 2. A data packet from an already-established connection (NCShark started mid-game)
                        if (session == null)
                        {
                            session = NewSession();
                            isNewSession = true;

                            if (tcpPacket.Syn && !tcpPacket.Ack)
                            {
                                mTotalSynPackets++;
                                System.Diagnostics.Debug.WriteLine("[NCShark] New SYN packet: " + tcpPacket.SourcePort + " -> " + tcpPacket.DestinationPort);
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("[NCShark] New data session (mid-game start): " + tcpPacket.SourcePort + " -> " + tcpPacket.DestinationPort);
                            }
                        }

                        if (session != null)
                        {
                            var res = session.BufferTCPPacket(tcpPacket, packet.Timeval.Date, started);

                            if (isNewSession && res == SessionForm.Results.Continue)
                            {
                                mTotalSessionsCreated++;
                                System.Diagnostics.Debug.WriteLine("[NCShark] Session shown: Port " + session.LocalPort + " -> " + session.RemotePort);
                                session.Show(mDockPanel, DockState.Document);
                            }

                            if (res == SessionForm.Results.CloseMe)
                            {
                                session.Close();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("[NCShark] Packet processing error: " + ex.ToString());
                        if (session != null && isNewSession)
                        {
                            session.Close();
                        }
                        session = null;
                    }
                }

                // Update diagnostic label periodically
                if (mTotalPacketsSeen % 10 == 0 || mTotalPacketsCaptured > 0)
                {
                    UpdateDiagnosticLabel();
                }
                mTimer.Enabled = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[NCShark] Timer tick error: " + ex.ToString());
                // CRITICAL: must re-enable timer even on exception, otherwise capture stops forever
                if (mDevice != null && !mDevice.Opened)
                {
                    try { mDevice.Open(DeviceMode.Promiscuous, 1); }
                    catch { System.Diagnostics.Debug.WriteLine("[NCShark] Failed to reopen device."); }
                }
                mTimer.Enabled = true; // ALWAYS re-enable timer
            }
        }

        private bool started = true; // Starts capturing immediately (packets visible from game start)
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            if (started)
            {
                started = false; //stop
                //mStopStartButton.Image = Properties.Resources.Button_Blank_Green_icon;
                mStopStartButton.Text = "Start sniffing";
            }
            else
            {
                started = true; //start
                //mStopStartButton.Image = Properties.Resources.Button_Blank_Red_icon;
                mStopStartButton.Text = "Stop sniffing";
            }
            UpdateDiagnosticLabel();
        }

        private void helpToolStripButton_Click(object sender, EventArgs e)
        {
            if (System.IO.File.Exists("Readme.txt"))
            {
                System.Diagnostics.Process.Start(Environment.CurrentDirectory + @"\Readme.txt");
            }
        }

        private void saveToolStripButton_Click(object sender, EventArgs e)
        {
            SessionForm session = mDockPanel.ActiveDocument as SessionForm;
            if (session != null)
            {
                session.RunSaveCMD();
            }
        }

        private void importJavapropertiesFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new frmImportProps().ShowDialog();
        }

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                bool okay = false;
                foreach (var file in files)
                {
                    switch (System.IO.Path.GetExtension(file))
                    {
                        case ".msb":
                        case ".pcap":
                        case ".txt":
                            okay = true;
                            continue;
                    }
                }

                e.Effect = okay ? DragDropEffects.Move : DragDropEffects.None;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var file in files)
            {
                if (!System.IO.File.Exists(file)) continue;

                switch (System.IO.Path.GetExtension(file))
                {
                    case ".msb":
                        {
                            SessionForm session = NewSession();
                            session.OpenReadOnly(file);
                            session.Show(mDockPanel, DockState.Document);
                            mSearchForm.RefreshOpcodes(false);
                            break;
                        }
                    case ".pcap":
                        {
                            device = new CaptureFileReaderDevice(file);
                            device.Open();
                            ParseImportedFile();
                            break;
                        }
                    case ".txt":
                        {
                            ReadMSnifferFile(file);
                            break;
                        }
                }
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Try to close all sessions
            List<SessionForm> sessionForms = new List<SessionForm>();
            
            foreach (var form in mDockPanel.Contents)
                if (form is SessionForm)
                    sessionForms.Add(form as SessionForm);

            int sessions = sessionForms.Count;
            bool doSaveQuestioning = true;
            if (sessions > 5)
            {
                doSaveQuestioning = MessageBox.Show("You've got " + sessions + " sessions open. Say 'Yes' if you want to get a question for each session, 'No' if you want to quit NCShark.", "NCShark", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes;
            }

            while (doSaveQuestioning && sessionForms.Count > 0)
            {
                SessionForm ses = sessionForms[0];
                if (!ses.Saved)
                {
                    ses.Focus();
                    DialogResult result = MessageBox.Show(string.Format("Do you want to save the session '{0}'?", ses.Text), "NCShark", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);

                    if (result == DialogResult.Yes)
                    {
                        ses.RunSaveCMD();
                    }
                    else if (result == DialogResult.Cancel)
                    {
                        e.Cancel = true;

                        return;
                    }
                }
                mDockPanel.Contents.Remove(ses);
                sessionForms.Remove(ses);
            }

            DefinitionsContainer.Instance.Save();
        }

        private void setupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ShowSetupForm() == System.Windows.Forms.DialogResult.OK)
            {
                // Restart sniffing
                var lastTimerState = mTimer.Enabled;
                if (lastTimerState) mTimer.Enabled = false;

                SetupAdapter();

                if (lastTimerState) mTimer.Enabled = true;
            }
        }

        private void importMSnifferToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Select MSniffer logfile";
            ofd.Filter = "All files|*.*";
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                ReadMSnifferFile(ofd.FileName);
        }

        private void ReadMSnifferFile(string filename)
        {
            SessionForm currentSession = null;
            Regex captureRegex = new Regex(@"Capturing MapleStory version (\d+) on ([0-9\.]+):(\d+) with unknown ""(.*)"".*");
            using (StreamReader sr = new StreamReader(filename))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    if (line == "" || (line[0] != '[' && line[0] != 'C')) continue;

                    if (line[0] == 'C')
                    {
                        // Most likely capturing text
                        var matches = captureRegex.Match(line);
                        if (matches.Captures.Count == 0) continue;
                        
                        Console.WriteLine("Version: {0}.{1} IP {2} Port {3}", matches.Groups[1].Value, matches.Groups[4].Value, matches.Groups[2].Value, matches.Groups[3].Value);

                        if (currentSession != null)
                            currentSession.Show(mDockPanel, DockState.Document);

                        currentSession = NewSession();
                        currentSession.SetGameInfo(ushort.Parse(matches.Groups[1].Value), matches.Groups[4].Value, 8, matches.Groups[2].Value, ushort.Parse(matches.Groups[3].Value));

                    }
                    else if (line[0] == '[' && currentSession != null)
                        currentSession.ParseMSnifferLine(line);
                }
            }

            if (currentSession != null)
                currentSession.Show(mDockPanel, DockState.Document);
        }

        /// <summary>
        /// Creates a SendPacketForm pre-populated with data from the active session's selected packet.
        /// Called from SessionForm's "Resend packet" context menu.
        /// </summary>
        public void ShowSendPacketFormForSelectedPacket()
        {
            SessionForm session = mDockPanel.ActiveDocument as SessionForm;
            if (session == null)
            {
                MessageBox.Show("No active session.", "NCShark", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            NCPacket packet = session.SelectedPacket;
            if (packet == null)
            {
                MessageBox.Show("No packet selected.", "NCShark", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (mDevice == null)
            {
                MessageBox.Show("No capture device available. Please setup NCShark first.", "NCShark", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SendPacketForm sendForm = new SendPacketForm();
            sendForm.SetDevice(mDevice, mDevice.Filter);
            sendForm.LoadFromPacket(packet, session);
            sendForm.Show();
        }
    }
}
