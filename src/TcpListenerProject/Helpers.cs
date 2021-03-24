using Newtonsoft.Json.Linq;
using OBSWebsocketDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcpListenerProject
{
	public static class Helpers
	{
		public static JObject TriggerHotkeyBySequence(this OBSWebsocket obs, OBSHotkey key, KeyModifier keyModifier = KeyModifier.None)
		{
            var requestFields = new JObject
            {
                { "keyId", key.ToString() },
                { "keyModifiers", new JObject{
                    { "shift", (keyModifier & KeyModifier.Shift) == KeyModifier.Shift },
                    { "alt", (keyModifier & KeyModifier.Alt) == KeyModifier.Alt },
                    { "control", (keyModifier & KeyModifier.Control) == KeyModifier.Control },
                    { "command", (keyModifier & KeyModifier.Command) == KeyModifier.Command } }
                }
            };

            return obs.SendRequest("TriggerHotkeyBySequence", requestFields);
        }
	}
}
