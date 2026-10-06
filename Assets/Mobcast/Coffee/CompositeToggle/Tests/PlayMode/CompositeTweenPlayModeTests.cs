using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Mobcast.Coffee.Toggles.Tests
{
	public class CompositeTweenPlayModeTests
	{
		GameObject go;
		CompositeToggle owner;
		CanvasGroup canvas;
		Property property;
		float previousTimeScale;
		bool previousRunInBackground;

		[SetUp]
		public void SetUp()
		{
			previousTimeScale = Time.timeScale;
			previousRunInBackground = Application.runInBackground;
			Application.runInBackground = true;
			go = new GameObject("Composite Tween PlayMode test");
			owner = go.AddComponent<CompositeToggle>();
			owner.Reflesh();
			canvas = go.AddComponent<CanvasGroup>();
			canvas.alpha = 0.6f;
			var values = new FloatParameterList();
			values.FitSize(2);
			values.SetObject(0, 0f);
			values.SetObject(1, 1f);
			property = new Property(string.Format(Property.ID_FORMAT, typeof(CanvasGroup).AssemblyQualifiedName,
				typeof(float).AssemblyQualifiedName, "set_alpha"), values);
			property.tween.enabled = true;
			property.tween.duration = 0.4f;
			property.tween.ease = ToggleTweenEase.Linear;
			owner.toggleProperties.Add(property);
		}

		[UnityTearDown]
		public IEnumerator TearDown()
		{
			Time.timeScale = previousTimeScale;
			Application.runInBackground = previousRunInBackground;
			if (go) Object.Destroy(go);
			yield return null;
		}

		static void Near(float actual, float expected)
		{
			Assert.That(actual, Is.EqualTo(expected).Within(0.0001f));
		}

		void ConfigureVisibility()
		{
			owner.deferDeactivation = true;
			typeof(CompositeToggle).GetField("m_ActivateObjects", BindingFlags.Instance | BindingFlags.NonPublic)
				.SetValue(owner, new List<GameObject> { null, go });
		}

		[UnityTest]
		public IEnumerator InitializationSnapsAndStateEventFiresBeforeAnimationCompletes()
		{
			yield return null;
			Near(canvas.alpha, 0);
			int events = 0;
			owner.onValueChanged.AddListener(_ => events++);
			owner.indexValue = 1;
			Near(canvas.alpha, 0);
			Assert.That(events, Is.EqualTo(1));
			yield return new WaitForSecondsRealtime(0.6f);
			Near(canvas.alpha, 1);
			Assert.That(events, Is.EqualTo(1));
		}

		[UnityTest]
		public IEnumerator UnscaledTweenCompletesWhileGameTimeIsPaused()
		{
			yield return null;
			Time.timeScale = 0;
			owner.indexValue = 1;
			yield return new WaitForSecondsRealtime(0.6f);
			Near(canvas.alpha, 1);
		}

		[UnityTest]
		public IEnumerator ScaledTweenWaitsForGameTimeToResume()
		{
			yield return null;
			property.tween.useUnscaledTime = false;
			Time.timeScale = 0;
			owner.indexValue = 1;
			yield return new WaitForSecondsRealtime(0.15f);
			Near(canvas.alpha, 0);
			Time.timeScale = 1;
			yield return new WaitForSecondsRealtime(0.6f);
			Near(canvas.alpha, 1);
		}

		[UnityTest]
		public IEnumerator RetargetStartsAtCurrentVisualValue()
		{
			yield return null;
			property.tween.duration = 2f;
			owner.indexValue = 1;
			yield return new WaitForSecondsRealtime(0.15f);
			float current = canvas.alpha;
			Assert.That(current, Is.GreaterThan(0).And.LessThan(1));
			property.tween.duration = 0.4f;
			owner.indexValue = 0;
			Near(canvas.alpha, current);
			yield return new WaitForSecondsRealtime(0.6f);
			Near(canvas.alpha, 0);
		}

		[UnityTest]
		public IEnumerator ExitWaitsForTweenAndInactiveObjectCanReopen()
		{
			yield return null;
			owner.indexValue = 1;
			yield return new WaitForSecondsRealtime(0.6f);
			ConfigureVisibility();
			owner.indexValue = 0;
			Assert.That(go.activeSelf, Is.True);
			yield return new WaitForSecondsRealtime(0.6f);
			Assert.That(go.activeSelf, Is.False);
			Near(canvas.alpha, 0);
			owner.indexValue = 1;
			Assert.That(go.activeSelf, Is.True);
			Near(canvas.alpha, 1);
		}

		[UnityTest]
		public IEnumerator ReopenCancelsPendingHide()
		{
			yield return null;
			owner.indexValue = 1;
			yield return new WaitForSecondsRealtime(0.6f);
			ConfigureVisibility();
			property.tween.duration = 2f;
			owner.indexValue = 0;
			yield return new WaitForSecondsRealtime(0.15f);
			Assert.That(go.activeSelf, Is.True);
			property.tween.duration = 0.4f;
			owner.indexValue = 1;
			yield return new WaitForSecondsRealtime(0.6f);
			Assert.That(go.activeSelf, Is.True);
			Near(canvas.alpha, 1);
		}

		[UnityTest]
		public IEnumerator DisablingOwnerCancelsAndReenableSettlesState()
		{
			yield return null;
			property.tween.duration = 2f;
			owner.indexValue = 1;
			yield return new WaitForSecondsRealtime(0.15f);
			owner.enabled = false;
			float current = canvas.alpha;
			yield return new WaitForSecondsRealtime(0.15f);
			Near(canvas.alpha, current);
			owner.enabled = true;
			Near(canvas.alpha, 1);
		}
	}
}
