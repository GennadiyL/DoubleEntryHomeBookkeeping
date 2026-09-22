using System;
using System.Collections.Generic;

namespace Shared.Impl.Messaging
{
	public delegate object ServiceFactory(Type serviceType);

	/// <summary>
	/// Defines service-factory resolution extensions.
	/// Resolves all registered instances of a requested service type through a delegate.
	/// The in-process publisher supplies the scoped service-provider delegate.
	/// Handler wrappers use the extension without retaining the service provider.
	/// The helper contains no message routing or lifetime ownership.
	/// </summary>
	public static class ServiceFactoryExtensions
	{
		public static IEnumerable<T> GetInstances<T>(this ServiceFactory serviceFactory) =>
			(IEnumerable<T>)serviceFactory(typeof(IEnumerable<T>));
	}
}
