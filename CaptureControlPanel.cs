//NCShark - By AlSch092 @ Github, thanks to @Diamondo25 for MapleShark
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace NCShark
{
    public partial class CaptureControlPanel : DockContent
    {
        private MainForm mainForm;
        private CheckBox chkUniversalCapture;
        private CheckBox chkAutoDetectSessions;
        private ComboBox cmbPacketTypes;
        private ListBox lstActiveSessions;
        private Button btnRefreshSessions;
        private Button btnClearFilters;
        private Button btnAddFilter;
        private ListBox lstFilterRules;
        private Button btnEditFilter;
        private Button btnDeleteFilter;
        private Button btnEnableDisableFilter;

        public CaptureControlPanel(MainForm parent)
        {
            mainForm = parent;
            InitializeComponent();
            SetupEventHandlers();
            RefreshFilterRules();
            RefreshActiveSessions();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Configurar o formulário
            this.Text = "Capture Control";
            this.Size = new Size(300, 500);
            this.DockAreas = DockAreas.DockLeft | DockAreas.DockRight | DockAreas.DockTop | DockAreas.DockBottom | DockAreas.Float;

            // Universal Capture Mode
            chkUniversalCapture = new CheckBox
            {
                Text = "Universal Capture Mode",
                Location = new Point(10, 10),
                Size = new Size(200, 20),
                Checked = mainForm?.IsUniversalCaptureMode ?? true
            };

            // Auto Detect Sessions
            chkAutoDetectSessions = new CheckBox
            {
                Text = "Auto Detect Sessions",
                Location = new Point(10, 35),
                Size = new Size(200, 20),
                Checked = true
            };

            // Packet Type Filter
            Label lblPacketTypes = new Label
            {
                Text = "Filter by Packet Type:",
                Location = new Point(10, 65),
                Size = new Size(120, 20)
            };

            cmbPacketTypes = new ComboBox
            {
                Location = new Point(10, 85),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // Popular tipos de pacotes
            cmbPacketTypes.Items.Add("All Types");
            foreach (PacketType packetType in Enum.GetValues(typeof(PacketType)))
            {
                cmbPacketTypes.Items.Add(PacketClassifier.GetFriendlyName(packetType));
            }
            cmbPacketTypes.SelectedIndex = 0;

            // Active Sessions
            Label lblSessions = new Label
            {
                Text = "Active Sessions:",
                Location = new Point(10, 120),
                Size = new Size(120, 20)
            };

            lstActiveSessions = new ListBox
            {
                Location = new Point(10, 140),
                Size = new Size(200, 80),
                SelectionMode = SelectionMode.One
            };

            btnRefreshSessions = new Button
            {
                Text = "Refresh",
                Location = new Point(220, 140),
                Size = new Size(60, 25)
            };

            // Filter Rules
            Label lblFilters = new Label
            {
                Text = "Filter Rules:",
                Location = new Point(10, 230),
                Size = new Size(120, 20)
            };

            lstFilterRules = new ListBox
            {
                Location = new Point(10, 250),
                Size = new Size(200, 100),
                SelectionMode = SelectionMode.One
            };

            btnAddFilter = new Button
            {
                Text = "Add",
                Location = new Point(10, 360),
                Size = new Size(50, 25)
            };

            btnEditFilter = new Button
            {
                Text = "Edit",
                Location = new Point(70, 360),
                Size = new Size(50, 25)
            };

            btnDeleteFilter = new Button
            {
                Text = "Delete",
                Location = new Point(130, 360),
                Size = new Size(50, 25)
            };

            btnEnableDisableFilter = new Button
            {
                Text = "Toggle",
                Location = new Point(190, 360),
                Size = new Size(50, 25)
            };

            btnClearFilters = new Button
            {
                Text = "Clear All Filters",
                Location = new Point(10, 395),
                Size = new Size(120, 25)
            };

            // Adicionar controles ao formulário
            this.Controls.AddRange(new Control[] {
                chkUniversalCapture,
                chkAutoDetectSessions,
                lblPacketTypes,
                cmbPacketTypes,
                lblSessions,
                lstActiveSessions,
                btnRefreshSessions,
                lblFilters,
                lstFilterRules,
                btnAddFilter,
                btnEditFilter,
                btnDeleteFilter,
                btnEnableDisableFilter,
                btnClearFilters
            });

            this.ResumeLayout(false);
        }

        private void SetupEventHandlers()
        {
            chkUniversalCapture.CheckedChanged += ChkUniversalCapture_CheckedChanged;
            cmbPacketTypes.SelectedIndexChanged += CmbPacketTypes_SelectedIndexChanged;
            btnRefreshSessions.Click += BtnRefreshSessions_Click;
            btnAddFilter.Click += BtnAddFilter_Click;
            btnEditFilter.Click += BtnEditFilter_Click;
            btnDeleteFilter.Click += BtnDeleteFilter_Click;
            btnEnableDisableFilter.Click += BtnEnableDisableFilter_Click;
            btnClearFilters.Click += BtnClearFilters_Click;
            lstFilterRules.DoubleClick += LstFilterRules_DoubleClick;
        }

        private void ChkUniversalCapture_CheckedChanged(object sender, EventArgs e)
        {
            if (mainForm != null)
            {
                mainForm.ToggleUniversalCaptureMode();
            }
        }

        private void CmbPacketTypes_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Implementar filtro por tipo de pacote
            if (cmbPacketTypes.SelectedIndex > 0)
            {
                PacketType selectedType = (PacketType)(cmbPacketTypes.SelectedIndex - 1);
                // Aplicar filtro de tipo de pacote
                ApplyPacketTypeFilter(selectedType);
            }
            else
            {
                // Remover filtro de tipo
                RemovePacketTypeFilter();
            }
        }

        private void BtnRefreshSessions_Click(object sender, EventArgs e)
        {
            RefreshActiveSessions();
        }

        private void BtnAddFilter_Click(object sender, EventArgs e)
        {
            // Abrir formulário para criar nova regra de filtro
            using (var filterForm = new FilterRuleForm())
            {
                if (filterForm.ShowDialog() == DialogResult.OK)
                {
                    mainForm?.FilterRuleManager.AddRule(filterForm.CreatedRule);
                    RefreshFilterRules();
                }
            }
        }

        private void BtnEditFilter_Click(object sender, EventArgs e)
        {
            if (lstFilterRules.SelectedItem is FilterRule selectedRule)
            {
                using (var filterForm = new FilterRuleForm(selectedRule))
                {
                    if (filterForm.ShowDialog() == DialogResult.OK)
                    {
                        RefreshFilterRules();
                    }
                }
            }
        }

        private void BtnDeleteFilter_Click(object sender, EventArgs e)
        {
            if (lstFilterRules.SelectedItem is FilterRule selectedRule)
            {
                if (MessageBox.Show($"Are you sure you want to delete the filter '{selectedRule.Name}'?", 
                    "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    mainForm?.FilterRuleManager.RemoveRule(selectedRule);
                    RefreshFilterRules();
                }
            }
        }

        private void BtnEnableDisableFilter_Click(object sender, EventArgs e)
        {
            if (lstFilterRules.SelectedItem is FilterRule selectedRule)
            {
                selectedRule.Enabled = !selectedRule.Enabled;
                selectedRule.ModifiedDate = DateTime.Now;
                mainForm?.FilterRuleManager.SaveRules();
                RefreshFilterRules();
            }
        }

        private void BtnClearFilters_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to clear all filter rules?", 
                "Confirm Clear", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                mainForm?.FilterRuleManager.ClearRules();
                RefreshFilterRules();
            }
        }

        private void LstFilterRules_DoubleClick(object sender, EventArgs e)
        {
            BtnEditFilter_Click(sender, e);
        }

        private void ApplyPacketTypeFilter(PacketType packetType)
        {
            // Criar filtro temporário para tipo de pacote
            var filter = PacketFilter.CreateByPacketType(packetType, FilterAction.Capture);
            var rule = new FilterRule($"Type Filter: {PacketClassifier.GetFriendlyName(packetType)}", filter, FilterAction.Capture);
            rule.Priority = 50; // Prioridade média
            
            mainForm?.FilterRuleManager.AddRule(rule);
            RefreshFilterRules();
        }

        private void RemovePacketTypeFilter()
        {
            // Remover filtros de tipo de pacote
            var typeRules = mainForm?.FilterRuleManager.Rules.Where(r => r.Name.StartsWith("Type Filter:")).ToList();
            if (typeRules != null)
            {
                foreach (var rule in typeRules)
                {
                    mainForm.FilterRuleManager.RemoveRule(rule);
                }
            }
            RefreshFilterRules();
        }

        public void RefreshFilterRules()
        {
            lstFilterRules.Items.Clear();
            if (mainForm?.FilterRuleManager != null)
            {
                foreach (var rule in mainForm.FilterRuleManager.Rules)
                {
                    lstFilterRules.Items.Add(rule);
                }
            }
        }

        public void RefreshActiveSessions()
        {
            lstActiveSessions.Items.Clear();
            if (mainForm != null)
            {
                var sessions = mainForm.GetActiveSessions();
                foreach (var session in sessions)
                {
                    lstActiveSessions.Items.Add($"Session: {session.Text} ({session.mPackets?.Count ?? 0} packets)");
                }
            }
        }

        public MainForm MainForm => mainForm;
    }
}