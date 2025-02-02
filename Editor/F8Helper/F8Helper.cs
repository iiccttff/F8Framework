﻿using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace F8Framework.Core.Editor
{
    public class F8Helper
    {
        [MenuItem("开发工具/F8Run _F8")]
        public static void F8Run()
        {
            GenerateCopyHotUpdateDll();
            LoadAllExcelData();
            BuildAssetBundles();
            
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }
        
        [MenuItem("开发工具/生成并复制热更新Dll-F8")]
        public static void GenerateCopyHotUpdateDll()
        {
            // HybridCLR.Editor.Commands.PrebuildCommand.GenerateAll();
            // FileTools.SafeClearDir(Application.dataPath + "/AssetBundles/Code");
            // FileTools.CheckDirAndCreateWhenNeeded(Application.dataPath + "/AssetBundles/Code");
            // List<string> hotUpdateDll = new List<string>()
            // {
            //     "HotUpdate", // 自行添加需要热更的程序集
            // };
            // foreach (var dll in hotUpdateDll)
            // {
            //     FileTools.SafeCopyFile(
            //         HybridCLR.Editor.SettingsUtil.GetHotUpdateDllsOutputDirByTarget(EditorUserBuildSettings.activeBuildTarget) + "/" + dll + ".dll",
            //         Application.dataPath + "/AssetBundles/Code/" + dll + ".bytes");
            //     LogF8.LogAsset("生成并复制热更新dll：" + dll);
            // }
            // AssetDatabase.Refresh();
        }
        
        [MenuItem("开发工具/Excel导表-F8")]
        public static void LoadAllExcelData()
        {
            ExcelDataTool.LoadAllExcelData();
        }
        
        [MenuItem("开发工具/打包AssetBundles目录资源-F8")]
        public static void BuildAssetBundles()
        {
            ABBuildTool.BuildAllAB();
        }

        // 加载时初始化方法，统一管理调用顺序
        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethods()
        {
            // 在Project面板按空格键相当于Show In Explorer
            EditorApplication.projectWindowItemOnGUI += ProjectWindowItemOnGUI;
            
            ExcelUpdater.Init();

            LocalizationEditorSettings.LoadEditorSettings();

            TMPIntegrationSwitcher.EnsureIntegrationState();
        }
        
        private static void ProjectWindowItemOnGUI(string guid, Rect selectionRect)
        {
            if (Event.current.type == EventType.KeyDown
                && Event.current.keyCode == KeyCode.Space
                && selectionRect.Contains(Event.current.mousePosition))
            {
                string strPath = AssetDatabase.GUIDToAssetPath(guid);
                
                EditorUtility.RevealInFinder(strPath);
                
                UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(strPath);
                if (obj != null)
                {
                    EditorGUIUtility.PingObject(obj);
                }
                Event.current.Use();
            }
        }
    }
}