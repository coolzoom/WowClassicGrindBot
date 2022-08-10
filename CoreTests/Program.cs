using Serilog;
using Serilog.Extensions.Logging;
using SharedLib.NpcFinder;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using Core.Goals;
using System.Numerics;
using System.Runtime.Serialization;
using Core;

#pragma warning disable 0162

namespace CoreTests
{
    class Program
    {
        private static Microsoft.Extensions.Logging.ILogger logger;

        private const bool LogSelf = false;

        public static void Main()
        {
            Vector3 start = new Vector3 ((float)-614.7, (float)-4335.4, (float)40.4 );
            Vector3 end = new Vector3 ( (float)-590.2, (float)-4206.1, (float)38.7 );

            var path = BloogNavigation.CalculatePath(1, start, end, false);

            var logConfig = new LoggerConfiguration()
                .WriteTo.File("names.log")
                .WriteTo.Debug()
                .CreateLogger();

            Log.Logger = logConfig;
            logger = new SerilogLoggerProvider(Log.Logger).CreateLogger(nameof(Program));

            Test_NPCNameFinder();
            //Test_Input();
        }

        private static void Test_NPCNameFinder()
        {
            //NpcNames types = NpcNames.Enemy;
            //NpcNames types = NpcNames.Corpse;
            NpcNames types = NpcNames.Enemy | NpcNames.Neutral;
            //NpcNames types = NpcNames.Friendly | NpcNames.Neutral;

            Test_NpcNameFinder test = new(logger, types);
            int count = 100;
            int i = 0;

            Stopwatch stopwatch = new();
            double[] sample = new double[count];

            Log.Logger.Information($"running {count} samples...");

            while (i < count)
            {
                if (LogSelf)
                    stopwatch.Restart();

                test.Execute();

                if (LogSelf)
                    sample[i] = stopwatch.ElapsedMilliseconds;

                i++;
                Thread.Sleep(150);
            }

            if (LogSelf)
                Log.Logger.Information($"sample: {count} | avg: {sample.Average(),0:0.00} | min: {sample.Min(),0:0.00} | max: {sample.Max(),0:0.00} | total: {sample.Sum()}");
        }

        private static void Test_Input()
        {
            Test_Input test = new(logger);
            test.Mouse_Movement();
            test.Mouse_Clicks();
            test.Clipboard();
        }
    }
}
