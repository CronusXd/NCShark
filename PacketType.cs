//NCShark - By AlSch092 @ Github, thanks to @Diamondo25 for MapleShark
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NCShark
{
    /// <summary>
    /// Enumeração dos tipos de pacotes do Night Crows
    /// </summary>
    public enum PacketType
    {
        Unknown = 0,
        Login = 1,
        Gameplay = 2,
        Chat = 3,
        Inventory = 4,
        Combat = 5,
        Movement = 6,
        Quest = 7,
        Trade = 8,
        Guild = 9,
        System = 10,
        Error = 11
    }

    /// <summary>
    /// Classe para classificação automática de pacotes
    /// </summary>
    public static class PacketClassifier
    {
        // Dicionário de opcodes conhecidos para cada tipo de pacote
        private static readonly Dictionary<PacketType, HashSet<ushort>> KnownOpcodes = new Dictionary<PacketType, HashSet<ushort>>
        {
            { PacketType.Login, new HashSet<ushort> { 0x0001, 0x0002, 0x0003, 0x0004, 0x0005 } },
            { PacketType.Chat, new HashSet<ushort> { 0x1001, 0x1002, 0x1003, 0x1004 } },
            { PacketType.Inventory, new HashSet<ushort> { 0x2001, 0x2002, 0x2003, 0x2004, 0x2005 } },
            { PacketType.Combat, new HashSet<ushort> { 0x3001, 0x3002, 0x3003, 0x3004, 0x3005 } },
            { PacketType.Movement, new HashSet<ushort> { 0x4001, 0x4002, 0x4003, 0x4004 } },
            { PacketType.Quest, new HashSet<ushort> { 0x5001, 0x5002, 0x5003, 0x5004 } },
            { PacketType.Trade, new HashSet<ushort> { 0x6001, 0x6002, 0x6003, 0x6004 } },
            { PacketType.Guild, new HashSet<ushort> { 0x7001, 0x7002, 0x7003, 0x7004 } },
            { PacketType.System, new HashSet<ushort> { 0x8001, 0x8002, 0x8003, 0x8004 } },
            { PacketType.Error, new HashSet<ushort> { 0x9001, 0x9002, 0x9003, 0x9004 } }
        };

        // Padrões de conteúdo para identificação de tipos de pacotes
        private static readonly Dictionary<PacketType, string[]> ContentPatterns = new Dictionary<PacketType, string[]>
        {
            { PacketType.Login, new[] { "login", "auth", "password", "username" } },
            { PacketType.Chat, new[] { "chat", "message", "say", "whisper" } },
            { PacketType.Inventory, new[] { "item", "inventory", "equip", "unequip" } },
            { PacketType.Combat, new[] { "attack", "damage", "skill", "battle" } },
            { PacketType.Movement, new[] { "move", "position", "walk", "run" } },
            { PacketType.Quest, new[] { "quest", "mission", "objective" } },
            { PacketType.Trade, new[] { "trade", "sell", "buy", "market" } },
            { PacketType.Guild, new[] { "guild", "clan", "alliance" } },
            { PacketType.System, new[] { "system", "ping", "pong", "heartbeat" } }
        };

        /// <summary>
        /// Classifica um pacote baseado no opcode e conteúdo
        /// </summary>
        /// <param name="opcode">Opcode do pacote</param>
        /// <param name="data">Dados do pacote</param>
        /// <param name="isOutbound">Se o pacote é de saída</param>
        /// <returns>Tipo do pacote identificado</returns>
        public static PacketType ClassifyPacket(ushort opcode, byte[] data, bool isOutbound)
        {
            // Primeiro, tenta classificar pelo opcode
            foreach (var kvp in KnownOpcodes)
            {
                if (kvp.Value.Contains(opcode))
                {
                    return kvp.Key;
                }
            }

            // Se não encontrou pelo opcode, tenta pelo conteúdo
            if (data != null && data.Length > 0)
            {
                string content = System.Text.Encoding.UTF8.GetString(data);
                content = content.ToLower();

                foreach (var kvp in ContentPatterns)
                {
                    foreach (string pattern in kvp.Value)
                    {
                        if (content.Contains(pattern))
                        {
                            return kvp.Key;
                        }
                    }
                }
            }

            // Classificação baseada no range do opcode
            return ClassifyByOpcodeRange(opcode);
        }

        /// <summary>
        /// Classifica pacote baseado no range do opcode
        /// </summary>
        /// <param name="opcode">Opcode do pacote</param>
        /// <returns>Tipo do pacote</returns>
        private static PacketType ClassifyByOpcodeRange(ushort opcode)
        {
            // Ranges aproximados baseados na estrutura típica de MMOs
            if (opcode >= 0x0001 && opcode <= 0x0FFF)
                return PacketType.Login;
            else if (opcode >= 0x1000 && opcode <= 0x1FFF)
                return PacketType.Chat;
            else if (opcode >= 0x2000 && opcode <= 0x2FFF)
                return PacketType.Inventory;
            else if (opcode >= 0x3000 && opcode <= 0x3FFF)
                return PacketType.Combat;
            else if (opcode >= 0x4000 && opcode <= 0x4FFF)
                return PacketType.Movement;
            else if (opcode >= 0x5000 && opcode <= 0x5FFF)
                return PacketType.Quest;
            else if (opcode >= 0x6000 && opcode <= 0x6FFF)
                return PacketType.Trade;
            else if (opcode >= 0x7000 && opcode <= 0x7FFF)
                return PacketType.Guild;
            else if (opcode >= 0x8000 && opcode <= 0x8FFF)
                return PacketType.System;
            else if (opcode >= 0x9000 && opcode <= 0x9FFF)
                return PacketType.Error;
            else
                return PacketType.Unknown;
        }

        /// <summary>
        /// Adiciona um novo opcode conhecido para um tipo de pacote
        /// </summary>
        /// <param name="packetType">Tipo do pacote</param>
        /// <param name="opcode">Opcode a ser adicionado</param>
        public static void AddKnownOpcode(PacketType packetType, ushort opcode)
        {
            if (!KnownOpcodes.ContainsKey(packetType))
            {
                KnownOpcodes[packetType] = new HashSet<ushort>();
            }
            KnownOpcodes[packetType].Add(opcode);
        }

        /// <summary>
        /// Remove um opcode conhecido
        /// </summary>
        /// <param name="packetType">Tipo do pacote</param>
        /// <param name="opcode">Opcode a ser removido</param>
        public static void RemoveKnownOpcode(PacketType packetType, ushort opcode)
        {
            if (KnownOpcodes.ContainsKey(packetType))
            {
                KnownOpcodes[packetType].Remove(opcode);
            }
        }

        /// <summary>
        /// Obtém todos os opcodes conhecidos para um tipo de pacote
        /// </summary>
        /// <param name="packetType">Tipo do pacote</param>
        /// <returns>HashSet com os opcodes conhecidos</returns>
        public static HashSet<ushort> GetKnownOpcodes(PacketType packetType)
        {
            return KnownOpcodes.ContainsKey(packetType) ? 
                new HashSet<ushort>(KnownOpcodes[packetType]) : 
                new HashSet<ushort>();
        }

        /// <summary>
        /// Obtém o nome amigável do tipo de pacote
        /// </summary>
        /// <param name="packetType">Tipo do pacote</param>
        /// <returns>Nome amigável</returns>
        public static string GetFriendlyName(PacketType packetType)
        {
            switch (packetType)
            {
                case PacketType.Login: return "Login/Authentication";
                case PacketType.Gameplay: return "Gameplay";
                case PacketType.Chat: return "Chat/Messages";
                case PacketType.Inventory: return "Inventory/Items";
                case PacketType.Combat: return "Combat/Battle";
                case PacketType.Movement: return "Movement/Position";
                case PacketType.Quest: return "Quest/Mission";
                case PacketType.Trade: return "Trade/Market";
                case PacketType.Guild: return "Guild/Clan";
                case PacketType.System: return "System/Network";
                case PacketType.Error: return "Error/Exception";
                default: return "Unknown";
            }
        }

        /// <summary>
        /// Obtém a cor associada ao tipo de pacote para exibição na UI
        /// </summary>
        /// <param name="packetType">Tipo do pacote</param>
        /// <returns>Cor em formato ARGB</returns>
        public static int GetColor(PacketType packetType)
        {
            switch (packetType)
            {
                case PacketType.Login: return unchecked((int)0xFF4CAF50); // Verde
                case PacketType.Gameplay: return unchecked((int)0xFF2196F3); // Azul
                case PacketType.Chat: return unchecked((int)0xFFFF9800); // Laranja
                case PacketType.Inventory: return unchecked((int)0xFF9C27B0); // Roxo
                case PacketType.Combat: return unchecked((int)0xFFF44336); // Vermelho
                case PacketType.Movement: return unchecked((int)0xFF00BCD4); // Ciano
                case PacketType.Quest: return unchecked((int)0xFF795548); // Marrom
                case PacketType.Trade: return unchecked((int)0xFF607D8B); // Azul acinzentado
                case PacketType.Guild: return unchecked((int)0xFFE91E63); // Rosa
                case PacketType.System: return unchecked((int)0xFF9E9E9E); // Cinza
                case PacketType.Error: return unchecked((int)0xFF000000); // Preto
                default: return unchecked((int)0xFF757575); // Cinza escuro
            }
        }
    }
}