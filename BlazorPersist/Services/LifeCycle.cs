using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace BlazorPersist.Services
{
	public class LifeCycle
	{
		public static readonly Dictionary<string, ILifeCycleCache> appState = new Dictionary<string, ILifeCycleCache>();

		/// <summary>
		/// This method should be hooked up by javascript to the window.beforeunload event.
		/// It will then save all state to localstorage.
		/// See js/blazorPersist.js
		/// </summary>
		/// <returns></returns>
		[JSInvokable("onbeforeunload")]
		public static string OnBeforeUnload()
		{
			if (appState == null)
				return null;

			foreach (var kvp in appState)
			{
				// This call ensures that we have the latest data for non-nullable value types
				kvp.Value.UpdateData();
				JSRuntime
					.Current
					.InvokeAsync<bool>("blazorPersist.savedata", kvp.Key, Json.Serialize(kvp.Value));
			}
			return null; //return a string to get a warning
		}

		public static async Task<LifeCycleCache<T>> GetAppState<T>(string name, int timeout = 0, Func<T> action = null)
		{
			await EnsurePresentAppState<T>(name, timeout);
			AttachAction<T>(name, action);
			return appState[name] as LifeCycleCache<T>;
		}

		private static void AttachAction<T>(string name, Func<T> action)
		{
			var localCache = appState[name] as LifeCycleCache<T>;
			localCache.SaveAction = action;
		}

		private static void HandleExpiry<T>(string name, int timeout)
		{
			var localCache = appState[name] as LifeCycleCache<T>;
			if (localCache.LifeTimeMinutes > 0 && localCache.GetExpiryDate() < DateTime.UtcNow)
			{
				Console.WriteLine($"APP:Expired appstate...");
				localCache.ObjectData = default;
				localCache.cacheExpires = DateTime.MinValue;
			}
		}

		private static async Task EnsurePresentAppState<T>(string name, int timeout)
		{
			Console.WriteLine($"APP:Getting {typeof(T).Name} {name}");
			if (!appState.ContainsKey(name))
			{
				Console.WriteLine($"APP:Need appstate, reading storage...");
				string stValue = null;
				try
				{
					stValue = await JSRuntime.Current.InvokeAsync<string>("blazorPersist.readdata", name);

				}
				catch (Exception ex)
				{
					Console.WriteLine("READDATA:" + ex.GetBaseException().Message);
				}
				Console.WriteLine($"APP:Read from storage: {stValue ?? "{null}"}");
				if (stValue != null)
				{
					appState[name] = Json.Deserialize<LifeCycleCache<T>>(stValue);
					Console.WriteLine($"APP:Got storage appstate...");
					HandleExpiry<T>(name, timeout);
				}
				else
				{
					Console.WriteLine($"APP:Need NEW appstate...");
					appState[name] = new LifeCycleCache<T>() { LifeTimeMinutes = timeout, ObjectData = default };
				}
			}
		}
	}
}
