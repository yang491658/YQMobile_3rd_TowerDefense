#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ScriptEditor
{
    private static bool IsPlaying() => !EditorApplication.isPlaying;

    #region 초기화
    private static void ResetInspector(Component _comp)
    {
        Undo.RegisterCompleteObjectUndo(_comp, "Reset");
        Unsupported.SmartReset(_comp);
        EditorUtility.SetDirty(_comp);
        EditorSceneManager.MarkSceneDirty(_comp.gameObject.scene);
    }

    private static void ResetManager(Type _type)
    {
        var _objs = UnityEngine.Object.FindObjectsByType(
            _type, FindObjectsInactive.Include, FindObjectsSortMode.None
        );
        for (int i = 0; i < _objs.Length; i++)
        {
            var _comp = _objs[i] as Component;
            if (_comp != null) ResetInspector(_comp);
        }
    }

    private static bool ResetComponents(GameObject _root, Type[] _types)
    {
        bool _changed = false;

        for (int t = 0; t < _types.Length; t++)
        {
            Type _type = _types[t];
            if (_type == null) continue;

            Component[] _comps = _root.GetComponentsInChildren(_type, true);
            if (_comps.Length == 0) continue;

            _changed = true;

            for (int i = 0; i < _comps.Length; i++)
            {
                Component _comp = _comps[i];
                if (_comp == null) continue;

                Unsupported.SmartReset(_comp);
                EditorUtility.SetDirty(_comp);
            }
        }

        return _changed;
    }

    private static void ResetPrefabs(Type[] _types)
    {
        string[] _searchFolders = { "Assets/Prefabs" };
        string[] _guids = AssetDatabase.FindAssets("t:Prefab", _searchFolders);

        var _changedPaths = new List<string>(_guids.Length);

        var _stage = PrefabStageUtility.GetCurrentPrefabStage();
        string _stagePath = (_stage != null) ? _stage.assetPath : null;

        AssetDatabase.StartAssetEditing();
        try
        {
            for (int i = 0; i < _guids.Length; i++)
            {
                string _path = AssetDatabase.GUIDToAssetPath(_guids[i]);
                if (string.IsNullOrEmpty(_path)) continue;

                if (_stage != null && _stagePath == _path)
                {
                    GameObject _root = _stage.prefabContentsRoot;
                    if (_root == null) continue;

                    if (!ResetComponents(_root, _types)) continue;

                    EditorUtility.SetDirty(_root);
                    EditorSceneManager.MarkSceneDirty(_stage.scene);

                    bool _success;
                    PrefabUtility.SaveAsPrefabAsset(_root, _path, out _success);
                    if (_success) _changedPaths.Add(_path);

                    continue;
                }

                GameObject _prefabRoot = PrefabUtility.LoadPrefabContents(_path);
                if (_prefabRoot == null) continue;

                try
                {
                    if (!ResetComponents(_prefabRoot, _types)) continue;

                    EditorUtility.SetDirty(_prefabRoot);

                    bool _success;
                    PrefabUtility.SaveAsPrefabAsset(_prefabRoot, _path, out _success);
                    if (_success) _changedPaths.Add(_path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(_prefabRoot);
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        for (int i = 0; i < _changedPaths.Count; i++)
        {
            string _path = _changedPaths[i];
            AssetDatabase.ImportAsset(
                _path,
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport
            );
        }

        AssetDatabase.Refresh();
        ActiveEditorTracker.sharedTracker.ForceRebuild();
        EditorApplication.RepaintProjectWindow();
    }

    [MenuItem("Tools/스크립트 초기화", true)]
    private static bool ResetScripts_Validate() => IsPlaying();
    [MenuItem("Tools/스크립트 초기화", false, 1)]
    private static void ResetScripts()
    {
        var _managers = new Type[]
        {
            typeof(AutoCamera),
            typeof(AutoUICanvas),
            typeof(AutoBackground),

            typeof(GameManager),
            typeof(SoundManager),
            typeof(DataManager),
            typeof(EntityManager),
            typeof(PoolManager),
            typeof(HandleManager),
            typeof(UIManager),
            typeof(ADManager),
#if TEST_Manager
            typeof(TestManager),
#endif
            typeof(TowerControl),
            typeof(TowerStore),
            typeof(TowerSlot),
            typeof(MonsterWave),
        };
        for (int i = 0; i < _managers.Length; i++) ResetManager(_managers[i]);

        var _prefabs = new Type[]
        {
            typeof(Tower),
            typeof(TowerBuff),
            typeof(Boss),
            typeof(Monster),
            typeof(MonsterDebuff),
            typeof(Bullet),
            typeof(Summon),
            typeof(ViewEffect),
            typeof(TextEffect),
        };
        ResetPrefabs(_prefabs);
    }
    #endregion

    #region 켜기/끄기
    private static T FindSingle<T>() where T : Component
        => UnityEngine.Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);

    private static bool AnyActive<T>() where T : Component
    {
        var c = FindSingle<T>();
        if (c == null) return false;
        var go = c.gameObject;
        return (go != null) && go.activeSelf;
    }

    private static void SetActive<T>(bool _on, string _onLabel, string _offLabel) where T : Component
    {
        var c = FindSingle<T>();
        if (c == null) return;
        var go = c.gameObject;
        if (go == null) return;

        Undo.RegisterFullObjectHierarchyUndo(go, _on ? _onLabel : _offLabel);
        go.SetActive(_on);
        EditorUtility.SetDirty(go);
        EditorSceneManager.MarkSceneDirty(go.scene);
    }

    private static void SetBtnActive(string _name, bool _on, string _undoOn, string _undoOff)
    {
        var _objs = UnityEngine.Object.FindObjectsByType<GameObject>(
            FindObjectsInactive.Include, FindObjectsSortMode.None
        );

        for (int i = 0; i < _objs.Length; i++)
        {
            var _obj = _objs[i];
            if (_obj == null || _obj.name != _name) continue;

            Undo.RegisterFullObjectHierarchyUndo(_obj, _on ? _undoOn : _undoOff);
            _obj.SetActive(_on);
            EditorUtility.SetDirty(_obj);
            EditorSceneManager.MarkSceneDirty(_obj.scene);
        }
    }

    private const string TestDefineSymbol = "TEST_Manager";

    private static void SetTestDefine(BuildTargetGroup _group, bool _on)
    {
        var named = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(_group);
        string symbols = PlayerSettings.GetScriptingDefineSymbols(named);

        var list = new List<string>(
            symbols.Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
        );

        bool contains = list.Contains(TestDefineSymbol);
        if (_on)
        {
            if (contains) return;
            list.Add(TestDefineSymbol);
        }
        else
        {
            if (!contains) return;
            list.Remove(TestDefineSymbol);
        }

        PlayerSettings.SetScriptingDefineSymbols(named, string.Join(";", list));
    }

    private static void SetTestActive(bool _on)
    {
#if TEST_Manager
        SetActive<TestManager>(_on, "테스트 켜기", "테스트 끄기");
#endif
        SetBtnActive("TestBtn", _on, "테스트 버튼 켜기", "테스트 버튼 끄기");
    }

    private static void SetQuitActive(bool _on)
        => SetBtnActive("QuitBtn", _on, "종료 버튼 켜기", "종료 버튼 끄기");

    private static bool AnyQuitOn()
    {
        var objs = UnityEngine.Object.FindObjectsByType<GameObject>(
            FindObjectsInactive.Include, FindObjectsSortMode.None
        );

        for (int i = 0; i < objs.Length; i++)
        {
            var obj = objs[i];
            if (obj == null || obj.name != "QuitBtn") continue;
            if (obj.activeSelf) return true;
        }
        return false;
    }

    private static bool AnyQuitOff()
    {
        var objs = UnityEngine.Object.FindObjectsByType<GameObject>(
            FindObjectsInactive.Include, FindObjectsSortMode.None
        );

        for (int i = 0; i < objs.Length; i++)
        {
            var obj = objs[i];
            if (obj == null || obj.name != "QuitBtn") continue;
            if (!obj.activeSelf) return true;
        }
        return false;
    }
    #endregion

    #region 빌드
    private static void SetWindowsBuild()
    {
        var group = BuildTargetGroup.Standalone;
        var target = BuildTarget.StandaloneWindows64;

        if (EditorUserBuildSettings.activeBuildTarget != target)
            EditorUserBuildSettings.SwitchActiveBuildTargetAsync(group, target);
    }

    private static void SetAndroidBuild(bool _useAppBundle)
    {
        var group = BuildTargetGroup.Android;
        var target = BuildTarget.Android;

        if (EditorUserBuildSettings.activeBuildTarget != target)
            EditorUserBuildSettings.SwitchActiveBuildTargetAsync(group, target);

        EditorUserBuildSettings.buildAppBundle = _useAppBundle;
    }

    private static void SetWebBuild()
    {
        var group = BuildTargetGroup.WebGL;
        var target = BuildTarget.WebGL;

        if (EditorUserBuildSettings.activeBuildTarget != target)
            EditorUserBuildSettings.SwitchActiveBuildTargetAsync(group, target);
    }

    private static void PrepareTest()
    {
        SetWindowsBuild();

        SetActive<UIManager>(true, "UI 켜기", "UI 끄기");
        SetActive<ADManager>(false, "광고 켜기", "광고 끄기");

        SetTestDefine(BuildTargetGroup.Standalone, true);
        SetTestDefine(BuildTargetGroup.Android, false);
        SetTestDefine(BuildTargetGroup.WebGL, false);
        SetTestActive(true);
        SetQuitActive(true);

        FindSingle<UIManager>()?.SetSkipCountdown(true);
    }


    private static void PrepareAndroid()
    {
        SetAndroidBuild(true);

        SetActive<UIManager>(true, "UI 켜기", "UI 끄기");
        SetActive<ADManager>(true, "광고 켜기", "광고 끄기");

        SetTestDefine(BuildTargetGroup.Standalone, false);
        SetTestDefine(BuildTargetGroup.Android, false);
        SetTestDefine(BuildTargetGroup.WebGL, false);
        SetTestActive(false);
        SetQuitActive(true);

        FindSingle<UIManager>()?.SetSkipCountdown(false);
    }

    private static void PrepareWebGL()
    {
        SetWebBuild();

        SetActive<UIManager>(true, "UI 켜기", "UI 끄기");
        SetActive<ADManager>(false, "광고 켜기", "광고 끄기");

        SetTestDefine(BuildTargetGroup.Standalone, false);
        SetTestDefine(BuildTargetGroup.Android, false);
        SetTestDefine(BuildTargetGroup.WebGL, false);
        SetTestActive(false);
        SetQuitActive(false);

        FindSingle<UIManager>()?.SetSkipCountdown(false);
    }

    [MenuItem("Tools/Test 빌드 준비", true)]
    private static bool TestBuildValidate() => IsPlaying();
    [MenuItem("Tools/Test 빌드 준비", false, 101)]
    private static void TestBuild() => PrepareTest();

    [MenuItem("Tools/Android 빌드 준비", true)]
    private static bool AndroidBuildValidate() => IsPlaying();
    [MenuItem("Tools/Android 빌드 준비", false, 102)]
    private static void AndroidBuild() => PrepareAndroid();

    [MenuItem("Tools/WebGL 빌드 준비", true)]
    private static bool WebGLBuildValidate() => IsPlaying();
    [MenuItem("Tools/WebGL 빌드 준비", false, 103)]
    private static void WebGLBuild() => PrepareWebGL();
    #endregion

    #region UI
    [MenuItem("Tools/UI 켜기", true)]
    private static bool UIsOnValidate() => IsPlaying() && !AnyActive<UIManager>();
    [MenuItem("Tools/UI 켜기", false, 201)]
    private static void UIsOn() => SetActive<UIManager>(true, "UI 켜기", "UI 끄기");

    [MenuItem("Tools/UI 끄기", true)]
    private static bool UIsOffValidate() => IsPlaying() && AnyActive<UIManager>();
    [MenuItem("Tools/UI 끄기", false, 202)]
    private static void UIsOff() => SetActive<UIManager>(false, "UI 켜기", "UI 끄기");
    #endregion

    #region 광고
    [MenuItem("Tools/광고 켜기", true)]
    private static bool ADsOnValidate() => IsPlaying() && !AnyActive<ADManager>();
    [MenuItem("Tools/광고 켜기", false, 301)]
    private static void ADsOn() => SetActive<ADManager>(true, "광고 켜기", "광고 끄기");

    [MenuItem("Tools/광고 끄기", true)]
    private static bool ADsOff_Validate() => IsPlaying() && AnyActive<ADManager>();
    [MenuItem("Tools/광고 끄기", false, 302)]
    private static void ADsOff() => SetActive<ADManager>(false, "광고 켜기", "광고 끄기");
    #endregion

#if TEST_Manager
    #region 테스트
    [MenuItem("Tools/테스트 켜기", true)]
    private static bool TestsOnValidate() => IsPlaying() && !AnyActive<TestManager>();
    [MenuItem("Tools/테스트 켜기", false, 401)]
    private static void TestsOn() => SetTestActive(true);

    [MenuItem("Tools/테스트 끄기", true)]
    private static bool TestsOffValidate() => IsPlaying() && AnyActive<TestManager>();
    [MenuItem("Tools/테스트 끄기", false, 402)]
    private static void TestsOff() => SetTestActive(false);
    #endregion
#endif

    #region 종료
    [MenuItem("Tools/종료 버튼 켜기", true)]
    private static bool QuitBtnsOnValidate() => IsPlaying() && !AnyQuitOn() && AnyQuitOff();
    [MenuItem("Tools/종료 버튼 켜기", false, 501)]
    private static void QuitBtnsOn() => SetQuitActive(true);

    [MenuItem("Tools/종료 버튼 끄기", true)]
    private static bool QuitBtnsOffValidate() => IsPlaying() && AnyQuitOn();
    [MenuItem("Tools/종료 버튼 끄기", false, 502)]
    private static void QuitBtnsOff() => SetQuitActive(false);
    #endregion
}
#endif
