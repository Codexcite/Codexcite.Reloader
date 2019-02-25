using System;
using System.Reactive.Disposables;

namespace Codexcite.Reloader.Monitor
{
	public static class DisposableExtensions
	{
		public static T DisposeWith<T>(this T @this, CompositeDisposable compositeDisposable)
			where T : IDisposable
		{
			compositeDisposable.Add(@this);
			return @this;
		}
	}
}
