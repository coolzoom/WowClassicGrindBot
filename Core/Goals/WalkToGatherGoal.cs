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
        private readonly IBitmapProvider bitmapProvider;
        private readonly WowProcessInput wowProcessInput;
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

        public WalkToGatherGoal(ILogger logger, ConfigurableInput input, Wait wait, AddonReader addonReader, Navigation navigation, StopMoving stopMoving, WowProcessInput wowProcessInput, IBitmapProvider bitmapProvider)
            : base(nameof(WalkToCorpseGoal))
        {
            this.logger = logger;
            this.wait = wait;
            this.input = input;
            this.wowProcessInput = wowProcessInput;
            this.bitmapProvider = bitmapProvider;

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

            navigation.SetWayPoints(new Vector3[] { playerReader.BestGatherPos });

            onEnterTime = DateTime.UtcNow;
        }

        public override void OnExit()
        {
            navigation.StopMovement();
            navigation.Stop();
        }

        public override void Update()
        {
            if (playerReader.BestGatherPos.WorldDistanceXYTo(playerReader.WorldPos)>3)
            {
                navigation.Update();
            }
            else
            {
                stopMoving.Stop();
                navigation.ResetStuckParameters();
                //set cusor to center screen
                Point p = new System.Drawing.Point(bitmapProvider.Rect.Width / 2, bitmapProvider.Rect.Height / 2);
                wowProcessInput.SetCursorPosition(p);
                input.Interact();
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