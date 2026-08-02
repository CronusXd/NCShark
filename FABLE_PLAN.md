# Fable Method — Plano de Implementação NCShark
## Análise de Viabilidade: Captura Total, Edição e Reenvio de Pacotes

**Data:** 2026-07-27
**Analista:** @fable-method-agent
**Status:** PLANO — Aguardando aprovação do usuário

---

## Resumo Executivo

**VIÁVEL com ressalvas.** A captura de TODOS os pacotes já funciona (não há filtro de opcode,
apenas filtro por porta TCP). A edição de pacotes via HexBox já é possível (falta conectar à UI).
O reenvio de pacotes possui 60% da infraestrutura pronta (SendPacketForm + SharpPcap) mas
precisa de correções: reativação da criptografia XOR, cálculo dinâmico de checksum TCP/IP,
e sincronização de números de sequência. O maior risco técnico é a criptografia stateful (XOR
com contador) que dessincroniza se pacotes forem perdidos na captura.

---

## 🔷 Fase A: Correção do NCStream.Read() e Parsing de Header

**Descrição:** O método NCStream.Read() está quebrado — usa `mBuffer.Length` como tamanho
de pacote em vez de ler o campo de tamanho do header. A estrutura exata do header do protocolo
Night Crows (tamanho total, posição do opcode, posição do tamanho) precisa ser engenharia reversa
e corrigida.

**Especialista:** @minimal-change-engineer

- **Task A.1:** Analisar a estrutura real do header dos pacotes Night Crows comparando o código
  existente (SessionForm.BufferTCPPacket, linhas 164-210) com capturas reais para determinar
  o formato correto do header (tamanho, posição do opcode, checksum/cipher flags)
- **Task A.2:** Corrigir NCStream.Read() para ler corretamente o campo de tamanho do pacote
  do header (em vez de usar `mBuffer.Length`). Ajustar offsets (linhas 59-67)
- **Task A.3:** Validar que SessionForm.BufferTCPPacket e NCStream não duplicam processamento
  (atualmente BufferTCPPacket faz parsing manual para NC E depois chama ProcessTCPPacket que
  usa NCStream.Read — investigar se isso é redundante)
- **Task A.4:** Criar testes unitários para NCStream.Read() com pacotes de exemplo reais ou simulados
- **Verificação:** NCStream.Read() retorna NCPackets com buffers de tamanho correto. Captura real
  de múltiplos pacotes não mostra duplicação ou dados corrompidos.

---

## 🔷 Fase B: Correção do DefinitionsContainer (Lookup de Opcodes)

**Descrição:** `DefinitionsContainer.GetDefinition()` retorna `null` sempre (linha 22 hard-coded).
Precisa ser corrigido para consultar as definições carregadas dos XML. Também é necessário
corrigir `SaveDefinition()` e salvar as definições de nome/ignore que o usuário configura.

**Especialista:** @backend-architect

- **Task B.1:** Corrigir `DefinitionsContainer.GetDefinition()` para consultar o dicionário
  `_definitions[locale][build]` em vez de retornar null hard-coded
- **Task B.2:** Corrigir `DefinitionsContainer.SaveDefinition()` para adicionar/atualizar definições
  no dicionário interno (linhas 29-37, código comentado)
- **Task B.3:** Criar estrutura de diretórios `Scripts/{locale}/{build}/` com pelo menos um
  `PacketDefinitions.xml` de exemplo
- **Verificação:** Ao nomear um pacote no context menu, o nome persiste ao reiniciar o NCShark.
  `GetDefinition()` retorna a definição correta para opcodes conhecidos.

---

## 🔷 Fase C: Conexão do SendPacketForm à UI (Interface de Envio)

**Descrição:** O SendPacketForm existe como classe mas NUNCA é instanciado. Precisamos
adicioná-lo à MainForm (View → Send Packet) e conectar a passagem de dados do NCPacket
selecionado (IPs, portas, sequência, buffer editado).

**Especialista:** @frontend-developer

- **Task C.1:** Adicionar menu item "Send Packet" à View menu da MainForm (MainForm.Designer.cs)
- **Task C.2:** Adicionar handler que instancia `SendPacketForm` e chama `SetDevice(device, filter)`
  na MainForm
- **Task C.3:** Adicionar método público em `SendPacketForm` para receber dados do NCPacket
  selecionado: sourceIP, destIP, sourcePort, destPort, lastSequenceNumber, lastAcknowledgmentNumber,
  buffer, etc.
- **Task C.4:** Integrar a seleção atual do SessionForm com o envio — ao clicar "Send Packet",
  os campos do formulário são preenchidos com os valores do pacote selecionado
- **Verificação:** Ao selecionar um pacote e abrir Send Data, os campos IP/porta/sequência/buffer
  são preenchidos automaticamente. O formulário envia pacote sem crash.

---

## 🔷 Fase D: Reativação da Criptografia XOR no Envio

**Descrição:** A linha `Cipher.XorBytes(...)` no SendPacketForm está comentada (linha 84).
Para que o servidor aceite pacotes reenviados, eles precisam ser criptografados com o mesmo
XOR stateful usado na captura. Isso requer sincronizar o estado do cipher.

**Especialista:** @backend-architect

- **Task D.1:** Analisar o estado do cipher XOR (contadores) no momento do envio — o cipher é
  stateful e precisa estar sincronizado com o servidor. Determinar como rastrear o contador
  correto
- **Task D.2:** Reativar `Cipher.XorBytes()` no SendPacketForm.sendPshAck() — descriptografar
  o payload do usuário ANTES de enviar (o jogo aplica XOR no recebimento, então o reenvio precisa
  do payload raw)
- **Task D.3:** Gerenciar a flag `firstSend` para o envio — determinar se um novo envio precisa
  resetar o estado do cipher ou continuar do estado atual
- **Task D.4:** Tratar caso o pacote enviado seja interceptado pelo anti-cheat — adicionar
  proteções contra detecção (timing, sequência)
- **Verificação:** Pacote reenviado com XOR reativado é aceito pelo servidor (resposta visível
  no log de captura). Estado do cipher permanece sincronizado.

---

## 🔷 Fase E: Cálculo Correto de Checksum TCP/IP

**Descrição:** O SendPacketForm usa checksum hard-coded (`Checksum = 0x69e0`, linha 115).
Isso precisa ser substituído por cálculo dinâmico correto (checksum TCP = pseudo-header IPv4 + 
segmento TCP). O SharpPcap tem suporte a checksum calculation.

**Especialista:** @minimal-change-engineer

- **Task E.1:** Implementar cálculo de checksum TCP correto (soma complemento de 1 sobre
  pseudo-header IPv4 + TCP header + payload)
- **Task E.2:** Verificar se o PacketDotNet recalcula checksum automaticamente ao setar
  `ipPacket.PayloadPacket = tcpPacket` (algumas bibliotecas fazem isso)
- **Task E.3:** Remover valores hard-coded de Checksum (0x69e0) e WindowSize (515)
- **Task E.4:** Calcular corretamente o IP ID (`lastIdentificationNum + 1` já existe mas
  precisa ser rastreado por sessão)
- **Verificação:** Pacote enviado com checksum calculado dinamicamente aparece como válido
  no Wireshark (checksum TCP correto, não "checksum 0x0000" ou incorreto)

---

## 🔷 Fase F: Edição de Pacotes no HexBox e Reenvio

**Descrição:** O HexBox em DataForm.cs já é um controle hexa editável. Precisamos permitir
que o usuário edite o buffer do NCPacket selecionado e reenvie o pacote modificado.

**Especialista:** @frontend-developer

- **Task F.1:** Adicionar botão "Apply Changes" no DataForm que atualiza o buffer do NCPacket
  atual com os bytes modificados no HexBox
- **Task F.2:** Adicionar mecanismo para marcar pacotes editados (cor diferente na lista, ícone)
- **Task F.3:** Conectar botão "Resend" no SessionForm (context menu ou toolbar) que pega o
  pacote editado e chama SendPacketForm com os dados atualizados
- **Task F.4:** Adicionar confirmação de segurança ("Tem certeza que deseja reenviar este pacote?")
  antes de enviar
- **Verificação:** Usuário edita bytes no HexBox, clica "Apply Changes", buffer do NCPacket
  é atualizado. Ao reenviar, o pacote modificado é enviado (confirmado por captura dupla).

---

## 🔷 Fase G: Gerenciamento de Sessão TCP para Reenvio

**Descrição:** Para reenviar pacotes corretamente, precisamos rastrear e atualizar números de
sequência TCP, acknowledgment, e janela, além de lidar com a possibilidade de múltiplos envios
(evitar duplicação de sequência).

**Especialista:** @backend-architect

- **Task G.1:** Rastrear por sessão: lastSequenceNumber, lastAcknowledgmentNumber, lastIPid.
  Atualmente SendPacketForm só tem campos soltos sem persistência entre envios
- **Task G.2:** Atualizar numbers após cada envio bem-sucedido (sequence += payload length)
- **Task G.3:** Adicionar opção de "forçar novo SYN" para iniciar nova sessão se necessário
- **Task G.4:** Tratar o caso de Time-Wait e RST — se o servidor rejeitar, resetar sessão
- **Verificação:** Múltiplos reenvios na mesma sessão mantêm sequência TCP consistente.
  Wireshark não mostra "retransmissão" ou "ack perdido".

---

## 🔷 Fase H: Testes Integrados e Validação Anti-Cheat

**Descrição:** Testes completos do pipeline editar → criptografar → reenviar, mais validação
de que as modificações não causam detecção por anti-cheat.

**Especialista:** @testing-reality-checker

- **Task H.1:** Criar teste de captura → editar → reenviar com pacotes de exemplo gravados
- **Task H.2:** Validar que a criptografia XOR produziu payload idêntico ao original para
  pacotes não modificados (roundtrip: capturar → descriptografar → recriptografar → enviar)
- **Task H.3:** Verificar que pacotes editados produzem XOR válido (não quebram o estado do cipher)
- **Task H.4:** Teste de estresse: reenviar 100+ pacotes em sequência sem perda de sincronia
- **Task H.5:** Análise de detecção: verificar se o servidor responde de forma diferente a
  pacotes reenviados vs originais
- **Verificação:** Suite de testes completa. Roundtrip XOR preserva payload. 100 envios
  consecutivos sem falha. Servidor aceita ou rejeita de forma consistente.

---

## 📊 Estimativa de Esforço

| Fase | Descrição | Esforço | Especialista |
|------|-----------|---------|-------------|
| A | Correção NCStream + Header | 3-5 dias | @minimal-change-engineer |
| B | DefinitionsContainer | 1-2 dias | @backend-architect |
| C | UI SendPacketForm | 2-3 dias | @frontend-developer |
| D | Criptografia XOR no envio | 3-5 dias | @backend-architect |
| E | Checksum TCP/IP | 1-2 dias | @minimal-change-engineer |
| F | Edição HexBox + Reenvio | 3-4 dias | @frontend-developer |
| G | Gerenciamento de Sessão TCP | 2-3 dias | @backend-architect |
| H | Testes e Validação | 3-5 dias | @testing-reality-checker |
| **Total** | | **18-29 dias** | |

---

## ⚠️ Riscos e Caveats

1. **Criptografia stateful (XOR):** Se o WinPcap perder pacotes durante a captura, o estado
   do XOR para o reenvio estará incorreto. Solução proposta: capturar em ambiente controlado
   com buffer grande e sem drops
2. **Estrutura do header desconhecida:** A Fase A pode revelar que a estrutura atual do header
   está totalmente errada. Se o `packetSize` não estiver nos bytes esperados, todo o parsing
   precisa ser refeito com base em engenharia reversa do binário do jogo
3. **Anti-cheat:** Night Crows pode usar detecção de pacotes anômalos. Mesmo com criptografia
   correta, o servidor pode detectar reenvio por números de sequência duplicados ou timing
   atípico
4. **Dependência de versão:** O jogo pode atualizar a criptografia (XOR key, algoritmo) ou
   a estrutura do header a qualquer momento, quebrando o reenvio

---

## ✅ Aprovação

**Aguardando decisão do usuário** para iniciar a execução. Após aprovação, o @AgentsOrchestrator
delegará cada fase ao especialista indicado via tabela de roteamento.
