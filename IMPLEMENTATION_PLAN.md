# Plano de Implementação para Melhorias do NCShark

## **Análise das Limitações Atuais**

### **Problemas Identificados:**

1. **Captura Limitada**: O sistema atual só captura pacotes de login devido ao filtro de portas restritivo (33004-35001)
2. **Sistema de Envio Incompleto**: O `SendPacketForm` existe mas tem limitações críticas:
   - Não implementa criptografia adequada
   - MAC addresses hardcoded
   - Checksums e sequências não calculados automaticamente
3. **Filtros Rígidos**: Apenas captura TCP em portas específicas
4. **Gerenciamento de Sessões**: Limitado a uma sessão por vez

## **Plano de Implementação Detalhado**

### **FASE 1: Captura Universal de Pacotes**

#### **1.1 Modificar Sistema de Filtros**
```csharp
// Atualizar MainForm.cs - SetupAdapter()
// Substituir filtro restritivo por captura mais ampla
mDevice.Filter = "tcp"; // Capturar todo tráfego TCP
// Adicionar filtros opcionais por IP/porta específica
```

#### **1.2 Implementar Detecção Automática de Sessões**
- **Auto-detecção de portas**: Escanear automaticamente portas ativas
- **Detecção de protocolo**: Identificar automaticamente tráfego do Night Crows
- **Múltiplas sessões simultâneas**: Permitir captura de várias conexões

#### **1.3 Melhorar Sistema de Classificação de Pacotes**
```csharp
// Adicionar em SessionForm.cs
public enum PacketType
{
    Login,
    Gameplay,
    Chat,
    Inventory,
    Combat,
    Unknown
}

public PacketType ClassifyPacket(byte[] data, ushort opcode)
{
    // Lógica para classificar tipo de pacote baseado no opcode e conteúdo
}
```

### **FASE 2: Sistema Avançado de Envio de Pacotes**

#### **2.1 Implementar Criptografia Adequada**
```csharp
// Melhorar SendPacketForm.cs
public class PacketSender
{
    private Cipher cipher;
    private uint sequenceNumber;
    private uint acknowledgmentNumber;
    
    public byte[] EncryptPacket(byte[] data, bool isOutbound)
    {
        // Implementar criptografia XOR adequada
        // Manter estado de sequência
    }
    
    public void SendPacket(byte[] data, string destIP, ushort destPort)
    {
        // Enviar pacote com criptografia e headers corretos
    }
}
```

#### **2.2 Auto-cálculo de Headers TCP/IP**
```csharp
// Adicionar classe TCPHeaderCalculator
public class TCPHeaderCalculator
{
    public ushort CalculateChecksum(IPv4Packet ipPacket, TcpPacket tcpPacket)
    {
        // Implementar cálculo automático de checksum
    }
    
    public ushort CalculateIPChecksum(IPv4Packet ipPacket)
    {
        // Implementar cálculo de checksum IP
    }
}
```

#### **2.3 Sistema de Injeção de Pacotes**
```csharp
// Melhorar SendPacketForm.cs
public class PacketInjector
{
    private LibPcapLiveDevice device;
    private NetworkInterface networkInterface;
    
    public void InjectPacket(byte[] packetData, string sourceIP, string destIP, 
                           ushort sourcePort, ushort destPort)
    {
        // Implementar injeção de pacotes com headers corretos
    }
}
```

### **FASE 3: Interface de Usuário Melhorada**

#### **3.1 Painel de Controle de Captura**
```csharp
// Adicionar CaptureControlPanel.cs
public partial class CaptureControlPanel : UserControl
{
    // Controles para:
    // - Seleção de interface de rede
    // - Filtros de IP/porta
    // - Tipos de pacotes a capturar
    // - Configurações de criptografia
}
```

#### **3.2 Editor de Pacotes Avançado**
```csharp
// Melhorar SendPacketForm.cs
public partial class AdvancedPacketEditor : Form
{
    // Funcionalidades:
    // - Editor hexadecimal visual
    // - Templates de pacotes
    // - Validação de estrutura
    // - Preview de criptografia
}
```

#### **3.3 Gerenciador de Sessões Múltiplas**
```csharp
// Adicionar SessionManager.cs
public class SessionManager
{
    private List<SessionForm> activeSessions;
    
    public void CreateNewSession(string sessionName, PacketFilter filter)
    {
        // Criar nova sessão com filtros específicos
    }
    
    public void CloseSession(SessionForm session)
    {
        // Fechar sessão específica
    }
}
```

### **FASE 4: Sistema de Filtros Avançados**

#### **4.1 Filtros por Conteúdo**
```csharp
// Adicionar PacketFilter.cs
public class PacketFilter
{
    public string SourceIP { get; set; }
    public string DestinationIP { get; set; }
    public ushort? SourcePort { get; set; }
    public ushort? DestinationPort { get; set; }
    public ushort? Opcode { get; set; }
    public PacketType? PacketType { get; set; }
    public string ContentPattern { get; set; } // Regex para conteúdo
    
    public bool Matches(NCPacket packet)
    {
        // Implementar lógica de filtro
    }
}
```

#### **4.2 Sistema de Regras**
```csharp
// Adicionar FilterRule.cs
public class FilterRule
{
    public string Name { get; set; }
    public PacketFilter Filter { get; set; }
    public FilterAction Action { get; set; } // Capture, Ignore, Highlight
    public bool Enabled { get; set; }
}

public enum FilterAction
{
    Capture,
    Ignore,
    Highlight,
    LogToFile
}
```

### **FASE 5: Melhorias de Performance e Estabilidade**

#### **5.1 Sistema de Buffer Inteligente**
```csharp
// Adicionar PacketBuffer.cs
public class PacketBuffer
{
    private Queue<NCPacket> packetQueue;
    private int maxBufferSize;
    
    public void AddPacket(NCPacket packet)
    {
        // Implementar buffer com priorização
    }
    
    public NCPacket GetNextPacket()
    {
        // Retornar próximo pacote com prioridade
    }
}
```

#### **5.2 Sistema de Threading Melhorado**
```csharp
// Melhorar MainForm.cs
public class PacketCaptureManager
{
    private Thread captureThread;
    private Thread processingThread;
    private PacketBuffer buffer;
    
    public void StartCapture()
    {
        // Implementar captura em thread separada
    }
    
    public void ProcessPackets()
    {
        // Processar pacotes em thread separada
    }
}
```

## **Cronograma de Implementação**

### **Semana 1-2: FASE 1**
- Modificar sistema de filtros
- Implementar detecção automática de sessões
- Testar captura universal

### **Semana 3-4: FASE 2**
- Implementar criptografia adequada
- Desenvolver sistema de envio de pacotes
- Testar injeção de pacotes

### **Semana 5-6: FASE 3**
- Criar interface melhorada
- Implementar gerenciador de sessões
- Desenvolver editor de pacotes

### **Semana 7-8: FASE 4**
- Implementar sistema de filtros avançados
- Criar sistema de regras
- Testar funcionalidades

### **Semana 9-10: FASE 5**
- Otimizar performance
- Implementar sistema de buffer
- Testes finais e correções

## **Arquivos Principais a Modificar**

1. **MainForm.cs** - Sistema de captura principal
2. **SessionForm.cs** - Gerenciamento de sessões
3. **SendPacketForm.cs** - Sistema de envio
4. **Config.cs** - Configurações do sistema
5. **Cipher.cs** - Sistema de criptografia

## **Novos Arquivos a Criar**

1. **PacketSender.cs** - Classe para envio de pacotes
2. **PacketFilter.cs** - Sistema de filtros
3. **SessionManager.cs** - Gerenciador de sessões
4. **CaptureControlPanel.cs** - Painel de controle
5. **AdvancedPacketEditor.cs** - Editor avançado
6. **PacketBuffer.cs** - Sistema de buffer
7. **FilterRule.cs** - Sistema de regras

## **Status de Implementação**

- [x] Plano de implementação criado
- [ ] FASE 1: Captura Universal de Pacotes
- [ ] FASE 2: Sistema Avançado de Envio de Pacotes
- [ ] FASE 3: Interface de Usuário Melhorada
- [ ] FASE 4: Sistema de Filtros Avançados
- [ ] FASE 5: Melhorias de Performance e Estabilidade

---

**Data de Criação**: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
**Versão**: 1.0
**Autor**: Assistant AI