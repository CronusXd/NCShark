//NCShark - By AlSch092 @ Github, thanks to @Diamondo25 for MapleShark
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Net;

namespace NCShark
{
    /// <summary>
    /// Ações que podem ser aplicadas a um pacote filtrado
    /// </summary>
    public enum FilterAction
    {
        Capture,     // Capturar o pacote
        Ignore,      // Ignorar o pacote
        Highlight,   // Destacar o pacote
        LogToFile,   // Salvar em arquivo específico
        Modify       // Modificar o pacote antes de processar
    }

    /// <summary>
    /// Classe para filtros de pacotes
    /// </summary>
    public class PacketFilter
    {
        public string Name { get; set; } = "Unnamed Filter";
        public bool Enabled { get; set; } = true;
        public FilterAction Action { get; set; } = FilterAction.Capture;
        
        // Filtros por endereço
        public string SourceIP { get; set; }
        public string DestinationIP { get; set; }
        public IPAddress SourceIPAddress { get; set; }
        public IPAddress DestinationIPAddress { get; set; }
        
        // Filtros por porta
        public ushort? SourcePort { get; set; }
        public ushort? DestinationPort { get; set; }
        public ushort? MinSourcePort { get; set; }
        public ushort? MaxSourcePort { get; set; }
        public ushort? MinDestinationPort { get; set; }
        public ushort? MaxDestinationPort { get; set; }
        
        // Filtros por opcode
        public ushort? Opcode { get; set; }
        public ushort? MinOpcode { get; set; }
        public ushort? MaxOpcode { get; set; }
        public List<ushort> OpcodeList { get; set; } = new List<ushort>();
        
        // Filtros por tipo de pacote
        public PacketType? PacketType { get; set; }
        public List<PacketType> PacketTypeList { get; set; } = new List<PacketType>();
        
        // Filtros por direção
        public bool? IsOutbound { get; set; }
        
        // Filtros por tamanho
        public int? MinSize { get; set; }
        public int? MaxSize { get; set; }
        
        // Filtros por conteúdo
        public string ContentPattern { get; set; }
        public bool CaseSensitive { get; set; } = false;
        public bool UseRegex { get; set; } = false;
        
        // Filtros por timestamp
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        
        // Configurações de arquivo para LogToFile
        public string LogFilePath { get; set; }
        public bool AppendToFile { get; set; } = true;

        /// <summary>
        /// Verifica se um pacote corresponde aos critérios do filtro
        /// </summary>
        /// <param name="packet">Pacote a ser verificado</param>
        /// <returns>True se o pacote corresponde ao filtro</returns>
        public bool Matches(NCPacket packet)
        {
            if (!Enabled) return false;

            // Verificar filtros de endereço IP
            if (!MatchesIPFilters(packet)) return false;

            // Verificar filtros de porta
            if (!MatchesPortFilters(packet)) return false;

            // Verificar filtros de opcode
            if (!MatchesOpcodeFilters(packet)) return false;

            // Verificar filtros de tipo de pacote
            if (!MatchesPacketTypeFilters(packet)) return false;

            // Verificar filtros de direção
            if (!MatchesDirectionFilters(packet)) return false;

            // Verificar filtros de tamanho
            if (!MatchesSizeFilters(packet)) return false;

            // Verificar filtros de conteúdo
            if (!MatchesContentFilters(packet)) return false;

            // Verificar filtros de timestamp
            if (!MatchesTimeFilters(packet)) return false;

            return true;
        }

        private bool MatchesIPFilters(NCPacket packet)
        {
            // Implementar verificação de IP quando disponível
            // Por enquanto, sempre retorna true
            return true;
        }

        private bool MatchesPortFilters(NCPacket packet)
        {
            // Implementar verificação de porta quando disponível
            // Por enquanto, sempre retorna true
            return true;
        }

        private bool MatchesOpcodeFilters(NCPacket packet)
        {
            if (Opcode.HasValue && packet.Opcode != Opcode.Value)
                return false;

            if (MinOpcode.HasValue && packet.Opcode < MinOpcode.Value)
                return false;

            if (MaxOpcode.HasValue && packet.Opcode > MaxOpcode.Value)
                return false;

            if (OpcodeList.Count > 0 && !OpcodeList.Contains(packet.Opcode))
                return false;

            return true;
        }

        private bool MatchesPacketTypeFilters(NCPacket packet)
        {
            if (PacketType.HasValue)
            {
                var packetType = PacketClassifier.ClassifyPacket(packet.Opcode, packet.Buffer, packet.Outbound);
                if (packetType != PacketType.Value)
                    return false;
            }

            if (PacketTypeList.Count > 0)
            {
                var packetType = PacketClassifier.ClassifyPacket(packet.Opcode, packet.Buffer, packet.Outbound);
                if (!PacketTypeList.Contains(packetType))
                    return false;
            }

            return true;
        }

        private bool MatchesDirectionFilters(NCPacket packet)
        {
            if (IsOutbound.HasValue && packet.Outbound != IsOutbound.Value)
                return false;

            return true;
        }

        private bool MatchesSizeFilters(NCPacket packet)
        {
            if (MinSize.HasValue && packet.Length < MinSize.Value)
                return false;

            if (MaxSize.HasValue && packet.Length > MaxSize.Value)
                return false;

            return true;
        }

        private bool MatchesContentFilters(NCPacket packet)
        {
            if (string.IsNullOrEmpty(ContentPattern))
                return true;

            try
            {
                string content = System.Text.Encoding.UTF8.GetString(packet.Buffer);
                
                if (!CaseSensitive)
                {
                    content = content.ToLower();
                    ContentPattern = ContentPattern.ToLower();
                }

                if (UseRegex)
                {
                    return Regex.IsMatch(content, ContentPattern);
                }
                else
                {
                    return content.Contains(ContentPattern);
                }
            }
            catch
            {
                return false;
            }
        }

        private bool MatchesTimeFilters(NCPacket packet)
        {
            if (StartTime.HasValue && packet.Timestamp < StartTime.Value)
                return false;

            if (EndTime.HasValue && packet.Timestamp > EndTime.Value)
                return false;

            return true;
        }

        /// <summary>
        /// Cria um filtro simples por opcode
        /// </summary>
        /// <param name="opcode">Opcode a filtrar</param>
        /// <param name="action">Ação a aplicar</param>
        /// <returns>Filtro configurado</returns>
        public static PacketFilter CreateByOpcode(ushort opcode, FilterAction action = FilterAction.Capture)
        {
            return new PacketFilter
            {
                Name = $"Opcode 0x{opcode:X4}",
                Opcode = opcode,
                Action = action
            };
        }

        /// <summary>
        /// Cria um filtro por tipo de pacote
        /// </summary>
        /// <param name="packetType">Tipo de pacote a filtrar</param>
        /// <param name="action">Ação a aplicar</param>
        /// <returns>Filtro configurado</returns>
        public static PacketFilter CreateByPacketType(PacketType packetType, FilterAction action = FilterAction.Capture)
        {
            return new PacketFilter
            {
                Name = $"Type: {PacketClassifier.GetFriendlyName(packetType)}",
                PacketType = packetType,
                Action = action
            };
        }

        /// <summary>
        /// Cria um filtro por direção
        /// </summary>
        /// <param name="isOutbound">Se deve filtrar pacotes de saída</param>
        /// <param name="action">Ação a aplicar</param>
        /// <returns>Filtro configurado</returns>
        public static PacketFilter CreateByDirection(bool isOutbound, FilterAction action = FilterAction.Capture)
        {
            return new PacketFilter
            {
                Name = isOutbound ? "Outbound Packets" : "Inbound Packets",
                IsOutbound = isOutbound,
                Action = action
            };
        }

        /// <summary>
        /// Cria um filtro por tamanho
        /// </summary>
        /// <param name="minSize">Tamanho mínimo</param>
        /// <param name="maxSize">Tamanho máximo</param>
        /// <param name="action">Ação a aplicar</param>
        /// <returns>Filtro configurado</returns>
        public static PacketFilter CreateBySize(int minSize, int maxSize, FilterAction action = FilterAction.Capture)
        {
            return new PacketFilter
            {
                Name = $"Size: {minSize}-{maxSize} bytes",
                MinSize = minSize,
                MaxSize = maxSize,
                Action = action
            };
        }

        /// <summary>
        /// Cria um filtro por conteúdo
        /// </summary>
        /// <param name="pattern">Padrão de conteúdo</param>
        /// <param name="useRegex">Se deve usar regex</param>
        /// <param name="action">Ação a aplicar</param>
        /// <returns>Filtro configurado</returns>
        public static PacketFilter CreateByContent(string pattern, bool useRegex = false, FilterAction action = FilterAction.Capture)
        {
            return new PacketFilter
            {
                Name = $"Content: {pattern}",
                ContentPattern = pattern,
                UseRegex = useRegex,
                Action = action
            };
        }

        /// <summary>
        /// Clona o filtro atual
        /// </summary>
        /// <returns>Nova instância do filtro</returns>
        public PacketFilter Clone()
        {
            return new PacketFilter
            {
                Name = this.Name + " (Copy)",
                Enabled = this.Enabled,
                Action = this.Action,
                SourceIP = this.SourceIP,
                DestinationIP = this.DestinationIP,
                SourceIPAddress = this.SourceIPAddress,
                DestinationIPAddress = this.DestinationIPAddress,
                SourcePort = this.SourcePort,
                DestinationPort = this.DestinationPort,
                MinSourcePort = this.MinSourcePort,
                MaxSourcePort = this.MaxSourcePort,
                MinDestinationPort = this.MinDestinationPort,
                MaxDestinationPort = this.MaxDestinationPort,
                Opcode = this.Opcode,
                MinOpcode = this.MinOpcode,
                MaxOpcode = this.MaxOpcode,
                OpcodeList = new List<ushort>(this.OpcodeList),
                PacketType = this.PacketType,
                PacketTypeList = new List<PacketType>(this.PacketTypeList),
                IsOutbound = this.IsOutbound,
                MinSize = this.MinSize,
                MaxSize = this.MaxSize,
                ContentPattern = this.ContentPattern,
                CaseSensitive = this.CaseSensitive,
                UseRegex = this.UseRegex,
                StartTime = this.StartTime,
                EndTime = this.EndTime,
                LogFilePath = this.LogFilePath,
                AppendToFile = this.AppendToFile
            };
        }

        public override string ToString()
        {
            return $"{Name} ({Action})";
        }
    }
}