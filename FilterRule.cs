//NCShark - By AlSch092 @ Github, thanks to @Diamondo25 for MapleShark
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace NCShark
{
    /// <summary>
    /// Classe para gerenciar regras de filtro
    /// </summary>
    public class FilterRule
    {
        public string Name { get; set; } = "Unnamed Rule";
        public string Description { get; set; } = "";
        public bool Enabled { get; set; } = true;
        public int Priority { get; set; } = 0; // Maior prioridade = executa primeiro
        public PacketFilter Filter { get; set; }
        public FilterAction Action { get; set; } = FilterAction.Capture;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime ModifiedDate { get; set; } = DateTime.Now;

        public FilterRule()
        {
            Filter = new PacketFilter();
        }

        public FilterRule(string name, PacketFilter filter, FilterAction action)
        {
            Name = name;
            Filter = filter;
            Action = action;
        }

        /// <summary>
        /// Aplica a regra a um pacote
        /// </summary>
        /// <param name="packet">Pacote a ser processado</param>
        /// <returns>True se a regra foi aplicada</returns>
        public bool Apply(NCPacket packet)
        {
            if (!Enabled || Filter == null)
                return false;

            if (Filter.Matches(packet))
            {
                ExecuteAction(packet);
                return true;
            }

            return false;
        }

        private void ExecuteAction(NCPacket packet)
        {
            switch (Action)
            {
                case FilterAction.Capture:
                    // Ação padrão - capturar o pacote
                    break;
                case FilterAction.Ignore:
                    // Marcar pacote para ser ignorado
                    packet.Tag = "IGNORED";
                    break;
                case FilterAction.Highlight:
                    // Marcar pacote para destaque
                    packet.Tag = "HIGHLIGHTED";
                    break;
                case FilterAction.LogToFile:
                    LogPacketToFile(packet);
                    break;
                case FilterAction.Modify:
                    // Marcar pacote para modificação
                    packet.Tag = "MODIFY";
                    break;
            }
        }

        private void LogPacketToFile(NCPacket packet)
        {
            if (string.IsNullOrEmpty(Filter.LogFilePath))
                return;

            try
            {
                string logEntry = $"[{packet.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] " +
                                 $"{(packet.Outbound ? "OUT" : "IN")} " +
                                 $"0x{packet.Opcode:X4} " +
                                 $"{packet.Length} bytes " +
                                 $"{packet.Name}\n";

                if (Filter.AppendToFile)
                {
                    File.AppendAllText(Filter.LogFilePath, logEntry);
                }
                else
                {
                    File.WriteAllText(Filter.LogFilePath, logEntry);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error logging packet: {ex.Message}");
            }
        }

        /// <summary>
        /// Clona a regra atual
        /// </summary>
        /// <returns>Nova instância da regra</returns>
        public FilterRule Clone()
        {
            return new FilterRule
            {
                Name = this.Name + " (Copy)",
                Description = this.Description,
                Enabled = this.Enabled,
                Priority = this.Priority,
                Filter = this.Filter?.Clone(),
                Action = this.Action,
                CreatedDate = this.CreatedDate,
                ModifiedDate = DateTime.Now
            };
        }

        public override string ToString()
        {
            return $"{Name} (Priority: {Priority}, Action: {Action})";
        }
    }

    /// <summary>
    /// Gerenciador de regras de filtro
    /// </summary>
    public class FilterRuleManager
    {
        private List<FilterRule> rules = new List<FilterRule>();
        private string rulesFilePath = "FilterRules.xml";

        public event EventHandler<FilterRuleEventArgs> RuleApplied;

        public List<FilterRule> Rules => new List<FilterRule>(rules);

        /// <summary>
        /// Adiciona uma nova regra
        /// </summary>
        /// <param name="rule">Regra a ser adicionada</param>
        public void AddRule(FilterRule rule)
        {
            if (rule != null)
            {
                rules.Add(rule);
                SortRulesByPriority();
                SaveRules();
            }
        }

        /// <summary>
        /// Remove uma regra
        /// </summary>
        /// <param name="rule">Regra a ser removida</param>
        public void RemoveRule(FilterRule rule)
        {
            if (rule != null && rules.Contains(rule))
            {
                rules.Remove(rule);
                SaveRules();
            }
        }

        /// <summary>
        /// Remove uma regra pelo nome
        /// </summary>
        /// <param name="ruleName">Nome da regra</param>
        public void RemoveRule(string ruleName)
        {
            var rule = rules.Find(r => r.Name == ruleName);
            if (rule != null)
            {
                RemoveRule(rule);
            }
        }

        /// <summary>
        /// Aplica todas as regras habilitadas a um pacote
        /// </summary>
        /// <param name="packet">Pacote a ser processado</param>
        /// <returns>True se alguma regra foi aplicada</returns>
        public bool ApplyRules(NCPacket packet)
        {
            bool anyRuleApplied = false;

            foreach (var rule in rules)
            {
                if (rule.Apply(packet))
                {
                    anyRuleApplied = true;
                    RuleApplied?.Invoke(this, new FilterRuleEventArgs(rule, packet));
                }
            }

            return anyRuleApplied;
        }

        /// <summary>
        /// Cria regras padrão para o Night Crows
        /// </summary>
        public void CreateDefaultRules()
        {
            // Regra para destacar pacotes de login
            var loginRule = new FilterRule(
                "Highlight Login Packets",
                PacketFilter.CreateByPacketType(PacketType.Login, FilterAction.Highlight),
                FilterAction.Highlight
            );
            loginRule.Priority = 100;
            AddRule(loginRule);

            // Regra para ignorar pacotes de ping/pong
            var pingRule = new FilterRule(
                "Ignore Ping/Pong Packets",
                PacketFilter.CreateByOpcode(0x8001, FilterAction.Ignore),
                FilterAction.Ignore
            );
            pingRule.Priority = 90;
            AddRule(pingRule);

            // Regra para capturar pacotes de chat
            var chatRule = new FilterRule(
                "Capture Chat Packets",
                PacketFilter.CreateByPacketType(PacketType.Chat, FilterAction.Capture),
                FilterAction.Capture
            );
            chatRule.Priority = 80;
            AddRule(chatRule);

            // Regra para capturar pacotes de combate
            var combatRule = new FilterRule(
                "Capture Combat Packets",
                PacketFilter.CreateByPacketType(PacketType.Combat, FilterAction.Capture),
                FilterAction.Capture
            );
            combatRule.Priority = 70;
            AddRule(combatRule);
        }

        /// <summary>
        /// Ordena as regras por prioridade (maior primeiro)
        /// </summary>
        private void SortRulesByPriority()
        {
            rules.Sort((r1, r2) => r2.Priority.CompareTo(r1.Priority));
        }

        /// <summary>
        /// Salva as regras em arquivo XML
        /// </summary>
        public void SaveRules()
        {
            try
            {
                var serializer = new XmlSerializer(typeof(List<FilterRule>));
                using (var writer = new StreamWriter(rulesFilePath))
                {
                    serializer.Serialize(writer, rules);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving rules: {ex.Message}");
            }
        }

        /// <summary>
        /// Carrega as regras do arquivo XML
        /// </summary>
        public void LoadRules()
        {
            try
            {
                if (File.Exists(rulesFilePath))
                {
                    var serializer = new XmlSerializer(typeof(List<FilterRule>));
                    using (var reader = new StreamReader(rulesFilePath))
                    {
                        rules = (List<FilterRule>)serializer.Deserialize(reader);
                    }
                    SortRulesByPriority();
                }
                else
                {
                    CreateDefaultRules();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading rules: {ex.Message}");
                CreateDefaultRules();
            }
        }

        /// <summary>
        /// Limpa todas as regras
        /// </summary>
        public void ClearRules()
        {
            rules.Clear();
            SaveRules();
        }

        /// <summary>
        /// Habilita/desabilita uma regra
        /// </summary>
        /// <param name="ruleName">Nome da regra</param>
        /// <param name="enabled">Se deve estar habilitada</param>
        public void SetRuleEnabled(string ruleName, bool enabled)
        {
            var rule = rules.Find(r => r.Name == ruleName);
            if (rule != null)
            {
                rule.Enabled = enabled;
                rule.ModifiedDate = DateTime.Now;
                SaveRules();
            }
        }

        /// <summary>
        /// Obtém uma regra pelo nome
        /// </summary>
        /// <param name="ruleName">Nome da regra</param>
        /// <returns>Regra encontrada ou null</returns>
        public FilterRule GetRule(string ruleName)
        {
            return rules.Find(r => r.Name == ruleName);
        }
    }

    /// <summary>
    /// Argumentos do evento de regra aplicada
    /// </summary>
    public class FilterRuleEventArgs : EventArgs
    {
        public FilterRule Rule { get; }
        public NCPacket Packet { get; }

        public FilterRuleEventArgs(FilterRule rule, NCPacket packet)
        {
            Rule = rule;
            Packet = packet;
        }
    }
}