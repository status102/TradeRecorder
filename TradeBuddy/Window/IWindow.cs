using System;

namespace TradeBuddy.Window
{
	public interface IWindow : IDisposable
	{
		public void Draw();
	}
}
