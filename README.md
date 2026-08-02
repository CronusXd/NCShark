# NCShark: PCap Packet Logger for Night Crows

# Credits
- AlSch092 @ Github
- Diamondo25 @ Github for MapleShark

# What is this?
NCShark is a pcap driver powered packet logging tool made in C# (fork of MapleShark) for the game Night Crows. This program bypasses any anti-cheat mechanisms to bring you ban-free data logging.

**This fork additionally enables reading, editing and re-sending packets** (the upstream project disables sending to prevent general abuse).

## Features

- **Capture** all TCP traffic on the game port range (default `33004-35001`), grouped into sessions.
- **Decode** the Night Crows XOR cipher and parse each packet's opcode (`Scripts/{locale}/{build}/PacketDefinitions.xml` maps opcodes to readable names).
- **Edit** any packet's bytes in the hex editor (Data panel → "Apply Changes").
- **Send / Replay** a captured (or edited) packet back onto the wire:
  - View menu → *Send Packet* (F8), or right-click a packet → *Resend packet*.
  - The resent payload is rebuilt with its protocol header and re-encrypted with the XOR key at the **current** stream position, so the server's cipher state accepts it.
  - The replay uses an **isolated** cipher state — it never desynchronizes live capture decryption.
  - Real MAC addresses are resolved automatically (capture adapter for source, ARP of the gateway/target for destination).
  - TCP checksums and IP checksums are recomputed on send.

## Limitations

- **Stateful cipher:** because the XOR key advances with every byte, capturing mid-connection (without a SYN) cannot decode traffic, and any dropped packet desynchronizes decoding for the rest of the session. Start capture before the game connects for best results.
- **Injection desyncs the connection:** injecting a packet advances the *server's* cipher state but not the *client's*, so a stateful session generally breaks after one injected packet. Replay works best for testing/crafting single packets.
- The program can become overwhelmed with data in areas of high inbound data activity (hundreds of entities moving nearby at once, for example).

## Building

Requires NuGet packages (`packages.config`). Restore with `nuget.exe restore`, then build `NCShark.csproj` (x86 / .NET Framework 4.8).

Run the test suite from `bin\x86\Debug`:
```
NCShark.Tests.exe
```

## .msb files

Saved sessions now store the captured encrypted payload and XOR key position per packet (format `0x2026`), so packets can be replayed even after reloading a session file.

# End note

Inevitably there will be users who attempt to monetize this project by making 'object/entity scanners'. Do not pay for cheats: if you do you are being ripped off and are likely supporting criminals. Bots/cheats made by 99% of people will also get you banned as the cheat maker likely has no proper experience/no deep skill, and will likely put malware onto your computer. Just say no to buying cheats.
