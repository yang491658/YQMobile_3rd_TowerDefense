#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ValueType))]
public class ValueTypeDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        ValueType current = (ValueType)property.intValue;
        string currentLabel = GetDisplayName(current);

        if (EditorGUI.DropdownButton(position, new GUIContent(currentLabel), FocusType.Keyboard))
        {
            GenericMenu menu = new GenericMenu();

            foreach (ValueType v in System.Enum.GetValues(typeof(ValueType)))
            {
                int code = (int)v;
                string category = GetCategoryName(code);
                if (string.IsNullOrEmpty(category))
                {
                    continue;
                }

                bool on = property.intValue == code;
                string name = GetDisplayName(v);
                string path = category + "/" + name;

                menu.AddItem(
                    new GUIContent(path),
                    on,
                    OnSelect,
                    new MenuData(property, code)
                );
            }

            menu.DropDown(position);
        }

        EditorGUI.EndProperty();
    }

    private string GetCategoryName(int code)
    {
        if (code >= 100 && code < 200) return "수치";
        if (code >= 200 && code < 300) return "비율";
        if (code >= 300 && code < 400) return "공간";
        if (code >= 400 && code < 500) return "시간";
        if (code >= 500 && code < 600) return "스택";
        if (code >= 600 && code < 700) return "자원";
        return string.Empty;
    }

    private sealed class MenuData
    {
        public SerializedProperty property;
        public int value;

        public MenuData(SerializedProperty _property, int _value)
        {
            property = _property;
            value = _value;
        }
    }

    private void OnSelect(object userData)
    {
        MenuData data = (MenuData)userData;
        data.property.serializedObject.Update();
        data.property.intValue = data.value;
        data.property.serializedObject.ApplyModifiedProperties();
    }

    private string GetDisplayName(ValueType value)
    {
        System.Type type = typeof(ValueType);
        System.Reflection.FieldInfo fi = type.GetField(value.ToString());
        if (fi != null)
        {
            InspectorNameAttribute[] attrs =
                (InspectorNameAttribute[])fi.GetCustomAttributes(typeof(InspectorNameAttribute), false);
            if (attrs != null && attrs.Length > 0)
            {
                return attrs[0].displayName;
            }
        }

        return value.ToString();
    }
}
#endif
