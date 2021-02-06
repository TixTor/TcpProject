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

namespace TcpListenerProject
{
	class Program
	{
		private static List<KeyCommand> keyCommands;

		private static string ipAddress;

		private static int portNumber;

		public static void GetLocalIPAddress()
		{
			var config = File.ReadAllText("config.json");
			var sp = JsonConvert.DeserializeObject<dynamic>(config);
			ipAddress = sp.ipAddress;
			portNumber = sp.portNumber;
		}

		private static void Main(string[] args)
		{
			InitKeyCommands();
			GetLocalIPAddress();
			Console.WriteLine($"Ipadres: {ipAddress}:{portNumber}");
			var server = new TcpListener(IPAddress.Parse(ipAddress), portNumber);
			server.Start();

			var bytes = new byte[256];

			// Enter the listening loop.
			while (true)
			{
				Console.Write("Waiting for a connection... ");

				// Perform a blocking call to accept requests.
				// You could also use server.AcceptSocket() here.
				var client = server.AcceptTcpClient();
				Console.WriteLine("Connected!");

				// Get a stream object for reading and writing
				var stream = client.GetStream();

				int i;

				// Loop to receive all the data sent by the client.
				while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
				{
					// Translate data bytes to a ASCII string.
					var data = Encoding.ASCII.GetString(bytes, 0, i);
					Console.WriteLine("Received: {0}", data);

					if (CommandSupported(data))
					{
						var keyCommand = keyCommands.First(kc => kc.CommandId == data);
						var sim = new InputSimulator();

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
						}
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
	}
}
