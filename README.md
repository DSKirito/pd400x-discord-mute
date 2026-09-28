# PD400X → Discord mute

[English](#english) · [Русский](#русский)

**[Download release](https://github.com/DSKirito/pd400x-discord-mute/releases/latest)**

---

## English

Make the physical mute button on the Maono PD400X mute and unmute Discord.

```
press mute on the mic     →     Discord muted
press again               →     Discord live
```

No Maono Link. No Discord plugin. No virtual cable. Windows 10/11.

### Download

1. Open the [latest release](https://github.com/DSKirito/pd400x-discord-mute/releases/latest).
2. Download the zip.
3. Unpack it and run `START.bat`.
4. In Discord set Mute to `Ctrl+Shift+Alt+M`, then close the Keybinds page.
5. Close Maono Link.

### Why it exists

The hardware mute on the PD400X only silences the signal inside the microphone DSP. Windows never receives a Core Audio mute flag, so Discord still shows you as live.

Maono Link can see that button. Discord cannot. This program reads vendor HID register `0x2022` on the PD400X and sends Discord the hotkey `Ctrl+Shift+Alt+M`.

### Quick start

1. In Discord open **Settings → Keybinds** and set Mute to `Ctrl + Shift + Alt + M`.
2. Close the Keybinds page. While it is open, Discord captures the combo for recording instead of mute.
3. Quit Maono Link, including the tray icon. It locks the HID interface.
4. Run `START.bat`. It builds `HidWatch.exe` with the Windows `csc.exe` compiler and starts it in the system tray.
5. Unmute Discord once by hand so both sides start in the same state, then tap the microphone.

Right-click the tray icon → **Выход** (Exit) to quit. There is no console window on the taskbar.

### Compatibility

This project is written for and tested on one device: the **Maono PD400X over USB**.

| Model | VID:PID | HID | Mute register | Tested |
|---|---|---|---|---|
| PD400X USB | `352F:0100` | report `0x4B`, interface `mi_03` | `0x2022` | Yes |
| PD200X | unknown here | looks related, not confirmed | unknown | No |
| PD300X | unknown here | unknown | unknown | No |
| PD400X XLR only | — | no HID | — | Impossible |

Other Maono USB mics may share pieces of the same vendor protocol. That does **not** mean their mute register or packet layout is the same. Do not assume this tool works on them.

XLR-only setups cannot work: the mute pad never reaches the PC without USB HID.

### FAQ

**Does Maono Link need to be running?**  
No. Close it. If Link is open, this program cannot open the HID device.

**Why a hotkey instead of a Discord plugin?**  
Discord has no public API for mute from a third-party executable. A user keybind is the reliable hook.

**Discord mute is inverted.**  
The tool toggles Discord when the hardware mute *changes*. Start with Discord unmuted and the microphone live, then tap.

**Windows volume mixer still shows the mic as open.**  
Expected. Hardware mute is not the WASAPI endpoint mute.

**Will this mute Zoom or Teams?**  
Yes, if you bind the same `Ctrl+Shift+Alt+M` there, or change the keys in `HidWatch.cs`.

**Is there a prebuilt EXE?**  
`START.bat` builds one on your PC. An unsigned EXE on GitHub often trips SmartScreen, so the release ships source plus the batch file.

### Known issues

- Maono Link and HidWatch cannot use the same HID handle at the same time.
- After launch, the first tap only stays in sync if Discord mute already matches the hardware.
- Register `0x2034` is a level / VU meter, not mute. Treating those packets as mute makes Discord flicker.
- No macOS or Linux build.

### HID protocol (PD400X)

Everything below was captured on a PD400X (`VID 352F`, `PID 0100`). Other Maono models are not documented here.

`HidP_GetCaps` on this unit reports **64-byte input** and **64-byte output**. Some HID stacks count the leading Report ID as an extra byte, so you will also see **65**. Both numbers describe the same buffer: byte 0 is report ID `0x4B`, then 64 payload bytes.

| Offset | Field |
|---:|---|
| 0 | Report ID `0x4B` |
| 1 | Magic `0xC4` |
| 2 | Payload length (`0x09` query, `0x0B` reply or event) |
| 5 | `0x04` query reply, `0x03` device event |
| 6–7 | Command, little-endian |
| 8–9 | Value, little-endian |
| 10–11 | Checksum `(-sum(bytes[1..end])) & 0xFFFF` |

Mute register on the PD400X:

```
cmd  0x2022
val  0 = live
val  1 = muted
```

Query:

```
4B C4 09 00 00 04 22 20 CS CS ...
```

Reply / event:

```
4B C4 0B 00 00 04 22 20 00 00 ...   live
4B C4 0B 00 00 04 22 20 01 00 ...   muted
```

While the PD400X is live it also streams register `0x2034` (VU, values around 1400–1700). Ignore everything except `cmd == 0x2022` and `val` 0 or 1.

### How it works

[`HidWatch.cs`](HidWatch.cs) is a single file for .NET Framework 4.x. No NuGet packages.

1. Enumerate HID devices with SetupAPI and pick the PD400X (`VID 352F` / `PID 0100` or the product string).
2. Open the device with `GENERIC_READ | GENERIC_WRITE` and `FILE_FLAG_OVERLAPPED`.
3. Listen on interrupt IN (about 15 ms). Send a `0x2022` query now and then as a fallback.
4. On a 0 ↔ 1 edge, send `Ctrl+Shift+Alt+M` with `SendInput`.
5. Run as a tray app (`winexe`), no console window.

### Files

| File | Role |
|---|---|
| `START.bat` | Stop the old process, compile, start the tray app |
| `HidWatch.cs` | HID reader and Discord hotkey |

### License

MIT. Personal project, not affiliated with Maono.

---

## Русский

Физическая кнопка mute на Maono PD400X глушит и размучивает Discord.

```
нажал mute на микрофоне     →     Discord замучен
нажал ещё раз               →     Discord снова живой
```

Maono Link не нужен. Плагин Discord не нужен. Виртуальный кабель не нужен. Windows 10/11.

### Скачать

1. Открой [последний релиз](https://github.com/DSKirito/pd400x-discord-mute/releases/latest).
2. Скачай zip.
3. Распакуй и запусти `START.bat`.
4. В Discord на Mute поставь `Ctrl+Shift+Alt+M` и закрой страницу горячих клавиш.
5. Закрой Maono Link.

### Зачем это

Сенсорная кнопка на PD400X глушит сигнал **внутри DSP микрофона**. Windows не получает системный флаг mute, и Discord считает, что микрофон живой.

Maono Link эту кнопку видит. Discord — нет. Программа читает HID-регистр `0x2022` у PD400X и отправляет в Discord `Ctrl+Shift+Alt+M`.

### Быстрый старт

1. В Discord: **Настройки → Горячие клавиши**. На Mute поставь `Ctrl + Shift + Alt + M`.
2. Закрой страницу горячих клавиш.
3. Закрой Maono Link, включая иконку в трее.
4. Запусти `START.bat`.
5. Один раз вручную размуть Discord, потом нажми кнопку на микрофоне.

Правый клик по иконке в трее → **Выход**.

### Совместимость

Проверен только **Maono PD400X по USB**.

| Модель | VID:PID | HID | Регистр mute | Проверено |
|---|---|---|---|---|
| PD400X USB | `352F:0100` | report `0x4B`, интерфейс `mi_03` | `0x2022` | Да |
| PD200X | не известно | похоже, не подтверждено | не известно | Нет |
| PD300X | не известно | не известно | не известно | Нет |
| PD400X только XLR | — | нет HID | — | Нельзя |

У других USB-микрофонов Maono могут быть похожие HID-пакеты. Это **не** значит, что регистр mute тот же. Не жди, что программа заработает на них.

Через XLR кнопка до компьютера не доходит.

### Частые вопросы

**Maono Link должен быть запущен?**  
Нет. Его надо закрыть.

**Почему хоткей, а не плагин Discord?**  
У Discord нет публичного API mute для чужого exe.

**В Discord всё наоборот.**  
Программа переключает Discord на смене железного mute. Запусти с размученным Discord и живым микрофоном.

**В микшере Windows микрофон «включён».**  
Нормально. Аппаратный mute — это не mute Windows.

**Можно Zoom или Teams?**  
Да, если там тоже стоит `Ctrl+Shift+Alt+M`.

**Где готовый EXE?**  
`START.bat` собирает его на твоём компьютере.

### Известные проблемы

- Maono Link и HidWatch не могут одновременно держать HID.
- Первое нажатие синхронно, только если mute в Discord уже совпадает с кнопкой.
- Регистр `0x2034` — уровень, не mute.
- Нет macOS и Linux.

### HID-протокол (PD400X)

Всё ниже снято с PD400X (`VID 352F`, `PID 0100`). Другие модели Maono здесь не описаны.

У этого экземпляра `HidP_GetCaps` даёт **64 байта input** и **64 байта output**. Если считать вместе с Report ID, получается **65**. Это один и тот же буфер: байт 0 — report ID `0x4B`, дальше 64 байта данных.

| Смещение | Поле |
|---:|---|
| 0 | Report ID `0x4B` |
| 1 | Магическое `0xC4` |
| 2 | Длина (`0x09` запрос, `0x0B` ответ или событие) |
| 5 | `0x04` ответ на запрос, `0x03` событие с устройства |
| 6–7 | Команда, little-endian |
| 8–9 | Значение, little-endian |
| 10–11 | Сумма `(-sum(bytes[1..end])) & 0xFFFF` |

Регистр mute у PD400X: `0x2022`, `0` = живой, `1` = mute. Регистр `0x2034` — уровень, не mute.

### Как устроена программа

[`HidWatch.cs`](HidWatch.cs) — один файл под .NET Framework 4.x. Слушает HID PD400X, на смене mute шлёт `Ctrl+Shift+Alt+M`, сидит в трее.

### Лицензия

MIT. Личный проект, к Maono не относится.
