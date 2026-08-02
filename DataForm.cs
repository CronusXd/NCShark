//NCShark - By AlSch092 @ Github, thanks to @Diamondo25 for MapleShark
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace NCShark
{
    public partial class DataForm : DockContent
    {
        public DataForm()
        {
            InitializeComponent();
        }

        public MainForm MainForm { get { return ParentForm as MainForm; } }
        public HexBox HexBox { get { return mHex; } }

        private void mHex_SelectionLengthChanged(object pSender, EventArgs pArgs)
        {
            if (mHex.SelectionLength == 0) MainForm.PropertyForm.Properties.SelectedObject = null;
            else
            {
                byte[] buffer = null;
                StructureNode match = null;
                foreach (TreeNode node in MainForm.StructureForm.Tree.Nodes)
                {
                    StructureNode realNode = node as StructureNode;
                    buffer = realNode.Buffer;
                    if (mHex.SelectionStart == realNode.Cursor && mHex.SelectionLength == realNode.Length)
                    {
                        match = realNode;
                        break;
                    }
                }
                MainForm.StructureForm.Tree.SelectedNode = match;
                if (buffer != null) MainForm.PropertyForm.Properties.SelectedObject = new StructureSegment(buffer, (int)mHex.SelectionStart, (int)mHex.SelectionLength, MainForm.Locale);
                else MainForm.PropertyForm.Properties.SelectedObject = null;
            }
        }

        private void mHex_KeyDown(object pSender, KeyEventArgs pArgs)
        {
            MainForm.CopyPacketHex(pArgs);
        }

        private void btnApplyChanges_Click(object sender, EventArgs e)
        {
            if (MainForm == null) return;

            SessionForm session = MainForm.ActiveSession;
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

            DynamicByteProvider provider = mHex.ByteProvider as DynamicByteProvider;
            if (provider == null) return;

            // Read current bytes from HexBox and update the NCPacket buffer
            byte[] editedBytes = provider.Bytes.ToArray();
            packet.Buffer = editedBytes;
            packet.Edited = true;

            // Update any open SendPacketForm that was loaded from this packet's session
            foreach (Form form in Application.OpenForms)
            {
                SendPacketForm sendForm = form as SendPacketForm;
                if (sendForm != null && !form.IsDisposed)
                {
                    // Re-read the buffer from the updated NCPacket
                    sendForm.RefreshPacketData(packet);
                }
            }
        }
    }
}
