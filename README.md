# PD400X → Discord mute

**Press the physical mute button on a Maono PD400X. Discord mutes.**

A small Windows HID bridge. No Maono Link. No Discord plugin. No virtual cable.

[Скачать релиз](https://github.com/DSKirito/pd400x-discord-mute/releases/latest) · [v1.1.0](https://github.com/DSKirito/pd400x-discord-mute/releases/tag/v1.1.0)

---

## Why this exists

The tap-to-mute pad on the PD400X mutes **inside the microphone DSP**. Windows never gets a Core Audio mute flag. Discord therefore keeps showing you as live even when the hardware LED says muted.

Maono Link can see the button. Discord cannot. This tool reads vendor HID register `0x2022` and sends Discord the hotkey `Ctrl+Shift+Alt+M`.

---

## Quick start

1. Discord → **Settings → Keybinds** → Mute = `Ctrl + Shift + Alt + M`.
2. **Close** the Keybinds page (Discord swallows the combo while that page is open).
3. Quit **Maono Link** (tray icon too). It exclusive-locks the HID interface.
4. Download a [release zip](https://github.com/DSKirito/pd400x-discord-mute/releases/latest) or clone the repo.
5. Run `START.bat`. It compiles `HidWatch.exe` with the built-in `csc.exe` and starts it in the system tray.
6. Unmute Discord once by hand so the two states match, then tap the mic.

Tray icon → right click → **Выход** to quit. No console window on the taskbar.

---

## Supported devices

| Device | Status |
|---|---|
| Maono **PD400X** USB (`VID 352F` / `PID 0100`, HID `mi_03`) | Tested |
| Other Maono USB mics on `VID 352F` / `31B2` (PD200X / PD300X family) | Same protocol, untested |
| PD400X over **XLR only** (no USB) | Not possible — no HID channel |

---

## FAQ

**Does Maono Link have to run?**  
No. Close it. If Link is open, this program cannot open the HID device.

**Why a Discord hotkey instead of a plugin?**  
Discord has no public API for mute from a third-party EXE. A user-defined keybind is the reliable hook.

**States got inverted.**  
The tool toggles Discord when hardware mute *changes*. Start with Discord unmuted, hardware live. Then tap.

**Windows volume mixer still shows the mic as open.**  
Correct. Hardware mute is not the WASAPI endpoint mute. That is the whole problem this project works around.

**Can it mute Zoom / Teams too?**  
Yes if you bind the same `Ctrl+Shift+Alt+M` there, or change the keys in `HidWatch.cs`.

**Is there a prebuilt EXE?**  
`START.bat` builds one locally. Shipping a signed binary is on the list; Windows SmartScreen hates unsigned random EXEs from the internet anyway.

---

## Known issues

- Maono Link and HidWatch cannot own the same HID handle at once.
- First tap after launch only syncs if Discord mute already matches hardware.
- VU packets on register `0x2034` look like traffic; they are **not** mute. Older builds that treated every packet as mute would flicker Discord.
- No macOS / Linux port. Protocol is USB HID; the hotkey side is Win32 `SendInput`.

---

## HID protocol (PD200X / PD400X family)

64/65-byte report, report ID `0x4B`.

| Offset | Field |
|---:|---|
| 0 | Report ID `0x4B` |
| 1 | Magic `0xC4` |
| 2 | Payload length (`0x09` query, `0x0B` reply/event) |
| 5 | `0x04` query reply, `0x03` device event |
| 6–7 | Command, little-endian |
| 8–9 | Value, little-endian |
| 10–11 | Checksum `(-sum(bytes[1..end])) & 0xFFFF` |

Mute register:

```
cmd  0x2022
val  0 = LIVE
val  1 = MUTE
```

Query:

```
4B C4 09 00 00 04 22 20 CS CS ...
```

Reply / event:

```
4B C4 0B 00 00 04 22 20 00 00 ...   LIVE
4B C4 0B 00 00 04 22 20 01 00 ...   MUTE
```

While live the mic also streams **`0x2034`** (VU / level, values ~1400–1700). Ignore anything that is not `cmd == 0x2022` and `val ∈ {0,1}`.

---

## How the program works

[`HidWatch.cs`](HidWatch.cs) — one file, .NET Framework 4.x, no NuGet.

1. SetupAPI HID enumeration, pick `VID 352F` / `31B2` or a Maono/PD400 name.
2. Open `GENERIC_READ | GENERIC_WRITE` + `FILE_FLAG_OVERLAPPED`.
3. Listen on interrupt IN (~15 ms). Occasional `0x2022` query as fallback.
4. On 0 ↔ 1 edge, `SendInput` of `Ctrl+Shift+Alt+M`.
5. Runs as `winexe` with a tray icon.

---

## Files

| File | Role |
|---|---|
| `START.bat` | Kill old process, compile, start tray app |
| `HidWatch.cs` | HID + Discord |

## License

MIT. Personal project, not a Maono product.
