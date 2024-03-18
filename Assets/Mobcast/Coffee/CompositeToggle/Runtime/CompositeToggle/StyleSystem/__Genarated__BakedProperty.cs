using System.Collections.Generic;

namespace Mobcast.Coffee.Toggles
{
	public static class BakedProperty
	{
		public delegate void PropertySetter(UnityEngine.Object target,IParameterList args,int index);

		public static Dictionary<string, PropertySetter> baked = new Dictionary<string, PropertySetter>()
		{
			// BAKED PROPERTIES START
			{ "UnityEngine.UI.Image, UnityEngine.UI;UnityEngine.Color, UnityEngine.CoreModule;set_color", (target, args, index) => (target as UnityEngine.UI.Image).color = ((UnityEngine.Color)(args as Mobcast.Coffee.Toggles.ColorParameterList).GetRaw (index)) },
			{ "UnityEngine.UI.Image, UnityEngine.UI;UnityEngine.Sprite, UnityEngine.CoreModule;set_sprite", (target, args, index) => (target as UnityEngine.UI.Image).sprite = ((UnityEngine.Sprite)(args as Mobcast.Coffee.Toggles.ObjectParameterList).GetRaw (index)) },
			// BAKED PROPERTIES END
		};
	}
}