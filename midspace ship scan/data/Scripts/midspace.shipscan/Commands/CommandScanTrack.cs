namespace midspace.shipscan
{
    using Sandbox.ModAPI;
    using System;
    using System.Text.RegularExpressions;
    using VRage.Game;
    using VRage.Game.ModAPI;
    using VRage.Game.ModAPI.Interfaces;

    public class CommandScanTrack : ChatCommand
    {
        private TrackDetail _track;
        private bool _isTracking;
        private int _objectiveLine = 0;
        private IMyControllableEntity _currentController;

        public CommandScanTrack()
            : base(ChatCommandSecurity.User, ChatCommandAccessibility.Client, "track", new[] { "/track" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/track <#>", "Tracking a scanned derelict ship.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
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
                InitHudObjective();
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

        public override void UpdateBeforeSimulation100()
        {
            ProcessPosition();
        }

        private void InitHudObjective()
        {
            IMyHudObjectiveLine objective = MyAPIGateway.Utilities.GetObjectiveLine();
            objective.Title = _track.Title;
            objective.Objectives.Clear();
            objective.Objectives.Add("");
            objective.Show();
        }

        private void Unload()
        {
            if (MyAPIGateway.Session != null)
            {
                try
                {
                    IMyHudObjectiveLine objective = MyAPIGateway.Utilities.GetObjectiveLine();

                    if (objective.Visible)
                        objective.Hide();
                }
                catch (Exception ex)
                {
                    MainChatCommandLogic.Instance.ClientLogger.WriteException(ex, "Exception caught during shutdown of mod.");
                    // catch and ignore issues during shutdown.
                }
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

                objective.Objectives[_objectiveLine] = $"Range:{distance:N}m, Pitch {pitchDirection}:{Math.Abs(heading.Y):N}°, Yaw {yawDirection}:{Math.Abs(heading.X):N}°";
            }
        }

        public override void Dispose()
        {
            Unload();
        }
    }
}
