using Exiled.API.Features;
using CommandSystem;
using MEC;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DashPlugin
{
    public class Plugin : Plugin<Config>
    {
        public override string Author => "Kasten";
        public override string Name => "DashPlugin";
        public override string Prefix => "dash";
        public override Version Version => new Version(1, 1, 0);
        public override Version RequiredExiledVersion => new Version(9, 0, 0);

        public override void OnEnabled()
        {
            Log.Info("DashPlugin aktiviert");
            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            Log.Info("DashPlugin deaktiviert");
            base.OnDisabled();
        }
    }

    [CommandHandler(typeof(ClientCommandHandler))]
    public class DashCommand : ICommand
    {
        public string Command => "dash";
        public string[] Aliases => Array.Empty<string>();
        public string Description => "Führe einen Dash aus.";

        private static readonly Dictionary<string, float> lastDashTime = new();

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var player = Player.Get(sender);
            if (player == null)
            {
                response = "Nur Spieler können diesen Befehl nutzen.";
                return false;
            }

            float currentTime = Time.time;
            float cooldown = 60f;

            if (lastDashTime.TryGetValue(player.UserId, out float lastTime))
            {
                float timeLeft = cooldown - (currentTime - lastTime);
                if (timeLeft > 0)
                {
                    response = $"Bitte warte noch {Mathf.CeilToInt(timeLeft)} Sekunden, bevor du wieder dashen kannst.";
                    return false;
                }
            }

            lastDashTime[player.UserId] = currentTime;

            Timing.RunCoroutine(PushPlayer(player));

            response = "Dash ausgeführt";
            return true;
        }

        private static IEnumerator<float> PushPlayer(Player pusher)
        {
            float force = 8f;  
            int iterations = 20; 

            Vector3 pushed = pusher.CameraTransform.forward * force;
            Vector3 endPosition = pusher.Position + new Vector3(pushed.x, 0, pushed.z);

            for (int i = 1; i <= iterations; i++)
            {
                Vector3 newPos = Vector3.MoveTowards(
                    pusher.Position,
                    endPosition,
                    force / iterations
                );

                if (Physics.Linecast(pusher.Position, newPos, out var hit))
                    if (!Player.TryGet(hit.collider, out _))
                        yield break;

                pusher.Position = newPos;
                yield return Timing.WaitForOneFrame;
            }
        }
    }
}