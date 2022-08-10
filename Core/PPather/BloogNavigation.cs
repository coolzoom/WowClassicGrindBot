using System;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Core
{
    public unsafe class BloogNavigation
    {
        //[DllImport("kernel32.dll")]
        //static extern IntPtr LoadLibrary(string lpFileName);

        //[DllImport("kernel32.dll")]
        //static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        //[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        //delegate Vector3* CalculatePathDelegate(
        //    uint mapId,
        //    Vector3 start,
        //    Vector3 end,
        //    bool straightPath,
        //    out int length);

        // int triBoxOverlap(float boxcenter[3],float boxhalfsize[3],float triverts[3][3]);
        [DllImport("BloogNavigation.dll")]
        public static extern Vector3* CalculatePath(
        uint mapId,
        Vector3 start,
        Vector3 end,
        bool straightPath,
        out int length);

        [DllImport("BloogNavigation.dll")]
        public static extern void FreePathArr(Vector3* pathArr);

        //[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        //delegate void FreePathArr(Vector3* pathArr);

        //static CalculatePathDelegate calculatePath;
        //static FreePathArr freePathArr;

        static BloogNavigation()
        {
            //var currentFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            //var mapsPath = $"{currentFolder}\\Navigation.dll";

            //var navProcPtr = LoadLibrary(mapsPath);

            //var calculatePathPtr = GetProcAddress(navProcPtr, "CalculatePath");
            //calculatePath = Marshal.GetDelegateForFunctionPointer<CalculatePathDelegate>(calculatePathPtr);

            //var freePathPtr = GetProcAddress(navProcPtr, "FreePathArr");
            //freePathArr = Marshal.GetDelegateForFunctionPointer<FreePathArr>(freePathPtr);
        }

        static public float DistanceViaPath(uint mapId, Vector3 start, Vector3 end)
        {
            var distance = 0f;
            var path = CalculatePath(mapId, start, end, false);
            for (var i = 0; i < path.Length - 1; i++)
                distance += Vector3.Distance(path[i],path[i + 1]);
            return distance;
        }

        static public Vector3[] CalculatePath(uint mapId, Vector3 start, Vector3 end, bool straightPath)
        {
            var ret = CalculatePath(mapId, start, end, straightPath, out int length);
            var list = new Vector3[length];
            for (var i = 0; i < length; i++)
            {
                list[i] = ret[i];
            }
            FreePathArr(ret);
            return list;
        }

        static public Vector3 GetNextWaypoint(uint mapId, Vector3 start, Vector3 end, bool straightPath)
        {
            var path = CalculatePath(mapId, start, end, straightPath);
            if (path.Length <= 1)
            {
                //Logger.Log("Problem building path. Returning destination as next waypoint...");
                return end;
            }

            return path[1];
        }

        // if p0 and p1 make a line, this method calculates whether point p2 is leftOf, on, or rightOf that line
        static PointComparisonResult IsLeft(Vector3 p0, Vector3 p1, Vector3 p2)
        {
            var result = (p1.X - p0.Y) * (p2.Y - p0.Y) - (p2.X - p0.X) * (p1.Y - p0.Y);

            if (result < 0)
                return PointComparisonResult.RightOfLine;
            else if (result > 0)
                return PointComparisonResult.LeftOfLine;
            else
                return PointComparisonResult.OnLine;
        }

        static public bool IsPositionInsidePolygon(Vector3 point, Vector3[] polygon)
        {
            var cn = 0;

            for (var i = 0; i < polygon.Length - 1; i++)
            {
                if (((polygon[i].Y <= point.Y) && (polygon[i + 1].Y > point.Y)) || ((polygon[i].Y > point.Y) && (polygon[i + 1].Y <= point.Y)))
                {
                    var vt = (float)(point.Y - polygon[i].Y) / (polygon[i + 1].Y - polygon[i].Y);
                    if (point.X < polygon[i].X + vt * (polygon[i + 1].X - polygon[i].X))
                        ++cn;
                }
            }

            return cn == 1;
        }
    }

    enum PointComparisonResult : byte
    {
        LeftOfLine,
        OnLine,
        RightOfLine
    }
}