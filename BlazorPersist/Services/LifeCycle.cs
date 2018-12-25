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
					.InvokeAsync<bool>("blazorPersist.savedata", kvp.Key, Json.Serialize( kvp.Value));
			}
			return null; //return a string to get a warning
		}

		public static void AddToAppState<T>(string name, T value, int timeout=0, Func<T> saveAction=null)
		{
			appState[name] = new LifeCycleCache<T> { ObjectData = value, LifeTimeMinutes = timeout, SaveAction=saveAction };
		}

		/// <summary>
		/// Retrieve data of type T from the App State
		/// </summary>
		/// <typeparam name="T">The type of value to store - can be class or a non-nullable value type such as int or string</typeparam>
		/// <param name="name">The unique name for the data</param>
		/// <param name="action">Only required for non-nullable value types such as "int".
		/// This Func will be called when the browser window is closed or refreshed. 
		/// It should return the current data to be saved.</param>
		/// <returns></returns>
		public static async Task<T> GetAppState<T>(string name, Func<T> action = null) 
		{
			if (!appState.ContainsKey(name))
			{
				var value = await JSRuntime.Current.InvokeAsync<string>("blazorPersist.readdata", name);
				appState[name] = Json.Deserialize<LifeCycleCache<T>>(value);
			}
			var localCache = appState[name] as LifeCycleCache<T>;
			if (localCache.LifeTimeMinutes > 0 && localCache.GetExpiryDate() < DateTime.UtcNow)
			{
				localCache.ObjectData = default(T);
				localCache.cacheExpires = DateTime.MinValue;
				await JSRuntime.Current.InvokeAsync<bool>("blazorPersist.cleardata", name);
			}
			localCache.SaveAction = action;
			return localCache.ObjectData;
		}

	}
	public interface ILifeCycleCache
	{
		void UpdateData();
	}
	[Serializable]
	public class LifeCycleCache<T> : ILifeCycleCache
	{
		public DateTime cacheExpires;
		public T ObjectData { get; set; }
		public int LifeTimeMinutes { get; set; }
		public Func<T> SaveAction { private get; set; }
		public DateTime GetExpiryDate() 
			=> cacheExpires == DateTime.MinValue ? DateTime.UtcNow.AddMinutes(LifeTimeMinutes) : cacheExpires;
		public LifeCycleCache()
		{

		}
		public string AsJson()
		{
			return Json.Serialize(this);
		}

		public void UpdateData()
		{
			cacheExpires = GetExpiryDate();
			if (SaveAction == null)
			{
				return;
			}
			ObjectData = SaveAction();
		}
	}
}
