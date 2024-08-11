using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mobcast.Coffee.Toggles;
using UnityEngine;
using UnityEngine.Events;

namespace Mobcast.Coffee.Toggles
{
    //使其能在Inspector面板显示，并且可以被赋予相应值
    [Serializable]
    public class CompositeToggleGroupData
    {
    	public string key;
        //Object并非C#基础中的Object，而是 UnityEngine.Object
        public CompositeToggle toggle;
        private int[] stateValues;

        private int cacheState = 0;
        public int[] StateValues
        {
            get
            {
                if (cacheState == stateValue)
                {
                    return stateValues;
                }
                stateValues = CompositeUtil.CalculationState(stateValue, toggle.count);
                cacheState = stateValue;
                return stateValues;
            }
        }
        
        public int stateValue;
        public string type = "";
        
        public CompositeToggleGroupData(string key, CompositeToggle tog)
        {
            this.key = key;
            toggle = tog;

            stateValues = new int[tog.count];
            for (int i = 0; i < tog.count; i++)
            {
                stateValues[i] = i;
            }
        }
    }

    public enum EnumOperatorType
    {
        And = 0,
        Or = 1,
    }
    
    /// <summary>
    /// 用于管理多个CompositeToggle的状态
    /// <see cref="https://www.fairygui.com/docs/editor/controller#%E5%B1%9E%E6%80%A7%E6%8E%A7%E5%88%B6"/>
    /// </summary>
    [ExecuteInEditMode]
    [AddComponentMenu("YIUI/控制器/复合开关组 【CompositeToggleGroup】")]
    public class CompositeToggleGroup : MonoBehaviour
    {
        //用于序列化的List
        public List<CompositeToggleGroupData> data = new List<CompositeToggleGroupData>();
        //Object并非C#基础中的Object，而是 UnityEngine.Object
        private readonly Dictionary<string, CompositeToggleGroupData> dict = new Dictionary<string, CompositeToggleGroupData>();

        private Dictionary<CompositeToggle, UnityAction<CompositeToggle>> events =
            new Dictionary<CompositeToggle, UnityAction<CompositeToggle>>();

        public EnumOperatorType Operator = EnumOperatorType.Or;

        private void Awake()
        {
            Init();
        }

        public void Init()
        {
            if (data.Count == 0)
            {
                return;
            }
            dict.Clear();
            foreach (CompositeToggleGroupData toggleData in data)
            {
                if (!dict.ContainsKey(toggleData.key))
                {
                    dict.Add(toggleData.key, toggleData);
                }

                if (toggleData.toggle == null)
                {
                    continue;
                }
                toggleData.toggle.onRefreshEvent = OnRefresh;
                if (!toggleData.toggle.ReferenceExToggles.Contains(this))
                {
                    toggleData.toggle.ReferenceExToggles.Add(this);
                }        
            }
            Refresh();
        }

        public void Remove(CompositeToggle toggle)
        {
            int i;
            for (i = 0; i < data.Count; i++)
            {
                if (data[i].toggle == null)
                {
                    continue;
                }
                if (data[i].toggle == toggle)
                {
                    data[i].toggle.onRefreshEvent = null;
                    events.Remove(data[i].toggle);
                    break;
                }
            }
        }
        
        public void RemoveAndSave(CompositeToggle toggle)
        {
            Remove(toggle);
            Save();
        }

        public void Clear()
        {
            events.Clear();
            for (int i = 0; i < data.Count; i++)
            {
                if (data[i].toggle == null)
                {
                    continue;
                }
                data[i].toggle.onRefreshEvent = null;
            }
        }

        private void Save()
        {
#if UNITY_EDITOR
            UnityEditor.SerializedObject serializedObject = new UnityEditor.SerializedObject(this);
            //根据PropertyPath读取prefab文件中的数据
            var dataProperty = serializedObject.FindProperty("data");
            dataProperty.ClearArray();
            UnityEditor.EditorUtility.SetDirty(this);
            serializedObject.ApplyModifiedProperties();
            serializedObject.UpdateIfRequiredOrScript();
#endif
        }
        
        public void OnRefresh(CompositeToggle toggle)
        {
            Refresh();
        }
        
        public void RefreshActiveState()
        {
            if (data.Count == 0)
            {
                return;
            }
            
            bool[] retState = new bool[data.Count];
            for (int i = 0; i < data.Count; i++)
            {
                CompositeToggleGroupData togGroupData = data[i];
                if (togGroupData.toggle == null)
                {
                    continue;
                }
                
                CompositeToggle toggle = togGroupData.toggle;
                int[] states = togGroupData.StateValues;
                if (togGroupData.toggle != null && states != null)
                {
                    if (togGroupData.toggle.indexValue < states.Length)
                    {
                        retState[i] = states[toggle.indexValue] == 1;
                    }
                }
            }
            
            //默认赋值，与操作，默认true，或操作，默认false,方便计算
            bool isShow = Operator == EnumOperatorType.And; 
            for (int i = 0; i < retState.Length; i++)
            {
                if (Operator == EnumOperatorType.And)
                {
                    //与操作，只要有一个是结果为false，就不需要显示
                    if (retState[i] == false)
                    {
                        isShow = false;
                        break;
                    }
                }
                else
                {
                    //或操作，只要有一个结果是显示的，就需要显示
                    if (retState[i] == true)
                    {
                        isShow = true;
                        break;
                    }
                }
            }
            gameObject.SetActive(isShow);
        }

        public void RefreshActiveState(int index)
        {
            if (index >= data.Count)
            {
                return;
            }
            RefreshActiveState(data[index]);
        }

        public void RefreshActiveState(CompositeToggleGroupData togGroupData)
        {
            if (togGroupData.toggle == null)
            {
                return;
            }
            CompositeToggle toggle = togGroupData.toggle;
            if (toggle != null)
            {
                int[] states = togGroupData.StateValues;
                switch (toggle.valueType)
                {
                    case CompositeToggle.ValueType.Boolean:
                        break;
                    case CompositeToggle.ValueType.Index:
                        if (togGroupData.toggle.indexValue < states.Length)
                        {
                            gameObject.SetActive(states[toggle.indexValue]==1);
                        }
                        break;
                    case CompositeToggle.ValueType.Count:
                        break;
                    case CompositeToggle.ValueType.Flag:
                        break;
                    default:
                        break;
                }
            }
        }

        public void Refresh()
        {
            for (int i = data.Count-1; i >= 0; i--)
            {
                CompositeToggleGroupData togGroupData = data[i];
                if (togGroupData == null)
                {
                    data.RemoveAt(i);
                    continue;
                }

                if (togGroupData.toggle == null)
                {
                    data.RemoveAt(i);
                    continue;
                }
                AddListener(togGroupData);
            }

            RefreshActiveState();
        }

        public void AddListener(CompositeToggleGroupData togGroupData)
        {
            if (togGroupData.toggle == null)
            {
                return;
            }
            CompositeToggle toggle = togGroupData.toggle;
            UnityAction<CompositeToggle> changeEvent = null;
            if (events.TryGetValue(toggle, out changeEvent))
            {
                toggle.onValueChanged.RemoveListener(changeEvent);
            }
            else
            {
                changeEvent = (value) =>
                {
                    OnValueChange(value, togGroupData);
                };
                events[toggle] = changeEvent;
            }
            toggle.onValueChanged.AddListener(changeEvent);
        }

        private void OnValueChange(CompositeToggle value, CompositeToggleGroupData togGroupData)
        {
            RefreshActiveState();
        }

        public CompositeToggle Get(string key)
        {
            if (dict.TryGetValue(key, out var collectorData))
            {
                return collectorData.toggle;
            }
            return null;
        }

        public void OnDestroy()
        {
            List<UnityAction<CompositeToggle>> tmpList = new List<UnityAction<CompositeToggle>>();
            foreach (var item in events)
            {
                if (item.Value == null)
                {
                    continue;
                }
                item.Key.onValueChanged.RemoveListener(item.Value);
                tmpList.Add(item.Value);
            }
            for (int i = tmpList.Count -1; i >= 0 ; i--)
            {
                tmpList[i] = null;
            }
            tmpList.Clear();
            events.Clear();
            for (int i = 0; i < data.Count; i++)
            {
                if (data[i].toggle == null)
                {
                    continue;
                }
                data[i].toggle.ReferenceExToggles.Remove(this);
            }
        }
    }
}
