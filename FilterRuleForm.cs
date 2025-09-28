//NCShark - By AlSch092 @ Github, thanks to @Diamondo25 for MapleShark
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace NCShark
{
    public partial class FilterRuleForm : Form
    {
        private FilterRule originalRule;
        private FilterRule workingRule;
        private bool isEditing;

        // Controles do formulário
        private TextBox txtName;
        private TextBox txtDescription;
        private CheckBox chkEnabled;
        private NumericUpDown numPriority;
        private ComboBox cmbAction;
        private GroupBox grpBasicFilters;
        private GroupBox grpAdvancedFilters;
        private Button btnOK;
        private Button btnCancel;

        public FilterRule CreatedRule { get; private set; }

        public FilterRuleForm() : this(null)
        {
        }

        public FilterRuleForm(FilterRule ruleToEdit)
        {
            originalRule = ruleToEdit;
            isEditing = ruleToEdit != null;
            InitializeComponent();
            SetupEventHandlers();
            LoadRuleData();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Configurar o formulário
            this.Text = isEditing ? "Edit Filter Rule" : "Create Filter Rule";
            this.Size = new Size(500, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Nome
            Label lblName = new Label
            {
                Text = "Name:",
                Location = new Point(10, 15),
                Size = new Size(50, 20)
            };

            txtName = new TextBox
            {
                Location = new Point(70, 12),
                Size = new Size(200, 25),
                Text = ""
            };

            // Descrição
            Label lblDescription = new Label
            {
                Text = "Description:",
                Location = new Point(10, 45),
                Size = new Size(70, 20)
            };

            txtDescription = new TextBox
            {
                Location = new Point(90, 42),
                Size = new Size(300, 25),
                Text = ""
            };

            // Habilitado
            chkEnabled = new CheckBox
            {
                Text = "Enabled",
                Location = new Point(10, 75),
                Size = new Size(80, 20),
                Checked = true
            };

            // Prioridade
            Label lblPriority = new Label
            {
                Text = "Priority:",
                Location = new Point(100, 75),
                Size = new Size(50, 20)
            };

            numPriority = new NumericUpDown
            {
                Location = new Point(160, 72),
                Size = new Size(60, 25),
                Minimum = 0,
                Maximum = 1000,
                Value = 0
            };

            // Ação
            Label lblAction = new Label
            {
                Text = "Action:",
                Location = new Point(240, 75),
                Size = new Size(50, 20)
            };

            cmbAction = new ComboBox
            {
                Location = new Point(300, 72),
                Size = new Size(100, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // Popular ações
            foreach (FilterAction action in Enum.GetValues(typeof(FilterAction)))
            {
                cmbAction.Items.Add(action.ToString());
            }
            cmbAction.SelectedIndex = 0;

            // Grupo de filtros básicos
            grpBasicFilters = new GroupBox
            {
                Text = "Basic Filters",
                Location = new Point(10, 110),
                Size = new Size(460, 200)
            };

            // Filtros básicos
            SetupBasicFilters();

            // Grupo de filtros avançados
            grpAdvancedFilters = new GroupBox
            {
                Text = "Advanced Filters",
                Location = new Point(10, 320),
                Size = new Size(460, 200)
            };

            // Filtros avançados
            SetupAdvancedFilters();

            // Botões
            btnOK = new Button
            {
                Text = "OK",
                Location = new Point(320, 540),
                Size = new Size(75, 30),
                DialogResult = DialogResult.OK
            };

            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(405, 540),
                Size = new Size(75, 30),
                DialogResult = DialogResult.Cancel
            };

            // Adicionar controles ao formulário
            this.Controls.AddRange(new Control[] {
                lblName, txtName,
                lblDescription, txtDescription,
                chkEnabled, lblPriority, numPriority,
                lblAction, cmbAction,
                grpBasicFilters, grpAdvancedFilters,
                btnOK, btnCancel
            });

            this.ResumeLayout(false);
        }

        private void SetupBasicFilters()
        {
            // Opcode
            Label lblOpcode = new Label
            {
                Text = "Opcode:",
                Location = new Point(10, 25),
                Size = new Size(60, 20)
            };

            TextBox txtOpcode = new TextBox
            {
                Location = new Point(80, 22),
                Size = new Size(80, 25),
                Text = "",
                PlaceholderText = "0x0000"
            };

            // Tipo de pacote
            Label lblPacketType = new Label
            {
                Text = "Packet Type:",
                Location = new Point(180, 25),
                Size = new Size(80, 20)
            };

            ComboBox cmbPacketType = new ComboBox
            {
                Location = new Point(270, 22),
                Size = new Size(120, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            cmbPacketType.Items.Add("Any");
            foreach (PacketType packetType in Enum.GetValues(typeof(PacketType)))
            {
                cmbPacketType.Items.Add(PacketClassifier.GetFriendlyName(packetType));
            }
            cmbPacketType.SelectedIndex = 0;

            // Direção
            Label lblDirection = new Label
            {
                Text = "Direction:",
                Location = new Point(10, 55),
                Size = new Size(60, 20)
            };

            ComboBox cmbDirection = new ComboBox
            {
                Location = new Point(80, 52),
                Size = new Size(80, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            cmbDirection.Items.AddRange(new string[] { "Any", "Outbound", "Inbound" });
            cmbDirection.SelectedIndex = 0;

            // Tamanho mínimo
            Label lblMinSize = new Label
            {
                Text = "Min Size:",
                Location = new Point(180, 55),
                Size = new Size(60, 20)
            };

            NumericUpDown numMinSize = new NumericUpDown
            {
                Location = new Point(250, 52),
                Size = new Size(60, 25),
                Minimum = 0,
                Maximum = 10000,
                Value = 0
            };

            // Tamanho máximo
            Label lblMaxSize = new Label
            {
                Text = "Max Size:",
                Location = new Point(320, 55),
                Size = new Size(60, 20)
            };

            NumericUpDown numMaxSize = new NumericUpDown
            {
                Location = new Point(390, 52),
                Size = new Size(60, 25),
                Minimum = 0,
                Maximum = 10000,
                Value = 10000
            };

            // Conteúdo
            Label lblContent = new Label
            {
                Text = "Content Pattern:",
                Location = new Point(10, 85),
                Size = new Size(100, 20)
            };

            TextBox txtContent = new TextBox
            {
                Location = new Point(120, 82),
                Size = new Size(200, 25),
                Text = ""
            };

            CheckBox chkUseRegex = new CheckBox
            {
                Text = "Use Regex",
                Location = new Point(330, 82),
                Size = new Size(80, 20)
            };

            CheckBox chkCaseSensitive = new CheckBox
            {
                Text = "Case Sensitive",
                Location = new Point(410, 82),
                Size = new Size(100, 20)
            };

            // Adicionar controles ao grupo
            grpBasicFilters.Controls.AddRange(new Control[] {
                lblOpcode, txtOpcode,
                lblPacketType, cmbPacketType,
                lblDirection, cmbDirection,
                lblMinSize, numMinSize,
                lblMaxSize, numMaxSize,
                lblContent, txtContent,
                chkUseRegex, chkCaseSensitive
            });
        }

        private void SetupAdvancedFilters()
        {
            // Filtros avançados serão implementados conforme necessário
            Label lblAdvanced = new Label
            {
                Text = "Advanced filters will be implemented in future versions",
                Location = new Point(10, 25),
                Size = new Size(400, 20),
                ForeColor = Color.Gray
            };

            grpAdvancedFilters.Controls.Add(lblAdvanced);
        }

        private void SetupEventHandlers()
        {
            btnOK.Click += BtnOK_Click;
        }

        private void LoadRuleData()
        {
            if (isEditing && originalRule != null)
            {
                workingRule = originalRule.Clone();
                
                txtName.Text = workingRule.Name;
                txtDescription.Text = workingRule.Description;
                chkEnabled.Checked = workingRule.Enabled;
                numPriority.Value = workingRule.Priority;
                cmbAction.SelectedItem = workingRule.Action.ToString();
            }
            else
            {
                workingRule = new FilterRule();
                txtName.Text = "New Filter Rule";
                txtDescription.Text = "";
                chkEnabled.Checked = true;
                numPriority.Value = 0;
                cmbAction.SelectedIndex = 0;
            }
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            if (ValidateInput())
            {
                CreateRuleFromInput();
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Please enter a name for the filter rule.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return false;
            }

            return true;
        }

        private void CreateRuleFromInput()
        {
            workingRule.Name = txtName.Text.Trim();
            workingRule.Description = txtDescription.Text.Trim();
            workingRule.Enabled = chkEnabled.Checked;
            workingRule.Priority = (int)numPriority.Value;
            workingRule.Action = (FilterAction)Enum.Parse(typeof(FilterAction), cmbAction.SelectedItem.ToString());

            // Configurar filtros básicos
            ConfigureBasicFilters();

            CreatedRule = workingRule;
        }

        private void ConfigureBasicFilters()
        {
            // Implementar configuração dos filtros básicos baseado nos controles
            // Por simplicidade, criaremos um filtro básico
            workingRule.Filter = new PacketFilter
            {
                Name = workingRule.Name,
                Action = workingRule.Action
            };

            // Configurar filtros baseado nos controles do formulário
            // (Implementação simplificada - em produção seria mais robusta)
        }
    }
}