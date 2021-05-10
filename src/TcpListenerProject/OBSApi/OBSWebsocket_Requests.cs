using Newtonsoft.Json.Linq;

namespace TcpListenerProject.OBSApi
{
    /// <summary>
    /// Instance of a connection with an obs-websocket server
    /// </summary>
    public partial class OBSWebsocket
    {
        /// <summary>
        /// Executes hotkey routine, identified by hotkey unique name
        /// </summary>
        /// <param name="hotkeyName">Unique name of the hotkey, as defined when registering the hotkey (e.g. "ReplayBuffer.Save")</param>
        public JObject TriggerHotkeyByName(string hotkeyName)
        {
            var requestFields = new JObject
            {
                { "hotkeyName", hotkeyName }
            };

            return SendRequest("TriggerHotkeyByName", requestFields);
        }

        /// <summary>
        /// Executes hotkey routine, identified by bound combination of keys. A single key combination might trigger multiple hotkey routines depending on user settings
        /// </summary>
        /// <param name="key">Main key identifier (e.g. OBS_KEY_A for key "A"). Available identifiers are here: https://github.com/obsproject/obs-studio/blob/master/libobs/obs-hotkeys.h</param>
        /// <param name="keyModifier">Optional key modifiers object. You can combine multiple key operators. e.g. KeyModifier.Shift | KeyModifier.Control</param>
        public JObject TriggerHotkeyBySequence(OBSHotkey key, KeyModifier keyModifier = KeyModifier.None)
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

            return SendRequest("TriggerHotkeyBySequence", requestFields);
        }
    }
}
