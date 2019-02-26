using System;
using System.Collections.Generic;
using System.Text;

namespace Codexcite.Reloader.Monitor
{
	public class EventArgs<T> : EventArgs
	{
		public T Data { get; set; }

		public EventArgs(T data)
		{
			Data = data;
		}
	}
}
