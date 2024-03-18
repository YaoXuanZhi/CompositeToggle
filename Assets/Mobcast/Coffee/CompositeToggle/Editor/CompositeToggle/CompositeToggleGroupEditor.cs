using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using Mobcast.Coffee.Toggles;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(CompositeToggleGroup))]
public class CompositeToggleGroupEditor : Editor
{
    private CompositeToggleGroup _toggleGroup;
    ReorderableList reorderableList;

    private List<string> typeList = new List<string>();
    private List<string> values = new List<string>();
    
    //[MenuItem("GameObject/刷新扩展控制器状态", false, -2)]
    static public void RefreshExToggleState(MenuCommand menuCommand)
    {
        GameObject go = menuCommand.context as GameObject;
        CompositeToggleGroup[] toggles = go.GetComponentsInChildren<CompositeToggleGroup>(true);
        for (int i = 0; i < toggles.Length; i++)
        {
            toggles[i].gameObject.SetActive(true);
            toggles[i].Init();
        }
    }

    private void OnEnable()
    {
        //将被选中的gameobject所挂载的ReferenceCollector赋值给编辑器类中的ReferenceCollector，方便操作
        _toggleGroup = (CompositeToggleGroup) target;
        reorderableList = new ReorderableList(serializedObject, serializedObject.FindProperty("data"));
        reorderableList.drawElementCallback = OnDrawElementCallback;		
        reorderableList.drawHeaderCallback = OnDrawHeaderCallback;
        reorderableList.elementHeightCallback = OnelementHeightCallback;
        reorderableList.displayAdd = false;
        reorderableList.displayRemove = false;
        
        typeList.Clear();
        typeList.AddRange(new string[]
        {
            //"Boolean",
            "Index",
            //"Count",
            //"Flag",
        });
    }

    private int GetIndexByName(string name)
    {
        for (int i = 0; i < typeList.Count; i++)
        {
            if (name == typeList[i])
            {
                return i;
            }
        }
        return 0;
    }
    
    private void SetObjectType(SerializedProperty property, CompositeToggle toggle, string type)
    {
        var refType = property.FindPropertyRelative("type");
        var refToggle = property.FindPropertyRelative("toggle");
        refType.stringValue = type;
        refToggle.objectReferenceValue = toggle;

        string refName = property.FindPropertyRelative("key").stringValue;
        if (string.IsNullOrEmpty(refName) && toggle != null)
        {
            property.FindPropertyRelative("key").stringValue = toggle.name;
        }
    }
    
    private void OnDrawElementCallback(Rect r, int index, bool selected, bool focused)
	{
        if (index >= reorderableList.serializedProperty.arraySize)
        {
            return;
        }

        GUI.backgroundColor = Color.white;
        var elementData = _toggleGroup.data[index];
        if (IsRepeat(elementData.key) && !string.IsNullOrEmpty(elementData.key))
        {
            GUI.backgroundColor = Color.red;
        }

        var element = reorderableList.serializedProperty.GetArrayElementAtIndex(index);
        var refType = element.FindPropertyRelative("type");        
        var refKey = element.FindPropertyRelative("key");

        //ID
        Rect[] rects = GetElementRects(r);
        GUI.Label(rects[0], index.ToString());        

        //名字
        var objTitle = new GUIContent("");
        EditorGUI.PropertyField(rects[1], refKey, objTitle);
        //EditorGUI.PropertyField(new Rect(rects[1].x, rects[1].y + 50, rects[1].width, rects[1].height), refKey, objTitle);

        //引用
        Rect refRect = rects[2];
        EditorGUI.PropertyField(refRect, element.FindPropertyRelative("toggle"), objTitle);

        CompositeToggle toggle = element.FindPropertyRelative("toggle").objectReferenceValue as CompositeToggle;
        //类型
        var objs = typeList.ToArray();
        var lastSelectIndex = GetIndexByName(refType.stringValue);
        var selectIndex = EditorGUI.Popup(rects[3], lastSelectIndex, objs);
        if (lastSelectIndex != selectIndex)
        {
            SetObjectType(element, toggle, objs[selectIndex]);
        }
        
        //控制器状态
        //List<string> values = new List<string>();
        values.Clear();
        if (toggle != null)
        {
            List<string> commoments = toggle.GetComments();
            for (int i = 0; i < toggle.count; i++)
            {
                if (commoments[i].Equals("Comment...") || string.IsNullOrEmpty(commoments[i]))
                {
                    values.Add(toggle.valueType.ToString() + " " + i);    
                }
                else
                {
                    values.Add(commoments[i]);
                }
            }
            int stateValue = element.FindPropertyRelative("stateValue").intValue;
            EditorGUI.PropertyField(refRect, element.FindPropertyRelative("toggle"), objTitle);
            element.FindPropertyRelative("stateValue").intValue = EditorGUI.MaskField(rects[4], stateValue, values.ToArray());
            if (element.FindPropertyRelative("stateValue").intValue != stateValue)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(serializedObject.targetObject);
                _toggleGroup.RefreshActiveState();
            }
        }

        //删除
        if (GUI.Button(rects[5], "×"))
        {
            toggle.ReferenceExToggles.Remove(_toggleGroup);
            _toggleGroup.Remove(toggle);
            reorderableList.serializedProperty.DeleteArrayElementAtIndex(index);
        };
    }
    
    private void OnDrawHeaderCallback(Rect headerRect)
    {
        headerRect.xMin += 14; // 忽略拖拽按钮的宽度
        headerRect.y++;
        headerRect.height = 15;

        Rect[] rects = GetElementRects(headerRect);
        int col = 0;
        string[] names = {
            "ID",
            "名字",
            "引用",
            "类型",
            "状态",
            "删除",
        };
        for (int i = 0; i < rects.Length; i++)
        {
            GUI.Label(rects[col], names[i], EditorStyles.label);
            col++;
        }
    }
    
    public Rect[] GetElementRects(Rect r)
    {
        Rect[] rects = new Rect[6];
        float remainingWidth = r.width;
        float orderWidth = 30;
        float delWidth = 30;
        float registWidth = 80;
        float offset = 4;
        float contentWidth = Mathf.FloorToInt((remainingWidth - orderWidth - registWidth - delWidth - offset * 5) / 3);
        int colIndex = 0;
        float x = r.x;
        //ID
        rects[colIndex] = new Rect(x, r.y, orderWidth, r.height);
        colIndex++;
        x += orderWidth + offset;
        //名字
        rects[colIndex] = new Rect(x, r.y, contentWidth, r.height);
        colIndex++;
        x += contentWidth + offset;
        //引用
        rects[colIndex] = new Rect(x, r.y, contentWidth, r.height);
        colIndex++;
        x += contentWidth + offset;
        //类型
        rects[colIndex] = new Rect(x, r.y, contentWidth, r.height);
        colIndex++;
        x += contentWidth + offset;
        //自动注册
        rects[colIndex] = new Rect(x, r.y, registWidth, r.height);
        colIndex++;
        x += registWidth + offset;
        //删除
        rects[colIndex] = new Rect(x, r.y, delWidth, r.height);
        return rects;
    }
    
    private bool IsRepeat(string key)
    {
        int count = 0;
        for (int i = 0; i < _toggleGroup.data.Count; i++)
        {
            var elementData = _toggleGroup.data[i];
            if (elementData.key.Equals(key))
            {
                count++;
            }
        }
        return count > 1;
    }
    
    private float OnelementHeightCallback(int index)
    {
        if (index == 0)
        {
            return 20;
        }

        return 20;
    }

    public override void OnInspectorGUI()
	{
        EditorGUI.BeginChangeCheck();
        
        //引用列表
		reorderableList.DoLayoutList();

        GUI.backgroundColor = Color.white;

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
                    CompositeToggle tempToggle = tempGo.GetComponent<CompositeToggle>();
                    AddReference(tempToggle);
                }
            }

            Event.current.Use();
        }
        
        EditorGUILayout.BeginHorizontal();
        _toggleGroup.Operator = (EnumOperatorType)EditorGUILayout.EnumPopup("操作符", _toggleGroup.Operator);
        if (GUILayout.Button("刷新状态"))
        {
            _toggleGroup.gameObject.SetActive(true);
            _toggleGroup.Init();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (_toggleGroup.gameObject.name.StartsWith("pnl")
            || _toggleGroup.gameObject.name.StartsWith("UI"))
        {
            if (GUILayout.Button("生成代码"))
            {
                Debug.Log("自动生成代码，暂时屏蔽");
                // string panelName = "";
                // string goName = exToggle.gameObject.name;
                // string[] uiName = goName.Split('_');
                // if (uiName.Length > 0)
                // {
                //     for (int i = 0; i < uiName.Length; i++)
                //     {
                //         panelName += UICodeSpawner.FirstToUpper(uiName[i]);
                //     }
                // }
                // else
                // {
                //     panelName = goName;
                // }
                // UICodeSpawner.SpawnUICode(exToggle.gameObject, panelName);
            }
        }

        if (GUILayout.Button("清空列表"))
        {
            if (EditorUtility.DisplayDialog("提示", "确定清空引用列表吗？", "确定", "取消"))
            {
                _toggleGroup.Clear();
            }            
        }
        EditorGUILayout.EndHorizontal();

        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(serializedObject.targetObject);
        }
    }
    
    private void AddReference(CompositeToggle obj)
    {
        if (obj == null)
        {
            Debug.LogError("该节点没有绑定CompositeToggle组件");
            return;
        }

        if (obj.valueType != CompositeToggle.ValueType.Index)
        {
            Debug.LogError("目前只支持Index控制类型");
            return;
        }
        CompositeToggleGroupData toggleGroupData = new CompositeToggleGroupData(obj.name, obj);
        //collectorData.SetObjectType(ObjectCollector.GetDefultType(obj));
        toggleGroupData.type = obj.valueType.ToString();
        _toggleGroup.data.Add(toggleGroupData);
        _toggleGroup.AddListener(toggleGroupData);
        _toggleGroup.RefreshActiveState(toggleGroupData);
        if (!obj.ReferenceExToggles.Contains(_toggleGroup))
        {
            obj.ReferenceExToggles.Add(_toggleGroup);    
        }
        EditorUtility.SetDirty(_toggleGroup);
    }
}
