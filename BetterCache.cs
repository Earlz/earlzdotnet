using System;
using System.Web.Caching;
using System.Web;

namespace Earlz.LastYearsWishes
{
	/// <summary>
	/// ASP.Net's caching sucks balls. This makes it better
	/// </summary>
	public static class BetterCache
	{
		public static T Get<T>(string key)
		{
			try
			{
				object tmp=HttpContext.Current.Cache[typeof(T).ToString()+key];
				if(tmp is T)
				{
					return (T) tmp;
				}
				else
				{
					return default(T);
				}
			}
			catch
			{
				return default(T);
			}
		}
		public static void Add<T>(string key, T obj)
		{
			Add (key, obj, CacheItemPriority.Default);
		}
		public static void Add<T>(string key, T obj, CacheItemPriority priority)
		{
			Add<T>(key, obj, null, null, priority);
		}
		public static void Add<T>(string key, T obj, TimeSpan slidingExpiration)
		{
			Add<T>(key, obj, null, slidingExpiration, CacheItemPriority.Default);
		}
		public static void Add<T>(string key, T obj, DateTime absoluteExpiration)
		{
			Add<T>(key, obj, absoluteExpiration, null, CacheItemPriority.Default);
		}
		public static void Add<T>(string key, T obj, DateTime? absolute, TimeSpan? sliding, CacheItemPriority priority)
		{
			HttpContext.Current.Cache.Add(typeof(T).ToString()+key, 
			                              obj,
			                              null,
			                              absolute ?? Cache.NoAbsoluteExpiration,
			                              sliding ?? Cache.NoSlidingExpiration,
			                              priority,
			                              null
          	);
		}
		public static T Remove<T>(string key)
		{
			var result=HttpContext.Current.Cache.Remove(typeof(T).ToString()+key);
			if(result is T)
			{
				return (T)result;
			}
			else
			{
				return default(T);
			}
		}
	}
}

