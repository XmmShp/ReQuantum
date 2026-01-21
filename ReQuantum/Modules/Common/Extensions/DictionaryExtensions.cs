using System;
using System.Collections.Generic;
using System.Text;

namespace ReQuantum.Modules.Common.Extensions;

public static class __Common_Extensions__
{
	extension(IDictionary<string, object?> dictionary)
	{
		public T Get<T>(string key, T defaultValue = default!)
		{
			if (dictionary.TryGetValue(key, out var value) && value is T typedValue)
			{
				return typedValue;
			}

			return defaultValue;
		}
	}
}
