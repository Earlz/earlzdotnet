using System;
using Earlz.BarelyMVC;
using System.Collections.Generic;

namespace Earlz.LastYearsWishes
{

	public class StringsToListConverter : IParameterConverter
	{
		public object Convert (string key, ParameterDictionary dictionary)
		{
			return new List<string>(dictionary[key].Split(new char[]{' '}, StringSplitOptions.RemoveEmptyEntries));
		}
	}
}

