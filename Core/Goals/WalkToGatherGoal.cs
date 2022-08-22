using Core.GOAP;
using Game;
using Microsoft.Extensions.Logging;
using SharedLib;
using SharedLib.Extensions;
using SharedLib.NpcFinder;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;

namespace Core.Goals
{
    public partial class WalkToGatherGoal : GoapGoal, IGoapEventListener, IRouteProvider, IDisposable
    {
        public override float Cost => 1f;

        private readonly ILogger logger;
        private readonly Wait wait;
        private readonly ConfigurableInput input;
        private readonly AddonReader addonReader;
        private readonly PlayerReader playerReader;
        private readonly Navigation navigation;
        private readonly StopMoving stopMoving;
        private readonly NpcNameFinder npcNameFinder;

        private DateTime onEnterTime;

        #region IRouteProvider

        public DateTime LastActive => navigation.LastActive;

        public Vector3[] PathingRoute()
        {
            return navigation.TotalRoute;
        }

        public bool HasNext()
        {
            return navigation.HasNext();
        }

        public Vector3 NextMapPoint()
        {
            return navigation.NextMapPoint();
        }

        #endregion

        public WalkToGatherGoal(ILogger logger, ConfigurableInput input, Wait wait, AddonReader addonReader, Navigation navigation, StopMoving stopMoving, NpcNameFinder npcNameFinder)
            : base(nameof(WalkToGatherGoal))
        {
            this.logger = logger;
            this.wait = wait;
            this.input = input;

            this.addonReader = addonReader;
            this.playerReader = addonReader.PlayerReader;
            this.stopMoving = stopMoving;
            this.npcNameFinder = npcNameFinder;

            this.navigation = navigation;

            AddPrecondition(GoapKey.foundgathertarget, true);
            AddPrecondition(GoapKey.incombat, false);
        }

        public void Dispose()
        {
            navigation.Dispose();
        }

        public void OnGoapEvent(GoapEventArgs e)
        {
            if (e is ResumeEvent)
            {
                navigation.ResetStuckParameters();
            }
        }

        public override void OnEnter()
        {
            playerReader.WorldPosZ = 0;

            Log($"Found Gather Target!");

            navigation.SetWorldWayPoints(new Vector3[] { playerReader.BestGatherPos });

            onEnterTime = DateTime.UtcNow;
        }

        public override void OnExit()
        {
            navigation.StopMovement();
            navigation.Stop();
        }

        public override void Update()
        {
            if (playerReader.BestGatherPos.WorldDistanceXYTo(playerReader.WorldPos) > 3)
            {
                navigation.Update();
                SendGoapEvent(new GoapStateEvent(GoapKey.reachgathertarget, false));
                //if reach the final still not close, do navigate again
                if (!navigation.HasWaypoint())
                {
                    //use better resulution
                    //playerReader.SetMinimapZoomLevel(6);

                    navigation.SetWorldWayPoints(new Vector3[] { playerReader.BestGatherPos });
                }
            }
            else
            {
                stopMoving.Stop();
                navigation.ResetStuckParameters();
                //reset zoom to 1
                //playerReader.SetMinimapZoomLevel(1);

                SendGoapEvent(new GoapStateEvent(GoapKey.reachgathertarget, true));

                //set cusor to center screen
                Rectangle clientrect = npcNameFinder.GetClientRect();

                int StartX = clientrect.Width / 4;
                int SizeX = clientrect.Width / 2;
                int StartY = clientrect.Height / 4;
                int SizeY = clientrect.Height / 2;
                int step = 100;

                for (int i = StartX; i < (StartX + SizeX); i = i + step)
                {
                    for (int j = StartY; j < (StartY + SizeY); j =j + step)
                    {

                        Point clickPostion = npcNameFinder.ToScreenCoordinates(i, j);
                        input.Proc.SetCursorPosition(clickPostion);

                        //cursor classifier
                        CursorClassifier.Classify(out CursorType cls);

                        if (cls == CursorType.Mine || cls == CursorType.Herb)
                        {
                            input.Proc.InteractMouseOver();
                            break;
                        }

                    }
                }

            }

            RandomJump();

            wait.Update();
        }

        private void RandomJump()
        {
            if ((DateTime.UtcNow - onEnterTime).TotalSeconds > 5 && input.ClassConfig.Jump.MillisecondsSinceLastClick > Random.Shared.Next(10_000, 25_000))
            {
                Log("Random jump");
                input.Jump();
            }
        }

        private bool AliveOrLoadingScreen()
        {
            return playerReader.CorpseMapPos == Vector3.Zero;
        }

        private void Log(string text)
        {
            logger.LogInformation($"[{nameof(WalkToCorpseGoal)}]: {text}");
        }
    }
}