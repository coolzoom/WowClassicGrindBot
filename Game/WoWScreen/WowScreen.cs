using Microsoft.Extensions.Logging;
using SharedLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using WinAPI;

namespace Game
{
    public sealed class WowScreen : IWowScreen, IBitmapProvider, IDisposable
    {
        private readonly ILogger logger;
        private readonly WowProcess wowProcess;

        public event Action OnScreenChanged;

        private readonly List<Action<Graphics>> drawActions = new();

        // TODO: make it work for higher resolution ex. 4k
        public int MinimapXSize { get; set; } = 168;//pixel adjusted under 1920x1080, so recommend 1920x1080
        public int MinimapYSize { get; set; } = 168;//pixel adjusted under 1920x1080, so recommend 1920x1080
        public int MinimapRightOffset { get; set; } = 27;
        public int MinimapTopOffset { get; set; } = 32;
        public float xscale { get; set; } = 1;
        public float yscale { get; set; } = 1;

        public bool Enabled { get; set; }

        public bool EnablePostProcess { get; set; }
        public Bitmap Bitmap { get; private set; }

        public Bitmap MiniMapBitmap { get; private set; }

        public IntPtr ProcessHwnd => wowProcess.Process.MainWindowHandle;

        private Rectangle rect;
        public Rectangle Rect => rect;

        private readonly Graphics graphics;
        private readonly Graphics graphicsMinimap;

        public WowScreen(ILogger logger, WowProcess wowProcess)
        {
            this.logger = logger;
            this.wowProcess = wowProcess;

            Point p = new();
            GetPosition(ref p);
            GetRectangle(out rect);
            rect.Location = p;


            //change scale
            UpdateScale();

            Bitmap = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppPArgb);
            graphics = Graphics.FromImage(Bitmap);

            MiniMapBitmap = new Bitmap(MinimapXSize, MinimapYSize, PixelFormat.Format32bppPArgb);
            graphicsMinimap = Graphics.FromImage(MiniMapBitmap);
        }

        public void UpdateScale()
        {
            //change scale
            //TODO, MINIMAP scale is different with the window scale
            xscale = (float) ((float)rect.Width / (float)1920);
            yscale = (float) ((float)rect.Height / (float)1080);


            MinimapXSize = (int)(168 * xscale);
            MinimapYSize = (int)(168 * xscale);
            MinimapRightOffset = (int)(27 * xscale);
            MinimapTopOffset = (int)(32 * xscale);
        }

        public void Update()
        {
            Point p = new();
            GetPosition(ref p);
            rect.Location = p;

            //change scale
            UpdateScale();
            
            graphics.CopyFromScreen(rect.Location, Point.Empty, Bitmap.Size);
        }

        public void AddDrawAction(Action<Graphics> a)
        {
            drawActions.Add(a);
        }

        public void PostProcess()
        {
            using (var gr = Graphics.FromImage(Bitmap))
            {
                using (var blackPen = new SolidBrush(Color.Black))
                {
                    gr.FillRectangle(blackPen, new Rectangle(new Point(Bitmap.Width / 15, Bitmap.Height / 40), new Size(Bitmap.Width / 15, Bitmap.Height / 40)));
                }

                drawActions.ForEach(x => x(gr));
            }

            OnScreenChanged?.Invoke();
        }

        public void GetPosition(ref Point point)
        {
            NativeMethods.GetPosition(wowProcess.Process.MainWindowHandle, ref point);
        }

        public void GetRectangle(out Rectangle rect)
        {
            NativeMethods.GetWindowRect(wowProcess.Process.MainWindowHandle, out rect);
        }


        public Bitmap GetBitmap(int width, int height)
        {
            Update();

            Bitmap bitmap = new(width, height);
            Rectangle sourceRect = new(0, 0, width, height);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.DrawImage(Bitmap, 0, 0, sourceRect, GraphicsUnit.Pixel);
            }
            return bitmap;
        }

        public Color GetColorAt(Point point)
        {
            return Bitmap.GetPixel(point.X, point.Y);
        }

        public void UpdateMinimapBitmap()
        {
            GetRectangle(out var rect);
            graphicsMinimap.CopyFromScreen(rect.Right - MinimapXSize - MinimapRightOffset, rect.Top + MinimapTopOffset, 0, 0, MiniMapBitmap.Size);
        }

        public void Dispose()
        {
            Bitmap.Dispose();
            graphics.Dispose();
            graphicsMinimap.Dispose();
        }

        private static Bitmap CropImage(Bitmap img, bool highlight)
        {
            int x = img.Width / 2;
            int y = img.Height / 2;
            int r = Math.Min(x, y);

            var tmp = new Bitmap(2 * r, 2 * r);
            using (Graphics g = Graphics.FromImage(tmp))
            {
                if (highlight)
                {
                    using (SolidBrush brush = new SolidBrush(Color.FromArgb(255, 0, 0)))
                    {
                        g.FillRectangle(brush, 0, 0, img.Width, img.Height);
                    }
                }

                g.SmoothingMode = SmoothingMode.None;
                g.TranslateTransform(tmp.Width / 2, tmp.Height / 2);
                using (var gp = new GraphicsPath())
                {
                    gp.AddEllipse(0 - r, 0 - r, 2 * r, 2 * r);
                    using (var region = new Region(gp))
                    {
                        g.SetClip(region, CombineMode.Replace);
                        using (var bmp = new Bitmap(img))
                        {

                            g.DrawImage(bmp, new Rectangle(-r, -r, 2 * r, 2 * r), new Rectangle(x - r, y - r, 2 * r, 2 * r), GraphicsUnit.Pixel);
                        }
                    }
                }
            }
            return tmp;
        }

        public static string ToBase64(Bitmap bitmap, int size)
        {
            int width, height;
            if (bitmap.Width > bitmap.Height)
            {
                width = size;
                height = Convert.ToInt32(bitmap.Height * size / (float)bitmap.Width);
            }
            else
            {
                width = Convert.ToInt32(bitmap.Width * size / (float)bitmap.Height);
                height = size;
            }
            var resized = new Bitmap(width, height);
            using (var graphics = Graphics.FromImage(resized))
            {
                graphics.CompositingQuality = CompositingQuality.HighSpeed;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.DrawImage(bitmap, 0, 0, width, height);
            }

            using (MemoryStream ms = new MemoryStream())
            {
                resized.Save(ms, ImageFormat.Png);
                resized.Dispose();
                byte[] byteImage = ms.ToArray();
                return Convert.ToBase64String(byteImage); // Get Base64
            }
        }

    }
}