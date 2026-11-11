// TANGHALAN VR's link to GAMA, built on the SIMPLE template's SimulationManager (Assets/Scripts/Gama).
// The template connects to the SIMPLE WebPlatform, joins the GAMA experiment, places the visitor where GAMA
// says, and keeps telling GAMA where the visitor is. This class adds what TANGHALAN needs on top:
//   - the messages GAMA sends (the flood on the street, the shaking, the neighbors), passed to Scenario;
//   - the actions TANGHALAN asks GAMA to do (start the storm, the earthquake, the neighbors' walk);
//   - the position of the TANGHALAN world inside GAMA's world (see the constructor).
// The model on the GAMA side is GAMA/models/TANGHALAN VR.gaml.
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Tanghalan.Vr
{
    public class TanghalanSimulationManager : SimulationManager
    {
        /// <summary>
        /// GAMA's world starts at its north-west corner and counts y southwards; TANGHALAN's web coordinates
        /// have their origin in the lobby. GAMA's (x, y) is the web's (x + 30, z + 25): the same numbers are
        /// in the GAML model (offset_x and offset_y).
        /// </summary>
        public const float OffsetX = 30f, OffsetY = 25f;

        public TanghalanSimulationManager()
        {
            // the template turns GAMA's (x, y) into Unity's (x * coef + offsetX, -y * coef + offsetY)
            GamaCRSOffsetX = -OffsetX;
            GamaCRSOffsetY = OffsetY;
        }

        /// <summary>Called for every TANGHALAN message from GAMA: its "tanghalan" kind and the whole message.</summary>
        public static event Action<string, JObject> OnTanghalanMessage;
        /// <summary>Called when the visitor presses the right controller's main button (the template's own hook).</summary>
        public static event Action OnMainButton;

        public static bool Linked
        {
            get
            {
                return ConnectionManager.Instance != null && ConnectionManager.Instance.IsConnectionState(ConnectionState.AUTHENTICATED)
                    && Instance != null && Instance.IsGameState(GameState.GAME);
            }
        }

        /// <summary>Asks the unity_linker agent in GAMA to do an action (only when connected).</summary>
        public static void Ask(string action, Dictionary<string, string> args)
        {
            if (!Linked) return;
            if (!args.ContainsKey("id")) args["id"] = ConnectionManager.Instance.GetConnectionId();
            ConnectionManager.Instance.SendExecutableAsk(action, args);
        }

        public static void Ask(string action, params string[] keyValues)
        {
            var args = new Dictionary<string, string>();
            for (int i = 0; i + 1 < keyValues.Length; i += 2) args[keyValues[i]] = keyValues[i + 1];
            Ask(action, args);
        }

        protected override void ManageOtherMessages(string content)
        {
            JObject o;
            try { o = JObject.Parse(content); }
            catch (Exception) { return; }
            var kind = (string)o["tanghalan"];
            if (kind == null) return;
            if (OnTanghalanMessage != null) OnTanghalanMessage(kind, o);
        }

        protected override void TriggerMainButton()
        {
            if (OnMainButton != null) OnMainButton();
        }
    }
}
