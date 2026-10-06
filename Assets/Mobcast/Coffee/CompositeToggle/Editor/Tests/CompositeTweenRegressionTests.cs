using System;
using System.Collections.Generic;
using System.Reflection;
using Mobcast.Coffee.Toggles;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Mobcast.Coffee.Toggles.Tests
{
	/// <summary>NUnit EditMode regression suite. Uses isolated preview scenes, never user objects.</summary>
	public class CompositeTweenRegressionTests
	{
		static readonly Type Manager = typeof(CompositeToggle).Assembly.GetType("Mobcast.Coffee.Toggles.ToggleTweenManager");
		static readonly MethodInfo ApplyMethod = typeof(Property).GetMethod("Apply", BindingFlags.Instance | BindingFlags.NonPublic);
		Fixture f;

		[SetUp]
		public void SetUp()
		{
			Assert.That(Application.isPlaying, Is.False);
			f = new Fixture();
		}

		[TearDown]
		public void TearDown()
		{
			if (f != null) f.Dispose();
			f = null;
		}

		sealed class Fixture : IDisposable
		{
			internal readonly Scene scene;
			internal readonly GameObject root;
			internal readonly CompositeToggle owner;
			internal readonly CanvasGroup canvas;
			internal Fixture()
			{
				scene = EditorSceneManager.NewPreviewScene();
				root = new GameObject("Tween regression fixture", typeof(RectTransform));
				SceneManager.MoveGameObjectToScene(root, scene);
				owner = root.AddComponent<CompositeToggle>();
				owner.Reflesh();
				canvas = root.AddComponent<CanvasGroup>();
				canvas.alpha = 0;
			}
			internal GameObject Child()
			{
				var child = new GameObject("Tween regression child");
				SceneManager.MoveGameObjectToScene(child, scene);
				child.transform.SetParent(root.transform, false);
				return child;
			}
			public void Dispose()
			{
				EditorSceneManager.ClosePreviewScene(scene);
				Call("Advance", 0f, 0f);
				Call("FlushVisibility");
			}
		}

		static void Call(string name, params object[] args)
		{
			Manager.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, args);
		}
		static void Apply(Property property, Object target, CompositeToggle owner, int index = 1)
		{
			ApplyMethod.Invoke(property, new object[] { target, index, owner, true });
		}
		static Property Make(Type targetType, string name, IParameterList values, params object[] states)
		{
			values.FitSize(states.Length);
			for (int i = 0; i < states.Length; i++) values.SetObject(i, states[i]);
			var info = targetType.GetProperty(name);
			var property = new Property(string.Format(Property.ID_FORMAT, targetType.AssemblyQualifiedName,
				info.PropertyType.AssemblyQualifiedName, info.GetSetMethod().Name), values);
			property.tween.enabled = true;
			property.tween.duration = 1;
			property.tween.ease = ToggleTweenEase.Linear;
			return property;
		}
		static Property Alpha() { return Make(typeof(CanvasGroup), "alpha", new FloatParameterList(), 0f, 1f); }
		static void Near(float actual, float expected, string message)
		{
			Assert.That(actual, Is.EqualTo(expected).Within(0.0001f), message);
		}
		static void Check(bool condition, string message) { Assert.That(condition, Is.True, message); }
		[Test]
		public void DefaultDisabledRemainsImmediate()
		{
			var p = Alpha();
			var defaults = new PropertyTweenSettings();
			Check(!defaults.enabled, "Tween must be opt-in");
			p.tween.enabled = false;
			Apply(p, f.canvas, f.owner);
			Near(f.canvas.alpha, 1, "Immediate value");
		}

		[Test]
		public void LinearInterpolationAndImmutableStateValues()
		{
			var p = Alpha(); Apply(p, f.canvas, f.owner);
			Near(f.canvas.alpha, 0, "Starts at actual value");
			Call("Advance", 0.5f, 0.5f); Near(f.canvas.alpha, 0.5f, "Midpoint");
			Near((float)p.parameterList.GetObject(1), 1, "Configured target unchanged");
			Call("Advance", 0.5f, 0.5f); Near(f.canvas.alpha, 1, "Exact end");
		}

		[Test]
		public void InterruptionRetargetsFromCurrentVisualValue()
		{
			var p = Alpha(); Apply(p, f.canvas, f.owner); Call("Advance", 0.4f, 0.4f);
			Apply(p, f.canvas, f.owner, 0); Near(f.canvas.alpha, 0.4f, "No completion jump");
			Call("Advance", 0.5f, 0.5f); Near(f.canvas.alpha, 0.2f, "Redirect midpoint");
			Call("Advance", 0.5f, 0.5f); Near(f.canvas.alpha, 0, "New end");
		}

		[Test]
		public void SameDestinationDoesNotRestart()
		{
			var p = Alpha(); Apply(p, f.canvas, f.owner); Call("Advance", 0.4f, 0.4f);
			Apply(p, f.canvas, f.owner); Call("Advance", 0.6f, 0.6f);
			Near(f.canvas.alpha, 1, "Repeated application finishes on original schedule");
		}

		[Test]
		public void DelayBeforeInterpolation()
		{
			var p = Alpha(); p.tween.delay = 0.5f; Apply(p, f.canvas, f.owner);
			Call("Advance", 0.25f, 0.25f); Near(f.canvas.alpha, 0, "Delay holds");
			Call("Advance", 0.75f, 0.75f); Near(f.canvas.alpha, 0.5f, "Elapsed excludes delay");
		}

		[Test]
		public void UnscaledClockAdvancesWhileScaledClockIsPaused()
		{
			var p = Alpha(); Apply(p, f.canvas, f.owner); Call("Advance", 0f, 0.5f);
			Near(f.canvas.alpha, 0.5f, "Unscaled progress");
		}

		[Test]
		public void ScaledClockRemainsPaused()
		{
			var p = Alpha(); p.tween.useUnscaledTime = false; Apply(p, f.canvas, f.owner);
			Call("Advance", 0f, 0.5f); Near(f.canvas.alpha, 0, "Scaled pause");
			Call("Advance", 0.5f, 0.5f); Near(f.canvas.alpha, 0.5f, "Scaled progress");
		}

		[Test]
		public void ImmediateWriteCancelsTween()
		{
			var p = Alpha(); Apply(p, f.canvas, f.owner); Call("Advance", 0.4f, 0.4f);
			p.Invoke(f.canvas, 0); Call("Advance", 1f, 1f); Near(f.canvas.alpha, 0, "Old tween cannot overwrite");
		}

		[Test]
		public void LastWriterWinsAcrossControllers()
		{
			var other = f.Child().AddComponent<CompositeToggle>();
			var p = Alpha(); Apply(p, f.canvas, f.owner); Call("Advance", 0.4f, 0.4f);
			var q = Alpha(); Apply(q, f.canvas, other, 0); Call("Advance", 0.5f, 0.5f);
			Near(f.canvas.alpha, 0.2f, "Only new owner writes");
		}

		[Test]
		public void OwnerCancellationHoldsCurrentValue()
		{
			var p = Alpha(); Apply(p, f.canvas, f.owner); Call("Advance", 0.4f, 0.4f);
			Call("CancelOwner", f.owner); Call("Advance", 1f, 1f); Near(f.canvas.alpha, 0.4f, "Cancelled");
		}

		[Test]
		public void DisableCancelsAndReenableAppliesCurrentState()
		{
			var p = Alpha(); f.owner.toggleProperties.Add(p);
			typeof(CompositeToggle).GetField("m_Started", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(f.owner, true);
			Apply(p, f.canvas, f.owner); Call("Advance", 0.4f, 0.4f);
			f.owner.enabled = false; Call("Advance", 1f, 1f); Near(f.canvas.alpha, 0.4f, "Disabled owner stopped");
			f.owner.enabled = true; Near(f.canvas.alpha, 0, "Reenable applies state zero immediately");
		}

		[Test]
		public void ZeroDurationIsImmediate()
		{
			var p = Alpha(); p.tween.duration = 0; p.tween.delay = 10;
			Apply(p, f.canvas, f.owner); Near(f.canvas.alpha, 1, "No division by zero / deferred write");
		}

		[Test]
		public void InvalidDurationSafelyAppliesImmediately()
		{
			var p = Alpha(); p.tween.duration = float.NaN;
			Apply(p, f.canvas, f.owner); Near(f.canvas.alpha, 1, "NaN rejected");
		}

		[Test]
		public void Vector2Position()
		{
			var rect = (RectTransform)f.root.transform;
			var p = Make(typeof(RectTransform), "anchoredPosition", new Vector2ParameterList(), Vector2.zero, new Vector2(8, 4));
			Apply(p, rect, f.owner); Call("Advance", 0.5f, 0.5f);
			Near(rect.anchoredPosition.x, 4, "Vector2 x"); Near(rect.anchoredPosition.y, 2, "Vector2 y");
		}

		[Test]
		public void Vector3Scale()
		{
			var p = Make(typeof(RectTransform), "localScale", new Vector3ParameterList(), Vector3.one, new Vector3(3, 5, 7));
			Apply(p, f.root.transform, f.owner); Call("Advance", 0.5f, 0.5f);
			Check(f.root.transform.localScale == new Vector3(2, 3, 4), "Vector3 midpoint");
		}

		[Test]
		public void ColorInterpolation()
		{
			var image = f.root.AddComponent<Image>(); image.color = Color.black;
			var p = Make(typeof(Image), "color", new ColorParameterList(), Color.black, Color.white);
			Apply(p, image, f.owner); Call("Advance", 0.5f, 0.5f);
			Near(image.color.r, 0.5f, "Color midpoint"); Near(image.color.a, 1, "Alpha retained");
		}

		[Test]
		public void CustomCurveAndExactEndpoint()
		{
			var p = Alpha(); p.tween.ease = ToggleTweenEase.Custom;
			p.tween.curve = AnimationCurve.Linear(0, 0, 1, 0.5f);
			Apply(p, f.canvas, f.owner); Call("Advance", 0.5f, 0.5f); Near(f.canvas.alpha, 0.25f, "Custom evaluation");
			Call("Advance", 0.5f, 0.5f); Near(f.canvas.alpha, 1, "Exact target regardless of curve endpoint");
		}

		[Test]
		public void DiscretePropertyRemainsImmediate()
		{
			var p = Make(typeof(CanvasGroup), "interactable", new BoolParameterList(), true, false);
			Check(!p.supportsTween, "bool unsupported"); Apply(p, f.canvas, f.owner);
			Check(!f.canvas.interactable, "bool applied once");
		}

		[Test]
		public void OrdinaryMethodsAreNotAnimated()
		{
			var values = new BoolParameterList(); values.FitSize(2); values.SetObject(0, true); values.SetObject(1, false);
			var p = new Property(string.Format(Property.ID_FORMAT, typeof(GameObject).AssemblyQualifiedName,
				typeof(bool).AssemblyQualifiedName, "SetActive"), values);
			Check(!p.supportsTween, "Method cannot be a tween");
		}

		[Test]
		public void DeferredHideWaitsForChildAnimation()
		{
			var child = f.Child(); var target = child.AddComponent<CanvasGroup>(); target.alpha = 0;
			Apply(Alpha(), target, f.owner);
			Call("SetActive", f.owner, f.root, false, true);
			Call("FlushVisibility"); Check(f.root.activeSelf, "Display lock retained");
			Call("Advance", 1f, 1f); Call("FlushVisibility"); Check(!f.root.activeSelf, "Hidden after final frame");
		}

		[Test]
		public void ReopenInvalidatesStaleHide()
		{
			Apply(Alpha(), f.canvas, f.owner);
			Call("SetActive", f.owner, f.root, false, true);
			Call("SetActive", f.owner, f.root, true, true);
			Call("Advance", 1f, 1f); Call("FlushVisibility"); Check(f.root.activeSelf, "Old hide cannot close reopened object");
		}

		[Test]
		public void DeferredHideCatchesChildTweenStartedLaterInSameFrame()
		{
			Call("SetActive", f.owner, f.root, false, true);
			Apply(Alpha(), f.canvas, f.owner);
			Call("FlushVisibility"); Check(f.root.activeSelf, "Resolve locks after controller propagation");
		}

		[Test]
		public void HideWithoutAnimationSettlesImmediatelyOnFlush()
		{
			Call("SetActive", f.owner, f.root, false, true); Call("FlushVisibility");
			Check(!f.root.activeSelf, "No orphan lock");
		}

		[Test]
		public void EditModeControllerAppliesImmediatelyAndEventsFireOnce()
		{
			f.owner.toggleProperties.Add(Alpha()); int changes = 0;
			f.owner.onValueChanged.AddListener(_ => changes++);
			f.owner.indexValue = 1; Near(f.canvas.alpha, 1, "No edit-mode tween");
			Check(changes == 1, "State event count");
		}

		[Test]
		public void DestroyedTargetIsRemovedSafely()
		{
			var child = f.Child(); var target = child.AddComponent<CanvasGroup>(); target.alpha = 0;
			Apply(Alpha(), target, f.owner); Object.DestroyImmediate(child); Call("Advance", 1f, 1f);
		}

		[Test]
		public void NegativeStateIndexIsIgnored()
		{
			Alpha().Invoke(f.canvas, -1); Near(f.canvas.alpha, 0, "Invalid index ignored");
		}

		[Test]
		public void InheritedSetterImmediateWriteCancelsCanonicalTween()
		{
			var animated = Make(typeof(RectTransform), "localScale", new Vector3ParameterList(), Vector3.one, Vector3.one * 3);
			Apply(animated, f.root.transform, f.owner); Call("Advance", 0.5f, 0.5f);
			var direct = Make(typeof(Transform), "localScale", new Vector3ParameterList(), Vector3.one, Vector3.one * 5);
			direct.Invoke(f.root.transform, 1); Call("Advance", 1f, 1f);
			Check(f.root.transform.localScale == Vector3.one * 5, "Inherited setter uses one animation key");
		}

		[Test]
		public void InitializationPreservesLegacyNonTweenPropertyBehavior()
		{
			var p = Alpha(); p.tween.enabled = false; f.owner.toggleProperties.Add(p); f.canvas.alpha = 0.6f;
			typeof(CompositeToggle).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(f.owner, null);
			Near(f.canvas.alpha, 0.6f, "Reset-on-start false must not newly invoke disabled properties");
		}

		[Test]
		public void InitializationSnapsEnabledTweenPropertyWithoutAnimation()
		{
			f.owner.toggleProperties.Add(Alpha()); f.canvas.alpha = 0.6f;
			typeof(CompositeToggle).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(f.owner, null);
			Near(f.canvas.alpha, 0, "Initial state applied immediately");
			Call("Advance", 0.5f, 0.5f); Near(f.canvas.alpha, 0, "No initial tween");
		}

		[Test]
		public void CountAndFlagPreserveExistingNoPropertySemantics()
		{
			f.owner.toggleProperties.Add(Alpha());
			foreach (var type in new[] { CompositeToggle.ValueType.Count, CompositeToggle.ValueType.Flag })
			{
				typeof(CompositeToggle).GetField("m_ValueType", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(f.owner, type);
				f.owner.InvokeToggleTarget(1); Near(f.canvas.alpha, 0, "Unsupported controller mode");
			}
		}

		[Test]
		public void SerializedTweenSettingsRoundTrip()
		{
			var p = Alpha(); p.tween.duration = 0.7f; p.tween.delay = 0.2f;
			p.tween.ease = ToggleTweenEase.Custom; p.tween.useUnscaledTime = false;
			p.tween.curve = AnimationCurve.Linear(0, 0, 1, 1);
			p.OnBeforeSerialize();
			var copy = JsonUtility.FromJson<Property>(JsonUtility.ToJson(p)); copy.OnAfterDeserialize();
			Check(copy.tween.enabled && copy.supportsTween, "Tween binding survives serialization");
			Near(copy.tween.duration, 0.7f, "Duration serialized"); Near(copy.tween.delay, 0.2f, "Delay serialized");
			Check(copy.tween.ease == ToggleTweenEase.Custom && !copy.tween.useUnscaledTime, "Options serialized");
			Near(copy.tween.curve.Evaluate(0.5f), 0.5f, "Curve serialized");
			Near((float)copy.parameterList.GetObject(1), 1, "State target serialized");
		}

		[Test]
		public void StyleStillAppliesImmediatelyAndSupersedesRunningTween()
		{
			var style = f.root.AddComponent<Style>();
			typeof(Style).GetField("m_IgnoreProperties", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(style, new string[0]);
			var asset = ScriptableObject.CreateInstance<StyleAsset>();
			try
			{
				asset.properties.Add(Alpha());
				Apply(Alpha(), f.canvas, f.owner); Call("Advance", 0.4f, 0.4f);
				style.styleAsset = asset; Near(f.canvas.alpha, 0, "Style uses state zero immediately");
				Call("Advance", 1f, 1f); Near(f.canvas.alpha, 0, "Style cancels conflicting animation");
			}
			finally { Object.DestroyImmediate(asset); }
		}

		[Test]
		public void Vector4Interpolation()
		{
			var target = ScriptableObject.CreateInstance<TweenRegressionValueTarget>();
			try
			{
				var p = Make(typeof(TweenRegressionValueTarget), "value", new Vector4ParameterList(), Vector4.zero, new Vector4(2, 4, 6, 8));
				Apply(p, target, f.owner); Call("Advance", 0.5f, 0.5f);
				Check(target.value == new Vector4(1, 2, 3, 4), "Vector4 midpoint");
			}
			finally { Object.DestroyImmediate(target); }
		}

		[Test]
		public void ReentrantSetterCanReplaceItsOwnAnimationSafely()
		{
			var target = ScriptableObject.CreateInstance<TweenRegressionValueTarget>();
			try
			{
				var p = Make(typeof(TweenRegressionValueTarget), "value", new Vector4ParameterList(), Vector4.zero, Vector4.one);
				Apply(p, target, f.owner);
				target.onWrite = () => { target.onWrite = null; Apply(p, target, f.owner, 0); };
				Call("Advance", 0.5f, 0.5f); Near(target.value.x, 0.5f, "New animation starts from setter's current value");
				Call("Advance", 0.5f, 0.5f); Near(target.value.x, 0.25f, "Replacement is not accidentally removed");
			}
			finally { Object.DestroyImmediate(target); }
		}

		[Test]
		public void NumericActionStillExecutesOnceInsteadOfTweening()
		{
			var target = ScriptableObject.CreateInstance<TweenRegressionValueTarget>();
			try
			{
				var values = new FloatParameterList(); values.FitSize(2); values.SetObject(1, 1f);
				var p = new Property(string.Format(Property.ID_FORMAT, typeof(TweenRegressionValueTarget).AssemblyQualifiedName,
					typeof(float).AssemblyQualifiedName, "NumericAction"), values);
				p.tween.enabled = true;
				Check(!p.supportsTween, "Numeric method lacks property semantics");
				Apply(p, target, f.owner); Call("Advance", 0.5f, 0.5f); Call("Advance", 0.5f, 0.5f);
				Check(target.actionCount == 1, "Method called once");
			}
			finally { Object.DestroyImmediate(target); }
		}

		[Test]
		public void UnclampedCustomCurveCanOvershootBeforeExactEnd()
		{
			var p = Make(typeof(RectTransform), "localScale", new Vector3ParameterList(), Vector3.one, Vector3.one * 2);
			p.tween.ease = ToggleTweenEase.Custom; p.tween.curve = AnimationCurve.Linear(0, 0, 1, 3);
			Apply(p, f.root.transform, f.owner); Call("Advance", 0.5f, 0.5f);
			Near(f.root.transform.localScale.x, 2.5f, "Curve overshoot retained");
			Call("Advance", 0.5f, 0.5f); Near(f.root.transform.localScale.x, 2, "Endpoint exact");
		}

	}

	public sealed class TweenRegressionValueTarget : ScriptableObject
	{
		Vector4 current;
		public Action onWrite;
		public int actionCount;
		public Vector4 value { get { return current; } set { current = value; if (onWrite != null) onWrite(); } }
		public void NumericAction(float ignored) { actionCount++; }
	}
}
