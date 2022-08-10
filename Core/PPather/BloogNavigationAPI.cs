using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PPather.Data;
using System.Threading;
using System.Numerics;
using SharedLib.Data;
using System.Runtime.InteropServices;
using Core.Database;
using SharedLib;

#pragma warning disable 162

namespace Core
{
    public sealed class BloogNavigationAPI : IPPather, IDisposable
    {
        private const bool debug = false;
        private readonly ILogger logger;
        private readonly WorldMapAreaDB worldMapAreaDB;

        private readonly CancellationTokenSource cts;

        public BloogNavigationAPI(ILogger logger, WorldMapAreaDB worldMapAreaDB)
        {
            this.logger = logger;
            this.worldMapAreaDB = worldMapAreaDB;

            cts = new();

        }

        public void Dispose()
        {

        }

        #region old

        public ValueTask DrawLines(List<LineArgs> lineArgs)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask DrawSphere(SphereArgs args)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask<List<Vector3>> FindRoute(int uiMapId, Vector3 fromPoint, Vector3 toPoint)
        {

            try
            {
                if (!worldMapAreaDB.TryGet(uiMapId, out WorldMapArea area))
                    return new ValueTask<List<Vector3>>();

                List<Vector3> result = new();

                Vector3 start = fromPoint;
                Vector3 end = toPoint;

                start = worldMapAreaDB.ToWorld(uiMapId, fromPoint, true);
                end = worldMapAreaDB.ToWorld(uiMapId, toPoint, true);

                // incase haven't asked a pathfinder for a route this value will be 0
                // that case use the highest location
                if (start.Z == 0)
                {
                    start.Z = area.LocTop / 2;
                    end.Z = area.LocTop / 2;
                }

                if (debug)
                    LogInformation($"Finding route from {fromPoint}({start}) map {uiMapId} to {toPoint}({end}) map {uiMapId}...");

                //not working becuase the Z is not accurate
                var ret = BloogNavigation.CalculatePath((uint)area.MapID, start, end, false);

                List<Vector3> resuult = new List<Vector3>();
                foreach (Vector3 v in ret)
                {
                    //resuult.Add(v);
                    if (debug)
                        LogInformation($"new float[] {{ {v.X}f, {v.Y}f, {v.Z}f }},");

                    result.Add(worldMapAreaDB.ToLocal(v, area.MapID, uiMapId));
                }
                return new ValueTask<List<Vector3>>(resuult);
            }
            catch (Exception ex)
            {
                LogError($"Finding route from {fromPoint} to {toPoint}", ex);
                Console.WriteLine(ex);
                return new ValueTask<List<Vector3>>();
            }
        }


        #endregion old

        #region Logging

        private void LogError(string text, Exception? ex = null)
        {
            logger.LogError($"{nameof(BloogNavigationAPI)}: {text}, Please check the BloogNavigation.dll and mmaps folder", ex);
        }

        private void LogInformation(string text)
        {
            logger.LogInformation($"{nameof(BloogNavigationAPI)}: {text}, Using BloogNavigation.dll and mmaps folder");
        }

        #endregion
    }
}