//NCShark - By AlSch092 @ Github, thanks to @Diamondo25 for MapleShark
namespace NCShark
{
    partial class SendPacketForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.textBox_Send = new System.Windows.Forms.TextBox();
            this.button_SendPacket = new System.Windows.Forms.Button();
            this.lblSourceIp = new System.Windows.Forms.Label();
            this.txtSourceIp = new System.Windows.Forms.TextBox();
            this.lblSourcePort = new System.Windows.Forms.Label();
            this.txtSourcePort = new System.Windows.Forms.TextBox();
            this.lblDestIp = new System.Windows.Forms.Label();
            this.txtDestIp = new System.Windows.Forms.TextBox();
            this.lblDestPort = new System.Windows.Forms.Label();
            this.txtDestPort = new System.Windows.Forms.TextBox();
            this.lblOpcode = new System.Windows.Forms.Label();
            this.txtOpcode = new System.Windows.Forms.TextBox();
            this.lblSequence = new System.Windows.Forms.Label();
            this.txtSequence = new System.Windows.Forms.TextBox();
            this.lblAck = new System.Windows.Forms.Label();
            this.txtAck = new System.Windows.Forms.TextBox();
            this.txtHexData = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // lblSourceIp
            // 
            this.lblSourceIp.AutoSize = true;
            this.lblSourceIp.Location = new System.Drawing.Point(12, 15);
            this.lblSourceIp.Name = "lblSourceIp";
            this.lblSourceIp.Size = new System.Drawing.Size(45, 13);
            this.lblSourceIp.TabIndex = 0;
            this.lblSourceIp.Text = "Src IP:";
            // 
            // txtSourceIp
            // 
            this.txtSourceIp.Location = new System.Drawing.Point(60, 12);
            this.txtSourceIp.Name = "txtSourceIp";
            this.txtSourceIp.ReadOnly = true;
            this.txtSourceIp.Size = new System.Drawing.Size(120, 20);
            this.txtSourceIp.TabIndex = 1;
            // 
            // lblSourcePort
            // 
            this.lblSourcePort.AutoSize = true;
            this.lblSourcePort.Location = new System.Drawing.Point(186, 15);
            this.lblSourcePort.Name = "lblSourcePort";
            this.lblSourcePort.Size = new System.Drawing.Size(26, 13);
            this.lblSourcePort.TabIndex = 2;
            this.lblSourcePort.Text = "Port:";
            // 
            // txtSourcePort
            // 
            this.txtSourcePort.Location = new System.Drawing.Point(215, 12);
            this.txtSourcePort.Name = "txtSourcePort";
            this.txtSourcePort.ReadOnly = true;
            this.txtSourcePort.Size = new System.Drawing.Size(55, 20);
            this.txtSourcePort.TabIndex = 3;
            // 
            // lblDestIp
            // 
            this.lblDestIp.AutoSize = true;
            this.lblDestIp.Location = new System.Drawing.Point(276, 15);
            this.lblDestIp.Name = "lblDestIp";
            this.lblDestIp.Size = new System.Drawing.Size(42, 13);
            this.lblDestIp.TabIndex = 4;
            this.lblDestIp.Text = "Dst IP:";
            // 
            // txtDestIp
            // 
            this.txtDestIp.Location = new System.Drawing.Point(324, 12);
            this.txtDestIp.Name = "txtDestIp";
            this.txtDestIp.ReadOnly = true;
            this.txtDestIp.Size = new System.Drawing.Size(120, 20);
            this.txtDestIp.TabIndex = 5;
            // 
            // lblDestPort
            // 
            this.lblDestPort.AutoSize = true;
            this.lblDestPort.Location = new System.Drawing.Point(450, 15);
            this.lblDestPort.Name = "lblDestPort";
            this.lblDestPort.Size = new System.Drawing.Size(26, 13);
            this.lblDestPort.TabIndex = 6;
            this.lblDestPort.Text = "Port:";
            // 
            // txtDestPort
            // 
            this.txtDestPort.Location = new System.Drawing.Point(479, 12);
            this.txtDestPort.Name = "txtDestPort";
            this.txtDestPort.ReadOnly = true;
            this.txtDestPort.Size = new System.Drawing.Size(55, 20);
            this.txtDestPort.TabIndex = 7;
            // 
            // lblOpcode
            // 
            this.lblOpcode.AutoSize = true;
            this.lblOpcode.Location = new System.Drawing.Point(540, 15);
            this.lblOpcode.Name = "lblOpcode";
            this.lblOpcode.Size = new System.Drawing.Size(47, 13);
            this.lblOpcode.TabIndex = 8;
            this.lblOpcode.Text = "Opcode:";
            // 
            // txtOpcode
            // 
            this.txtOpcode.Location = new System.Drawing.Point(593, 12);
            this.txtOpcode.Name = "txtOpcode";
            this.txtOpcode.ReadOnly = true;
            this.txtOpcode.Size = new System.Drawing.Size(70, 20);
            this.txtOpcode.TabIndex = 9;
            // 
            // lblSequence
            // 
            this.lblSequence.AutoSize = true;
            this.lblSequence.Location = new System.Drawing.Point(12, 41);
            this.lblSequence.Name = "lblSequence";
            this.lblSequence.Size = new System.Drawing.Size(56, 13);
            this.lblSequence.TabIndex = 10;
            this.lblSequence.Text = "Sequence:";
            // 
            // txtSequence
            // 
            this.txtSequence.Location = new System.Drawing.Point(72, 38);
            this.txtSequence.Name = "txtSequence";
            this.txtSequence.ReadOnly = true;
            this.txtSequence.Size = new System.Drawing.Size(120, 20);
            this.txtSequence.TabIndex = 11;
            // 
            // lblAck
            // 
            this.lblAck.AutoSize = true;
            this.lblAck.Location = new System.Drawing.Point(198, 41);
            this.lblAck.Name = "lblAck";
            this.lblAck.Size = new System.Drawing.Size(58, 13);
            this.lblAck.TabIndex = 12;
            this.lblAck.Text = "Ack Num:";
            // 
            // txtAck
            // 
            this.txtAck.Location = new System.Drawing.Point(262, 38);
            this.txtAck.Name = "txtAck";
            this.txtAck.ReadOnly = true;
            this.txtAck.Size = new System.Drawing.Size(120, 20);
            this.txtAck.TabIndex = 13;
            // 
            // txtHexData
            // 
            this.txtHexData.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtHexData.Location = new System.Drawing.Point(12, 64);
            this.txtHexData.Multiline = true;
            this.txtHexData.Name = "txtHexData";
            this.txtHexData.ReadOnly = true;
            this.txtHexData.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtHexData.Size = new System.Drawing.Size(726, 60);
            this.txtHexData.TabIndex = 14;
            // 
            // textBox_Send
            // 
            this.textBox_Send.Location = new System.Drawing.Point(12, 130);
            this.textBox_Send.Name = "textBox_Send";
            this.textBox_Send.Size = new System.Drawing.Size(645, 20);
            this.textBox_Send.TabIndex = 15;
            // 
            // button_SendPacket
            // 
            this.button_SendPacket.Location = new System.Drawing.Point(663, 128);
            this.button_SendPacket.Name = "button_SendPacket";
            this.button_SendPacket.Size = new System.Drawing.Size(75, 23);
            this.button_SendPacket.TabIndex = 16;
            this.button_SendPacket.Text = "Send";
            this.button_SendPacket.UseVisualStyleBackColor = true;
            this.button_SendPacket.Click += new System.EventHandler(this.button_SendPacket_Click);
            // 
            // SendPacketForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(750, 165);
            this.Controls.Add(this.txtHexData);
            this.Controls.Add(this.txtAck);
            this.Controls.Add(this.lblAck);
            this.Controls.Add(this.txtSequence);
            this.Controls.Add(this.lblSequence);
            this.Controls.Add(this.txtOpcode);
            this.Controls.Add(this.lblOpcode);
            this.Controls.Add(this.txtDestPort);
            this.Controls.Add(this.lblDestPort);
            this.Controls.Add(this.txtDestIp);
            this.Controls.Add(this.lblDestIp);
            this.Controls.Add(this.txtSourcePort);
            this.Controls.Add(this.lblSourcePort);
            this.Controls.Add(this.txtSourceIp);
            this.Controls.Add(this.lblSourceIp);
            this.Controls.Add(this.button_SendPacket);
            this.Controls.Add(this.textBox_Send);
            this.Name = "SendPacketForm";
            this.ShowIcon = false;
            this.Text = "Send Packet";
            this.Load += new System.EventHandler(this.SendPacketForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBox_Send;
        private System.Windows.Forms.Button button_SendPacket;
        private System.Windows.Forms.Label lblSourceIp;
        private System.Windows.Forms.TextBox txtSourceIp;
        private System.Windows.Forms.Label lblSourcePort;
        private System.Windows.Forms.TextBox txtSourcePort;
        private System.Windows.Forms.Label lblDestIp;
        private System.Windows.Forms.TextBox txtDestIp;
        private System.Windows.Forms.Label lblDestPort;
        private System.Windows.Forms.TextBox txtDestPort;
        private System.Windows.Forms.Label lblOpcode;
        private System.Windows.Forms.TextBox txtOpcode;
        private System.Windows.Forms.Label lblSequence;
        private System.Windows.Forms.TextBox txtSequence;
        private System.Windows.Forms.Label lblAck;
        private System.Windows.Forms.TextBox txtAck;
        private System.Windows.Forms.TextBox txtHexData;
    }
}