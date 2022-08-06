using System;

namespace TradeBuddy.Window
{
	public interface IWindow : IDisposable
	{
		public void Draw();
	}
	public interface IExternalWindow : IDisposable
	{
		public void Draw(ref bool show);
	}
}
