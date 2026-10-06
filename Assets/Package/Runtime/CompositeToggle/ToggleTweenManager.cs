using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mobcast.Coffee.Toggles
{
	/// <summary>One writer per target/setter; configuration and runtime state remain separate.</summary>
	internal static class ToggleTweenManager
	{
		sealed class Tween
		{
			internal CompositeToggle owner;
			internal Object target;
			internal TweenPropertyAccessor accessor;
			internal Vector4 start, end;
			internal float elapsed;
			internal PropertyTweenSettings settings;
			internal bool cancelled;
			internal readonly object[] arguments = new object[1];
		}

		sealed class PendingHide
		{
			internal CompositeToggle owner;
			internal GameObject target;
		}

		static readonly List<Tween> active = new List<Tween>();
		static readonly List<Tween> updateBuffer = new List<Tween>();
		static readonly List<PendingHide> pendingHides = new List<PendingHide>();
		static readonly List<PendingHide> hideBuffer = new List<PendingHide>();
		static CompositeTweenRunner runner;
		static bool updating;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		static void Reset()
		{
			foreach (var tween in active) tween.cancelled = true;
			active.Clear();
			updateBuffer.Clear();
			pendingHides.Clear();
			hideBuffer.Clear();
			updating = false;
			if (runner) Object.Destroy(runner.gameObject);
			runner = null;
		}

		static void EnsureRunner()
		{
			if (runner || !Application.isPlaying) return;
			var go = new GameObject("Composite Toggle Tweens");
			go.hideFlags = HideFlags.HideAndDontSave;
			Object.DontDestroyOnLoad(go);
			runner = go.AddComponent<CompositeTweenRunner>();
		}

		internal static void RunnerDestroyed(CompositeTweenRunner instance)
		{
			if (runner != instance) return;
			runner = null;
			foreach (var tween in active) tween.cancelled = true;
			active.Clear();
			pendingHides.Clear();
		}

		internal static void Start(CompositeToggle owner, Object target, TweenPropertyAccessor accessor,
			object endValue, PropertyTweenSettings settings)
		{
			var end = accessor.Pack(endValue);
			// A repeated application of the same destination must not restart its delay/duration.
			foreach (var tween in active)
				if (tween.target == target && TweenPropertyAccessor.SameSetter(tween.accessor.setter, accessor.setter)
					&& tween.owner == owner && tween.end.Equals(end)) return;

			Cancel(target, accessor.setter);
			var start = accessor.Read(target);
			if (start.Equals(end)) return;
			active.Add(new Tween
			{
				owner = owner, target = target, accessor = accessor, start = start, end = end,
				settings = new PropertyTweenSettings
				{
					duration = settings.duration,
					delay = float.IsNaN(settings.delay) || float.IsInfinity(settings.delay) ? 0 : Mathf.Max(0, settings.delay),
					ease = settings.ease, curve = settings.curve, useUnscaledTime = settings.useUnscaledTime
				}
			});
			EnsureRunner();
		}

		internal static void Cancel(Object target, MethodInfo setter)
		{
			for (int i = active.Count - 1; i >= 0; i--)
				if (active[i].target == target && TweenPropertyAccessor.SameSetter(active[i].accessor.setter, setter))
					Remove(active[i]);
		}

		static void Remove(Tween tween)
		{
			tween.cancelled = true;
			active.Remove(tween);
		}

		internal static void CancelOwner(CompositeToggle owner)
		{
			for (int i = active.Count - 1; i >= 0; i--)
				if (active[i].owner == owner) Remove(active[i]);
			// Keep pending visibility decisions: LateUpdate settles them once no tween owns a lock.
			// A later explicit SetActive request can still supersede them before then.
		}

		internal static void Advance(float deltaTime, float unscaledDeltaTime)
		{
			if (updating) return;
			updating = true;
			updateBuffer.Clear();
			updateBuffer.AddRange(active);
			try
			{
				foreach (var tween in updateBuffer)
				{
					if (tween.cancelled) continue;
					if (!tween.owner || !tween.target || !tween.owner.isActiveAndEnabled)
					{
						Remove(tween);
						continue;
					}
					tween.elapsed += Mathf.Max(0, tween.settings.useUnscaledTime ? unscaledDeltaTime : deltaTime);
					if (tween.elapsed < tween.settings.delay) continue;
					float t = Mathf.Clamp01((tween.elapsed - tween.settings.delay) / tween.settings.duration);
					try
					{
						// Force the exact destination even for custom curves whose endpoint isn't 1.
						Vector4 value = t >= 1 ? tween.end : Vector4.LerpUnclamped(tween.start, tween.end, tween.settings.Evaluate(t));
						tween.accessor.Write(tween.target, value, tween.arguments);
						if (t >= 1 && !tween.cancelled) Remove(tween);
					}
					catch (Exception exception)
					{
						Remove(tween);
						Debug.LogException(exception, tween.owner);
					}
				}
			}
			finally { updateBuffer.Clear(); updating = false; }
		}

		internal static void SetActive(CompositeToggle owner, GameObject target, bool value, bool defer)
		{
			if (!target) return;
			// Latest intent wins. No old completion callback can hide a newly reopened object.
			for (int i = pendingHides.Count - 1; i >= 0; i--)
				if (pendingHides[i].target == target) pendingHides.RemoveAt(i);
			if (value || !defer || !target.activeInHierarchy)
			{
				target.SetActive(value);
				return;
			}
			// Resolve in LateUpdate, after child/synced controllers have had a chance to start their tweens.
			pendingHides.Add(new PendingHide { owner = owner, target = target });
			EnsureRunner();
		}

		static bool HasTweenInHierarchy(GameObject target)
		{
			foreach (var tween in active)
			{
				var component = tween.target as Component;
				if (!tween.cancelled && tween.owner && tween.owner.isActiveAndEnabled && component
					&& (component.gameObject == target || component.transform.IsChildOf(target.transform))) return true;
			}
			return false;
		}

		internal static void FlushVisibility()
		{
			hideBuffer.Clear();
			hideBuffer.AddRange(pendingHides);
			foreach (var hide in hideBuffer)
			{
				if (!pendingHides.Contains(hide)) continue;
				if (!hide.target || !hide.owner)
				{
					pendingHides.Remove(hide);
					continue;
				}
				if (HasTweenInHierarchy(hide.target)) continue;
				pendingHides.Remove(hide);
				hide.target.SetActive(false);
			}
			hideBuffer.Clear();
		}
	}
}
