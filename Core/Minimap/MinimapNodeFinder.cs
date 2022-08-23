using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Game;
using Microsoft.Extensions.Logging;
using SharedLib.Extensions;

namespace Core
{
    public class MinimapNodeFinder
    {
        private struct Score
        {
            public int X;
            public int Y;
            public int count;
        }

        private readonly ILogger logger;
        private readonly WowScreen wowScreen;
        private readonly IAddonReader addonReader;
        public event EventHandler<MinimapNodeEventArgs>? NodeEvent;

        private const int MinScore = 2;
        private const int MaxBlue = 34;
        private const int MinRedGreen = 176;


        //minimap 

        // TODO: adjust these values based on resolution
        // The reference resolution is 1920x1080
        int MiniMapZoomLevel=5; //0(default),1,2,3,4,5
        int minX = 0;
        int maxX = 168;
        int minY = 0;
        int maxY = 168;

        Rectangle rect;
        Point center;
        float radius;

        public MinimapNodeFinder(ILogger logger, WowScreen wowScreen, IAddonReader addonReader)
        {
            this.logger = logger;
            this.wowScreen = wowScreen;
            this.addonReader = addonReader;
        }

        public void TryFind()
        {
            wowScreen.UpdateMinimapBitmap();

            var list = FindYellowPoints();
            ScorePoints(list, out Score best);

            if (best.X != 0 && best.Y != 0)
            {
                Vector3 gp = GetMiniMapWorldLoc(best.X, best.Y, MiniMapZoomLevel);

                //pick the nearest, do not jumping around
                float vcorrent = addonReader.PlayerReader.WorldPos.WorldDistanceXYTo(addonReader.PlayerReader.BestGatherPos);
                float vnew = addonReader.PlayerReader.WorldPos.WorldDistanceXYTo(gp);

                if (vnew < vcorrent)
                {
                    addonReader.PlayerReader.BestGatherPos = gp;
                }
                
            }
            else
            {
                addonReader.PlayerReader.BestGatherPos = new Vector3();
                //WIP try to change herb/mine
                
            }

            //we should not add more if player bestgatherpos is not empty,it wil jumping around if there is few nodes near together
            //also remember to clear bestgatherpos
            if (addonReader.PlayerReader.BestGatherPos.X==0 && addonReader.PlayerReader.BestGatherPos.Y==0)
            {
  
            }

            
            NodeEvent?.Invoke(this, new MinimapNodeEventArgs(best.X, best.Y, list.Count(x => x.count > MinScore)));
        }

        private List<Score> FindYellowPoints()
        {
            //find
            MiniMapZoomLevel = addonReader.PlayerReader.MiniMapZoomLevel;
            List<Score> points = new(100);
            Bitmap bitmap = wowScreen.MiniMapBitmap;
            int minX = 0;
            int maxX = wowScreen.MiniMapBitmap.Width;
            int minY = 0;
            int maxY = wowScreen.MiniMapBitmap.Height;

            rect = new(minX, minY, maxX - minX, maxY - minY);
            center = rect.Centre();
            radius = (maxX - minX) / 2f;

            unsafe
            {
                BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
                int bytesPerPixel = Bitmap.GetPixelFormatSize(bitmap.PixelFormat) / 8;

                //for (int y = minY; y < maxY; y++)
                Parallel.For(minY, maxY, y =>
                {
                    byte* currentLine = (byte*)data.Scan0 + (y * data.Stride);
                    for (int x = minX; x < maxX; x++)
                    {
                        if (!IsValidSquareLocation(x, y, center, radius))
                            continue;

                        int xi = x * bytesPerPixel;
                        if (IsMatch(currentLine[xi + 2], currentLine[xi + 1], currentLine[xi]))
                        {
                            if (points.Capacity == points.Count)
                                return;

                            points.Add(new Score { X = x, Y = y, count = 0 });
                            currentLine[xi + 2] = 255;
                            currentLine[xi + 1] = 0;
                            currentLine[xi + 0] = 0;
                        }
                    }
                });

                bitmap.UnlockBits(data);
            }

            if (points.Count == points.Capacity)
            {
                logger.LogWarning("Too much yellow in this image!");
                points.Clear();
            }

            return points;
        }

        public Vector3 GetMiniMapWorldLoc(int x, int y, int minimapzoomlevel)
        {
            //zoom from 0-5
            //at Zoom5, the world size covered by the minimap is 120,120
            //at Zoom0(default zoom), the world size covered by the minimap is 440,440
            //Also tested few other zoom and it looks that
            //the size vs zoom can be calculated by size = 440 - (GetZoom * 64)(edited)
            //while at 1920x1080, the minimap world size is at 168px x 168px (166x166 probably ok as well)
            //               x+
            //world map   y+ p  y-
            //               x-

            //               y-
            //image       x- p  x+
            //               y+
            float worldsize = 440 - (minimapzoomlevel * 64);//actual world size in minimap
            float Xdistanceperpixel = worldsize / (float)wowScreen.MinimapXSize; //0.71423f; //at 1902x1080 it is (120 / 168); //zoom 5
            float Ydistanceperpixel = worldsize / (float)wowScreen.MinimapYSize; //0.71423f; //at 1902x1080 it is (120 / 168); //zoom 5
            int xoff = x - center.X;
            int yoff = y - center.Y;
            float finalx = (float)(addonReader.PlayerReader.WorldPos.X - yoff * Xdistanceperpixel);
            float finaly = (float)(addonReader.PlayerReader.WorldPos.Y - xoff * Ydistanceperpixel);
            Vector3 vector3 = new Vector3(finalx, finaly, addonReader.PlayerReader.WorldPosZ);
            return vector3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMatch(byte red, byte green, byte blue)
        {
            return blue < MaxBlue && red > MinRedGreen && green > MinRedGreen;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsValidSquareLocation(int x, int y, Point center, float width)
        {
            return Math.Sqrt(((x - center.X) * (x - center.X)) + ((y - center.Y) * (y - center.Y))) < width;
        }

        private static bool ScorePoints(List<Score> points, out Score best)
        {
            best = new Score();
            const int size = 5;

            for (int i = 0; i < points.Count; i++)
            {
                Score p = points[i];
                p.count =
                    points.Where(s => Math.Abs(s.X - p.X) < size) // + or - n pixels horizontally
                    .Count(s => Math.Abs(s.Y - p.Y) < size);

                points[i] = p;
            }

            points.Sort((a, b) => a.count.CompareTo(b.count));

            if (points.Count > 0 && points[^1].count > MinScore)
            {
                best = points[^1];
                return true;
            }

            return false;
        }
    }
}