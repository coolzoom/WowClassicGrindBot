using Core.GOAP;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Core.Goals
{
    public partial class WalkToSafePointGoal : GoapGoal, IGoapEventListener, IRouteProvider, IDisposable
    {
        public override float Cost => 1f;

        private readonly ILogger logger;
        private readonly Wait wait;
        private readonly ConfigurableInput input;

        private readonly AddonReader addonReader;
        private readonly PlayerReader playerReader;
        private readonly Navigation navigation;
        private readonly StopMoving stopMoving;

        private readonly Random random = new();

        private DateTime onEnterTime;

        #region IRouteProvider

        public DateTime LastActive => navigation.LastActive;

        public List<Vector3> PathingRoute()
        {
            return navigation.TotalRoute;
        }

        public bool HasNext()
        {
            return navigation.HasNext();
        }

        public Vector3 NextPoint()
        {
            return navigation.NextPoint();
        }

        #endregion

        public WalkToSafePointGoal(ILogger logger, ConfigurableInput input, Wait wait, AddonReader addonReader, Navigation navigation, StopMoving stopMoving)
            : base(nameof(WalkToSafePointGoal))
        {
            this.logger = logger;
            this.wait = wait;
            this.input = input;

            this.addonReader = addonReader;
            this.playerReader = addonReader.PlayerReader;
            this.stopMoving = stopMoving;

            this.navigation = navigation;

            AddPrecondition(GoapKey.pulled, true);
            //AddPrecondition(GoapKey.newtarget, false);
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
        private Vector3 safeloc = new Vector3((float)30.1156, (float)74.1506, (float)0.0);
        public override void OnEnter()
        {
            //playerReader.ZCoord = 0;
            //addonReader.PlayerDied();

            //wait.While(AliveOrLoadingScreen);
            //Log($"Player teleported to the graveyard!");

            if (Math.Abs(addonReader.PlayerReader.PlayerLocation.X - safeloc.X) < 0.1 && Math.Abs(addonReader.PlayerReader.PlayerLocation.Y - safeloc.Y) < 0.1)
            {
                //we should set the pulled to false, because we alreay in position and should start combat
                PulledState = false; 
                //AddEffect(GoapKey.pulled,false);
                //SendGoapEvent(new GoapStateEvent(GoapKey.pulled, false));
            }
            else
            {
                Log($"safe is {safeloc}");

                navigation.SetWayPoints(new Vector3[] { safeloc });

                onEnterTime = DateTime.UtcNow;
            }

        }

        public override void OnExit()
        {
            navigation.StopMovement();
            navigation.Stop();
            
        }

        public override void Update()
        {

            if (playerReader.Bits.IsDrowning())
                input.Jump();

            if (Math.Abs(addonReader.PlayerReader.PlayerLocation.X - safeloc.X) < 0.1 && Math.Abs(addonReader.PlayerReader.PlayerLocation.Y - safeloc.Y) < 0.1)
            {
                //we should set the pulled to false, because we alreay in position and should start combat
                PulledState = false;
            }
            else
            {
                navigation.Update();
            }

            

            RandomJump();

            wait.Update();
        }

        private void RandomJump()
        {
            if ((DateTime.UtcNow - onEnterTime).TotalSeconds > 5 && input.ClassConfig.Jump.MillisecondsSinceLastClick > random.Next(10_000, 25_000))
            {
                Log("Random jump");
                input.Jump();
            }
        }

        private void Log(string text)
        {
            logger.LogInformation($"[{nameof(WalkToSafePointGoal)}]: {text}");
        }
    }
}