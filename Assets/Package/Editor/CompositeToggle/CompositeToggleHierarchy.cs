using UnityEditor;
using UnityEngine;

namespace Mobcast.Coffee.Toggles
{
	/// <summary>Editor-only identification; never renames or modifies scene objects.</summary>
	[InitializeOnLoad]
	internal static class CompositeToggleHierarchy
	{
		static readonly GUIContent Badge = new GUIContent("[复合开关]");
		static GUIStyle badgeStyle;

		static CompositeToggleHierarchy()
		{
			EditorApplication.hierarchyWindowItemOnGUI += DrawBadge;
			EditorApplication.hierarchyChanged += EditorApplication.RepaintHierarchyWindow;
			Undo.undoRedoPerformed += EditorApplication.RepaintHierarchyWindow;
			EditorApplication.RepaintHierarchyWindow();
		}

		static bool HasToggle(GameObject gameObject)
		{
			// Check only this object, including inactive objects and disabled components.
			return gameObject && gameObject.TryGetComponent<CompositeToggle>(out _);
		}

		static void DrawBadge(int instanceId, Rect row)
		{
			if (Event.current.type != EventType.Repaint ||
				!HasToggle(EditorUtility.InstanceIDToObject(instanceId) as GameObject))
				return;

			if (badgeStyle == null)
			{
				badgeStyle = new GUIStyle(EditorStyles.miniLabel) {
					alignment = TextAnchor.MiddleRight,
					fontSize = 11,
					clipping = TextClipping.Clip
				};
			}

			float width = Mathf.Ceil(badgeStyle.CalcSize(Badge).x) + 6f;
			// Leave room for the icon/name and Unity's right-edge prefab controls.
			if (row.width < width + 48f)
				return;
			var rect = new Rect(row.xMax - width - 18f, row.y, width, row.height);
			bool dark = EditorGUIUtility.isProSkin;
			badgeStyle.normal.textColor = dark ? new Color(0.40f, 0.70f, 1f) : new Color(0.08f, 0.35f, 0.80f);
			GUI.Label(rect, Badge, badgeStyle);
		}
	}
}
