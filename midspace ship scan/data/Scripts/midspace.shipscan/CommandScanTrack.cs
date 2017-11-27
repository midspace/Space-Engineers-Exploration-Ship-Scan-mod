namespace midspace.shipscan
{
    using System;
    using System.Text.RegularExpressions;
    using Sandbox.ModAPI;
    using VRage.Game;
    using VRage.Game.ModAPI;
    using VRageMath;
    using IMyControllableEntity = VRage.Game.ModAPI.Interfaces.IMyControllableEntity;

    public class CommandScanTrack : ChatCommand
    {
        private TrackDetail _track;
        private bool _isTracking;
        private int _objectiveLine = 0;
        private IMyControllableEntity _currentController;

        public CommandScanTrack()
            : base(ChatCommandSecurity.User, "track", new[] { "/track" })
        {
        }

        public override void Help()
        {
            MyAPIGateway.Utilities.ShowMessage("/track <#>", "Tracking a scanned derelict ship.");
        }

        public override bool Invoke(string messageText)
        {
            var match = Regex.Match(messageText, @"/track\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var shipName = match.Groups["Key"].Value;
                TrackDetail detail = null;

                int index;
                if (shipName.Substring(0, 1) == "#" && Int32.TryParse(shipName.Substring(1), out index) && index > 0 && index <= CommandScan.ShipCache.Count)
                {
                    detail = CommandScan.ShipCache[index - 1];
                }

                if (detail == null)
                {
                    MyAPIGateway.Utilities.ShowMessage("Object", "'{0}' not found.", shipName);
                    return true;
                }

                _track = detail;
                _currentController = MyAPIGateway.Session.Player.Controller.ControlledEntity;
                _isTracking = true;
                Init();
                MyAPIGateway.Utilities.ShowMessage("Tracking", "object '{0}'.", _track.Title);
                return true;
            }

            if (_isTracking)
            {
                _isTracking = false;
                MyAPIGateway.Utilities.ShowNotification("Cancelling tracking", 2000, MyFontEnum.White);
                Unload();
                return true;
            }

            return false;
        }

        public override void UpdateBeforeSimulation1000()
        {
            base.UpdateBeforeSimulation1000();
            ProcessPosition();
        }

        private bool Init()
        {
            IMyHudObjectiveLine objective = MyAPIGateway.Utilities.GetObjectiveLine();
            objective.Title = _track.Title;
            objective.Objectives.Clear();
            objective.Objectives.Add("");
            objective.Show();
            return true;
        }

        private void Unload()
        {
            if (MyAPIGateway.Session != null)
            {
                IMyHudObjectiveLine objective = MyAPIGateway.Utilities.GetObjectiveLine();

                if (objective.Visible)
                    objective.Hide();
            }
        }

        public void ProcessPosition()
        {
            IMyHudObjectiveLine objective = MyAPIGateway.Utilities.GetObjectiveLine();
            if (_isTracking)
            {
                if (MyAPIGateway.Session.Player.Controller == null || MyAPIGateway.Session.Player.Controller.ControlledEntity == null || MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity == null)
                    return;

                if (!objective.Visible || objective.Title != _track.Title)
                {
                    _isTracking = false;
                    MyAPIGateway.Utilities.ShowNotification("Objective changed - tracking cancelled", 2500, MyFontEnum.Red);
                    return;
                }

                if (MyAPIGateway.Session.Player.Controller.ControlledEntity != _currentController)
                {
                    _isTracking = false;
                    MyAPIGateway.Utilities.ShowNotification("Cockpit changed - tracking cancelled", 2500, MyFontEnum.Red);
                    Unload();
                    CommandScan.ShipCache.Clear();
                    return;
                }

                //Vector3D position = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.GetPosition();
                if (objective.Title != _track.Title)
                    objective.Title = _track.Title;

                var playerPosition = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.GetPosition();
                var heading = Support.GetRotationAngle(MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.WorldMatrix, _track.Position - playerPosition);
                var distance = Math.Sqrt((playerPosition - _track.Position).LengthSquared());

                var pitchDirection = "Up";
                if (heading.Y < 0)
                    pitchDirection = "Down";

                var yawDirection = "Right";
                if (heading.X < 0)
                    yawDirection = "Left";

                objective.Objectives[_objectiveLine] = string.Format("Range:{0:N}m, Pitch {1}:{2:N}°, Yaw {3}:{4:N}°", distance, pitchDirection, Math.Abs(heading.Y), yawDirection, Math.Abs(heading.X));
            }
        }

        protected override void Dispose()
        {
            Unload();
        }
    }
}
