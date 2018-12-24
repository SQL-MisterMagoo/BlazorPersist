using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace BlazorLifeCycle.Services
{
	public class LifeCycle
	{
		public static readonly Dictionary<string, object> appState = new Dictionary<string, object>();
		public static readonly Dictionary<string, int> appStateTimeout = new Dictionary<string, int>();

		//private static async Task ReloadAppState()
		//{
		//	string[] cookie = await JSRuntime.Current.InvokeAsync<string[]>("blazorLifeCycle.listdata");
		//	await ProcessCookies(cookie);
		//}

		//private static async Task ProcessCookies(string[] cookies)
		//{
		//	Console.WriteLine($"TEST: cookies={cookies}");
		//	for (int i = 0; i < cookies.Length; i++)
		//	{
		//		string name = cookies[i];
		//		string value = await JSRuntime.Current.InvokeAsync<string>("blazorLifeCycle.readdata", name);
		//		Console.WriteLine($"TEST: name={name}, value={value}");
		//		if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(value))
		//		{
		//			ExtractAndReHydrate(name, value);
		//		}
		//	}
		//}

		//private static void ExtractAndReHydrate(string name, string value)
		//{
		//	var names = name.Split('|');
		//	var objName = names?[0];
		//	var objType = names?[1];
		//	Console.WriteLine($"TEST: objName={objName}, objType={objType}, value={value}");
		//	if (!string.IsNullOrWhiteSpace(objType))
		//	{
		//		ReHydrateState(value, objName, objType);
		//	}
		//}

		private static void ReHydrateState(string value, string objName, string objType)
		{
			Type type = Type.GetType(objType);
			Console.WriteLine($"REHYDRATE: type={type?.FullName}");
			var mi = typeof(Json).GetMethod("Deserialize");
			var desRef = mi.MakeGenericMethod(type);
			var result = desRef.Invoke(null, new[] { value });
			Console.WriteLine($"REHYDRATE: result={result?.GetType().FullName}");
			appState[objName] = result;
		}

		[JSInvokable("onbeforeunload")]
		public static string OnBeforeUnload()
		{
			Console.WriteLine("In LifeCycle - OnBeforeUnload");

			if (appState == null)
				return null;

			foreach (var kvp in appState)
			{
				var timeout = appStateTimeout.ContainsKey(kvp.Key) ? appStateTimeout[kvp.Key] : 0;
				//string timeoutString = timeout > 0 ? DateTime.UtcNow.AddDays(timeout).AddMinutes(-1).ToString("r") : null;

				JSRuntime
					.Current
					.InvokeAsync<bool>("blazorLifeCycle.savedata", $"{kvp.Key}|{kvp.Value.GetType().FullName}", Json.Serialize(kvp.Value), timeout);
			}
			return null; //return a string to get a warning
		}

		public static void AddToAppState(string name, object value, int timeout=0)
		{
			appState[name] = value;
			appStateTimeout[name] = timeout;
		}

		public static async Task<T> GetAppState<T>(string name, int timeout=0) where T : class
		{
			Console.WriteLine($"LIFE: Looking for {name} AppState");
			if (!appState.ContainsKey(name))
			{
				Console.WriteLine($"LIFE: Appstate for {name} not found");
				string stName = $"{name}|{typeof(T).FullName}";
				Console.WriteLine($"LIFE: Reading localstorage {stName}");
				string value = await JSRuntime.Current.InvokeAsync<string>("blazorLifeCycle.readdata", stName);
				Console.WriteLine($"LIFE: GOT localstorage {value}");
				ReHydrateState(value, name, typeof(T).FullName);
			}
			appStateTimeout[name] = timeout;
			return appState[name] as T;
		}

	}
}
