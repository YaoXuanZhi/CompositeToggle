using System;
using UnityEngine;

namespace Mobcast.Coffee.Toggles
{
	public enum ToggleTweenEase { Linear, QuadIn, QuadOut, QuadInOut, Custom }

	/// <summary>Serialized configuration only. Running tweens never modify state values.</summary>
	[Serializable]
	public sealed class PropertyTweenSettings
	{
		public bool enabled;
		[Min(0)] public float duration = 0.3f;
		[Min(0)] public float delay;
		public ToggleTweenEase ease = ToggleTweenEase.QuadOut;
		public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
		public bool useUnscaledTime = true;

		internal float Evaluate(float t)
		{
			switch (ease)
			{
				case ToggleTweenEase.QuadIn: return t * t;
				case ToggleTweenEase.QuadOut: return t * (2 - t);
				case ToggleTweenEase.QuadInOut:
					return t < 0.5f ? 2 * t * t : 1 - 2 * (1 - t) * (1 - t);
				case ToggleTweenEase.Custom: return curve == null ? t : curve.Evaluate(t);
				default: return t;
			}
		}
	}
}
