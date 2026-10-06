using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Mobcast.Coffee.Toggles
{
	/// <summary>Authors real, inspectable scene bindings; no demo-only runtime animation engine.</summary>
	public static class TweenDemoSceneBuilder
	{
		public const string ScenePath = "Assets/Mobcast/Coffee/CompositeToggle/Demo/Demo.unity";
		public const string RootPath = "Canvas/Scroll View/Viewport/Vertical Layout/Tween Playground";
		static readonly Color CardColor = new Color(0.12f, 0.22f, 0.36f);
		static readonly Color StageColor = new Color(0.08f, 0.15f, 0.25f);
		static readonly Color Accent = new Color(0.36f, 0.88f, 0.79f);
		static readonly Color Secondary = new Color(0.69f, 0.79f, 0.91f);
		static Font font;

		[MenuItem("Tools/Composite Toggle/Add Tween Examples to Demo")]
		public static void AddExamples()
		{
			if (Application.isPlaying) throw new InvalidOperationException("Build the examples in EditMode.");
			var scene = SceneManager.GetSceneByPath(ScenePath);
			if (!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("Open Demo.unity first.");
			var existing = GameObject.Find(RootPath);
			if (existing)
			{
				Selection.activeGameObject = existing;
				Debug.Log("Tween Playground already exists; existing examples were not overwritten.");
				return;
			}
			var canvas = scene.GetRootGameObjects().First(go => go.name == "Canvas");
			var content = canvas.transform.Find("Scroll View/Viewport/Vertical Layout");
			font = canvas.GetComponentsInChildren<Text>(true).First(t => t.font).font;
			Undo.IncrementCurrentGroup();
			int undoGroup = Undo.GetCurrentGroup();
			Undo.SetCurrentGroupName("Add Tween Playground");
			var root = Rect(content, "Tween Playground");
			Undo.RegisterCreatedObjectUndo(root.gameObject, "Add Tween Playground");
			root.SetAsFirstSibling();
			var layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
			layout.spacing = 12;
			layout.childControlWidth = layout.childControlHeight = true;
			layout.childForceExpandWidth = true;
			layout.childForceExpandHeight = false;

			var header = Rect(root, "Introduction");
			Height(header, 112);
			Label(header, "Title", "Composite Toggle / Tween", 24, 2, 32, 0, 0, FontStyle.Bold);
			Label(header, "Description", "Six live examples. Change a state and watch its properties travel to their new values.", 15, 39, 40, 0, 0);
			Label(header, "Hint", "TRY RAPID CLICKS  /  no jumps to the previous destination", 11, 86, 20, 0, 0).color = Accent;

			BuildPosition(root);
			BuildScaleRotation(root);
			BuildColorAlpha(root);
			BuildDelayedSize(root);
			BuildCustomCurve(root);
			BuildVisibility(root);

			foreach (var toggle in root.GetComponentsInChildren<CompositeToggle>(true))
			{
				toggle.forceNotifyNext = true;
				toggle.indexValue = toggle.indexValue;
				foreach (var property in toggle.toggleProperties) property.OnBeforeSerialize();
				EditorUtility.SetDirty(toggle);
			}
			Canvas.ForceUpdateCanvases();
			LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)content);
			var scroll = canvas.transform.Find("Scroll View").GetComponent<ScrollRect>();
			Undo.RecordObject(scroll, "Show Tween Playground");
			scroll.verticalNormalizedPosition = 1;
			EditorSceneManager.MarkSceneDirty(scene);
			Undo.CollapseUndoOperations(undoGroup);
			Selection.activeGameObject = root.gameObject;
			Debug.Log("Added six Tween examples. Save Demo.unity to retain the serialized examples.");
		}

		static RectTransform Rect(Transform parent, string name)
		{
			var go = new GameObject(name, typeof(RectTransform));
			go.layer = parent.gameObject.layer;
			var rect = (RectTransform)go.transform;
			rect.SetParent(parent, false);
			return rect;
		}

		static void Height(RectTransform rect, float height)
		{
			var element = rect.gameObject.AddComponent<LayoutElement>();
			element.minHeight = element.preferredHeight = height;
			element.flexibleHeight = 0;
		}

		static RectTransform Span(Transform parent, string name, float top, float height, float left = 16, float right = 16)
		{
			var rect = Rect(parent, name);
			rect.anchorMin = new Vector2(0, 1);
			rect.anchorMax = Vector2.one;
			rect.pivot = new Vector2(0.5f, 1);
			rect.anchoredPosition = new Vector2((left - right) / 2, -top);
			rect.sizeDelta = new Vector2(-left - right, height);
			return rect;
		}

		static Text Label(Transform parent, string name, string value, int size, float top, float height,
			float left = 16, float right = 16, FontStyle style = FontStyle.Normal)
		{
			var text = Span(parent, name, top, height, left, right).gameObject.AddComponent<Text>();
			text.font = font;
			text.fontSize = size;
			text.fontStyle = style;
			text.color = Color.white;
			text.text = value;
			text.raycastTarget = false;
			text.alignment = TextAnchor.UpperLeft;
			return text;
		}

		static Image Background(RectTransform rect, Color color)
		{
			var image = rect.gameObject.AddComponent<Image>();
			image.color = color;
			image.raycastTarget = false;
			return image;
		}

		static RectTransform Card(Transform root, string name, string title, string description, string timing)
		{
			var card = Rect(root, name);
			Height(card, 252);
			Background(card, CardColor);
			Label(card, "Title", title, 18, 14, 26, style: FontStyle.Bold);
			Label(card, "Description", description, 13, 44, 36).color = Secondary;
			Background(Span(card, "Stage", 87, 88), StageColor);
			var controls = Span(card, "Controls", 187, 36);
			var row = controls.gameObject.AddComponent<HorizontalLayoutGroup>();
			row.spacing = 8;
			row.childControlWidth = row.childControlHeight = true;
			row.childForceExpandWidth = true;
			row.childForceExpandHeight = false;
			Label(card, "Timing", timing, 11, 230, 18).color = Secondary;
			return card;
		}

		static RectTransform Tile(Transform stage, string name, Vector2 size, Color color, string caption)
		{
			var rect = Rect(stage, name);
			rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
			rect.sizeDelta = size;
			Background(rect, color);
			if (!string.IsNullOrEmpty(caption))
			{
				var label = Label(rect, "Label", caption, 13, 0, size.y, 0, 0, FontStyle.Bold);
				label.alignment = TextAnchor.MiddleCenter;
				label.color = StageColor;
				label.rectTransform.anchorMin = Vector2.zero;
				label.rectTransform.anchorMax = Vector2.one;
				label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
			}
			return rect;
		}

		static CompositeToggle Controller(GameObject go, int id, int states = 2, int initial = 0)
		{
			var toggle = go.AddComponent<CompositeToggle>();
			var so = new SerializedObject(toggle);
			so.FindProperty("m_UniqueId").intValue = id;
			so.FindProperty("m_ValueType").enumValueIndex = states == 2 ? 0 : 1;
			so.FindProperty("m_Count").intValue = states;
			so.FindProperty("m_Value").intValue = 1 << initial;
			so.ApplyModifiedPropertiesWithoutUndo();
			toggle.Reflesh();
			return toggle;
		}

		static Property Bind(CompositeToggle owner, Component component, string name, IParameterList values,
			object[] states, float duration = 0.65f, ToggleTweenEase ease = ToggleTweenEase.QuadOut, float delay = 0)
		{
			values.FitSize(states.Length);
			for (int i = 0; i < states.Length; i++) values.SetObject(i, states[i]);
			var info = component.GetType().GetProperty(name);
			var property = new Property(string.Format(Property.ID_FORMAT, component.GetType().AssemblyQualifiedName,
				info.PropertyType.AssemblyQualifiedName, info.GetSetMethod().Name), values);
			property.tween.enabled = true;
			property.tween.duration = duration;
			property.tween.delay = delay;
			property.tween.ease = ease;
			property.tween.useUnscaledTime = true;
			owner.toggleProperties.Add(property);
			return property;
		}

		static Button Button(Transform card, string name, string caption)
		{
			var rect = Rect(card.Find("Controls"), name);
			var image = Background(rect, new Color(0.22f, 0.37f, 0.55f));
			image.raycastTarget = true;
			var button = rect.gameObject.AddComponent<Button>();
			button.targetGraphic = image;
			var colors = button.colors;
			colors.highlightedColor = new Color(0.8f, 1, 1);
			colors.pressedColor = new Color(0.6f, 0.85f, 0.9f);
			colors.fadeDuration = 0.08f;
			button.colors = colors;
			var element = rect.gameObject.AddComponent<LayoutElement>();
			element.preferredHeight = 36;
			element.flexibleWidth = 1;
			var text = Label(rect, "Label", caption, 14, 0, 36, 0, 0, FontStyle.Bold);
			text.alignment = TextAnchor.MiddleCenter;
			return button;
		}

		static void StateButton(Transform card, string name, string caption, CompositeToggle toggle, int index)
		{
			var action = (UnityAction<int>)Delegate.CreateDelegate(typeof(UnityAction<int>), toggle,
				typeof(CompositeToggle).GetProperty("indexValue").GetSetMethod());
			UnityEventTools.AddIntPersistentListener(Button(card, name, caption).onClick, action, index);
		}

		static void ToggleButton(Transform card, CompositeToggle toggle, string caption)
		{
			UnityEventTools.AddPersistentListener(Button(card, "Toggle", caption).onClick, toggle.Toggle);
		}

		static void BuildPosition(Transform root)
		{
			var card = Card(root, "01 Position", "01  Position + interruption", "Pick A, B or C. Click again mid-flight to change the destination without a jump.", "anchoredPosition  /  0.8s  /  QuadOut  /  3 states");
			var stage = card.Find("Stage");
			var track = Tile(stage, "Track", new Vector2(292, 2), new Color(0.27f, 0.4f, 0.55f), null);
			var target = Tile(stage, "Target", new Vector2(52, 44), Accent, "MOVE");
			var toggle = Controller(target.gameObject, 1001, 3);
			Bind(toggle, target, "anchoredPosition", new Vector2ParameterList(), new object[] { new Vector2(-132, 0), Vector2.zero, new Vector2(132, 0) }, 0.8f);
			StateButton(card, "A", "A / LEFT", toggle, 0);
			StateButton(card, "B", "B / CENTER", toggle, 1);
			StateButton(card, "C", "C / RIGHT", toggle, 2);
		}

		static void BuildScaleRotation(Transform root)
		{
			var card = Card(root, "02 Scale Rotation", "02  Scale + rotation", "A single state drives two properties, each with its own duration.", "localScale 0.55s  +  localEulerAngles 0.75s  /  QuadInOut");
			var target = Tile(card.Find("Stage"), "Target", new Vector2(40, 40), new Color(0.54f, 0.67f, 1), "UI");
			var toggle = Controller(target.gameObject, 1002);
			Bind(toggle, target, "localScale", new Vector3ParameterList(), new object[] { Vector3.one * 0.75f, Vector3.one * 1.25f }, 0.55f, ToggleTweenEase.QuadInOut);
			Bind(toggle, target, "localEulerAngles", new Vector3ParameterList(), new object[] { Vector3.zero, new Vector3(0, 0, 135) }, 0.75f, ToggleTweenEase.QuadInOut);
			ToggleButton(card, toggle, "SCALE + ROTATE");
		}

		static void BuildColorAlpha(Transform root)
		{
			var card = Card(root, "03 Color Alpha", "03  Color + opacity", "Blend Image.color and CanvasGroup.alpha together. State values stay untouched.", "color + alpha  /  0.65s  /  QuadOut");
			var target = Tile(card.Find("Stage"), "Target", new Vector2(208, 54), Accent, "COLOR + ALPHA");
			var canvas = target.gameObject.AddComponent<CanvasGroup>();
			var toggle = Controller(target.gameObject, 1003);
			Bind(toggle, target.GetComponent<Image>(), "color", new ColorParameterList(), new object[] { new Color(0.35f, 0.7f, 1), new Color(1, 0.68f, 0.32f) });
			Bind(toggle, canvas, "alpha", new FloatParameterList(), new object[] { 0.35f, 1f });
			ToggleButton(card, toggle, "BLEND COLOR + OPACITY");
		}

		static void BuildDelayedSize(Transform root)
		{
			var card = Card(root, "04 Delayed Size", "04  Delayed resize", "Wait for a short delay, then expand. The layout does not own this animated rectangle.", "sizeDelta  /  0.25s delay + 0.7s  /  QuadInOut");
			var target = Tile(card.Find("Stage"), "Target", new Vector2(86, 28), new Color(0.96f, 0.56f, 0.61f), "SIZE");
			var toggle = Controller(target.gameObject, 1004);
			Bind(toggle, target, "sizeDelta", new Vector2ParameterList(), new object[] { new Vector2(86, 28), new Vector2(260, 60) }, 0.7f, ToggleTweenEase.QuadInOut, 0.25f);
			ToggleButton(card, toggle, "RESIZE AFTER DELAY");
		}

		static void BuildCustomCurve(Transform root)
		{
			var card = Card(root, "05 Custom Curve", "05  Custom overshoot", "A custom curve goes beyond the target, then settles at the exact state value.", "localScale  /  0.7s  /  Custom curve with overshoot");
			var target = Tile(card.Find("Stage"), "Target", new Vector2(44, 44), new Color(0.95f, 0.78f, 0.4f), "POP");
			var toggle = Controller(target.gameObject, 1005);
			var property = Bind(toggle, target, "localScale", new Vector3ParameterList(), new object[] { Vector3.one * 0.7f, Vector3.one * 1.35f }, 0.7f, ToggleTweenEase.Custom);
			property.tween.curve = new AnimationCurve(new Keyframe(0, 0, 0, 3), new Keyframe(0.6f, 1.15f, 0, 0), new Keyframe(1, 1, 0, 0));
			ToggleButton(card, toggle, "POP / SETTLE");
		}

		static void BuildVisibility(Transform root)
		{
			var card = Card(root, "06 Deferred Hide", "06  Fade out, then hide", "HIDE waits for the exit. Press SHOW during the fade to cancel the pending hide.", "alpha + scale  /  0.65s  /  deferred SetActive(false)");
			var panel = Tile(card.Find("Stage"), "Target", new Vector2(252, 60), Accent, "I STAY ALIVE UNTIL THE FADE ENDS");
			panel.GetComponentInChildren<Text>().fontSize = 11;
			var canvas = panel.gameObject.AddComponent<CanvasGroup>();
			var panelToggle = Controller(panel.gameObject, 1007, 2, 1);
			Bind(panelToggle, canvas, "alpha", new FloatParameterList(), new object[] { 0f, 1f });
			Bind(panelToggle, panel, "localScale", new Vector3ParameterList(), new object[] { Vector3.one * 0.9f, Vector3.one });
			var controller = Controller(card.gameObject, 1006, 2, 1);
			controller.deferDeactivation = true;
			controller.syncedToggles.Add(panelToggle);
			var so = new SerializedObject(controller);
			var activations = so.FindProperty("m_ActivateObjects");
			activations.arraySize = 2;
			activations.GetArrayElementAtIndex(0).objectReferenceValue = null;
			activations.GetArrayElementAtIndex(1).objectReferenceValue = panel.gameObject;
			so.ApplyModifiedPropertiesWithoutUndo();
			StateButton(card, "Show", "SHOW / REOPEN", controller, 1);
			StateButton(card, "Hide", "HIDE AFTER FADE", controller, 0);
		}
	}
}
