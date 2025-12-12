#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ValueType))]
public class ValueTypeDrawer : PropertyDrawer
{
    private static readonly ValueType[] values = (ValueType[])System.Enum.GetValues(typeof(ValueType));
    private static readonly Dictionary<ValueType, string> displayNameCache = new Dictionary<ValueType, string>();

    public override void OnGUI(Rect _position, SerializedProperty _property, GUIContent _label)
    {
        EditorGUI.BeginProperty(_position, _label, _property);

        ValueType current = (ValueType)_property.intValue;
        string currentLabel = GetDisplayName(current);

        if (EditorGUI.DropdownButton(_position, new GUIContent(currentLabel), FocusType.Keyboard))
        {
            GenericMenu menu = new GenericMenu();

            for (int i = 0; i < values.Length; i++)
            {
                ValueType v = values[i];
                int code = (int)v;

                string category = GetCategoryName(code);
                if (string.IsNullOrEmpty(category))
                    continue;

                bool on = _property.intValue == code;
                string name = GetDisplayName(v);
                string path = category + "/" + name;

                menu.AddItem(
                    new GUIContent(path),
                    on,
                    OnSelect,
                    new MenuData(_property, code)
                );
            }

            menu.DropDown(_position);
        }

        EditorGUI.EndProperty();
    }

    private string GetCategoryName(int _code)
    {
        if (_code >= 100 && _code < 200) return "수치";
        if (_code >= 200 && _code < 300) return "비율";
        if (_code >= 300 && _code < 400) return "공간";
        if (_code >= 400 && _code < 500) return "시간";
        if (_code >= 500 && _code < 600) return "스택";
        if (_code >= 600 && _code < 700) return "자원";
        return string.Empty;
    }

    private sealed class MenuData
    {
        public SerializedObject serializedObject;
        public string propertyPath;
        public int value;

        public MenuData(SerializedProperty _property, int _value)
        {
            serializedObject = _property.serializedObject;
            propertyPath = _property.propertyPath;
            value = _value;
        }
    }

    private void OnSelect(object _userData)
    {
        MenuData data = (MenuData)_userData;

        data.serializedObject.Update();
        SerializedProperty prop = data.serializedObject.FindProperty(data.propertyPath);
        prop.intValue = data.value;
        data.serializedObject.ApplyModifiedProperties();
    }

    private string GetDisplayName(ValueType _value)
    {
        if (displayNameCache.TryGetValue(_value, out string cached))
            return cached;

        System.Type type = typeof(ValueType);
        System.Reflection.FieldInfo fi = type.GetField(_value.ToString());
        if (fi != null)
        {
            InspectorNameAttribute[] attrs =
                (InspectorNameAttribute[])fi.GetCustomAttributes(typeof(InspectorNameAttribute), false);
            if (attrs != null && attrs.Length > 0)
            {
                string name = attrs[0].displayName;
                displayNameCache[_value] = name;
                return name;
            }
        }

        string fallback = _value.ToString();
        displayNameCache[_value] = fallback;
        return fallback;
    }
}
#endif
