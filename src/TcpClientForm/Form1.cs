using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

using Newtonsoft.Json;

namespace TcpClientForm
{
	public partial class TcpClientFormWindow : Form
	{
		private string ipAddress
		{
			get;
			set;
		}

		private string portNumber
		{
			get;
			set;
		}

		private Form serverConfigForm
		{
			get;
			set;
		}

		public TcpClientFormWindow()
		{
			this.ReadConfig();
			this.InitServer();
			this.InitializeComponent();
			this.InitServerConfigForm();
		}

		private void ReadConfig()
		{
			var uiConfig = File.ReadAllText("UIConfig.json");
			var buttonsConfig = JsonConvert.DeserializeObject<dynamic>(uiConfig);

			foreach (var buttonConfig in buttonsConfig)
			{
				int width = buttonConfig.width;
				int height = buttonConfig.height;
				int xpos = buttonConfig.xpos;
				int ypos = buttonConfig.ypos;

				var button = new System.Windows.Forms.Button
				{
					BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch,
					Font = new System.Drawing.Font(
						"Microsoft Sans Serif",
						(Single)16,//buttonConfig.fontSize,
						System.Drawing.FontStyle.Regular,
						System.Drawing.GraphicsUnit.Point,
						((byte)(0))),
					ForeColor = System.Drawing.SystemColors.ControlText,
					Location = new System.Drawing.Point(xpos, ypos),
					Margin = new System.Windows.Forms.Padding(2),
					Name = buttonConfig.name,
					Size = new System.Drawing.Size(width, height),
					Text = buttonConfig.text,
					TabIndex = 30,
					UseVisualStyleBackColor = true,
					BackgroundImage = ToGrayscale(buttonConfig.imagePath != null ? new Bitmap(buttonConfig.imagePath.ToString()) : null, 90)
				};

				button.Click += this.button_Click;
				button.Visible = true;
				Controls.Add(button);
			}
		}

		private static Image ToGrayscale(Image s, int alpha)
		{
			if (s == null) return null;

			Bitmap tImage = new Bitmap(s);

			for (int x = 0; x < tImage.Width; x++)
			{
				for (int y = 0; y < tImage.Height; y++)
				{
					Color tCol = tImage.GetPixel(x, y);
					Color newColor = Color.FromArgb(alpha, tCol.R, tCol.G, tCol.B);
					tImage.SetPixel(x, y, newColor);
				}
			}
			return tImage;

		}

		private void InitServer()
		{
			var serverConfigText = File.ReadAllText("config.json");
			var serverConfig = JsonConvert.DeserializeObject<dynamic>(serverConfigText);

			this.ipAddress = serverConfig.ipAddress;
			this.portNumber = serverConfig.portNumber;
		}

		private void button_Click(object sender, EventArgs e)
		{
			var button = sender as Button;
			this.SendMessage(button.Text);
		}

		private void SendMessage(string message)
		{
			TcpClient client = null;
			try
			{
				client = new TcpClient(this.ipAddress, int.Parse(this.portNumber));
			}
			catch (Exception e)
			{
				MessageBox.Show($"Error while connection to TcpClient.\r\nError: {e.Message}");
				return;
			}

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

		private void toolStripMenuItem2_Click(object sender, EventArgs e)
		{
			this.serverConfigForm.Show();
		}

		private void InitServerConfigForm()
		{
			var btn = new Button
			{
				Text = "Save",
				Name = "saveButton",
				Location = new Point(5, 75),
				Size = new Size(40, 30)
			};

			this.serverConfigForm = new Form
			{
				TopMost = true,
				Size = new Size(250, 160)
			};

			var ipLabel = new Label
			{
				Text = "IP address",
				Name = "ip_lbl",
				Location = new Point(5, 5),
				Size = new Size(90, 30),
				Font = new Font(FontFamily.GenericSansSerif, 12)
			};
			this.serverConfigForm.Controls.Add(ipLabel);

			var ipTextBox = new TextBox
			{
				Text = this.ipAddress,
				Name = "ip",
				Location = new Point(95, 5),
				Size = new Size(130, 30),
				Font = new Font(FontFamily.GenericSansSerif, 12)
			};
			this.serverConfigForm.Controls.Add(ipTextBox);

			var portLabel = new Label
			{
				Text = "Port number",
				Name = "port_lbl",
				Location = new Point(5, 40),
				Size = new Size(90, 30),
				Font = new Font(FontFamily.GenericSansSerif, 12)
			};
			this.serverConfigForm.Controls.Add(portLabel);

			var portTextBox = new TextBox
			{
				Text = this.portNumber,
				Name = "port",
				Location = new Point(95, 40),
				Size = new Size(130, 30),
				Font = new Font(FontFamily.GenericSansSerif, 12)
			};
			this.serverConfigForm.Controls.Add(portTextBox);

			this.serverConfigForm.Controls.Add(btn);

			btn.Click += (o, args) =>
			{
				this.ipAddress = ipTextBox.Text;
				this.portNumber = portTextBox.Text;
				this.serverConfigForm.Hide();
				var serverConfigText = JsonConvert.SerializeObject(
					new
					{
						ipAddress = this.ipAddress,
						portNumber = this.portNumber
					});
				File.WriteAllText("config.json", serverConfigText);
			};

			this.serverConfigForm.Closing += (sender, args) =>
			{
				args.Cancel = true;
				this.serverConfigForm.Hide();
			};

			this.ipAddress = ipTextBox.Text;
			this.portNumber = portTextBox.Text;
		}
	}
}
