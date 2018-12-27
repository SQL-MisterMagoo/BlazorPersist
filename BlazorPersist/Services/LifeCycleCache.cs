using Microsoft.JSInterop;
using System;

namespace BlazorPersist.Services
{
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
