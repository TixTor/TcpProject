using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using WindowsInput;
using WindowsInput.Native;
using MessageCommands;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocketSharp;
using OBSWebsocketDotNet;

namespace TcpListenerProject
{
	class Program
	{
		private static List<KeyCommand> keyCommands;

		private static string ipAddress;

		private static int portNumber;

		private static string password;

		private static List<string> whiteListedIpAddresses = new List<string>();

		public static void GetLocalIPAddress()
		{
			var config = File.ReadAllText("config.json");
			var sp = JsonConvert.DeserializeObject<dynamic>(config);
			ipAddress = sp.ipAddress;
			portNumber = sp.portNumber;
			try
			{
				password = sp.password;
			}
			catch
			{

			}
			var jObject = (JObject)sp;
			if(sp.ContainsKey("whiteListedIpAddresses") )
			{
				for(var i = 0; i < sp.whiteListedIpAddresses.Count; i++)
				{
					var whiteIp = sp.whiteListedIpAddresses[i].ToString();
					whiteListedIpAddresses.Add(whiteIp);
				}
			}
		}

		private static void Main(string[] args)
		{
			InitKeyCommands();
			GetLocalIPAddress();
			if(password == null)
			{
				Console.WriteLine("Enter OBS Websocket password");
				password = Console.ReadLine();
				var fileContent = JsonConvert.SerializeObject(new JObject
				{
					{ "ipAddress", ipAddress },
					{ "portNumber", portNumber },
					{ "password", password }
				});
				File.WriteAllText("config.json", fileContent);
			}
			
			Console.WriteLine($"Ipadres: {ipAddress}:{portNumber}");
			var server = new TcpListener(IPAddress.Parse(ipAddress), portNumber);
			server.Start();
			var connection = new OBSWebsocket();
			connection.Connect($"ws://{"127.0.0.1"}:{4444}", password);

			var bytes = new byte[256];

			// Enter the listening loop.
			while (true)
			{
				Console.Write("Waiting for a connection... ");

				// Perform a blocking call to accept requests.
				// You could also use server.AcceptSocket() here.
				var client = server.AcceptTcpClient();
				Console.WriteLine("Connected!");

				if( whiteListedIpAddresses.Count > 0)
				{
					// check if the ip is whitelisted
					var remoteIp = client.Client.RemoteEndPoint.ToString();
					remoteIp = remoteIp.Substring(0, remoteIp.IndexOf(":"));

					var localIp = client.Client.LocalEndPoint.ToString();
					localIp = localIp.Substring(0, localIp.IndexOf(":"));
				}

				// Get a stream object for reading and writing.
				var stream = client.GetStream();

				int i;

				// Loop to receive all the data sent by the client.
				while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
				{
					// Translate data bytes to a ASCII string.
					var data = Encoding.ASCII.GetString(bytes, 0, i);
					Console.WriteLine("Received: {0} from {1}", data, client.Client.RemoteEndPoint.ToString());

					if (CommandSupported(data))
					{
						var keyCommand = keyCommands.First(kc => kc.CommandId == data);
						var control = false;
						var shift = false;
						var alt = false;
						OBSHotkey key = OBSHotkey.OBS_KEY_NONE;

						foreach(var keyPress in keyCommand.KeyPresses.Where(kp => kp.Pause == 0 && kp.KeyDown))
						{
							if( (VirtualKeyCode)keyPress.Key == VirtualKeyCode.LCONTROL)
							{
								control = true;
								continue;
							}

							if ((VirtualKeyCode)keyPress.Key == VirtualKeyCode.MENU)
							{
								alt = true;
								continue;
							}

							if ((VirtualKeyCode)keyPress.Key == VirtualKeyCode.LSHIFT)
							{
								shift = true;
								continue;
							}

							key = keyMapper[(VirtualKeyCode)keyPress.Key];
						}

						if (key != OBSHotkey.OBS_KEY_NONE)
						{
							KeyModifier modifiers = KeyModifier.None;
							if (control) modifiers |= KeyModifier.Control;
							if (alt) modifiers |= KeyModifier.Alt;
							if (shift) modifiers |= KeyModifier.Shift;
							var response = connection.TriggerHotkeyBySequence(key, modifiers);
						}

						/*var sim = new InputSimulator();

						foreach (var keyPress in keyCommand.KeyPresses)
						{
							if (keyPress.Pause > 0)
							{
								sim.Keyboard.Sleep(keyPress.Pause);
							}
							else if (keyPress.KeyDown)
							{
								sim.Keyboard.KeyDown((VirtualKeyCode)keyPress.Key);
							}
							else
							{
								sim.Keyboard.KeyUp((VirtualKeyCode)keyPress.Key);
							}
						}*/
					}

					// Process the data sent by the client.
					data = data.ToUpper();

					var msg = Encoding.ASCII.GetBytes(data);

					// Send back a response.
					stream.Write(msg, 0, msg.Length);
					Console.WriteLine("Sent: {0}", data);
				}

				// Shutdown and end connection
				client.Close();
			}
        }

		private static void InitKeyCommands()
		{
			var config = File.ReadAllText("KeyCommandsConfig.json");
			keyCommands = JsonConvert.DeserializeObject<KeyCommand[]>(config).ToList();
		}

		private static bool CommandSupported(string commandId)
		{
			return keyCommands.FirstOrDefault(kc => kc.CommandId == commandId) != null;
		}

		private static Dictionary<VirtualKeyCode, OBSHotkey> keyMapper => new Dictionary<VirtualKeyCode, OBSHotkey>
		{
			{ VirtualKeyCode.VK_A, OBSHotkey.OBS_KEY_A },
			{ VirtualKeyCode.VK_B, OBSHotkey.OBS_KEY_B },
			{ VirtualKeyCode.VK_C, OBSHotkey.OBS_KEY_C },
			{ VirtualKeyCode.VK_D, OBSHotkey.OBS_KEY_D },
			{ VirtualKeyCode.VK_E, OBSHotkey.OBS_KEY_E },
			{ VirtualKeyCode.VK_F, OBSHotkey.OBS_KEY_F },
			{ VirtualKeyCode.VK_G, OBSHotkey.OBS_KEY_G },
			{ VirtualKeyCode.VK_H, OBSHotkey.OBS_KEY_H },
			{ VirtualKeyCode.VK_I, OBSHotkey.OBS_KEY_I },
			{ VirtualKeyCode.VK_J, OBSHotkey.OBS_KEY_J },
			{ VirtualKeyCode.VK_K, OBSHotkey.OBS_KEY_K },
			{ VirtualKeyCode.VK_L, OBSHotkey.OBS_KEY_L },
			{ VirtualKeyCode.VK_M, OBSHotkey.OBS_KEY_M },
			{ VirtualKeyCode.VK_N, OBSHotkey.OBS_KEY_N },
			{ VirtualKeyCode.VK_O, OBSHotkey.OBS_KEY_O },
			{ VirtualKeyCode.VK_P, OBSHotkey.OBS_KEY_P },
			{ VirtualKeyCode.VK_Q, OBSHotkey.OBS_KEY_Q },
			{ VirtualKeyCode.VK_R, OBSHotkey.OBS_KEY_R },
			{ VirtualKeyCode.VK_S, OBSHotkey.OBS_KEY_S },
			{ VirtualKeyCode.VK_T, OBSHotkey.OBS_KEY_T },
			{ VirtualKeyCode.VK_U, OBSHotkey.OBS_KEY_U },
			{ VirtualKeyCode.VK_V, OBSHotkey.OBS_KEY_V },
			{ VirtualKeyCode.VK_W, OBSHotkey.OBS_KEY_W },
			{ VirtualKeyCode.VK_X, OBSHotkey.OBS_KEY_X },
			{ VirtualKeyCode.VK_Y, OBSHotkey.OBS_KEY_Y },
			{ VirtualKeyCode.VK_Z, OBSHotkey.OBS_KEY_Z },
			{ VirtualKeyCode.NUMPAD0, OBSHotkey.OBS_KEY_NUM0 },
			{ VirtualKeyCode.NUMPAD1, OBSHotkey.OBS_KEY_NUM1 },
			{ VirtualKeyCode.NUMPAD2, OBSHotkey.OBS_KEY_NUM2 },
			{ VirtualKeyCode.NUMPAD3, OBSHotkey.OBS_KEY_NUM3 },
			{ VirtualKeyCode.NUMPAD4, OBSHotkey.OBS_KEY_NUM4 },
			{ VirtualKeyCode.NUMPAD5, OBSHotkey.OBS_KEY_NUM5 },
			{ VirtualKeyCode.NUMPAD6, OBSHotkey.OBS_KEY_NUM6 },
			{ VirtualKeyCode.NUMPAD7, OBSHotkey.OBS_KEY_NUM7 },
			{ VirtualKeyCode.NUMPAD8, OBSHotkey.OBS_KEY_NUM8 },
			{ VirtualKeyCode.NUMPAD9, OBSHotkey.OBS_KEY_NUM9 },
			{ VirtualKeyCode.VK_0, OBSHotkey.OBS_KEY_0 },
			{ VirtualKeyCode.VK_1, OBSHotkey.OBS_KEY_1 },
			{ VirtualKeyCode.VK_2, OBSHotkey.OBS_KEY_2 },
			{ VirtualKeyCode.VK_3, OBSHotkey.OBS_KEY_3 },
			{ VirtualKeyCode.VK_4, OBSHotkey.OBS_KEY_4 },
			{ VirtualKeyCode.VK_5, OBSHotkey.OBS_KEY_5 },
			{ VirtualKeyCode.VK_6, OBSHotkey.OBS_KEY_6 },
			{ VirtualKeyCode.VK_7, OBSHotkey.OBS_KEY_7 },
			{ VirtualKeyCode.VK_8, OBSHotkey.OBS_KEY_8 },
			{ VirtualKeyCode.VK_9, OBSHotkey.OBS_KEY_9 },
			{ VirtualKeyCode.DIVIDE, OBSHotkey.OBS_KEY_NUMSLASH },
			{ VirtualKeyCode.MULTIPLY, OBSHotkey.OBS_KEY_NUMASTERISK },
			{ VirtualKeyCode.SUBTRACT, OBSHotkey.OBS_KEY_NUMMINUS },
			{ VirtualKeyCode.ADD, OBSHotkey.OBS_KEY_NUMPLUS },
			{ VirtualKeyCode.HOME, OBSHotkey.OBS_KEY_HOME },
			{ VirtualKeyCode.END, OBSHotkey.OBS_KEY_END },
			{ VirtualKeyCode.PRIOR, OBSHotkey.OBS_KEY_PAGEUP },
			{ VirtualKeyCode.NEXT, OBSHotkey.OBS_KEY_PAGEDOWN },
			{ VirtualKeyCode.OEM_4, OBSHotkey.OBS_KEY_BRACKETLEFT },
			{ VirtualKeyCode.OEM_6, OBSHotkey.OBS_KEY_BRACKETRIGHT },
			{ VirtualKeyCode.OEM_7, OBSHotkey.OBS_KEY_QUOTE  },
			{ VirtualKeyCode.OEM_1, OBSHotkey.OBS_KEY_SEMICOLON },
		};
	}
}
