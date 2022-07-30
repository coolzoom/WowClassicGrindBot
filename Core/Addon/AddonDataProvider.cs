using Game;
//using PlayerMonitor;
using Process.NET;
using Process.NET.Patterns;
using SharedLib;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Core
{
    public sealed class AddonDataProvider : IDisposable
    {
        private readonly WowScreen wowScreen;

        private readonly DataFrame[] frames;
        private readonly int[] data;

        //                                 B  G  R
        private readonly byte[] fColor = { 0, 0, 0 };
        private readonly byte[] lColor = { 129, 132, 30 };

        private const PixelFormat pixelFormat = PixelFormat.Format32bppPArgb;
        private readonly int bytesPerPixel;

        private readonly Rectangle rect;
        private readonly Bitmap bitmap;
        private readonly Graphics graphics;


        private static ProcessSharp processSharp;
        private static PatternScanner patternScanner;

        private float[] fxyz;
        private float fOrie;
        private int iMapID;

        public AddonDataProvider(WowScreen wowScreen, DataFrame[] frames)
        {
            this.wowScreen = wowScreen;
            this.frames = frames;

            data = new int[this.frames.Length];
            //fxyz = new float[3];

            for (int i = 0; i < this.frames.Length; i++)
            {
                if (frames[i].X > rect.Width)
                    rect.Width = frames[i].X;

                if (frames[i].Y > rect.Height)
                    rect.Height = frames[i].Y;
            }
            rect.Width++;
            rect.Height++;

            bytesPerPixel = Image.GetPixelFormatSize(pixelFormat) / 8;
            bitmap = new(rect.Width, rect.Height, pixelFormat);

            graphics = Graphics.FromImage(bitmap);
        }

        public void Update()
        {
            Point windowLoc = new();
            wowScreen.GetPosition(ref windowLoc);
            graphics.CopyFromScreen(windowLoc, Point.Empty, rect.Size);

            unsafe
            {
                //
                processSharp = new ProcessSharp(wowScreen.wowProcess.Process, Process.NET.Memory.MemoryType.Remote);
                patternScanner = new PatternScanner(processSharp[processSharp.Native.MainModule.ModuleName]);
				//test read plyer xyz only,
                //2.5.2 40892 46848120 0x2CAD878
                //2.5.4.44400 44792952 0x2AB7C78
				//2.5.4 44833 45337720 0x2B3CC78
                fxyz = processSharp.Memory.Read<float>(IntPtr.Add(processSharp.Native.MainModule.BaseAddress, 0x2B3CC78), 3);
				//Orientation 
                //2.5.2 40892 46842736  0x2CAC370
                //2.5.4.44400 46920064  0x2CBF180
				//2.5.4 44833 45716584  0x2B99468
                fOrie = processSharp.Memory.Read<float>(IntPtr.Add(processSharp.Native.MainModule.BaseAddress, 0x2B99468));
				//MapID 
                //2.5.2 40892 46869872  0x2CB2D70
                //2.5.4 44400 46114844  0x2BFA81C
				//2.5.4 44833 46659612  0x2C7F81C
                iMapID = processSharp.Memory.Read<int>(IntPtr.Add(processSharp.Native.MainModule.BaseAddress, 0x2C7F81C));

                BitmapData bd = bitmap.LockBits(rect, ImageLockMode.ReadOnly, pixelFormat);

                byte* fLine = (byte*)bd.Scan0 + (frames[0].Y * bd.Stride);
                int fx = frames[0].X * bytesPerPixel;

                byte* lLine = (byte*)bd.Scan0 + (frames[^1].Y * bd.Stride);
                int lx = frames[^1].X * bytesPerPixel;

                for (int i = 0; i < 3; i++)
                {
                    if (fLine[fx + i] != fColor[i] || lLine[lx + i] != lColor[i])
                        goto Exit;
                }

                for (int i = 0; i < frames.Length; i++)
                {
                    fLine = (byte*)bd.Scan0 + (frames[i].Y * bd.Stride);
                    fx = frames[i].X * bytesPerPixel;

                    data[frames[i].Index] = (fLine[fx + 2] * 65536) + (fLine[fx + 1] * 256) + fLine[fx];
                }

            Exit:
                bitmap.UnlockBits(bd);
            }
        }

        public float GetXYZ(int i)
        {
            return fxyz[i];
        }

        public Vector3 GetXYZ()
        {
            Vector3 pos = new Vector3(fxyz[0], fxyz[1], fxyz[2]);
            return pos;
        }

        public Vector3 GetXYZUI()
        {
            Vector3 pos = new Vector3(0 - fxyz[1]/200, 0 - fxyz[0]/200, fxyz[2]);
            return pos;
        }

        public float GetOrientation()
        {
            return fOrie;
        }

        public int GetMapID()
        {
            return iMapID;
        }

        public int GetInt(int index)
        {
            return data[index];
        }

        public float GetFixed(int index)
        {
            return GetInt(index) / 100000f;
        }

        public string GetString(int index)
        {
            int color = GetInt(index);
            if (color != 0)
            {
                string colorString = color.ToString();
                if (colorString.Length > 6) { return string.Empty; }
                string colorText = "000000"[..(6 - colorString.Length)] + colorString;
                return ToChar(colorText, 0) + ToChar(colorText, 2) + ToChar(colorText, 4);
            }
            else
            {
                return string.Empty;
            }
        }

        private static string ToChar(string colorText, int start)
        {
            return ((char)int.Parse(colorText.Substring(start, 2))).ToString();
        }

        public void Dispose()
        {
            bitmap.Dispose();
            graphics.Dispose();
        }
    }
}

