# PD400X → Discord mute

[English](#english) · [Русский](#русский)

[Latest release](https://github.com/DSKirito/pd400x-discord-mute/releases/latest)

---

## English

Press the mute pad on a Maono PD400X. Discord mutes with it.

Small Windows tool. No Maono Link, no Discord plugin, no virtual audio cable.

### Why it exists

The hardware mute on the PD400X only silences the signal inside the microphone DSP. Windows never receives a Core Audio mute flag, so Discord still shows you as live.

Maono Link can see that button. Discord cannot. This program reads vendor HID register `0x2022` and sends Discord the hotkey `Ctrl+Shift+Alt+M`.

### Quick start

1. In Discord open **Settings → Keybinds** and set Mute to `Ctrl + Shift + Alt + M`.
2. Close the Keybinds page. While it is open, Discord captures the combo for recording instead of mute.
3. Quit Maono Link, including the tray icon. It locks the HID interface.
4. Download the [release zip](https://github.com/DSKirito/pd400x-discord-mute/releases/latest) or clone the repo.
5. Run `START.bat`. It builds `HidWatch.exe` with the Windows `csc.exe` compiler and starts it in the system tray.
6. Unmute Discord once by hand so both sides start in the same state, then tap the microphone.

Right-click the tray icon → **Exit** to quit. There is no console window on the taskbar.

### Supported devices

| Device | Status |
|---|---|
| Maono PD400X over USB (`VID 352F`, `PID 0100`, HID `mi_03`) | Tested |
| Other Maono USB mics on `VID 352F` / `31B2` (PD200X / PD300X family) | Same protocol, not tested here |
| PD400X over XLR only, no USB | Not possible — no HID channel |

### FAQ

**Does Maono Link need to be running?**  
No. Close it. If Link is open, this program cannot open the HID device.

**Why a hotkey instead of a Discord plugin?**  
Discord has no public API for mute from a third-party executable. A user keybind is the reliable hook.

**Discord mute is inverted.**  
The tool toggles Discord when the hardware mute *changes*. Start with Discord unmuted and the microphone live, then tap.

**Windows volume mixer still shows the mic as open.**  
Expected. Hardware mute is not the WASAPI endpoint mute. That is the bug this project works around.

**Will this mute Zoom or Teams?**  
Yes, if you bind the same `Ctrl+Shift+Alt+M` there, or change the keys in `HidWatch.cs`.

**Is there a prebuilt EXE?**  
`START.bat` builds one on your PC. An unsigned EXE uploaded to GitHub often trips SmartScreen, so the release ships source plus the batch file.

### Known issues

- Maono Link and HidWatch cannot use the same HID handle at the same time.
- After launch, the first tap only stays in sync if Discord mute already matches the hardware.
- Register `0x2034` is a level / VU meter, not mute. Treating those packets as mute makes Discord flicker.
- No macOS or Linux build. The protocol is USB HID; the hotkey side is Win32 `SendInput`.

### HID protocol

Same family as PD200X. Report size 64/65 bytes, report ID `0x4B`.

| Offset | Field |
|---:|---|
| 0 | Report ID `0x4B` |
| 1 | Magic `0xC4` |
| 2 | Payload length (`0x09` query, `0x0B` reply or event) |
| 5 | `0x04` query reply, `0x03` device event |
| 6–7 | Command, little-endian |
| 8–9 | Value, little-endian |
| 10–11 | Checksum `(-sum(bytes[1..end])) & 0xFFFF` |

Mute register:

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

While the mic is live it also streams register `0x2034` (VU, values around 1400–1700). Ignore everything except `cmd == 0x2022` and `val` 0 or 1.

### How it works

[`HidWatch.cs`](HidWatch.cs) is a single file for .NET Framework 4.x. No NuGet packages.

1. Enumerate HID devices with SetupAPI and pick `VID 352F` / `31B2` or a Maono / PD400 name.
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

Нажал кнопку mute на Maono PD400X — в Discord тоже mute.

Небольшая программа для Windows. Maono Link не нужен, плагин Discord не нужен, виртуальный кабель не нужен.

### Зачем это

Сенсорная кнопка на PD400X глушит сигнал **внутри DSP микрофона**. Windows не получает системный флаг mute, и Discord считает, что микрофон живой.

Maono Link эту кнопку видит. Discord — нет. Программа читает HID-регистр `0x2022` и отправляет в Discord комбинацию `Ctrl+Shift+Alt+M`.

### Быстрый старт

1. В Discord: **Настройки → Горячие клавиши**. На Mute поставь `Ctrl + Shift + Alt + M`.
2. Закрой страницу горячих клавиш. Пока она открыта, Discord ловит комбинацию для записи бинда, а не для mute.
3. Закрой Maono Link, включая иконку в трее. Он занимает HID.
4. Скачай [zip релиза](https://github.com/DSKirito/pd400x-discord-mute/releases/latest) или склонируй репозиторий.
5. Запусти `START.bat`. Он соберёт `HidWatch.exe` штатным `csc.exe` и уберёт программу в трей.
6. Один раз вручную размуть Discord, чтобы состояния совпали, потом нажми кнопку на микрофоне.

Правый клик по иконке в трее → **Выход**. Окна консоли в панели задач нет.

### Какие устройства

| Устройство | Статус |
|---|---|
| Maono PD400X по USB (`VID 352F`, `PID 0100`, HID `mi_03`) | Проверен |
| Другие USB-микрофоны Maono на `VID 352F` / `31B2` (семейство PD200X / PD300X) | Тот же протокол, у нас не проверяли |
| PD400X только по XLR, без USB | Нельзя — нет HID |

### Частые вопросы

**Maono Link должен быть запущен?**  
Нет. Его надо закрыть. Если Link открыт, HID не откроется.

**Почему хоткей, а не плагин Discord?**  
У Discord нет публичного API mute для чужого exe. Горячая клавиша — рабочий способ.

**В Discord всё наоборот.**  
Программа переключает Discord, когда меняется железный mute. Запусти с размученным Discord и живым микрофоном, потом нажми кнопку.

**В микшере Windows микрофон всё равно «включён».**  
Так и должно быть. Аппаратный mute — это не mute устройства Windows. Именно из-за этого программа и существует.

**Можно глушить Zoom или Teams?**  
Да, если там тоже поставить `Ctrl+Shift+Alt+M`, или поменять клавиши в `HidWatch.cs`.

**Где готовый EXE?**  
`START.bat` собирает его у тебя на компьютере. Выкладывать неподписанный exe на GitHub обычно бессмысленно: SmartScreen его ругает.

### Известные проблемы

- Maono Link и HidWatch не могут одновременно держать HID.
- После запуска первое нажатие синхронно, только если mute в Discord уже совпадает с кнопкой.
- Регистр `0x2034` — это уровень сигнала, не mute. Если реагировать на него, Discord мигает.
- Нет сборки под macOS и Linux. Протокол — USB HID, хоткей — Win32 `SendInput`.

### HID-протокол

То же семейство, что у PD200X. Отчёт 64/65 байт, report ID `0x4B`.

| Смещение | Поле |
|---:|---|
| 0 | Report ID `0x4B` |
| 1 | Магическое `0xC4` |
| 2 | Длина (`0x09` запрос, `0x0B` ответ или событие) |
| 5 | `0x04` ответ на запрос, `0x03` событие с устройства |
| 6–7 | Команда, little-endian |
| 8–9 | Значение, little-endian |
| 10–11 | Сумма `(-sum(bytes[1..end])) & 0xFFFF` |

Регистр mute:

```
cmd  0x2022
val  0 = микрофон живой
val  1 = mute
```

Запрос:

```
4B C4 09 00 00 04 22 20 CS CS ...
```

Ответ / событие:

```
4B C4 0B 00 00 04 22 20 00 00 ...   живой
4B C4 0B 00 00 04 22 20 01 00 ...   mute
```

Пока микрофон живой, с устройства идёт регистр `0x2034` (уровень, числа около 1400–1700). Это не mute. Смотри только `cmd == 0x2022` и значение 0 или 1.

### Как устроена программа

[`HidWatch.cs`](HidWatch.cs) — один файл под .NET Framework 4.x, без NuGet.

1. Перечисляет HID через SetupAPI, берёт `VID 352F` / `31B2` или имя Maono / PD400.
2. Открывает устройство с `GENERIC_READ | GENERIC_WRITE` и `FILE_FLAG_OVERLAPPED`.
3. Слушает interrupt IN (около 15 мс). Иногда шлёт запрос `0x2022`.
4. На смене 0 ↔ 1 отправляет `Ctrl+Shift+Alt+M` через `SendInput`.
5. Работает как приложение в трее (`winexe`), без окна консоли.

### Файлы

| Файл | Зачем |
|---|---|
| `START.bat` | Гасит старый процесс, собирает, запускает в трей |
| `HidWatch.cs` | Чтение HID и хоткей Discord |

### Лицензия

MIT. Личный проект, к Maono не относится.
