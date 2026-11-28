using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Amodus;

/// <summary>
/// Puts text on the clipboard. Three ways are tried one after another and the result is read back each time,
/// so a way that silently does nothing is noticed: the game's own helper, Unity's, and the Windows API directly.
/// </summary>
internal static class Clipboard
{
    private const uint GmemMoveable = 0x0002;
    private const uint CfUnicodeText = 13;

    /// <summary>Returns true if the text is on the clipboard now.</summary>
    public static bool Set(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        try
        {
            ClipboardHelper.PutClipboardString(text);
            if (Same(ClipboardHelper.GetClipboardString(), text))
                return true;
        }
        catch (Exception e)
        {
            ErrorOnce.Log("Clipboard (game)", e);
        }

        try
        {
            GUIUtility.systemCopyBuffer = text;
            if (Same(GUIUtility.systemCopyBuffer, text))
                return true;
        }
        catch (Exception e)
        {
            ErrorOnce.Log("Clipboard (Unity)", e);
        }

        return SetWithWindowsApi(text);
    }

    private static bool Same(string a, string b)
    {
        return a != null && b != null && a.TrimEnd('\0', '\r', '\n') == b;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr newOwner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint format, IntPtr memory);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalAlloc(uint flags, UIntPtr bytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr memory);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalUnlock(IntPtr memory);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalFree(IntPtr memory);

    private static bool SetWithWindowsApi(string text)
    {
        try
        {
            // The clipboard must be opened with a real window, otherwise SetClipboardData fails.
            var owner = GetActiveWindow();
            if (owner == IntPtr.Zero)
                owner = GetForegroundWindow();

            if (!OpenClipboard(owner))
                return false;

            try
            {
                EmptyClipboard();

                var characters = (text + "\0").ToCharArray();
                var memory = GlobalAlloc(GmemMoveable, (UIntPtr)(uint)(characters.Length * 2));
                if (memory == IntPtr.Zero)
                    return false;

                var pointer = GlobalLock(memory);
                if (pointer == IntPtr.Zero)
                {
                    GlobalFree(memory);
                    return false;
                }

                Marshal.Copy(characters, 0, pointer, characters.Length);
                GlobalUnlock(memory);

                // After a successful SetClipboardData the system owns the memory.
                if (SetClipboardData(CfUnicodeText, memory) == IntPtr.Zero)
                {
                    GlobalFree(memory);
                    return false;
                }

                return true;
            }
            finally
            {
                CloseClipboard();
            }
        }
        catch (Exception e)
        {
            ErrorOnce.Log("Clipboard (Windows)", e);
            return false;
        }
    }
}
