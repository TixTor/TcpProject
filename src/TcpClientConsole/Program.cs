using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TcpClientConsole
{
	internal class Program
	{
		private static string serverAddress;

		private static int port;

		private static readonly HttpClient _HttpClient = new HttpClient();

		private static async Task Main(string[] args)
		{
			System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
			var str = string.Empty;

			var request = (HttpWebRequest)WebRequest.Create("https://studio.youtube.com/live_chat?is_popout=1&v=RRj_ZLuCHDQ");
			request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.66 Safari/537.36";
			var response = await request.GetResponseAsync();
			var stream = response.GetResponseStream();
			var reader = new StreamReader(stream);
			str = reader.ReadToEnd();
			Console.WriteLine(str);
			//var response = await GetResponse("https://studio.youtube.com/live_chat?is_popout=1&v=RRj_ZLuCHDQ");
			//str = response;

			/*Console.Write("ip: ");
			serverAddress = Console.ReadLine();
			Console.Write("Port: ");
			if (!int.TryParse(Console.ReadLine(), out port))
			{
				Console.WriteLine($"Invalid port");
				Console.ReadKey();
				return;
			}

			try
			{
				//new TcpClient(serverAddress, port);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				Console.ReadKey();
				return;
			}

			Console.WriteLine($"Connected to {serverAddress}");
			Console.WriteLine($"Waiting for input");

			while (true)
			{
				var message = Console.ReadLine();
				if (string.IsNullOrEmpty(message))
				{
					break;
				}

				SendMessage(message);
			}*/

			Console.ReadKey();
		}

		private static async Task<string> GetResponse(string url)
		{
			using (var request = new HttpRequestMessage(HttpMethod.Get, new Uri(url)))
			{
				request.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml");
				request.Headers.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate");
				request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 6.2; WOW64; rv:19.0) Gecko/20100101 Firefox/19.0");
				request.Headers.TryAddWithoutValidation("Accept-Charset", "ISO-8859-1");
				//request.

				using (var response = await _HttpClient.SendAsync(request).ConfigureAwait(false))
				{
					response.EnsureSuccessStatusCode();
					using (var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
						using (var decompressedStream = new GZipStream(responseStream, CompressionMode.Decompress))
							using (var streamReader = new StreamReader(decompressedStream))
							{
								return await streamReader.ReadToEndAsync().ConfigureAwait(false);
							}
				}
			}
		}

		private static void SendMessage(string message)
		{
			var client = new TcpClient(serverAddress, port);
			// Translate the passed message into ASCII and store it as a Byte array.
			var data = Encoding.ASCII.GetBytes(message);

			var stream = client.GetStream();

			// Send the message to the connected TcpServer.
			stream.Write(data, 0, data.Length);

			Console.WriteLine("Sent: {0}", message);

			// Buffer to store the response bytes.
			data = new byte[256];

			// String to store the response ASCII representation.
			var responseData = string.Empty;

			// Read the first batch of the TcpServer response bytes.
			var bytes = stream.Read(data, 0, data.Length);
			responseData = Encoding.ASCII.GetString(data, 0, bytes);
			Console.WriteLine("Received: {0}", responseData);

			// Close everything.
			stream.Close();
			client.Close();
		}
	}
}
