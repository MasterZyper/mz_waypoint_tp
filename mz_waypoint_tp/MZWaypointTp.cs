
using CitizenFX.Core;
using CitizenFX.Core.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mz_waypoint_tp
{
    public class MZWaypointTp : BaseScript
    {
        public short new_tp_job = 0;
        public MZWaypointTp()
        {
            Tick += TpJob;
            API.RegisterCommand("SAVETP", new Action<int, List<object>, string>(async (player, value, raw) =>
            {
                new_tp_job = 1;
                await Delay(1);
            }), false);
            API.RegisterCommand("TP", new Action<int, List<object>, string>(async (player, value, raw) =>
            {
                new_tp_job = 2;
                await Delay(1);
            }), false);
        }

        private async Task TpJob()
        {
            Debug.Write($"Z:{World.GetGroundHeight(Game.PlayerPed.Position)} Z2: {World.GetNextPositionOnSidewalk(Game.PlayerPed.Position).Z}");
            Blip waypoint = World.GetWaypointBlip();
            if (waypoint != null)
            {
                switch (new_tp_job)
                {
                    case 1:
                        await DoSaveTp(waypoint);
                        new_tp_job = 0;
                        break;
                    case 2:
                        await DoTp(waypoint);
                        new_tp_job = 0;
                        break;
                    default:
                        await Delay(1000);
                        break;
                }
            }
            else
            {
                await Delay(1000);
            }
        }

        private async Task DoTp(Blip waypoint)
        {
            if (Game.PlayerPed.IsInVehicle())
            {
                Game.PlayerPed.CurrentVehicle.Position = waypoint.Position;
                Game.PlayerPed.CurrentVehicle.IsPositionFrozen = true;
                while (!API.HasCollisionLoadedAroundEntity(Game.PlayerPed.Handle))
                {
                    await Delay(1);
                }
                Game.PlayerPed.CurrentVehicle.IsPositionFrozen = false;
            }
            else
            {
                Vector3 target_pos = waypoint.Position;
                Game.PlayerPed.Position = target_pos;
                target_pos.Z = 0;
                Game.PlayerPed.IsPositionFrozen = true;
                while (target_pos.Z == 0)
                {
                    target_pos.Z = World.GetGroundHeight(target_pos);
                    await Delay(1);
                }
                Game.PlayerPed.Position = target_pos;
                while (!API.HasCollisionLoadedAroundEntity(Game.PlayerPed.Handle))
                {
                    await Delay(1);
                }
                Game.PlayerPed.IsPositionFrozen = false;
            }
            waypoint.Position = Game.PlayerPed.Position;
        }
        private async Task DoSaveTp(Blip waypoint)
        {
            if (Game.PlayerPed.IsInVehicle())
            {
                if (Game.PlayerPed.CurrentVehicle.ClassType == VehicleClass.Boats)
                {
                    Game.PlayerPed.CurrentVehicle.Position = waypoint.Position;
                }
                else
                {
                    Vector3 target_pos = World.GetNextPositionOnStreet(waypoint.Position);
                    Game.PlayerPed.CurrentVehicle.Position = target_pos;
                }
                Game.PlayerPed.CurrentVehicle.IsPositionFrozen = true;
                while (!API.HasCollisionLoadedAroundEntity(Game.PlayerPed.Handle))
                {
                    await Delay(1);
                }
                Game.PlayerPed.CurrentVehicle.IsPositionFrozen = false;
            }
            else
            {
                await Delay(100);
                Vector3 targetpos = waypoint.Position;
                Game.PlayerPed.Position = targetpos;
                Game.PlayerPed.IsPositionFrozen = true;
                await Delay(1);
                targetpos.Z = World.GetGroundHeight(targetpos);
                int trys = 0;
                while (targetpos.Z == 0 && trys < 120)
                {
                    Debug.Write("Warte auf Boden" + trys);
                    trys++;
                    targetpos.Z = World.GetGroundHeight(targetpos);
                    await Delay(1);
                }
                Game.PlayerPed.Position = targetpos;
                await Delay(1);                
                API.GetSafeCoordForPed(
                      Game.PlayerPed.Position.X,
                      Game.PlayerPed.Position.Y,
                      Game.PlayerPed.Position.Z, true, ref targetpos, 16);
                trys = 0;
                while (targetpos.Z == 0 && trys < 120)
                {
                    trys++;
                    Debug.Write("Warte auf Fußweg");
                    targetpos = World.GetNextPositionOnSidewalk(targetpos);
                    API.GetSafeCoordForPed(
                         Game.PlayerPed.Position.X,
                         Game.PlayerPed.Position.Y,
                         Game.PlayerPed.Position.Z, true, ref targetpos, 16);
                    await Delay(1);
                }
                Game.PlayerPed.Position = targetpos;

                while (!API.HasCollisionLoadedAroundEntity(Game.PlayerPed.Handle) && trys < 300)
                {
                    //Warten auf Kollission
                    await Delay(1);
                }
                Game.PlayerPed.IsPositionFrozen = false;
                float water_height = -1000f;
                API.GetWaterHeight(Game.PlayerPed.Position.X, Game.PlayerPed.Position.Y, Game.PlayerPed.Position.Z, ref water_height);
                if (water_height > Game.PlayerPed.Position.Z)
                {
                    //Warten auf Wasser
                    Game.PlayerPed.Position = new Vector3(Game.PlayerPed.Position.X, Game.PlayerPed.Position.Y, water_height + 5);
                }

            }
            waypoint.Position = Game.PlayerPed.Position;
        }
    }
}

