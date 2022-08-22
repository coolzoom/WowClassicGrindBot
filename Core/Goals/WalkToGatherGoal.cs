using Core.GOAP;
using Game;
using Microsoft.Extensions.Logging;
using SharedLib;
using SharedLib.Extensions;
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

        public WalkToGatherGoal(ILogger logger, ConfigurableInput input, Wait wait, AddonReader addonReader, Navigation navigation, StopMoving stopMoving)
            : base(nameof(WalkToCorpseGoal))
        {
            this.logger = logger;
            this.wait = wait;
            this.input = input;

            this.addonReader = addonReader;
            this.playerReader = addonReader.PlayerReader;
            this.stopMoving = stopMoving;

            this.navigation = navigation;

            AddPrecondition(GoapKey.foundgathertarget, true);
            
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
            if (playerReader.BestGatherPos.WorldDistanceXYTo(playerReader.WorldPos)>2)
            {
                navigation.Update();

                //if reach the final still not close, do navigate again
                if (!navigation.HasWaypoint())
                {
                    //use better resulution
                    playerReader.SetMinimapZoomLevel(6);

                    navigation.SetWorldWayPoints(new Vector3[] { playerReader.BestGatherPos });
                }
            }
            else
            {
                stopMoving.Stop();
                navigation.ResetStuckParameters();
                //reset zoom to 1
                playerReader.SetMinimapZoomLevel(1);
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