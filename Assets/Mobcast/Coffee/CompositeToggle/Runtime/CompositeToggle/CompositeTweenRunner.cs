using UnityEngine;

namespace Mobcast.Coffee.Toggles
{
	[AddComponentMenu("")]
	public sealed class CompositeTweenRunner : MonoBehaviour
	{
		void Update() { ToggleTweenManager.Advance(Time.deltaTime, Time.unscaledDeltaTime); }
		void LateUpdate() { ToggleTweenManager.FlushVisibility(); }
		void OnDestroy() { ToggleTweenManager.RunnerDestroyed(this); }
	}
}
