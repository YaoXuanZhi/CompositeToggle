using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Text;

namespace Mobcast.Coffee.Toggles
{
	using ValueType = CompositeToggle.ValueType;

	/// <summary>
	/// 複合トグルエディタ.
	/// </summary>
	[CustomEditor(typeof(CompositeToggle), true)]
	[CanEditMultipleObjects]
	public class CompositeToggleEditor : Editor
	{
		//---- ▼ GUIキャッシュ ▼ ----
		static GUIContent contentHierarchy;
		static GUIContent contentPlus;
		static GUIContent contentMinus;
		static GUIContent contentAction;
		static GUIContent contentGroup;
		static GUIContent contentActivation;
		static GUIContent contentUnActivation;
		static GUIContent contentActiveDatasVation;
		static GUIContent contentOnValueChanged;
		static GUIStyle styleTitle;
		protected static GUIStyle styleHeader;
		protected static GUIStyle styleInner;
		protected static GUIStyle styleComment;


		static bool cached;


		protected void CacheGUI()
		{
			if (cached)
				return;
			cached = true;

			contentPlus = new GUIContent(string.Empty, EditorGUIUtility.FindTexture("Toolbar Plus"), "Add Element");
			contentMinus = new GUIContent(string.Empty, EditorGUIUtility.FindTexture("Toolbar Minus"), "Remove Element");
			contentHierarchy = new GUIContent(EditorGUIUtility.FindTexture("unityeditor.hierarchywindow"), "Auto-Construction based on children");

			contentAction = new GUIContent("每个选中事件监听");
			contentGroup = new GUIContent("Toggle组");
			contentActivation = new GUIContent("激活单个物体");
			contentUnActivation = new GUIContent("隐藏单个物体");
			contentActiveDatasVation = new GUIContent("激活\\隐藏列表");
			contentOnValueChanged = new GUIContent("变更事件监听(持久的)");


			styleTitle = new GUIStyle("GUIEditor.BreadcrumbLeft");
			styleTitle.fixedHeight = 17;
			styleTitle.contentOffset = new Vector2(12, 0);
			styleTitle.alignment = TextAnchor.UpperLeft;

			styleHeader = new GUIStyle("RL Header");
			styleHeader.alignment = TextAnchor.MiddleLeft;
			styleHeader.fontSize = 11;
			styleHeader.margin = new RectOffset(0, 0, 0, 0);
			styleHeader.padding = new RectOffset(8, 8, 0, 0);
			styleHeader.normal.textColor = EditorStyles.label.normal.textColor;

			styleInner = new GUIStyle("RL Background");
			styleInner.margin = new RectOffset(0, 0, 0, 0);
			styleInner.padding = new RectOffset(4, 4, 3, 6);

			styleComment = new GUIStyle("ProfilerBadge");
			styleComment.fixedHeight = 0;
			styleComment.contentOffset += new Vector2(5,0);
			styleComment.fontSize = 11;
		}

		//---- ▲ GUIキャッシュ ▲ ----
		ReorderableList roSyncToggles;
		ReorderableList roSyncToggleObjects;
		ReorderableList roExActiveObjects;
		CompositeToggle current;

		//		MethodInfo miTransformParentChanged;

		SerializedProperty spComments;
		SerializedProperty spActions;
		SerializedProperty spGroupToggles;
		SerializedProperty spActivations;
		SerializedProperty spExActiveDatas;
		SerializedProperty spUnActivations;
		SerializedProperty spOnValueChanged;

		SerializedProperty spValueType;
		SerializedProperty spUniqueId;
		SerializedProperty spIgnoreParentToggle;
		SerializedProperty spResetValueOnAwake;
		SerializedProperty spSyncedToggles;
		SerializedProperty spSyncedToggleObjects;

		static CompositeToggle[] allToggles;
		static List<CompositeToggle> syncedByOtherToggles = new List<CompositeToggle>();
		static HashSet<CompositeToggle> multipleRelations;

		protected void OnEnable()
		{
//			rec = false;
			RefleshAllToggles();
			
			roSyncToggles = new ReorderableList(serializedObject, serializedObject.FindProperty("m_SyncedToggles"), false, false, true, true);
			roSyncToggles.drawElementCallback = (rect, index, isActive, isFocus) =>
			{
				var sp = roSyncToggles.serializedProperty.GetArrayElementAtIndex(index);
				EditorGUI.PropertyField(rect, sp, GetLabelWithWarning("Notifiy Target", sp.objectReferenceValue as CompositeToggle));
			};
			roSyncToggles.elementHeight = 16;
			roSyncToggles.headerHeight = 0;
			
			roSyncToggleObjects = new ReorderableList(serializedObject, serializedObject.FindProperty("m_SyncedToggleDatas"), true, true, true, true);
			roSyncToggleObjects.drawHeaderCallback = (Rect rect) =>
			{
				rect.xMin += 14; // 忽略拖拽按钮的宽度

				float x = rect.x;
				float y = rect.y;
				float height = rect.height;
				float space = 8;
				float remainingWidth = rect.width;
				float contentWidth = Mathf.FloorToInt((remainingWidth - 40 - space * 4 - 30) / 3);
				
				Rect rect1 = new Rect(x, y, 40, height);
				GUI.Label(rect1, "序号", EditorStyles.label);

				x = x + 80 + space;
				Rect rect2 = new Rect(x, y, contentWidth, height);
				GUI.Label(rect2, "Toggle物体", EditorStyles.label);
				
				x = x + contentWidth + space;
				Rect rect3 = new Rect(x, y, contentWidth, height);
				GUI.Label(rect3, "Toggle唯一ID", EditorStyles.label);
				
				x = x + contentWidth + space; 
				Rect rect4 = new Rect(x, y, contentWidth, height);
				GUI.Label(rect4, "Toggle", EditorStyles.label);
			};
			roSyncToggleObjects.drawElementCallback = (rect, index, isActive, isFocus) =>
			{
				var objTitle = new GUIContent("");
				var element = roSyncToggleObjects.serializedProperty.GetArrayElementAtIndex(index);
				
				float x = rect.x;
				float y = rect.y;
				float height = rect.height;
				float space = 8;
				float remainingWidth = rect.width;
				float contentWidth = Mathf.FloorToInt((remainingWidth - 40 - space * 4 - 30) / 3);
				
				Rect rect1 = new Rect(x, y, 40, height);
				GUI.Label(rect1, index.ToString());

				x = x + 80 + space;
				Rect rect2 = new Rect(x, y, contentWidth, height);
				EditorGUI.PropertyField(rect2, element.FindPropertyRelative("go"), objTitle);
				
				x = x + contentWidth + space;
				Rect rect3 = new Rect(x, y, contentWidth, height);
				EditorGUI.PropertyField(rect3, element.FindPropertyRelative("id"), objTitle);
				
				GUI.enabled = false;
				x = x + contentWidth + space; 
				Rect rect4 = new Rect(x, y, contentWidth, height);
				EditorGUI.PropertyField(rect4, element.FindPropertyRelative("toggle"), objTitle);
				
				int id = element.FindPropertyRelative("id").intValue;
				Object goObject = element.FindPropertyRelative("go").objectReferenceValue;
				if (goObject != null)
				{
					GameObject go = goObject as GameObject;
					CompositeToggle[] toggles = go.GetComponents<CompositeToggle>();
					bool isExist = false;
					for (int i = 0; i < toggles.Length; i++)
					{
						if (toggles[i].UniqueId == id)
						{
							isExist = true;
							element.FindPropertyRelative("toggle").objectReferenceValue = toggles[i];
							break;
						}
					}

					if (!isExist)
					{
						element.FindPropertyRelative("toggle").objectReferenceValue = null;
					}
				}
				GUI.enabled = true;
			};
			roSyncToggleObjects.elementHeight = 16;
			roSyncToggleObjects.headerHeight = 16;
			
			roExActiveObjects = new ReorderableList(serializedObject, serializedObject.FindProperty("m_ExActiveDatas"), true, true, true, true);
			roExActiveObjects.drawHeaderCallback = HeaderCallback;
			roExActiveObjects.drawElementCallback = ElementCallback;
			roExActiveObjects.elementHeight = 16;
			roExActiveObjects.headerHeight = 16;

			current = target as CompositeToggle;
			current.Reflesh();

			spComments = serializedObject.FindProperty("m_Comments");
			spActions = serializedObject.FindProperty("m_Actions");
			spGroupToggles = serializedObject.FindProperty("m_GroupedToggles");
			spActivations = serializedObject.FindProperty("m_ActivateObjects");
			spExActiveDatas = serializedObject.FindProperty("m_ExActiveDatas");
			spUnActivations = serializedObject.FindProperty("m_UnActivateObjects");
			spOnValueChanged = serializedObject.FindProperty("m_OnValueChanged");

			spValueType = serializedObject.FindProperty("m_ValueType");
			spUniqueId = serializedObject.FindProperty("m_UniqueId");
			spIgnoreParentToggle = serializedObject.FindProperty("m_IgnoreParent");
			spResetValueOnAwake = serializedObject.FindProperty("m_ResetValueOnAwake");
			spSyncedToggles = serializedObject.FindProperty("m_SyncedToggles");
			spSyncedToggleObjects = serializedObject.FindProperty("m_SyncedToggleDatas");

			s_AvailableReflectedTypes = new HashSet<Type>(current.GetComponents<Component>().Where(x => x != null).Select(x => x.GetType()));
			s_AvailableReflectedTypes.Add(typeof(GameObject));

			syncedByOtherToggles.Clear();
			foreach (var toggle in Resources.FindObjectsOfTypeAll<CompositeToggle>())
			{
				if (toggle == current)
					continue;
				
				var sp = new SerializedObject(toggle).FindProperty("m_SyncedToggles");
				if (Enumerable.Range(0, sp.arraySize).Any(i => sp.GetArrayElementAtIndex(i).objectReferenceValue == current))
					syncedByOtherToggles.Add(toggle);
			}

			UpdateMultipleRelations();
		}

		protected void OnDisable()
		{
//			rec = false;
		}


		void UpdateMultipleRelations()
		{
			current.SyncedObjects.Clear();
			for (int i = 0; i < current.SyncedToggleDatas.Count; i++)
			{
				current.SyncedObjects.Add(current.SyncedToggleDatas[i].toggle);
			}
			
			multipleRelations = new HashSet<CompositeToggle>(
				current.children
				.Concat(new []{current.parent})
				.Concat(current.groupedToggles)
				.Concat(current.syncedToggles)
				//.Concat(current.syncedToggleObjects)
//				.Concat(Enumerable.Range(0,spGroupToggles.arraySize).Select(i=>spGroupToggles.GetArrayElementAtIndex(i).objectReferenceValue as CompositeToggle))
//				.Concat(Enumerable.Range(0,spSyncedToggles.arraySize).Select(i=>spSyncedToggles.GetArrayElementAtIndex(i).objectReferenceValue as CompositeToggle))
				.Where(x => x)
				.GroupBy(x => x)
				.Where(x => 1 < x.Count())
				.Select(g => g.FirstOrDefault()));
		}


		/// <summary>
		/// インスペクタGUIコールバック.
		/// Inspectorウィンドウを表示するときにコールされます.
		/// </summary>
		public override void OnInspectorGUI()
		{
			CacheGUI();
			serializedObject.Update();


			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(-13);
			EditorGUILayout.BeginVertical();

			// Draw toggle summary(value type, ignore in parent, sync).
			OnDrawSummary();

			// Draw toggle relations(parent/children, synced, grouped by).
			OnDrawRelation();

			//トグルを全件表示します.
			OnDrawToggles();
			
			DrawExActiveObjects();

			//コールバック
			if (current.onValueChanged.GetPersistentEventCount() != 0)
				EditorGUILayout.PropertyField(spOnValueChanged);

			EditorGUILayout.EndVertical();
			GUILayout.Space(-4);
			EditorGUILayout.EndHorizontal();

			serializedObject.ApplyModifiedProperties();
		}

		/// <summary>
		/// 概要を表示します.
		/// </summary>
		protected void OnDrawSummary()
		{
			serializedObject.Update();

			//グループに所属している場合、パラメータを規定のものに変更.
			int indexInGroup = current.indexInGroup;
			if (0 <= indexInGroup)
			{
				spValueType.intValue = (int)ValueType.Boolean;
				spIgnoreParentToggle.boolValue = true;
				spResetValueOnAwake.boolValue = false;
				spSyncedToggles.ClearArray();
				spSyncedToggleObjects.ClearArray();

				if (serializedObject.ApplyModifiedProperties())
					current.OnTransformParentChanged();
				return;
			}
			
			using (new EditorGUILayout.VerticalScope("box"))
			{
				//トグルタイプポップアップを描画します.
				EditorGUILayout.PropertyField(spValueType, new GUIContent("控制类型"));
				EditorGUILayout.PropertyField(spUniqueId, new GUIContent("唯一ID"));
				EditorGUILayout.PropertyField(spIgnoreParentToggle, new GUIContent("忽略父节点影响"));
				EditorGUILayout.PropertyField(spResetValueOnAwake, new GUIContent("启动重设值"));

				//自分の値が変更されたときに通知するトグルリストを描画します.
				EditorGUILayout.BeginHorizontal();
				bool hasSyncToggles = 0 < roSyncToggles.count;
				if (EditorGUILayout.Toggle("同步到其它控制器(目标所有)", hasSyncToggles) != hasSyncToggles)
				{
					hasSyncToggles = !hasSyncToggles;
					if (hasSyncToggles)
					{
						roSyncToggles.serializedProperty.InsertArrayElementAtIndex(0);
					}
					else
						roSyncToggles.serializedProperty.ClearArray();
				}
				if (hasSyncToggles)
				{
					current.isSyncedSameType = EditorGUILayout.Toggle("只同步相同类型", current.isSyncedSameType);	
				}
				EditorGUILayout.EndHorizontal();

				if (hasSyncToggles)
				{
					//GUILayout.Space(-2);
					roSyncToggles.DoLayoutList();
				}
				
				bool hasSyncToggleObjects = 0 < roSyncToggleObjects.count;
				if (EditorGUILayout.Toggle("同步到其它控制器(ID查询)", hasSyncToggleObjects) != hasSyncToggleObjects)
				{
					hasSyncToggleObjects = !hasSyncToggleObjects;
					if (hasSyncToggleObjects)
						roSyncToggleObjects.serializedProperty.InsertArrayElementAtIndex(0);
					else
						roSyncToggleObjects.serializedProperty.ClearArray();
				}

				if (hasSyncToggleObjects)
				{
					GUILayout.Space(-2);
					roSyncToggleObjects.DoLayoutList();
				}
			}

			if (serializedObject.ApplyModifiedProperties())
			{
				current.OnTransformParentChanged();
				current.Reflesh();
				ResetToggleValue();
				serializedObject.Update();

				UpdateMultipleRelations();
			}
		}

		static StringBuilder s_StringBuilder = new StringBuilder();

		GUIContent GetLabelWithWarning(string label, CompositeToggle otherToggle)
		{
			if (!otherToggle)
				return new GUIContent(label);
			
			s_StringBuilder.Length = 0;

			// Self-reference.
			if (current == otherToggle)
				s_StringBuilder.AppendFormat("- You can not specify the toggle in itself.\n");

			// Conflict reference.
			if (multipleRelations.Contains(otherToggle))
				s_StringBuilder.AppendFormat("- The toggle can not have multiple relations.\n");

			// Value type or toggole count is mismatched.
			if (!current.groupedToggles.Contains(otherToggle))
			{
				bool isIndexedBoth = (current.valueType <= ValueType.Index && otherToggle.valueType <= ValueType.Index);
				if (!isIndexedBoth && current.valueType != otherToggle.valueType)
					s_StringBuilder.AppendFormat("- Value type is mismatched: {0}\n", otherToggle.valueType);

				if (current.count != otherToggle.count)
					s_StringBuilder.AppendFormat("- Toggle count is mismatched: {0}\n", otherToggle.count);
			}

			if (0 < s_StringBuilder.Length)
			{
				s_StringBuilder.Length--;
				return new GUIContent(label, EditorGUIUtility.FindTexture("console.warnicon.sml"), s_StringBuilder.ToString());
			}
			else
				return new GUIContent(label);
		}


		/// <summary>
		/// トグル関係を表示します.
		/// </summary>
		protected void OnDrawRelation()
		{

			int indexInGroup = current.indexInGroup;
//			Debug.LogFormat("indexInGroup:{0}, children:{1}, syncedByOtherToggles:{2}", indexInGroup, current.children.Count, syncedByOtherToggles.Count);
			if (indexInGroup < 0 && current.parent == null && current.children.Count == 0 && syncedByOtherToggles.Count == 0)
				return;

			using (new EditorGUI.DisabledGroupScope(true))
			using (new EditorGUILayout.VerticalScope("box"))
			{
				//リレーション一覧
				if (current.parent)
				{
					EditorGUILayout.ObjectField(GetLabelWithWarning("Parent", current.parent), current.parent, typeof(CompositeToggle), true);
				}
				else if (0 <= indexInGroup)
				{
					EditorGUILayout.ObjectField(new GUIContent(GetIndexedLabel(current.groupParent.valueType, indexInGroup, true)), current.groupParent, typeof(CompositeToggle), true);
				}

				foreach (var toggle in current.children)
				{
					EditorGUILayout.ObjectField(GetLabelWithWarning("Child", toggle), toggle, typeof(CompositeToggle), true);
				}

				foreach (var toggle in syncedByOtherToggles)
				{
					EditorGUILayout.ObjectField(GetLabelWithWarning("Synced By", toggle), toggle, typeof(CompositeToggle), true);
				}
			}
		}


		/// <summary>
		/// トグルを全件表示します.
		/// アクションモードではトグルアクションを、グループモードではトグルグループを描画します.
		/// </summary>
		protected void OnDrawToggles()
		{
			// Draw toolbar.
			DrawToggleToolbar();

			//トグルを全件表示.
			EditorGUILayout.BeginVertical(styleInner, GUILayout.MinHeight(1f));
			{
				//アクション内容を全件表示.
				bool drawTarget = current.toggleProperties.Count != 0 
				                  || spActions.arraySize != 0 
				                  || spGroupToggles.arraySize != 0 
				                  || spActivations.arraySize != 0 
				                  || spUnActivations.arraySize != 0;
				for (int i = 0; i < current.count; i++)
				{
					//エレメントタイトル.
					DrawElementTitle(i);

					if (drawTarget)
						DrawTargetIndex(i);
				}
			}
			EditorGUILayout.EndVertical();

			// Draw footer 
			DrawToggleFooter();
		}


		/// <summary>
		/// Draws the style sheet toolbar.
		/// </summary>
		/// <param name="behavior">Target behavior.</param>
		/// <param name="styleSheetAsset">Style sheet asset.</param>
		void DrawToggleToolbar()
		{
			bool changed = GUI.changed;

			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

			// Save style property from target behavior.
			using (var cc = new EditorGUI.DisabledGroupScope(!current || ValueType.Index < current.valueType))
			{
				if (GUILayout.Button(new GUIContent("保存"), EditorStyles.toolbarButton))
				{
					foreach (var property in current.toggleProperties)
					{
						property.parameterList.FitSize(current.count);
						PropertyEditor.FillArgumentsByGetter(current, property.methodInfo, property.parameterList, current.indexValue);
					}
				}

				if (GUILayout.Button(new GUIContent("加载"), EditorStyles.toolbarButton))
				{
					ResetToggleValue();
				}
			}

			GUILayout.Space(8);

			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Bake", EditorStyles.toolbarButton))
			{
				PropertyEditor.Bake(current.toggleProperties);
			}

			if (GUILayout.Button("添加属性", EditorStyles.toolbarPopup))
			{
				var menu = PropertyEditor.CreateTargetMenu(current, current.toggleProperties, current.count);
				AppendAvailableOptionToMenu(menu).ShowAsContext();
			}


			//自動構築ボタン.
			EditorGUI.BeginDisabledGroup(spGroupToggles.arraySize == 0 && spActivations.arraySize == 0 && spUnActivations.arraySize == 0 );
			if (GUILayout.Button(contentHierarchy, EditorStyles.toolbarButton))
			{
				int count = current.count;
				var sb = new StringBuilder("Are you sure you want to reconstruct toggles based on child transforms as following?\n");
				var transform = current.transform;
				var children = Enumerable.Range(0, transform.childCount)
					.Select(i => transform.GetChild(i))
					.ToList();

				var childToggles = children
					.Select(trans => trans.GetComponent<CompositeToggle>())
					.Where(toggle => toggle != null)
					.ToList();

				if (0 < spActivations.arraySize)
				{
					count = Mathf.Max(count, children.Count);
					sb.AppendFormat("\n{0} GameObjects for 'Set Activation' :\n{1}",
						children.Count,
						children.Aggregate(new StringBuilder(), (a, b) => a.AppendFormat(" - {0}\n", b.name))
					);
				}
				
				if (0 < spUnActivations.arraySize)
				{
					count = Mathf.Max(count, children.Count);
					sb.AppendFormat("\n{0} GameObjects for 'Set UnActivation' :\n{1}",
						children.Count,
						children.Aggregate(new StringBuilder(), (a, b) => a.AppendFormat(" - {0}\n", b.name))
					);
				}
				
				if (0 < spGroupToggles.arraySize)
				{
					count = Mathf.Max(count, childToggles.Count);
					sb.AppendFormat("\n{0} CompositeToggles for 'Toggle Grouping' :\n{1}",
						childToggles.Count,
						childToggles.Aggregate(new StringBuilder(), (a, b) => a.AppendFormat(" - {0}\n", b.name))
					);
				}

				if (EditorUtility.DisplayDialog("Auto-Construction Based on Children", sb.ToString(), "Yes", "No"))
				{
					current.count = count;
					serializedObject.Update();

					for (int i = 0; i < Mathf.Min(spGroupToggles.arraySize, childToggles.Count); i++)
						spGroupToggles.GetArrayElementAtIndex(i).objectReferenceValue = childToggles[i];
				}
			}
			EditorGUI.EndDisabledGroup();

			EditorGUILayout.EndHorizontal();
			GUI.changed = changed;
		}
		
		void DrawExActiveObjects()
		{
			bool hasExActiveObjects = current.hasExActiveObjects; 
			bool isSupported = current.valueType == ValueType.Boolean || current.valueType == ValueType.Index;
			if (!isSupported)
			{
				if (hasExActiveObjects)
				{
					EditorUtility.DisplayDialog("提示", $"激活列表不支持该类型:{current.valueType.ToString()}", "确认");
				}
				hasExActiveObjects = false;
			}
			
			if (hasExActiveObjects)
			{
				EditorGUILayout.LabelField("激活列表");
				roExActiveObjects.DoLayoutList();
				
				//在Inspector 窗口上创建区域，向区域拖拽资源对象，获取到拖拽到区域的对象
				var eventType = Event.current.type;        
				if (eventType == EventType.DragUpdated || eventType == EventType.DragPerform)
				{
					// Show a copy icon on the drag
					DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

					if (eventType == EventType.DragPerform)
					{
						DragAndDrop.AcceptDrag();
						foreach (var o in DragAndDrop.objectReferences)
						{
							GameObject tempGo = o as GameObject;
							CompositeToggleGroupActiveData groupActiveData = new CompositeToggleGroupActiveData(tempGo, current.count);
							current.AddExActiveDatas(groupActiveData);
						}
					}
					Event.current.Use();
				}
			}
			else
			{
				roExActiveObjects.serializedProperty.ClearArray();
			}
		}
		
		public Rect[] GetElementRects(Rect r)
		{
			Rect[] rects = new Rect[6];
			
			float x = r.x;
			float y = r.y;
			float height = r.height;
			float space = 8;
			float remainingWidth = r.width;
			float delWidth = 30;
			float idWidth = 40;
			float contentWidth = Mathf.FloorToInt((remainingWidth - idWidth - delWidth - space * 4) / 3);
			
			int colIndex = 0;

			//序号
			rects[colIndex] = new Rect(x, y, idWidth, height);
			colIndex++;
			x += idWidth + space;
			
			//游戏物体
			rects[colIndex] = new Rect(x, y, contentWidth, height);
			colIndex++;
			x += contentWidth + space;
			
			//状态
			rects[colIndex] = new Rect(x, y, contentWidth, height);
			colIndex++;
			x += contentWidth + space;
			
			//状态值
			rects[colIndex] = new Rect(x, y, contentWidth, height);
			colIndex++;
			x += contentWidth + space;
			
			//删除
			rects[colIndex] = new Rect(x, y, delWidth, height);
			return rects;
		}

		private void HeaderCallback(Rect rect)
		{
			rect.xMin += 14; // 忽略拖拽按钮的宽度
			
			Rect[] rects = GetElementRects(rect);
			GUI.Label(rects[0], "序号", EditorStyles.label);
			GUI.Label(rects[1], "游戏物体", EditorStyles.label);
			GUI.Label(rects[2], "状态", EditorStyles.label);
			GUI.Label(rects[3], "状态值", EditorStyles.label);
			GUI.Label(rects[4], "删除", EditorStyles.label);
		}
		
		private List<string> values = new List<string>();
		private bool IsRepeat(GameObject go)
		{
			if (go == null)
			{
				return false;
			}
			
			int count = 0;
			for (int i = 0; i < current.ExActiveDatas.Count; i++)
			{
				var elementData = current.ExActiveDatas[i];
				if (elementData.gameObject == go)
				{
					count++;
				}
			}
			return count > 1;
		}
		
		public void ElementCallback(Rect rect, int index, bool isActive, bool isFocused)
		{
			if (index >= roExActiveObjects.serializedProperty.arraySize)
			{
				return;
			}
			
			var objTitle = new GUIContent("");
			var element = roExActiveObjects.serializedProperty.GetArrayElementAtIndex(index);

			GUI.backgroundColor = Color.white;
			if (element.FindPropertyRelative("gameObject") != null)
			{
				GameObject eleGameObject = element.FindPropertyRelative("gameObject").objectReferenceValue as GameObject;
				if (IsRepeat(eleGameObject))
				{
					GUI.backgroundColor = Color.red;
				}
			}

			Rect[] rects = GetElementRects(rect);
			
			//序号
			GUI.Label(rects[0], index.ToString());
			
			//游戏物体
			EditorGUI.PropertyField(rects[1], element.FindPropertyRelative("gameObject"), objTitle);
			
			element.FindPropertyRelative("count").intValue = current.count;
			
			//状态
			values.Clear();
			List<string> commoments = current.GetComments();
			for (int i = 0; i < current.count; i++)
			{
				if (commoments[i].Equals("Comment...") || string.IsNullOrEmpty(commoments[i]))
				{
					values.Add(current.valueType.ToString() + " " + i);    
				}
				else
				{
					values.Add(commoments[i]);
				}
			}
			int stateValue = element.FindPropertyRelative("stateValue").intValue;
			element.FindPropertyRelative("stateValue").intValue = EditorGUI.MaskField(rects[2], stateValue, values.ToArray());

			//状态值
			int[] stateValues = CompositeUtil.CalculationState(stateValue, current.count);
			string retState = "";
			for (int i = 0; i < stateValues.Length; i++)
			{
				if (stateValues[i] == 1)
				{
					retState += (retState.Length > 0 ? "," : "") + i;	
				}
			}
			if (string.IsNullOrEmpty(retState))
			{
				retState = "无";
			}
			GUI.Label(rects[3], retState);

			//删除
			if (GUI.Button(rects[4], "×"))
			{
				roExActiveObjects.serializedProperty.DeleteArrayElementAtIndex(index);
			};
		}

		/// <summary>
		/// トグルの追加、削除、グループ自動構築メニューを含む、トグルフッターを描画します.
		/// </summary>
		void DrawToggleFooter()
		{
			if (current.valueType == ValueType.Boolean)
				return;

			using (new EditorGUILayout.HorizontalScope())
			{
				//フッター背景.
				GUILayout.FlexibleSpace();
				var rect = GUILayoutUtility.GetRect(58, 58, 20, 20);
				GUI.Label(rect, GUIContent.none, "RL Footer");

				//+ボタン.
				rect.y -= 3;
				rect.x += 5;
				rect.width = 20;
				if (GUI.Button(rect, contentPlus, EditorStyles.label))
				{
					current.count++;
					EditorUtility.SetDirty(current.gameObject);
					serializedObject.Update();
				}

				//-ボタン.
				rect.x += 25;
				using (new EditorGUI.DisabledGroupScope(current.count <= 2))
				{
					if (GUI.Button(rect, contentMinus, EditorStyles.label))
					{
						current.count--;
						EditorUtility.SetDirty(current.gameObject);
						serializedObject.Update();
					}
				}
			}
			GUILayout.Space(-5);
		}

		/// <summary>
		/// トグル項目のタイトルを描画します.
		/// 背景、トグルコントロール、トグルタイトルを1セットとして描画します.
		/// インスペクタ上でトグルコントロールを切り替えることで、トグル状態を変更できます.
		/// </summary>
		/// <param name="rect">描画矩形.</param>
		/// <param name="index">項目インデックス.</param>
		protected void DrawElementTitle(int index)
		{
			int val = 1 << index;
			bool isActive = 0 < (current.maskValue & val);

			//トグル名.
			Rect rect = GUILayoutUtility.GetRect(10, 17, styleTitle, GUILayout.ExpandWidth(true));
			float width = rect.width - 10;
			rect.x += 6;
			rect.y += 1;
			rect.width = 80;
			GUI.Label(rect, GetIndexedLabel(current.valueType, index, false), styleTitle);

			//トグル操作した場合、値を変更.
			rect.x -= 2;
			bool isRadio = current.valueType <= ValueType.Index;
			if (EditorGUI.Toggle(rect, isActive, isRadio ? EditorStyles.radioButton : EditorStyles.toggle) != isActive)
			{
				RefleshAllToggles();

				//valueTypeに応じて次のトグル状態に更新.
				switch (current.valueType)
				{
					case ValueType.Boolean:
					case ValueType.Index:
						current.indexValue = !isActive ? index : ((index + 1) % current.count);
						break;
					case ValueType.Count:
						current.countValue = !isActive ? index + 1 : index;
						break;
					case ValueType.Flag:
						current.maskValue = !isActive ? (current.maskValue | val) : (current.maskValue & ~val);
						break;
				}
			}

			//コメント
			rect.x += rect.width + 15;
			rect.width = width - rect.width - 10;
			rect.height -= 2;
			var spComment = spComments.GetArrayElementAtIndex(index);
			using (new EditorGUI.PropertyScope(rect, null, spComment))
			{
				EditorGUI.BeginChangeCheck();

				GUI.SetNextControlName("Comment_" + index);
				Color backgroundColor = GUI.backgroundColor;
				GUI.backgroundColor = isActive ? Color.white : new Color(1, 1, 1, 0.75f);
				string comment = EditorGUI.TextField(rect, spComment.stringValue, styleComment);

				if (string.IsNullOrEmpty(spComment.stringValue) && !spComment.hasMultipleDifferentValues && GUI.GetNameOfFocusedControl() != ("Comment_" + index))
				{
					GUI.backgroundColor = Color.clear;
					GUI.Label(rect, "Comment...", styleComment);
				}

				if (EditorGUI.EndChangeCheck())
				{
					spComment.stringValue = comment;
					spComment.serializedObject.ApplyModifiedProperties();
				}
				GUI.backgroundColor = backgroundColor;
			}
		}

		void RefleshAllToggles()
		{
			//非再生中の場合、CompositeToggle全てに対して親子関係を再計算.
			if (Application.isPlaying)
				return;
			
			foreach (var toggle in Resources.FindObjectsOfTypeAll<CompositeToggle>())
			{
				//　シーン上にないオブジェクトは除外されます.
				if (!toggle.gameObject || !toggle.gameObject.scene.IsValid())
					return;
				
				toggle.OnTransformParentChanged();
				toggle.Reflesh();
			}
		}

		void ResetToggleValue()
		{
			//valueTypeに応じて次のトグル状態に更新.
			current.forceNotifyNext = true;
			switch (current.valueType)
			{
				case ValueType.Boolean:
				case ValueType.Index:
					current.indexValue = current.indexValue;
					break;
				case ValueType.Count:
					current.countValue = current.countValue;
					break;
				case ValueType.Flag:
					current.maskValue = current.maskValue;
					break;
			}
		}

		public static string GetIndexedLabel(CompositeToggle.ValueType valueType, int index, bool inGroupSufix)
		{
			string format = inGroupSufix ? "'{0}' In Group" : "{0}";

			switch (valueType)
			{
				case CompositeToggle.ValueType.Boolean:
					return string.Format(format, index == 0 ? "False" : "True");
				case CompositeToggle.ValueType.Count:
					return string.Format(format, string.Format("Count {1}", valueType, index+1));
				default:
					return string.Format(format, string.Format("{0} {1}", valueType, index));
			}
		}


		static HashSet<Type> s_AvailableReflectedTypes = new HashSet<Type>();

		void DrawTargetIndex(int index)
		{
			GUILayout.Space(5);
			EditorGUILayout.BeginVertical("helpbox");

			if (!serializedObject.isEditingMultipleObjects)
			{
				bool isIndex = current.valueType == ValueType.Boolean || current.valueType == ValueType.Index;
				EditorGUI.BeginDisabledGroup(!isIndex);
				// Draw each targets.
				foreach (var property in current.toggleProperties)
				{
					bool enable = s_AvailableReflectedTypes.Contains(property.methodInfo.ReflectedType);
					if (PropertyEditor.DrawPropertyField(property, index, enable))
					{
						if (0 != (current.maskValue & (1 << index)))
							ResetToggleValue();

						EditorUtility.SetDirty(current.gameObject);
						serializedObject.Update();
					}
				}
				EditorGUI.EndDisabledGroup();
			}
			else
			{
				EditorGUILayout.HelpBox("Multi-Properties editing is not supported.", MessageType.None);
			}

			// Draw external action.
			if (!spActions.hasMultipleDifferentValues && index < spActions.arraySize)
				EditorGUILayout.PropertyField(spActions.GetArrayElementAtIndex(index), contentAction);

			// Draw group toggle.
			if (!spGroupToggles.hasMultipleDifferentValues && index < spGroupToggles.arraySize)
			{
				var spToggle = spGroupToggles.GetArrayElementAtIndex(index);
				Rect r = EditorGUILayout.GetControlRect();
				using (new EditorGUI.PropertyScope(r, null, spToggle))
				{
					EditorGUI.BeginChangeCheck();

					EditorGUI.PropertyField(r, spToggle, GetLabelWithWarning("Grouping Toggle", spToggle.objectReferenceValue as CompositeToggle));
					if (EditorGUI.EndChangeCheck())
					{
						UpdateMultipleRelations();
					}
				}
			}

			// Draw activation.
			if (index < spActivations.arraySize)
				EditorGUILayout.PropertyField(spActivations.GetArrayElementAtIndex(index), contentActivation);
			
			if (index < spUnActivations.arraySize)
				EditorGUILayout.PropertyField(spUnActivations.GetArrayElementAtIndex(index), contentUnActivation);
			
			EditorGUILayout.EndVertical();
		}

		GenericMenu AppendAvailableOptionToMenu(GenericMenu menu)
		{
			// Options.
			menu.AddSeparator("");
			menu.AddDisabledItem(new GUIContent("Options"));

			menu.AddItem(contentAction, 0 < spActions.arraySize, () => SwitchActivate(spActions));
			menu.AddItem(contentGroup, 0 < spGroupToggles.arraySize, () => SwitchActivate(spGroupToggles));
			menu.AddItem(contentActivation, 0 < spActivations.arraySize, () => SwitchActivate(spActivations));
			menu.AddItem(contentUnActivation, 0 < spUnActivations.arraySize, () => SwitchActivate(spUnActivations));
			menu.AddItem(contentActiveDatasVation, current.hasExActiveObjects, () => SwitchActivate(spExActiveDatas));
			
			var ev = current.onValueChanged;
			menu.AddItem(contentOnValueChanged, ev.GetPersistentEventCount() != 0,
				() =>
				{
					if (ev.GetPersistentEventCount() != 0)
					{
						while (0 < ev.GetPersistentEventCount())
							UnityEditor.Events.UnityEventTools.RemovePersistentListener(ev, 0);
					}
					else
						UnityEditor.Events.UnityEventTools.AddPersistentListener(ev);
				}
			);
			return menu;
		}

		void SwitchActivate(SerializedProperty sp)
		{
			if (sp == spExActiveDatas)
			{
				current.hasExActiveObjects = !current.hasExActiveObjects;
				bool isSupported = current.valueType == ValueType.Boolean || current.valueType == ValueType.Index;
				if (!isSupported)
				{
					EditorUtility.DisplayDialog("提示", $"激活列表不支持该类型:{current.valueType.ToString()}", "确认");
					current.hasExActiveObjects = false;
				}
			}
			else
			{
				if (0 < sp.arraySize)
				{
					while (0 < sp.arraySize)
						sp.DeleteArrayElementAtIndex(0);
				}
				else
				{
					while (sp.arraySize < current.count)
						sp.InsertArrayElementAtIndex(sp.arraySize);
				}
			}
			
			sp.serializedObject.ApplyModifiedProperties();
		}
	}
}
