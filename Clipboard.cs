using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace PhotoBridge;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class Clipboard
{
    private static readonly Lock LockObject = new();

    public static bool SetText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        lock (LockObject)
        {
            try
            {
                var textWithNull = text + "\0";
                var bytes = Encoding.Unicode.GetBytes(textWithNull);
                var hGlobal = Marshal.AllocHGlobal(bytes.Length);
                try
                {
                    Marshal.Copy(bytes, 0, hGlobal, bytes.Length);

                    if (!OpenClipboard(IntPtr.Zero))
                        return false;

                    try
                    {
                        EmptyClipboard();
                        IntPtr result = SetClipboardData(CF_UNICODETEXT, hGlobal);
                        return result != IntPtr.Zero;
                    }
                    finally
                    {
                        CloseClipboard();
                    }
                }
                catch
                {
                    Marshal.FreeHGlobal(hGlobal);
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Clipboard error: {ex.Message}");
                return false;
            }
        }
    }

    private const uint CF_UNICODETEXT = 13;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();
}
