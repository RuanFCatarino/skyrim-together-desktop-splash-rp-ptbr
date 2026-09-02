using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Reflection;
using System.Threading;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;

[assembly: AssemblyTitle("Desktop Splash Screen Together - Add-on Launcher")]
[assembly: AssemblyDescription("Add-on for the original Desktop Splash Screen with Skyrim Together Reborn compatibility")]
[assembly: AssemblyCompany("RuanFCatarino")]
[assembly: AssemblyProduct("Desktop Splash Screen - Together Compatible")]
[assembly: AssemblyCopyright("Copyright (c) 2026 RuanFCatarino")]
[assembly: AssemblyVersion("1.2.0.0")]
[assembly: AssemblyFileVersion("1.2.0.0")]

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        bool ownsMutex;
        using (var instanceMutex = new Mutex(true, "DesktopSplashScreenTogetherLauncher", out ownsMutex))
        {
            if (!ownsMutex)
                return;

            Run(args);
            GC.KeepAlive(instanceMutex);
        }
    }

    private static void Run(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        string launcherDir = AppDomain.CurrentDomain.BaseDirectory;
        string togetherExe = Path.Combine(launcherDir, "SkyrimTogether.exe");
        string dataDir = Directory.GetParent(launcherDir.TrimEnd(Path.DirectorySeparatorChar)).FullName;
        string gameRoot = Directory.GetParent(dataDir).FullName;
        string originalDll = Path.Combine(dataDir, "SKSE", "Plugins", "_SplashScreen.dll");
        string originalPreload = Path.Combine(dataDir, "SKSE", "Plugins", "_SplashScreen_preload.txt");
        string addOnDir = Path.Combine(dataDir, "Interface", "DesktopSplashTogether");
        string addOnGif = Path.Combine(addOnDir, "splash.gif");
        string addOnPng = Path.Combine(addOnDir, "splash.png");
        string originalPng = Path.Combine(dataDir, "Interface", "splash.png");
        string splashPath = new[] { addOnGif, addOnPng, originalPng }.FirstOrDefault(File.Exists);

        if (!File.Exists(originalDll) || !File.Exists(originalPreload))
        {
            MessageBox.Show(
                "O Desktop Splash Screen original não foi encontrado.\n\n" +
                "Instale e ative primeiro o mod original Nexus 83470:\n" +
                "https://www.nexusmods.com/skyrimspecialedition/mods/83470\n\n" +
                "Depois, inicie este complemento pelo Vortex ou MO2.",
                "Dependência obrigatória ausente",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        if (!File.Exists(togetherExe))
        {
            MessageBox.Show(
                "SkyrimTogether.exe não foi encontrado em:\n" + togetherExe +
                "\n\nInstale este pacote na mesma pasta Data do Skyrim Together Reborn.",
                "Desktop Splash Screen - Together",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        if (!File.Exists(splashPath))
        {
            MessageBox.Show(
                "Nenhuma imagem da splash foi encontrada. Use um destes arquivos:\n" +
                addOnGif + "\n" + addOnPng + "\n" + originalPng,
                "Desktop Splash Screen - Together",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        using (var form = new SplashForm(splashPath))
        {
            form.Shown += delegate
            {
                try
                {
                    var startInfo = new ProcessStartInfo(togetherExe)
                    {
                        Arguments = JoinArguments(args),
                        WorkingDirectory = gameRoot,
                        UseShellExecute = false
                    };

                    Process process = Process.Start(startInfo);
                    var timer = new System.Windows.Forms.Timer { Interval = 100 };
                    int elapsed = 0;
                    timer.Tick += delegate
                    {
                        elapsed += timer.Interval;
                        process.Refresh();

                        // Keep the desktop splash up through the launcher's early bootstrap.
                        // The original SKSE plugin takes over and dismisses on kInputLoaded.
                        IntPtr gameWindow = process.HasExited ? IntPtr.Zero : process.MainWindowHandle;
                        bool gameIsVisible = gameWindow != IntPtr.Zero && NativeMethods.IsWindowVisible(gameWindow);

                        // STR does not expose numeric startup progress. Advance smoothly up to
                        // 92%, then reserve completion for the real visible game window.
                        // A slower curve avoids making startup look almost complete too early.
                        // It approaches 90% gradually and still completes only on game visibility.
                        double estimated = 0.90 * (1.0 - Math.Exp(-elapsed / 20000.0));
                        form.SetLaunchProgress((float)estimated);

                        // Skyrim Together creates internal/hidden windows during bootstrap.
                        // Do not dismiss until the actual game window is visible.
                        if (process.HasExited)
                        {
                            timer.Stop();
                            timer.Dispose();
                            form.Close();
                        }
                        else if (gameIsVisible)
                        {
                            timer.Stop();
                            timer.Dispose();
                            form.SetLaunchProgress(1.0f);

                            var completionTimer = new System.Windows.Forms.Timer { Interval = 450 };
                            completionTimer.Tick += delegate
                            {
                                completionTimer.Stop();
                                completionTimer.Dispose();
                                form.Close();
                            };
                            completionTimer.Start();
                        }
                    };
                    timer.Start();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Não foi possível iniciar SkyrimTogether.exe.\n\n" + ex.Message,
                        "Desktop Splash Screen - Together",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    form.Close();
                }
            };

            Application.Run(form);
        }
    }

    private static string JoinArguments(string[] args)
    {
        return string.Join(" ", args.Select(QuoteArgument).ToArray());
    }

    private static string QuoteArgument(string value)
    {
        if (value.Length > 0 && value.IndexOfAny(new[] { ' ', '\t', '"' }) < 0)
            return value;

        return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }
}

internal sealed class SplashForm : Form
{
    private readonly MemoryStream imageStream;
    private readonly Image image;
    private readonly FrameDimension frameDimension;
    private readonly System.Windows.Forms.Timer animationTimer;
    private int currentFrame;
    private readonly int frameCount;
    private float launchProgress;
    private readonly int footerHeight;

    public SplashForm(string imagePath)
    {
        imageStream = new MemoryStream(File.ReadAllBytes(imagePath));
        image = Image.FromStream(imageStream);

        if (image.RawFormat.Guid == ImageFormat.Gif.Guid && image.FrameDimensionsList.Length > 0)
        {
            frameDimension = new FrameDimension(image.FrameDimensionsList[0]);
            frameCount = image.GetFrameCount(frameDimension);
            if (frameCount > 1)
            {
                animationTimer = new System.Windows.Forms.Timer();
                animationTimer.Interval = GetFrameDelay(0);
                animationTimer.Tick += AdvanceFrame;
            }
        }

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;
        TopMost = true;
        footerHeight = 90;
        ClientSize = new Size(image.Width, image.Height + footerHeight);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams parameters = base.CreateParams;
            parameters.ExStyle |= NativeMethods.WS_EX_LAYERED | NativeMethods.WS_EX_TOOLWINDOW;
            return parameters;
        }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        ApplyPerPixelAlpha();
        if (animationTimer != null)
            animationTimer.Start();
    }

    private void AdvanceFrame(object sender, EventArgs e)
    {
        currentFrame = (currentFrame + 1) % frameCount;
        image.SelectActiveFrame(frameDimension, currentFrame);
        animationTimer.Interval = GetFrameDelay(currentFrame);
        ApplyPerPixelAlpha();
    }

    public void SetLaunchProgress(float progress)
    {
        launchProgress = Math.Max(0.0f, Math.Min(1.0f, progress));
        ApplyPerPixelAlpha();
    }

    private int GetFrameDelay(int frame)
    {
        try
        {
            PropertyItem delay = image.GetPropertyItem(0x5100);
            int offset = Math.Min(frame * 4, delay.Value.Length - 4);
            int hundredths = BitConverter.ToInt32(delay.Value, offset);
            return Math.Max(20, hundredths * 10);
        }
        catch
        {
            return 100;
        }
    }

    private void ApplyPerPixelAlpha()
    {
        IntPtr screenDc = NativeMethods.GetDC(IntPtr.Zero);
        IntPtr memoryDc = NativeMethods.CreateCompatibleDC(screenDc);
        IntPtr bitmapHandle = IntPtr.Zero;
        IntPtr previousBitmap = IntPtr.Zero;

        try
        {
            using (var frame = new Bitmap(ClientSize.Width, ClientSize.Height, PixelFormat.Format32bppArgb))
            {
                using (Graphics graphics = Graphics.FromImage(frame))
                {
                    graphics.Clear(Color.FromArgb(255, 7, 7, 8));
                    graphics.DrawImageUnscaled(image, 0, 0);
                    using (var footer = new System.Drawing.Drawing2D.LinearGradientBrush(
                        new Rectangle(0, image.Height, frame.Width, footerHeight),
                        Color.FromArgb(255, 18, 14, 12),
                        Color.FromArgb(255, 5, 5, 7),
                        System.Drawing.Drawing2D.LinearGradientMode.Vertical))
                        graphics.FillRectangle(footer, 0, image.Height, frame.Width, footerHeight);
                    using (var separator = new Pen(Color.FromArgb(160, 122, 88, 48), 1.0f))
                        graphics.DrawLine(separator, 0, image.Height, frame.Width, image.Height);
                    DrawLaunchProgress(graphics, frame.Width, image.Height);
                }
                bitmapHandle = frame.GetHbitmap(Color.FromArgb(0));
            }
            previousBitmap = NativeMethods.SelectObject(memoryDc, bitmapHandle);

            var size = new NativeMethods.Size(ClientSize.Width, ClientSize.Height);
            var source = new NativeMethods.Point(0, 0);
            Rectangle workingArea = Screen.FromControl(this).WorkingArea;
            var destination = new NativeMethods.Point(
                workingArea.Left + (workingArea.Width - ClientSize.Width) / 2,
                workingArea.Top + (workingArea.Height - ClientSize.Height) / 2);
            var blend = new NativeMethods.BlendFunction
            {
                BlendOp = NativeMethods.AC_SRC_OVER,
                SourceConstantAlpha = 255,
                AlphaFormat = NativeMethods.AC_SRC_ALPHA
            };

            if (!NativeMethods.UpdateLayeredWindow(
                    Handle, screenDc, ref destination, ref size, memoryDc,
                    ref source, 0, ref blend, NativeMethods.ULW_ALPHA))
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }
        finally
        {
            if (previousBitmap != IntPtr.Zero)
                NativeMethods.SelectObject(memoryDc, previousBitmap);
            if (bitmapHandle != IntPtr.Zero)
                NativeMethods.DeleteObject(bitmapHandle);
            if (memoryDc != IntPtr.Zero)
                NativeMethods.DeleteDC(memoryDc);
            if (screenDc != IntPtr.Zero)
                NativeMethods.ReleaseDC(IntPtr.Zero, screenDc);
        }
    }

    private void DrawLaunchProgress(Graphics graphics, int width, int imageHeight)
    {
        float center = width / 2.0f;
        float barHalfWidth = width * 0.22f;
        float left = center - barHalfWidth;
        float right = center + barHalfWidth;
        float textY = imageHeight + 9.0f;
        float y = imageHeight + 66.0f;
        float end = left + (right - left) * launchProgress;
        float scale = Math.Max(0.8f, width / 1024.0f);

        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        int animationTime = Environment.TickCount & Int32.MaxValue;
        int dotCount = (animationTime / 450) % 4;
        string loadingText = "CARREGANDO" + new string('.', dotCount);
        float pulse = 0.5f + 0.5f * (float)Math.Sin(animationTime / 420.0);
        int textAlpha = 205 + (int)(50 * pulse);

        using (var font = new Font("Palatino Linotype", 20.0f * scale, FontStyle.Bold, GraphicsUnit.Pixel))
        using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
        using (var textShadow = new SolidBrush(Color.FromArgb(220, 10, 7, 4)))
        using (var textGlow = new SolidBrush(Color.FromArgb(45 + (int)(45 * pulse), 255, 166, 67)))
        using (var textBrush = new SolidBrush(Color.FromArgb(textAlpha, 232, 199, 139)))
        using (var ornament = new Pen(Color.FromArgb(120 + (int)(70 * pulse), 194, 143, 79), 1.2f * scale))
        {
            var textArea = new RectangleF(0, textY, width, 30.0f * scale);
            var shadowArea = new RectangleF(2.0f * scale, textY + 2.0f * scale, width, 30.0f * scale);
            var leftGlowArea = new RectangleF(-1.5f * scale, textY, width, 30.0f * scale);
            var rightGlowArea = new RectangleF(1.5f * scale, textY, width, 30.0f * scale);
            graphics.DrawString(loadingText, font, textGlow, leftGlowArea, format);
            graphics.DrawString(loadingText, font, textGlow, rightGlowArea, format);
            graphics.DrawString(loadingText, font, textShadow, shadowArea, format);
            graphics.DrawString(loadingText, font, textBrush, textArea, format);

            float ornamentY = textY + 15.0f * scale;
            graphics.DrawLine(ornament, center - 205.0f * scale, ornamentY, center - 115.0f * scale, ornamentY);
            graphics.DrawLine(ornament, center + 115.0f * scale, ornamentY, center + 205.0f * scale, ornamentY);
        }

        using (var shadow = new Pen(Color.FromArgb(180, 20, 13, 8), 7.0f * scale))
        using (var glow = new Pen(Color.FromArgb(115, 255, 153, 50), 6.0f * scale))
        using (var fill = new Pen(Color.FromArgb(255, 244, 199, 118), 2.5f * scale))
        {
            graphics.DrawLine(shadow, left, y, right, y);
            if (launchProgress > 0.001f)
            {
                graphics.DrawLine(glow, left, y, end, y);
                graphics.DrawLine(fill, left, y, end, y);
                float radius = 4.0f * scale;
                using (var marker = new SolidBrush(Color.FromArgb(255, 255, 224, 157)))
                    graphics.FillEllipse(marker, end - radius, y - radius, radius * 2, radius * 2);
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (animationTimer != null)
                animationTimer.Dispose();
            image.Dispose();
            imageStream.Dispose();
        }
        base.Dispose(disposing);
    }
}

internal static class NativeMethods
{
    internal const int WS_EX_LAYERED = 0x00080000;
    internal const int WS_EX_TOOLWINDOW = 0x00000080;
    internal const byte AC_SRC_OVER = 0;
    internal const byte AC_SRC_ALPHA = 1;
    internal const int ULW_ALPHA = 2;

    [StructLayout(LayoutKind.Sequential)]
    internal struct Point
    {
        internal int X;
        internal int Y;
        internal Point(int x, int y) { X = x; Y = y; }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Size
    {
        internal int Width;
        internal int Height;
        internal Size(int width, int height) { Width = width; Height = height; }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct BlendFunction
    {
        internal byte BlendOp;
        internal byte BlendFlags;
        internal byte SourceConstantAlpha;
        internal byte AlphaFormat;
    }

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr destinationDc,
        ref Point destination, ref Size size, IntPtr sourceDc, ref Point source,
        int colorKey, ref BlendFunction blend, int flags);

    [DllImport("user32.dll")]
    internal static extern IntPtr GetDC(IntPtr hwnd);

    [DllImport("user32.dll")]
    internal static extern int ReleaseDC(IntPtr hwnd, IntPtr dc);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsWindowVisible(IntPtr hwnd);

    [DllImport("gdi32.dll")]
    internal static extern IntPtr CreateCompatibleDC(IntPtr dc);

    [DllImport("gdi32.dll")]
    internal static extern bool DeleteDC(IntPtr dc);

    [DllImport("gdi32.dll")]
    internal static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);

    [DllImport("gdi32.dll")]
    internal static extern bool DeleteObject(IntPtr obj);
}

