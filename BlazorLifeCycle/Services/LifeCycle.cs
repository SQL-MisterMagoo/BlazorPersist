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

		private static async Task ReloadAppState()
		{
			string cookie = await JSRuntime.Current.InvokeAsync<string>("blazorLifeCycle.readcookie");
			ProcessCookies(cookie);
		}

		private static void ProcessCookies(string cookie)
		{
			Console.WriteLine($"TEST: cookie={cookie}");
			string[] cookies = cookie.Split(';');
			for (int i = 0; i < cookies.Length; i++)
			{
				string[] kvp = cookies[i].Split('=');
				var name = kvp?[0];
				var value = kvp?[1];
				Console.WriteLine($"TEST: name={name}, value={value}");
				if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(value))
				{
					ExtractAndReHydrate(name, value);
				}
			}
		}

		private static void ExtractAndReHydrate(string name, string value)
		{
			var names = name.Split('|');
			var objName = names?[0];
			var objType = names?[1];
			Console.WriteLine($"TEST: objName={objName}, objType={objType}, value={value}");
			if (!string.IsNullOrWhiteSpace(objType))
			{
				ReHydrateState(value, objName, objType);
			}
		}

		private static void ReHydrateState(string value, string objName, string objType)
		{
			Type type = Type.GetType(objType);
			Console.WriteLine($"TEST: type={type?.FullName}");
			var mi = typeof(Json).GetMethod("Deserialize");
			var desRef = mi.MakeGenericMethod(type);
			var result = desRef.Invoke(null, new[] { value });
			Console.WriteLine($"TEST: result={result?.GetType().FullName}");
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
				string timeoutString = timeout > 0 ? $"expires={DateTime.UtcNow.AddDays(timeout).AddMinutes(-1).ToString("r")};" : "";

				JSRuntime
					.Current
					.InvokeAsync<bool>("blazorLifeCycle.savecookie", $"{kvp.Key}|{kvp.Value.GetType().FullName}", $"{Json.Serialize(kvp.Value)};{timeoutString}path=/");
			}
			return null; //return a string to get a warning
		}

		public static void AddToAppState(string name, object value, int timeout=0)
		{
			appState[name] = value;
			appStateTimeout[name] = timeout;
		}

		public static async Task<T> GetAppState<T>(string name) where T : class
		{
			if (!appState.ContainsKey(name))
				await ReloadAppState();
			return appState[name] as T;
		}

	}
}
