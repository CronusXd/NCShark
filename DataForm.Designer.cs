//NCShark - By AlSch092 @ Github, thanks to @Diamondo25 for MapleShark
namespace NCShark
{
    partial class DataForm
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
            this.mHex = new System.Windows.Forms.HexBox();
            this.panelActions = new System.Windows.Forms.Panel();
            this.btnApplyChanges = new System.Windows.Forms.Button();
            this.panelActions.SuspendLayout();
            this.SuspendLayout();
            // 
            // mHex
            // 
            this.mHex.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mHex.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.mHex.LineInfoForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(192)))));
            this.mHex.LineInfoVisible = true;
            this.mHex.Location = new System.Drawing.Point(0, 0);
            this.mHex.Name = "mHex";
            this.mHex.ReadOnly = false;
            this.mHex.SelectionBackColor = System.Drawing.Color.Black;
            this.mHex.ShadowSelectionColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(60)))), ((int)(((byte)(188)))), ((int)(((byte)(255)))));
            this.mHex.Size = new System.Drawing.Size(613, 147);
            this.mHex.StringViewVisible = true;
            this.mHex.TabIndex = 2;
            this.mHex.UseFixedBytesPerLine = true;
            this.mHex.VScrollBarVisible = true;
            this.mHex.SelectionLengthChanged += new System.EventHandler(this.mHex_SelectionLengthChanged);
            this.mHex.KeyDown += new System.Windows.Forms.KeyEventHandler(this.mHex_KeyDown);
            // 
            // panelActions
            // 
            this.panelActions.Controls.Add(this.btnApplyChanges);
            this.panelActions.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelActions.Location = new System.Drawing.Point(0, 147);
            this.panelActions.Name = "panelActions";
            this.panelActions.Size = new System.Drawing.Size(613, 28);
            this.panelActions.TabIndex = 3;
            // 
            // btnApplyChanges
            // 
            this.btnApplyChanges.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnApplyChanges.Location = new System.Drawing.Point(521, 3);
            this.btnApplyChanges.Name = "btnApplyChanges";
            this.btnApplyChanges.Size = new System.Drawing.Size(89, 23);
            this.btnApplyChanges.TabIndex = 4;
            this.btnApplyChanges.Text = "Apply Changes";
            this.btnApplyChanges.UseVisualStyleBackColor = true;
            this.btnApplyChanges.Click += new System.EventHandler(this.btnApplyChanges_Click);
            // 
            // DataForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(613, 175);
            this.Controls.Add(this.mHex);
            this.Controls.Add(this.panelActions);
            this.DockAreas = ((WeifenLuo.WinFormsUI.Docking.DockAreas)(((((WeifenLuo.WinFormsUI.Docking.DockAreas.Float | WeifenLuo.WinFormsUI.Docking.DockAreas.DockLeft)
                        | WeifenLuo.WinFormsUI.Docking.DockAreas.DockRight)
                        | WeifenLuo.WinFormsUI.Docking.DockAreas.DockTop)
                        | WeifenLuo.WinFormsUI.Docking.DockAreas.DockBottom)));
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.HideOnClose = true;
            this.Name = "DataForm";
            this.ShowHint = WeifenLuo.WinFormsUI.Docking.DockState.DockBottom;
            this.Text = "Data";
            this.panelActions.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.HexBox mHex;
        private System.Windows.Forms.Panel panelActions;
        private System.Windows.Forms.Button btnApplyChanges;
    }
}