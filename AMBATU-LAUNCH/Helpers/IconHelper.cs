#nullable enable
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;

namespace AMBATU_LAUNCH.Helpers
{
    public static class IconHelper
    {
        public static async Task<BitmapImage> GetIconFromPath(string path)
        {
            var iconPath = GetIconSourcePath(path);

            var extractedIcon = await ExtractIconFromExeAsync(iconPath);
            if (extractedIcon != null)
            {
                return extractedIcon;
            }

            var thumbnail = await TryGetThumbnailAsync(iconPath);
            if (thumbnail != null)
            {
                return thumbnail;
            }

            return await CreatePlaceholderIconAsync();
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHDefExtractIcon(
            string pszIconFile,
            int iIndex,
            uint uFlags,
            ref IntPtr phiconLarge,
            ref IntPtr phiconSmall,
            uint nIconSize);

        private static async Task<BitmapImage?> ExtractIconFromExeAsync(string path)
        {
            IntPtr hIconLarge = IntPtr.Zero;
            IntPtr hIconSmall = IntPtr.Zero;
            try
            {
                // Request a 256x256 icon (Jumbo)
                uint size = (uint)((256 << 16) | 256);
                int result = SHDefExtractIcon(path, 0, 0, ref hIconLarge, ref hIconSmall, size);
                
                if (result == 0 && hIconLarge != IntPtr.Zero)
                {
                    var icon = System.Drawing.Icon.FromHandle(hIconLarge);
                    using var bmp = icon.ToBitmap();
                    using var memoryStream = new MemoryStream();
                    bmp.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                    memoryStream.Position = 0;
                    
                    var stream = new InMemoryRandomAccessStream();
                    var destStream = stream.AsStreamForWrite();
                    await memoryStream.CopyToAsync(destStream);
                    await destStream.FlushAsync();
                    stream.Seek(0);
                    
                    var bitmapImage = new BitmapImage();
                    await bitmapImage.SetSourceAsync(stream);
                    return bitmapImage;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"High res icon extraction error: {ex}");
            }
            finally
            {
                if (hIconLarge != IntPtr.Zero) DestroyIcon(hIconLarge);
                if (hIconSmall != IntPtr.Zero) DestroyIcon(hIconSmall);
            }

            return null;
        }

        private static async Task<BitmapImage?> TryGetThumbnailAsync(string path)
        {
            try
            {
                var file = await StorageFile.GetFileFromPathAsync(path);
                using var thumbnail = await file.GetThumbnailAsync(
                    ThumbnailMode.SingleItem,
                    512,
                    ThumbnailOptions.UseCurrentScale | ThumbnailOptions.ResizeThumbnail);

                if (thumbnail != null && thumbnail.Size > 0)
                {
                    var bitmapImage = new BitmapImage();
                    bitmapImage.DecodePixelWidth = 256;
                    await bitmapImage.SetSourceAsync(thumbnail);
                    return bitmapImage;
                }
            }
            catch (Exception)
            {
                // Ignore and fall back to shell icon extraction.
            }

            return null;
        }

        private static async Task<BitmapImage> CreatePlaceholderIconAsync()
        {
            const string transparentPngBase64 =
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/xcAAgMBgGg0q1YAAAAASUVORK5CYII=";

            var bytes = Convert.FromBase64String(transparentPngBase64);
            using var stream = new InMemoryRandomAccessStream();
            await stream.WriteAsync(bytes.AsBuffer());
            stream.Seek(0);

            var image = new BitmapImage();
            await image.SetSourceAsync(stream);
            return image;
        }

        private static string GetIconSourcePath(string path)
        {
            if (!IsShortcut(path))
            {
                return path;
            }

            var resolved = ResolveShortcutTarget(path);
            if (!string.IsNullOrWhiteSpace(resolved))
            {
                return resolved;
            }

            return path;
        }

        private static bool IsShortcut(string path)
        {
            return path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase);
        }

        private static string? ResolveShortcutTarget(string shortcutPath)
        {
            try
            {
                var shellLink = (IShellLinkW)new ShellLink();
                try
                {
                    ((IPersistFile)shellLink).Load(shortcutPath, 0);
                    var sb = new StringBuilder(260);
                    shellLink.GetPath(sb, sb.Capacity, out var data, SLGP_RAWPATH);
                    var target = sb.ToString();
                    return string.IsNullOrWhiteSpace(target) ? null : target;
                }
                finally
                {
                    Marshal.ReleaseComObject(shellLink);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private const uint SLGP_RAWPATH = 0x00000004;

        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        private class ShellLink
        {
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        private interface IShellLinkW
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cch, out WIN32_FIND_DATAW pfd, uint fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cch);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cch);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cch);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out short wHotkey);
            void SetHotkey(short wHotkey);
            void GetShowCmd(out int iShowCmd);
            void SetShowCmd(int iShowCmd);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cch, out int iIcon);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
            void Resolve(IntPtr hwnd, int fFlags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("0000010b-0000-0000-C000-000000000046")]
        private interface IPersistFile
        {
            void GetClassID(out Guid pClassID);
            [PreserveSig]
            int IsDirty();
            void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
            void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, bool fRemember);
            void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
            void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WIN32_FIND_DATAW
        {
            public uint dwFileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }
    }
}
