using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

internal static class Program
{
    const uint KEYEVENTF_KEYUP = 0x0002;
    const int INPUT_KEYBOARD = 1;
    const ushort VK_SHIFT = 0x10, VK_CONTROL = 0x11, VK_MENU = 0x12, VK_M = 0x4D;
    const uint DIGCF_PRESENT = 2, DIGCF_DEVICEINTERFACE = 0x10;
    const uint GENERIC_READ = 0x80000000, GENERIC_WRITE = 0x40000000;
    const uint FILE_SHARE_READ = 1, FILE_SHARE_WRITE = 2;
    const uint OPEN_EXISTING = 3;
    const uint FILE_FLAG_OVERLAPPED = 0x40000000;
    const int SW_HIDE = 0;

    static readonly Guid GUID_DEVINTERFACE_HID = new Guid("4D1E55B2-F16F-11CF-88CB-001111000030");

    [DllImport("hid.dll")] static extern void HidD_GetHidGuid(out Guid g);
    [DllImport("hid.dll")] static extern bool HidD_GetAttributes(IntPtr h, ref HATTR a);
    [DllImport("hid.dll")] static extern bool HidD_GetProductString(IntPtr h, byte[] buf, int len);
    [DllImport("hid.dll")] static extern bool HidD_GetManufacturerString(IntPtr h, byte[] buf, int len);
    [DllImport("hid.dll")] static extern bool HidD_SetNumInputBuffers(IntPtr h, int n);
    [DllImport("hid.dll")] static extern bool HidD_GetInputReport(IntPtr h, byte[] buf, int len);
    [DllImport("hid.dll")] static extern bool HidD_SetOutputReport(IntPtr h, byte[] buf, int len);
    [DllImport("hid.dll")] static extern bool HidD_GetFeature(IntPtr h, byte[] buf, int len);
    [DllImport("hid.dll")] static extern bool HidD_SetFeature(IntPtr h, byte[] buf, int len);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool WriteFile(IntPtr h, byte[] buf, int n, out int written, IntPtr ov);
    [DllImport("hid.dll")] static extern bool HidD_GetPreparsedData(IntPtr h, out IntPtr p);
    [DllImport("hid.dll")] static extern bool HidD_FreePreparsedData(IntPtr p);
    [DllImport("hid.dll")] static extern int HidP_GetCaps(IntPtr p, IntPtr caps);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool GetOverlappedResult(IntPtr h, IntPtr ov, out int n, bool wait);

    [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
    static extern IntPtr SetupDiGetClassDevs(ref Guid g, IntPtr e, IntPtr p, uint f);
    [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
    static extern bool SetupDiEnumDeviceInterfaces(IntPtr h, IntPtr i, ref Guid g, uint idx, ref SPDID id);
    [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr h, ref SPDID id, IntPtr det, uint sz, out uint need, IntPtr info);
    [DllImport("setupapi.dll")] static extern bool SetupDiDestroyDeviceInfoList(IntPtr h);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern IntPtr CreateFile(string n, uint acc, uint share, IntPtr sec, uint disp, uint flags, IntPtr t);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool ReadFile(IntPtr h, byte[] buf, int n, out int read, IntPtr ov);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool GetOverlappedResult(IntPtr h, ref OVERLAPPED ov, out int n, bool wait);
    [DllImport("kernel32.dll")] static extern IntPtr CreateEvent(IntPtr a, bool m, bool i, string n);
    [DllImport("kernel32.dll")] static extern uint WaitForSingleObject(IntPtr h, uint ms);
    [DllImport("kernel32.dll")] static extern bool CancelIo(IntPtr h);
    [DllImport("kernel32.dll")] static extern IntPtr GetConsoleWindow();
    [DllImport("user32.dll", SetLastError = true)]
    static extern uint SendInput(uint n, INPUT[] p, int cb);
    [DllImport("user32.dll")] static extern uint MapVirtualKey(uint c, uint t);
    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [StructLayout(LayoutKind.Sequential)]
    struct HATTR { public int Size; public ushort VID, PID, Ver; }
    [StructLayout(LayoutKind.Sequential)]
    struct SPDID { public int Size; public Guid InterfaceClassGuid; public int Flags; public IntPtr Reserved; }
    [StructLayout(LayoutKind.Sequential)]
    struct OVERLAPPED
    {
        public IntPtr Internal, InternalHigh;
        public int Offset, OffsetHigh;
        public IntPtr hEvent;
    }
    [StructLayout(LayoutKind.Sequential)]
    struct KEYBDINPUT { public ushort wVk, wScan; public uint dwFlags, time; public IntPtr dwExtraInfo; }
    [StructLayout(LayoutKind.Sequential)]
    struct MOUSEINPUT { public int dx, dy; public uint mouseData, dwFlags, time; public IntPtr dwExtraInfo; }
    [StructLayout(LayoutKind.Explicit)]
    struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
    }
    [StructLayout(LayoutKind.Sequential)]
    struct INPUT { public int type; public InputUnion U; }

    class HidDev
    {
        public string Path, Name;
        public ushort Vid, Pid;
        public int InLen, OutLen;
        public IntPtr Handle;
    }

    class Watcher
    {
        public HidDev Dev;
        public bool Stop;
    }

    static string ReadZ(byte[] buf)
    {
        try { return Encoding.Unicode.GetString(buf).TrimEnd('\0').Trim(); }
        catch { return ""; }
    }

    static bool LooksMaono(ushort vid, string name, string path)
    {
        string blob = ((name ?? "") + " " + (path ?? "")).ToLowerInvariant();
        if (vid == 0x31B2 || vid == 0x352F) return true;
        if (blob.IndexOf("vid_31b2") >= 0 || blob.IndexOf("vid_352f") >= 0) return true;
        if (blob.IndexOf("maono") >= 0 || blob.IndexOf("pd400") >= 0) return true;
        if (blob.IndexOf("pd200") >= 0 || blob.IndexOf("pd300") >= 0) return true;
        return false;
    }

    static ushort ParseVid(string path)
    {
        if (path == null) return 0;
        string p = path.ToLowerInvariant();
        int i = p.IndexOf("vid_");
        if (i < 0) return 0;
        try { return Convert.ToUInt16(p.Substring(i + 4, 4), 16); }
        catch { return 0; }
    }
    static ushort ParsePid(string path)
    {
        if (path == null) return 0;
        string p = path.ToLowerInvariant();
        int i = p.IndexOf("pid_");
        if (i < 0) return 0;
        try { return Convert.ToUInt16(p.Substring(i + 4, 4), 16); }
        catch { return 0; }
    }

    static IntPtr OpenHid(string path)
    {
        string[] paths = new string[] {
            path,
            path.StartsWith("\\\\?\\") ? path : "\\\\?\\" + path.TrimStart('\\'),
            path.StartsWith("?\\") ? "\\\\" + path : path
        };
        uint[] acc = new uint[] { GENERIC_READ | GENERIC_WRITE, GENERIC_READ, 0 };
        for (int p = 0; p < paths.Length; p++)
        {
            if (string.IsNullOrEmpty(paths[p])) continue;
            for (int i = 0; i < acc.Length; i++)
            {
                IntPtr h = CreateFile(paths[p], acc[i], FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
                if (h != IntPtr.Zero && h != new IntPtr(-1)) return h;
            }
        }
        return IntPtr.Zero;
    }

    static List<HidDev> EnumMaono()
    {
        var list = new List<HidDev>();
        Guid g;
        try { HidD_GetHidGuid(out g); }
        catch { g = GUID_DEVINTERFACE_HID; }
        IntPtr set = SetupDiGetClassDevs(ref g, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
        if (set == new IntPtr(-1)) return list;
        int seen = 0;
        try
        {
            uint idx = 0;
            while (true)
            {
                SPDID id = new SPDID();
                id.Size = Marshal.SizeOf(typeof(SPDID));
                if (!SetupDiEnumDeviceInterfaces(set, IntPtr.Zero, ref g, idx, ref id)) break;
                idx++;
                uint need = 0;
                SetupDiGetDeviceInterfaceDetail(set, ref id, IntPtr.Zero, 0, out need, IntPtr.Zero);
                if (need < 8) continue;
                IntPtr det = Marshal.AllocHGlobal((int)need + 16);
                try
                {
                    for (int z = 0; z < (int)need + 16; z++) Marshal.WriteByte(det, z, 0);
                    Marshal.WriteInt32(det, IntPtr.Size == 8 ? 8 : 5);
                    if (!SetupDiGetDeviceInterfaceDetail(set, ref id, det, need, out need, IntPtr.Zero))
                    {
                        Marshal.WriteInt32(det, IntPtr.Size == 8 ? 8 : 6);
                        if (!SetupDiGetDeviceInterfaceDetail(set, ref id, det, need, out need, IntPtr.Zero))
                            continue;
                    }
                    string path = Marshal.PtrToStringAuto(new IntPtr(det.ToInt64() + 4));
                    if (string.IsNullOrEmpty(path) || path[0] == '?')
                    {
                        string p8 = Marshal.PtrToStringAuto(new IntPtr(det.ToInt64() + 8));
                        if (!string.IsNullOrEmpty(p8) && (p8.StartsWith("\\\\") || p8.StartsWith("hid#") || p8.StartsWith("?\\")))
                            path = p8;
                    }
                    if (string.IsNullOrEmpty(path)) continue;
                    if (path.StartsWith("?\\")) path = "\\\\" + path;
                    else if (path.StartsWith("\\?\\") && !path.StartsWith("\\\\?\\")) path = "\\" + path;
                    else if (path.StartsWith("hid#")) path = "\\\\?\\" + path;
                    seen++;
                    ushort vid = ParseVid(path);
                    ushort pid = ParsePid(path);
                    string prod = "";
                    IntPtr qh = OpenHid(path);
                    if (qh != IntPtr.Zero)
                    {
                        HATTR a = new HATTR();
                        a.Size = Marshal.SizeOf(typeof(HATTR));
                        if (HidD_GetAttributes(qh, ref a)) { vid = a.VID; pid = a.PID; }
                        byte[] ps = new byte[256];
                        if (HidD_GetProductString(qh, ps, ps.Length)) prod = ReadZ(ps);
                        CloseHandle(qh);
                    }
                    if (!LooksMaono(vid, prod, path)) continue;
                    IntPtr h = OpenHid(path);
                    if (h == IntPtr.Zero) continue;
                    int inLen = 65, outLen = 65;
                    IntPtr prep;
                    if (HidD_GetPreparsedData(h, out prep) && prep != IntPtr.Zero)
                    {
                        IntPtr caps = Marshal.AllocHGlobal(64);
                        try
                        {
                            for (int z = 0; z < 64; z++) Marshal.WriteByte(caps, z, 0);
                            int st = HidP_GetCaps(prep, caps);
                            if (st == 0x00110000)
                            {
                                inLen = Math.Max(2, (int)Marshal.ReadInt16(caps, 4));
                                outLen = Math.Max(2, (int)Marshal.ReadInt16(caps, 6));
                            }
                        }
                        finally { Marshal.FreeHGlobal(caps); }
                        HidD_FreePreparsedData(prep);
                    }
                    CloseHandle(h);
                    h = CreateFile(path, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, FILE_FLAG_OVERLAPPED, IntPtr.Zero);
                    if (h == IntPtr.Zero || h == new IntPtr(-1))
                        h = CreateFile(path, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
                    if (h == IntPtr.Zero || h == new IntPtr(-1)) continue;
                    HidD_SetNumInputBuffers(h, 16);
                    list.Add(new HidDev { Path = path, Name = prod, Vid = vid, Pid = pid, InLen = inLen, OutLen = outLen, Handle = h });
                }
                finally { Marshal.FreeHGlobal(det); }
            }
        }
        finally { SetupDiDestroyDeviceInfoList(set); }
        return list;
    }

    static uint SendMuteHotkey()
    {
        INPUT[] seq = new INPUT[8];
        ushort[] keys = new ushort[] { VK_CONTROL, VK_SHIFT, VK_MENU, VK_M };
        for (int i = 0; i < 4; i++)
        {
            seq[i].type = INPUT_KEYBOARD;
            seq[i].U.ki.wVk = keys[i];
            seq[i].U.ki.wScan = (ushort)MapVirtualKey(keys[i], 0);
        }
        for (int i = 0; i < 4; i++)
        {
            ushort vk = keys[3 - i];
            seq[4 + i].type = INPUT_KEYBOARD;
            seq[4 + i].U.ki.wVk = vk;
            seq[4 + i].U.ki.wScan = (ushort)MapVirtualKey(vk, 0);
            seq[4 + i].U.ki.dwFlags = KEYEVENTF_KEYUP;
        }
        return SendInput(8, seq, Marshal.SizeOf(typeof(INPUT)));
    }

    static ushort Csum(byte[] p, int end)
    {
        uint s = 0;
        for (int i = 1; i < end; i++) s += p[i];
        return (ushort)((~s + 1) & 0xFFFF);
    }

    static byte[] QueryMute(byte reportId)
    {
        byte[] p = new byte[65];
        p[0] = reportId;
        p[1] = 0xC4; p[2] = 0x09; p[3] = 0; p[4] = 0; p[5] = 0x04;
        p[6] = 0x22; p[7] = 0x20;
        ushort cs = Csum(p, 8);
        p[8] = (byte)cs; p[9] = (byte)(cs >> 8);
        return p;
    }

    static int ParseMute(byte[] b, int n)
    {
        if (b == null || n < 12) return -1;
        int off = 0;
        if (b[off + 1] != 0xC4 && !(off == 0 && b[0] == 0xC4))
        {
            if (n > 2 && b[1] == 0xC4) off = 0;
            else return -1;
        }
        int cmd = b[off + 6] | (b[off + 7] << 8);
        int val = b[off + 8] | (b[off + 9] << 8);
        if (cmd != 0x2022) return -1;
        if (val != 0 && val != 1) return -1;
        return val;
    }

    static int OvXfer(IntPtr h, bool write, byte[] buf, int len, uint timeoutMs)
    {
        IntPtr ev = CreateEvent(IntPtr.Zero, true, false, null);
        OVERLAPPED ov = new OVERLAPPED();
        ov.hEvent = ev;
        IntPtr pov = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(OVERLAPPED)));
        Marshal.StructureToPtr(ov, pov, false);
        int n = 0;
        bool ok = write ? WriteFile(h, buf, len, out n, pov) : ReadFile(h, buf, len, out n, pov);
        int err = ok ? 0 : Marshal.GetLastWin32Error();
        if (!ok && err == 997)
        {
            uint wr = WaitForSingleObject(ev, timeoutMs);
            if (wr == 0) GetOverlappedResult(h, pov, out n, false);
            else { CancelIo(h); n = 0; }
        }
        else if (!ok) n = 0;
        CloseHandle(ev);
        Marshal.FreeHGlobal(pov);
        return n;
    }

    static void WatchOne(object o)
    {
        Watcher w = (Watcher)o;
        IntPtr h = w.Dev.Handle;
        int inLen = w.Dev.InLen > 0 ? w.Dev.InLen : 65;
        int outLen = w.Dev.OutLen > 0 ? w.Dev.OutLen : 65;
        byte[] boot = QueryMute(0x4B);
        OvXfer(h, true, boot, Math.Min(outLen, boot.Length), 40);
        int lastMute = -2;
        int loops = 0;
        while (!w.Stop)
        {
            loops++;
            byte[] buf = new byte[Math.Max(inLen, 65)];
            int rn = OvXfer(h, false, buf, inLen, 15);
            int muteVal = -1;
            if (rn > 0) muteVal = ParseMute(buf, rn);
            else if ((loops % 6) == 1)
            {
                byte[] q = QueryMute(0x4B);
                OvXfer(h, true, q, Math.Min(outLen, q.Length), 25);
            }
            if (muteVal < 0) continue;
            if (lastMute == -2) lastMute = muteVal;
            else if (muteVal != lastMute)
            {
                lastMute = muteVal;
                SendMuteHotkey();
            }
        }
    }

    [STAThread]
    static int Main()
    {
        try
        {
            IntPtr hwnd = GetConsoleWindow();
            if (hwnd != IntPtr.Zero) ShowWindow(hwnd, SW_HIDE);
        }
        catch { }

        List<HidDev> devs = EnumMaono();
        if (devs.Count == 0)
        {
            MessageBox.Show(
                "PD400X не найден.\nПодключи микрофон по USB и закрой Maono Link.",
                "PD400X → Discord",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return 1;
        }
        foreach (HidDev d in devs)
        {
            Watcher w = new Watcher();
            w.Dev = d;
            Thread t = new Thread(WatchOne);
            t.IsBackground = true;
            t.Start(w);
        }

        NotifyIcon tray = new NotifyIcon();
        tray.Text = "PD400X → Discord mute";
        tray.Icon = SystemIcons.Application;
        tray.Visible = true;
        ContextMenuStrip menu = new ContextMenuStrip();
        menu.Items.Add("Выход", null, delegate { tray.Visible = false; Application.Exit(); });
        tray.ContextMenuStrip = menu;
        tray.ShowBalloonTip(2500, "PD400X → Discord", "Работает в трее. Правый клик → Выход.", ToolTipIcon.Info);
        Application.Run();
        tray.Visible = false;
        return 0;
    }
}
