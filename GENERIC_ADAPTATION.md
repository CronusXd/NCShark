# GENERIC_ADAPTATION.md — Guia de Adaptação Multi-Jogo

## NCShark / MapleShark Engine

> Este documento descreve como adaptar o **NCShark** (fork do MapleShark) para funcionar como um sniffer de pacotes TCP para **qualquer jogo online**, não apenas Night Crows. O guia cobre a arquitetura do software, os pontos que exigem modificação por jogo, e as partes 100% reaproveitáveis.

---

## 1. Arquitetura Genérica (Diagrama de Componentes)

```
┌─────────────────────────────────────────────────────────────────┐
│                    NCShark Engine (Genérico)                     │
│                                                                  │
│  ┌──────────────┐   ┌──────────────────┐   ┌─────────────────┐  │
│  │  SharpPcap/   │   │  TCP Reassembly   │   │  Session/       │  │
│  │  WinPcap      │──▶│  (SessionForm.    │──▶│  Packet List    │  │
│  │  (Captura     │   │   ProcessTCPPkt)  │   │  (ListView)     │  │
│  │   raw)        │   └──────────────────┘   └─────────────────┘  │
│  └──────────────┘                                               │
│         │                                                       │
│         ▼                                                       │
│  ┌──────────────┐   ┌──────────────────┐   ┌─────────────────┐  │
│  │  MainForm     │   │  Docking UI      │   │  HexBox / Data  │  │
│  │  (Orquestra-  │   │  (WeifenLuo      │   │  Form (editor   │  │
│  │   ção)        │   │   DockPanel)     │   │   hex)          │  │
│  └──────────────┘   └──────────────────┘   └─────────────────┘  │
│                                                                  │
│  ┌──────────────┐   ┌──────────────────┐   ┌─────────────────┐  │
│  │ ScriptDotNet  │   │  Definitions     │   │  SendPacketForm │  │
│  │ (S# scripting)│   │  Container       │   │  (Envio de      │  │
│  │              │   │  (XML opcodes)    │   │   pacotes)      │  │
│  └──────────────┘   └──────────────────┘   └─────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                            │
                            ▼ (dados criptografados/buffer TCP)
┌─────────────────────────────────────────────────────────────────┐
│              Camada Específica do Jogo (4 pontos)               │
│                                                                  │
│  ┌──────────────┐   ┌──────────────────┐   ┌─────────────────┐  │
│  │  Filtro Porta │   │  Criptografia    │   │  Header Parsing │  │
│  │  Config.cs    │   │  Cipher.cs       │   │  NCStream.Read()│  │
│  │  Low/HighPort │   │  Algoritmo XOR/  │   │  Tamanho/Opcode │  │
│  │              │   │  AES/etc         │   │  /Checksum      │  │
│  └──────────────┘   └──────────────────┘   └─────────────────┘  │
│                                                                  │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  Definições de Pacotes (Opcodes)                          │   │
│  │  Scripts/{locale}/{build}/PacketDefinitions.xml           │   │
│  └──────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

### Componentes Genéricos (nunca precisam ser modificados)

| Componente | Arquivo(s) | Função |
|-----------|-----------|--------|
| Captura raw | `MainForm.cs` + SharpPcap | Inicializa dispositivo, filtro BPF, loop de captura |
| Reconstrução TCP | `SessionForm.ProcessTCPPacket()` | Reordenação de pacotes, buffering por sequence number |
| Interface principal | `MainForm.cs` + `*.Designer.cs` | Menu, toolbar, docking |
| Docking | `Docking/` | WeifenLuo DockPanel |
| Hex editor | `HexBox/` | Visualização/edição hexadecimal |
| Sistema de scripting | `ScriptDotNet/`, `ScriptAPI.cs`, `ScriptForm.cs` | S# scripts para parse de estruturas |
| Container de definições | `DefinitionsContainer.cs` | Load/save de opcodes XML |
| Envio de pacotes | `SendPacketForm.cs` | Injeção de pacotes via SharpPcap |
| Leitura de pacotes | `NCPacket.cs`, `PacketReader.cs`, `AbstractPacket.cs` | Modelo de dados e helpers de leitura |
| Log/export | `OutputForm.cs`, métodos de save | Export .msb, .txt |

### Componentes Específicos por Jogo (4 pontos)

| Componente | Arquivo(s) | Função |
|-----------|-----------|--------|
| Filtro de porta | `Config.cs` | Range de portas TCP do jogo |
| Criptografia | `Cipher.cs` | Algoritmo de decodificação dos pacotes |
| Header parsing | `NCStream.cs`, `SessionForm.BufferTCPPacket()` | Estrutura do header: tamanho, opcode, checksum |
| Definições de pacotes | `Scripts/{locale}/{build}/PacketDefinitions.xml` | Mapa opcode → nome |

---

## 2. Passo a Passo de Adaptação Para Novo Jogo

### 2.1. Descobrir a Porta do Jogo

#### Como identificar a porta TCP que o jogo usa

1. **Wireshark**: Capture o tráfego enquanto o jogo está rodando. Filtre por `ip.addr == <server_ip>` e observe a porta TCP destino.
2. **Netstat**: Execute `netstat -b -n` enquanto o jogo está aberto. Procure conexões ESTABLISHED com o IP do servidor do jogo.
3. **Process Monitor**: Use procmon ou TCPView da Sysinternals para filtrar conexões do executável do jogo.
4. **Análise do cliente**: Procure a porta em arquivos de configuração do jogo (`config.ini`, `setting.xml`, etc.) ou no binário via strings/hexdump.

#### Onde modificar

**Arquivo:** `Config.cs` — linhas 14-15:

```csharp
public ushort LowPort = 33004;   // ← Modificar: porta mínima do range
public ushort HighPort = 35001;  // ← Modificar: porta máxima do range
```

**Exemplo** — Jogo na porta 16000:
```csharp
public ushort LowPort = 16000;
public ushort HighPort = 16000;  // Porta única: Low = High
```

**Como o filtro é aplicado** — `MainForm.cs` linha 114:
```csharp
mDevice.Filter = string.Format("tcp portrange {0}-{1}", Config.Instance.LowPort, Config.Instance.HighPort);
```

Este filtro BPF é aplicado diretamente no SharpPcap/WinPcap, então apenas pacotes TCP dentro do range serão recebidos pelo driver de captura.

> 💡 **Dica**: Se o jogo usa portas dinâmicas ou várias portas, defina um range amplo (ex: 10000-60000) e deixe o SessionForm fazer o filtro fino por IP.

---

### 2.2. Analisar a Criptografia

#### Métodos para engenharia reversa

1. **x64dbg / IDA Pro**: Analise o binário do jogo. Procure por funções que processam buffers antes de enviar via `send()`.
   - Breakpoint em `send()` e `recv()` do Winsock (ws2_32.dll)
   - Observe o buffer antes/depois da chamada para identificar transformações
2. **Frida**: Use scripts de hooking dinâmico:
   ```javascript
   Interceptor.attach(Module.findExportByName("ws2_32.dll", "send"), {
       onEnter: function(args) {
         console.log(hexdump(args[1], { length: args[2].toInt32() }));
       }
   });
   ```
3. **Comparação de bytes**: Capture o mesmo pacote no cliente e no servidor; a diferença entre os dois é a chave/algoritmo.
4. **Padrões comuns em MMORPGs**:
   - **XOR stateful**: XOR com uma key table + contador que avança a cada byte
   - **XOR com IV**: Primeiros bytes são um IV que diciona a posição inicial na key table
   - **AES-CBC**: AES de 128 bits com chave fixa e IV geralmente nos primeiros bytes
   - **RC4/ARCFOUR**: Stream cipher comum em jogos antigos
   - **Nenhuma**: Alguns jogos trafegam em plaintext (raro hoje em dia)

#### Como implementar em Cipher.cs

A classe `Cipher.cs` atual implementa XOR stateful. Você deve substituir o conteúdo inteiro da classe pelo algoritmo apropriado.

**Estrutura esperada** — a classe deve expor métodos estáticos que transformam um buffer in-place:

```csharp
public class Cipher
{
    // Estado para outbound (cliente → servidor)
    public static XorKeyLookup xor_out = new XorKeyLookup();
    
    // Estado para inbound (servidor → cliente)
    public static XorKeyLookup xor_in = new XorKeyLookup();

    public unsafe static void XorBytes(XorKeyLookup keytable, byte[] buffer, int length, bool firstSend)
    {
        // 1. Se firstSend, inicializar o estado (keytable)
        // 2. Para cada byte em buffer:
        //    - obter byte da key table na posição atual do contador
        //    - buffer[i] ^= key_byte
        //    - incrementar/atualizar contador
    }
}
```

**Exemplo — Criptografia AES simples:**

```csharp
using System.Security.Cryptography;

public class Cipher
{
    private static readonly byte[] Key = { 0x00, 0x01, 0x02, ... }; // 16 bytes
    private static readonly byte[] IV  = { 0x00, 0x01, 0x02, ... }; // 16 bytes

    public static byte[] Decrypt(byte[] cipherText)
    {
        using (var aes = Aes.Create())
        {
            aes.Key = Key;
            aes.IV = IV;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.None;

            var decryptor = aes.CreateDecryptor();
            byte[] result = new byte[cipherText.Length];
            decryptor.TransformBlock(cipherText, 0, cipherText.Length, result, 0);
            return result;
        }
    }
}
```

**Exemplo — XOR com chave fixa simples:**

```csharp
public class Cipher
{
    private static readonly byte[] XorKey = { 0xDE, 0xAD, 0xBE, 0xEF };

    public static void Decrypt(byte[] buffer)
    {
        for (int i = 0; i < buffer.Length; i++)
            buffer[i] ^= XorKey[i % XorKey.Length];
    }
}
```

#### Onde a criptografia é invocada

Em `SessionForm.cs`, dentro do bloco `if (Config.Instance.Protocol == "Night Crows")` (linhas 164-224):

```csharp
// OUTBOUND (cliente → servidor)
Cipher.XorBytes(Cipher.xor_out, tcpDataDecrypted, tcpDataDecrypted.Length, firstSend_out);

// INBOUND (servidor → cliente)
Cipher.XorBytes(Cipher.xor_in, tcpDataDecrypted, tcpDataDecrypted.Length, firstSend_in);
```

**Ao adaptar**, você pode:
- **Opção A**: Substituir a chamada para usar seu novo método (ex: `Cipher.Decrypt(tcpDataDecrypted)`)
- **Opção B**: Modificar `XorBytes` para chamar seu algoritmo internamente
- **Opção C**: Extrair a lógica para um método virtual/interface (`ICipher`) se quiser suportar múltiplos jogos

#### Dicas para a engenharia reversa

- **XOR stateful**: A key table é tipicamente extraída do binário do jogo. O contador pode ser um offset de 4 ou 8 bytes. Observe se existem múltiplos contadores (um para cada direção).
- **AES**: A chave pode estar hardcoded no binário ou ser derivada de um seed/password. Procure por constantes de 16 bytes.
- **Handshake**: Muitos jogos negociam a chave durante o handshake TCP (primeiros pacotes). Você pode precisar capturar a sessão desde o SYN.
- **Primeiro pacote**: O parâmetro `firstSend` indica se é o primeiro pacote de uma direção — use para resetar o estado do cipher.

---

### 2.3. Reversar o Header do Protocolo

#### Estrutura típica de header de MMORPG

A maioria dos jogos MMORPG usa um formato de pacote similar a:

```
┌──────────┬──────────┬────────────┬──────────────────┬────────────┐
│ Tamanho  │  Opcode  │   Dados    │   Checksum       │   Flags?   │
│  (2 bytes)│ (2 bytes)│  (variável)│  (0-4 bytes)     │  (0-2 bytes)│
└──────────┴──────────┴────────────┴──────────────────┴────────────┘
```

Variações comuns:

| Jogo | Tamanho | Opcode | Checksum | Extra |
|------|---------|--------|----------|-------|
| Night Crows (atual) | Offset 5? | offset 2, 2 bytes | ? | Header 5 bytes? |
| MapleStory (original) | 2 bytes (LE) | 2 bytes (LE) | Nenhum | 4 bytes header |
| World of Warcraft | 2-6 bytes (varlen) | 4 bytes (LE) | Nenhum | Tamanho inclui opcode |
| Ragnarok Online | 2 bytes (LE) | 2 bytes (LE) | Nenhum | Tamanho inclui header |
| Lineage 2 | 2 bytes (LE) | 2 bytes (LE) | 2 bytes? | Header 4 bytes |

#### Métodos para descobrir a estrutura

1. **Bytes fixos**: Observe os primeiros bytes de pacotes pequenos (ex: movimento, ping). Se os primeiros 2 bytes mudam pouco, provavelmente é opcode.
2. **Correlação tamanho**: Pacotes maiores têm tamanho maior nos bytes de header. Mude um valor no cliente e veja qual byte do header muda.
3. **Checksum/CRC**: Desative checksum no cliente (se possível) para confirmar. Procure por funções CRC32/adler32 no binário.
4. **Padding**: Preste atenção a padding — alguns jogos alinham pacotes em limites de 4 ou 8 bytes.

#### Onde modificar o header parsing

Dois lugares precisam ser alterados:

**1. `NCStream.Read()`** — para parsing do stream reconstruído:

```csharp
public NCPacket Read(DateTime pTransmitted)
{
    if (mCursor < 4)            // ← Mínimo de bytes necessários (ajuste conforme necessário)
        return null;

    int packetSize = this.mBuffer.Length;  // ← EXTRAIR TAMANHO real do header!
                                            //    Ex: BitConverter.ToUInt16(mBuffer, 0)

    if (mCursor < (packetSize + 4))        // ← Ajustar offset do header
        return null;

    byte[] packetBuffer = new byte[packetSize];
    Buffer.BlockCopy(mBuffer, 5, packetBuffer, 0, packetSize);  // ← Offset 5 = header size

    ushort opcode = (ushort)(packetBuffer[0] | (packetBuffer[1] << 8)); // ← Posição do opcode
    
    // ... remover opcode dos dados, criar NCPacket
}
```

**Adaptação genérica para header [tamanho(2) | opcode(2) | dados | checksum opcional]:**

```csharp
private const int HEADER_SIZE = 4;        // tamanho(2) + opcode(2)
private const int SIZE_OFFSET = 0;         // posição do campo tamanho
private const int SIZE_LENGTH = 2;         // bytes do campo tamanho (pode ser 1, 2, ou 4)
private const int OPCODE_OFFSET = 2;       // posição do campo opcode
private const int OPCODE_LENGTH = 2;       // bytes do campo opcode (1 ou 2)
private const int CHECKSUM_SIZE = 0;       // 0 se não tem checksum
private const bool SIZE_INCLUDES_HEADER = true; // o tamanho conta o próprio header?

public NCPacket Read(DateTime pTransmitted)
{
    int minSize = HEADER_SIZE + CHECKSUM_SIZE;
    if (mCursor < minSize)
        return null;

    // Extrair tamanho
    int packetSize;
    if (SIZE_LENGTH == 2)
        packetSize = BitConverter.ToUInt16(mBuffer, SIZE_OFFSET);
    else if (SIZE_LENGTH == 4)
        packetSize = BitConverter.ToInt32(mBuffer, SIZE_OFFSET);
    else
        packetSize = mBuffer[0];

    // Se o tamanho inclui o header, subtrair
    if (SIZE_INCLUDES_HEADER)
        packetSize -= HEADER_SIZE;

    if (mCursor < (packetSize + HEADER_SIZE + CHECKSUM_SIZE))
        return null;

    // Extrair payload (pular header)
    byte[] packetBuffer = new byte[packetSize];
    Buffer.BlockCopy(mBuffer, HEADER_SIZE, packetBuffer, 0, packetSize);

    // Extrair opcode (antes dos dados, depois do tamanho)
    ushort opcode;
    if (OPCODE_LENGTH == 2)
        opcode = BitConverter.ToUInt16(packetBuffer, 0);
    else
        opcode = packetBuffer[0];
    
    // Remover opcode dos dados
    byte[] dataBuffer = new byte[packetSize - OPCODE_LENGTH];
    Buffer.BlockCopy(packetBuffer, OPCODE_LENGTH, dataBuffer, 0, packetSize - OPCODE_LENGTH);

    // Avançar cursor: descartar header + dados + checksum
    mCursor -= (packetSize + HEADER_SIZE + CHECKSUM_SIZE);
    if (mCursor > 0)
        Buffer.BlockCopy(mBuffer, packetSize + HEADER_SIZE + CHECKSUM_SIZE, mBuffer, 0, mCursor);

    Definition definition = Config.Instance.GetDefinition(mOutbound, opcode);
    return new NCPacket(pTransmitted, mOutbound, opcode, 
        definition == null ? "" : definition.Name, dataBuffer);
}
```

**2. `SessionForm.BufferTCPPacket()`** — para parsing direto do payload TCP:

```csharp
// Substituir o bloco "if (Config.Instance.Protocol == "Night Crows")"
// por uma chamada genérica:
if (Config.Instance.Protocol == "Night Crows")
{
    // 1. Decriptar (se necessário)
    byte[] tcpDataDecrypted = new byte[tcpData.Length];
    Buffer.BlockCopy(tcpData, 0, tcpDataDecrypted, 0, tcpData.Length);
    Cipher.XorBytes(Cipher.xor_out, tcpDataDecrypted, tcpDataDecrypted.Length, firstSend_out);
    firstSend_out = false;

    // 2. Extrair opcode (posição e tamanho específicos do jogo)
    ushort opcode = BitConverter.ToUInt16(tcpDataDecrypted, 2); // ← Ajustar offset

    // 3. Extrair dados (remover header/opcode)
    byte[] tcpDataToLog = new byte[tcpData.Length - 2];
    Buffer.BlockCopy(tcpDataDecrypted, 2, tcpDataToLog, 0, tcpData.Length - 2);

    packet = new NCPacket(pArrivalTime, true, opcode, 
        definition?.Name ?? "", tcpDataToLog);
}
```

> ⚠️ **Nota importante**: O método `BufferTCPPacket()` faz parsing **inline** para exibição imediata, enquanto `NCStream.Read()` faz o parsing do stream ordenado. Ambos precisam ser mantidos sincronizados com a mesma estrutura de header.

---

### 2.4. Mapear Opcodes

#### Como criar PacketDefinitions.xml

O `DefinitionsContainer.cs` carrega definições de `Scripts/{locale}/{build}/PacketDefinitions.xml`:

```xml
<?xml version="1.0"?>
<ArrayOfDefinition xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" 
                   xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <Definition>
    <Locale>1</Locale>
    <Outbound>true</Outbound>
    <Opcode>29184</Opcode>
    <Name>PING_REQ</Name>
    <Ignore>false</Ignore>
  </Definition>
  <Definition>
    <Locale>1</Locale>
    <Outbound>false</Outbound>
    <Opcode>29185</Opcode>
    <Name>PING_ACK</Name>
    <Ignore>false</Ignore>
  </Definition>
</ArrayOfDefinition>
```

A classe `Definition.cs` que cada entry deve seguir:

```csharp
public sealed class Definition
{
    public byte Locale = 0;      // Região: 1 = global, 8 = custom
    public bool Outbound = false; // true = cliente→servidor, false = servidor→cliente
    public ushort Opcode = 0;    // Valor numérico do opcode (decimal)
    public string Name = "";     // Nome legível do pacote
    public bool Ignore = false;  // Se true, oculta da lista
}
```

#### Estrutura de diretórios esperada

```
Scripts/
├── 1/                     ← Locale (byte)
│   ├── 12345/             ← Build/Version (ushort)
│   │   ├── PacketDefinitions.xml   ← Definições de opcodes
│   │   ├── Common.txt              ← Script comum (opcional)
│   │   ├── Outbound/               ← Scripts por opcode (opcional)
│   │   │   ├── 0x1234.txt
│   │   │   └── 0x5678.txt
│   │   └── Inbound/
│   │       ├── 0x1234.txt
│   │       └── 0x5678.txt
│   └── 54321/
│       └── PacketDefinitions.xml
└── 8/                     ← Outro locale
    └── ...
```

#### Ferramentas auxiliares

1. **Auto-detect via session**: Capture uma sessão e nomeie os opcodes manualmente via interface → clique direito → "Name packet". O NCShark salva em `PacketDefinitions.xml`.
2. **Script de análise**: Use `ScriptAPI.cs` para criar scripts de análise de pacotes:
   ```csharp
   // Common.txt
   if (Opcode == 0x1234) {
       AddUShort("Field1");
       AddString("Name", 12);
   }
   ```
3. **Importação de properties**: Use `frmImportProps.cs` para importar arquivos `.properties` no formato Java:
   ```properties
   # send.properties
   PING_REQ = 0x7200
   LOGIN_REQ = 0x7201
   ```

---

## 3. Checklist de Adaptação

| # | Item | Onde modificar | Descrição | Status |
|---|------|---------------|-----------|--------|
| 1 | **Porta de captura** | `Config.cs` (L14-15) | Range de portas TCP que o jogo usa | Pendente |
| 2 | **Criptografia** | `Cipher.cs` + `SessionForm.cs` BufferTCPPacket | Algoritmo de decodificação (XOR, AES, etc) | Pendente |
| 3 | **Header parsing** | `NCStream.cs` Read() + `SessionForm.cs` BufferTCPPacket | Tamanho, posição opcode, checksum | Pendente |
| 4 | **Definições de opcodes** | `Scripts/{locale}/{build}/PacketDefinitions.xml` | Mapa opcode → nome legível | Pendente |
| 5 | **Locale** | `SessionForm.cs` mLocale (L32) | Região/locale do jogo (byte) | Pendente |
| 6 | **Protocol name** | `Config.cs` Protocol (L11) + `SetupForm.cs` | Nome do protocolo no seletor | Pendente |
| 7 | **Scripts de parse** | `Scripts/{locale}/{build}/*.txt` | Scripts S# para estruturar pacotes | Opcional |
| 8 | **Send packet cipher** | `SendPacketForm.cs` sendPshAck() | Criptografia para envio de pacotes | Opcional |

---

## 4. Exemplo Hipotético: Adaptação Para Um Novo MMORPG

### Cenário: "ExampleRPG" — Um MMORPG fictício

**Características do protocolo descobertas por engenharia reversa:**

- **Porta**: TCP 25000 (conexão única)
- **Criptografia**: AES-128-CBC com chave fixa `F1E2D3C4B5A69788796A5B4C3D2E1F0A` e IV fixo `1234567890ABCDEF1234567890ABCDEF`
- **Header**: 6 bytes = [tamanho(2 LE) | opcode(2 LE) | checksum(2)]
  - Tamanho inclui os 6 bytes de header
  - Checksum: XOR de todos os bytes do payload (apenas verificação, não criptografia)
- **Opcodes**: 0x0001 (LoginReq), 0x0002 (LoginAck), 0x0101 (MoveReq), etc.

### Passo 1: Config.cs — Porta

```csharp
public ushort LowPort = 25000;
public ushort HighPort = 25000;
public string Protocol = "ExampleRPG";
```

### Passo 2: Cipher.cs — AES-128-CBC

```csharp
using System.Security.Cryptography;

public class Cipher
{
    private static readonly byte[] Key = 
        { 0xF1, 0xE2, 0xD3, 0xC4, 0xB5, 0xA6, 0x97, 0x88, 
          0x79, 0x6A, 0x5B, 0x4C, 0x3D, 0x2E, 0x1F, 0x0A };
    private static readonly byte[] IV = 
        { 0x12, 0x34, 0x56, 0x78, 0x90, 0xAB, 0xCD, 0xEF,
          0x12, 0x34, 0x56, 0x78, 0x90, 0xAB, 0xCD, 0xEF };

    public static byte[] Decrypt(byte[] cipherText)
    {
        using (var aes = Aes.Create())
        {
            aes.Key = Key;
            aes.IV = IV;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.None;
            using (var decryptor = aes.CreateDecryptor())
                return decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
        }
    }
}
```

### Passo 3: NCStream.cs — Header parsing

```csharp
public NCPacket Read(DateTime pTransmitted)
{
    const int HEADER_SIZE = 6;         // tamanho(2) + opcode(2) + checksum(2)
    const int CHECKSUM_SIZE = 2;

    if (mCursor < HEADER_SIZE)
        return null;

    // Tamanho: primeiros 2 bytes, inclui o header
    int totalSize = BitConverter.ToUInt16(mBuffer, 0);
    int payloadSize = totalSize - HEADER_SIZE;

    if (mCursor < totalSize)
        return null;

    // Verificar checksum (XOR de todos os bytes do payload)
    // Assumindo que checksum está nos últimos 2 bytes do header
    ushort expectedChecksum = BitConverter.ToUInt16(mBuffer, 4);
    ushort actualChecksum = 0;
    for (int i = HEADER_SIZE; i < totalSize; i++)
        actualChecksum ^= mBuffer[i];
    if (expectedChecksum != actualChecksum)
    {
        Console.WriteLine($"[WARN] Checksum mismatch: expected 0x{expectedChecksum:X4}, got 0x{actualChecksum:X4}");
        // Descartar pacote com checksum inválido
        mCursor -= totalSize;
        if (mCursor > 0)
            Buffer.BlockCopy(mBuffer, totalSize, mBuffer, 0, mCursor);
        return null;
    }

    // Extrair payload (pular header)
    byte[] packetBuffer = new byte[payloadSize];
    Buffer.BlockCopy(mBuffer, HEADER_SIZE, packetBuffer, 0, payloadSize);

    // Opcode está nos bytes 2-3
    ushort opcode = BitConverter.ToUInt16(mBuffer, 2);

    // Avançar cursor
    mCursor -= totalSize;
    if (mCursor > 0)
        Buffer.BlockCopy(mBuffer, totalSize, mBuffer, 0, mCursor);

    // Remover checksum dos dados (se checksum estiver no payload)
    // Neste caso, checksum está no header, então dados são limpos

    Definition definition = Config.Instance.GetDefinition(mOutbound, opcode);
    return new NCPacket(pTransmitted, mOutbound, opcode,
        definition == null ? "" : definition.Name, packetBuffer);
}
```

### Passo 4: SessionForm.cs — Parsing inline

```csharp
// Dentro de BufferTCPPacket(), substituir o bloco Night Crows:

byte[] decrypted = Cipher.Decrypt(tcpData);

if (decrypted.Length >= 6)  // HEADER_SIZE
{
    ushort opcode = BitConverter.ToUInt16(decrypted, 2);
    Definition definition = Config.Instance.GetDefinition(
        pTCPPacket.SourcePort == mLocalPort, opcode);
    
    byte[] payload = new byte[decrypted.Length - 6]; // remover header
    Buffer.BlockCopy(decrypted, 6, payload, 0, payload.Length);
    
    packet = new NCPacket(pArrivalTime, 
        pTCPPacket.SourcePort == mLocalPort, opcode,
        definition?.Name ?? "", payload);
}
```

### Passo 5: Scripts/{locale}/{build}/PacketDefinitions.xml

```xml
<?xml version="1.0"?>
<ArrayOfDefinition>
  <Definition>
    <Locale>1</Locale>
    <Outbound>true</Outbound>
    <Opcode>1</Opcode>
    <Name>LOGIN_REQ</Name>
  </Definition>
  <Definition>
    <Locale>1</Locale>
    <Outbound>false</Outbound>
    <Opcode>2</Opcode>
    <Name>LOGIN_ACK</Name>
  </Definition>
  <Definition>
    <Locale>1</Locale>
    <Outbound>true</Outbound>
    <Opcode>257</Opcode>
    <Name>MOVE_REQ</Name>
  </Definition>
</ArrayOfDefinition>
```

---

## 5. Arquivos que NUNCA Precisam Ser Modificados

A carcaça do NCShark contém dezenas de arquivos que implementam funcionalidades **genéricas de um sniffer TCP**. Estes arquivos **não precisam ser alterados** ao adaptar para um novo jogo:

### UI e Forms
| Arquivo | Função |
|---------|--------|
| `MainForm.cs` | Orquestração principal, toolbar, docking |
| `MainForm.Designer.cs` | Layout da janela principal |
| `SetupForm.cs` | Configuração de interface de rede e portas |
| `DataForm.cs` | Visualização hex dos pacotes |
| `DataForm.Designer.cs` | Layout do hex viewer |
| `StructureForm.cs` | Árvore de estrutura dos pacotes |
| `PropertyForm.cs` | Propriedades do pacote selecionado |
| `SearchForm.cs` | Busca e filtro de opcodes |
| `SessionInformation.cs` | Janela de informações da sessão |
| `OutputForm.cs` | Saída de log |
| `frmSplash.cs` | Tela de splash |
| `frmLocale.cs` | Seletor de locale |
| `frmImportProps.cs` | Importação de properties |

### Captura e Rede
| Arquivo | Função |
|---------|--------|
| `Program.cs` | Entry point, verificação WinPcap |
| `SharpPcap/` | Wrapper da biblioteca de captura |
| `PacketDotNet.dll` | Biblioteca de parsing de pacotes (assembléia referenciada) |

### TCP Reassembly
| Arquivo | Função |
|---------|--------|
| `SessionForm.ProcessTCPPacket()` | Reordenação por sequence number e buffering |
| `SessionForm.MatchTCPPacket()` | Match de sessão por porta |
| `SessionForm.BufferTCPPacket()` (parte de reassembly) | Append no stream |

### Hex Editor
| Arquivo | Função |
|---------|--------|
| `HexBox/` | Editor hexadecimal completo |

### Scripting
| Arquivo | Função |
|---------|--------|
| `ScriptDotNet/` | Runtime S# para scripts de parse |
| `ScriptAPI.cs` | API exposta para scripts |
| `ScriptForm.cs` | Editor de scripts |

### Docking
| Arquivo | Função |
|---------|--------|
| `Docking/` | WeifenLuo DockPanel Suite |

### Modelos e Serialização
| Arquivo | Função |
|---------|--------|
| `NCPacket.cs` | Modelo de pacote (opcode, buffer, timestamp) — **mas o Read() pode precisar de novos campos** |
| `Definition.cs` | Modelo de definição de opcode |
| `DefinitionsContainer.cs` | Gerenciamento de opcodes XML |
| `AbstractPacket.cs` | Classe base abstrata |
| `PacketReader.cs` | Leitor de pacotes MapleLib |
| `Pair.cs` | Classe utilitária par (key, value) |
| `Extensions.cs` | Extension methods |
| `SafeNativeMethods.cs` | P/Invoke seguro |
| `AppUpdates.cs` | Verificador de atualizações |

> ⚠️ **Exceção**: `NCPacket.cs` pode precisar de novos campos se seu protocolo exigir. Exemplo: adicionar `Checksum`, `SequenceNumber`, `EncryptedHeader`. Nesse caso, **adicione campos**, não remova os existentes.

---

## 6. Troubleshooting

### 6.1. Pacotes não aparecendo

| Causa possível | Verificação | Solução |
|---------------|-------------|---------|
| Porta errada | Wireshark: o tráfego está na porta configurada? | Ajustar LowPort/HighPort |
| Filtro BPF | Wireshark: o filtro `tcp portrange X-Y` captura? | Verificar `MainForm.cs` L114 |
| WinPcap não instalado | NCShark mostra erro ao iniciar? | Instalar WinPcap/Npcap |
| Sem permissão admin | NCShark não captura pacotes? | Executar como Administrador |
| Interface errada | SetupForm mostra a interface correta? | Selecionar interface ativa |
| Jogo usa TLS/HTTPS | Wireshark mostra dados criptografados? | NCShark não suporta TLS — precisa de proxy |


### 6.2. Parsing incorreto

| Sintoma | Causa provável | Solução |
|---------|---------------|---------|
| Pacotes aparecem com tamanho 0 | Tamanho está no offset errado | Ajustar `SIZE_OFFSET` e `SIZE_LENGTH` |
| Opcodes todos 0 ou valores estranhos | Posição do opcode errada | Ajustar `OPCODE_OFFSET` |
| Dados truncados ou com lixo extra | Tamanho não inclui/exclui header | Ajustar `SIZE_INCLUDES_HEADER` |
| Pacotes "quebrados" no meio | Checksum está sendo tratado como dados | Configurar `CHECKSUM_SIZE` |
| Stream nunca completa um pacote | Tamanho mínimo (`minSize`) muito baixo | Ajustar `HEADER_SIZE` |

### 6.3. Checksum inválido

| Causa | Solução |
|-------|---------|
| Checksum algorithm errado | Testar CRC32, XOR, Adler32, ou soma simples |
| Checksum inclui dados criptografados | Calcular checksum antes de decriptar |
| Checksum inclui o próprio checksum | Excluir campo checksum do cálculo |
| Checksum é opcional | Ignorar quando for 0 ou desabilitar verificação |

### 6.4. Criptografia errada

| Sintoma | Causa | Solução |
|---------|-------|---------|
| Dados decriptados ainda parecem lixo | Algoritmo errado | Re-analisar o binário com x64dbg |
| Primeiros bytes OK, resto lixo | Cipher stateful sem reset correto | Garantir que `firstSend` reseta o estado |
| Apenas primeiro pacote OK | IV/seed muda por sessão | Extrair IV do handshake |
| Dados parecem aleatórios mas tamanho correto | AES com chave/IV errados | Extrair chave do binário |
| Dados parcialmente legíveis (ex: textos) | XOR com chave curta ou apenas alguns bytes cifrados | Tentar XOR com diferentes key lengths |

### 6.5. Scripts de parse não funcionam

| Causa | Solução |
|-------|---------|
| Caminho do script errado | Verificar `SessionForm.cs` L638 — o path deve ser `Scripts/{locale}/{build}/{Outbound|Inbound}/0x{opcode:X4}.txt` |
| Script não carrega | Verificar `ScriptDotNet/` e `RuntimeConfig.xml` |
| API não disponível | `ScriptAPI` expõe `AddByte`, `AddUShort`, `AddString`, etc. Verificar se o nome do método está correto |

### 6.6. Envio de pacotes não funciona

| Causa | Solução |
|-------|---------|
| Criptografia diferente | `SendPacketForm.sendPshAck()` linha 84 tem XOR comentado — implementar cipher correto |
| MAC address errado | Ajustar MAC source/destination em `SendPacketForm.cs` L90-91 |
| Checksum TCP | NCShark não recalcula checksum TCP automaticamente — o driver/placa de rede geralmente faz, mas se não, configure |

---

## Apêndice A: Fluxo Decisão para Adaptação

```
Início
  │
  ▼
┌─────────────────────────┐
│ 1. Descobrir porta TCP   │
│    (Wireshark/netstat)   │
└─────────┬───────────────┘
          ▼
┌─────────────────────────┐
│ 2. Configurar filtro     │
│    Config.cs Low/HighPort│
└─────────┬───────────────┘
          ▼
┌─────────────────────────┐
│ 3. Capturar sem cipher   │
│    (Cipher vazio/null)   │
└─────────┬───────────────┘
          ▼
┌─────────────────────────────────┐
│ 4. Dados legíveis no hex?       │
│    ┌─── Sim ───┐ ┌─── Não ──┐   │
│    ▼           │ ▼          │   │
│  Pular         │ Analisar   │   │
│  passo 5      │ binário    │   │
│               │ p/ cipher  │   │
│               └─────┬──────┘   │
│                     ▼          │
│               ┌──────────────┐ │
│               │ 5. Implementar│ │
│               │ Cipher.cs    │ │
│               └──────┬───────┘ │
└──────────────────────┼─────────┘
                       ▼
┌───────────────────────────────┐
│ 6. Analisar estrutura header  │
│    (tamanho, opcode, checksum)│
└──────────┬───────────────────┘
           ▼
┌───────────────────────────────┐
│ 7. Modificar NCStream.Read()  │
│    + SessionForm inline parse │
└──────────┬───────────────────┘
           ▼
┌───────────────────────────────┐
│ 8. Criar PacketDefinitions.xml│
│    (mapear opcodes)           │
└──────────┬───────────────────┘
           ▼
┌───────────────────────────────┐
│ 9. Testar com sessão real     │
│    ✓ Pacotes aparecendo       │
│    ✓ Nomes corretos           │
│    ✓ Tamanhos corretos        │
│    ✓ Checksums válidos        │
└───────────────────────────────┘
```

---

## Apêndice B: Resumo de Alterações Mínimas

Para adaptar o NCShark a um novo jogo, o conjunto **mínimo** de arquivos que você precisa tocar é:

```
NCShark/
├── Config.cs                 → LowPort, HighPort, Protocol
├── Cipher.cs                 → Algoritmo de criptografia
├── NCStream.cs               → Header parsing (Read())
├── SessionForm.cs            → Parsing inline (BufferTCPPacket)
└── Scripts/
    └── 1/
        └── 12345/
            └── PacketDefinitions.xml  → Mapa opcodes
```

**Total: 4 arquivos .cs + 1 arquivo XML** — o resto da engine é 100% reaproveitável.

---

*Documento gerado em: Julho 2026*
*Baseado no código-fonte do NCShark v0.2025+ (fork MapleShark)*
