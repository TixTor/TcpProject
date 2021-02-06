using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MessageCommands
{
	public class KeyCommand
	{
		public string CommandId
		{
			get;
			set;
		}

		public KeyPress[] KeyPresses
		{
			get;
			set;
		}
	}

	public class KeyPress
	{
		public bool KeyDown
		{
			get;
			set;
		}

		public int Key
		{
			get;
			set;
		}

		public int Pause
		{
			get;
			set;
		}
	}
}
