using System;
using System.Collections.Generic;
using System.IO;
using F8Framework.AssetMap;
using UnityEngine;
using Object = UnityEngine.Object;

namespace F8Framework.Core
{
    //异步加载完成的回调
    public delegate void OnAssetObject<T>(T obj)
        where T : Object;
    
    public class AssetManager : Singleton<AssetManager>
    {
        //资产信息
        public class AssetInfo
        {
            //目标资产类型
            public readonly AssetTypeEnum AssetType;
            
            //直接资产请求路径相对路径，Assets开头的
            public readonly string[] AssetPath;
            
            //直接资产捆绑请求路径（仅适用于资产捆绑类型），完全路径
            public readonly string AssetBundlePath;
            
            //AB名
            public readonly string AbName;
            
            //AB资产路径不包含AB名
            public readonly string AssetBundlePathWithoutAb;
            
            public AssetInfo(
                AssetTypeEnum assetType,
                string[] assetPath,
                string assetBundlePathWithoutAb,
                string abName)
            {
                AssetType = assetType;
                AssetPath = assetPath;
                AssetBundlePath = assetBundlePathWithoutAb + abName;
                AbName = abName;
                AssetBundlePathWithoutAb = assetBundlePathWithoutAb;
            }

            //如果信息合法，则该值为真
            public bool IsLegal
            {
                get
                {
                    if (AssetType == AssetTypeEnum.NONE)
                        return false;

                    if (AssetType == AssetTypeEnum.RESOURCE &&
                        AssetPath == null)
                        return false;

                    if (AssetType == AssetTypeEnum.ASSET_BUNDLE &&
                        (AssetPath == null || AssetBundlePath == null))
                        return false;

                    return true;
                }
            }
        }
             //资产访问标志
            [System.Flags]
            public enum AssetAccessMode
            {
                NONE = 0b1,
                UNKNOWN = 0b10,
                RESOURCE = 0b100,
                ASSET_BUNDLE = 0b1000,
                REMOTE_ASSET_BUNDLE = 0b10000
            }

            //资产类型
            public enum AssetTypeEnum
            {
                NONE,
                RESOURCE,
                ASSET_BUNDLE
            }
            // 是否采用编辑器模式
            private bool _isEditorMode = false;
            public bool IsEditorMode
            {
                get
                {
#if UNITY_EDITOR
                    return _isEditorMode;
#else
                    return false;
#endif
                }
                set
                {
                    _isEditorMode = value;
                }
            }
            /// <summary>
            /// 根据提供的资产路径和访问选项推断资产类型。
            /// </summary>
            /// <param name="assetName">资产路径字符串。</param>
            /// <param name="accessMode">访问模式。</param>
            /// <returns>资产信息。</returns>
            public AssetInfo GetAssetInfo(string assetName,
                AssetAccessMode accessMode = AssetAccessMode.UNKNOWN)
            {
                if (accessMode.HasFlag(AssetAccessMode.RESOURCE))
                {
                    return GetAssetInfoFromResource(assetName);
                }
                else if (accessMode.HasFlag(AssetAccessMode.ASSET_BUNDLE))
                {
                    return GetAssetInfoFromAssetBundle(assetName);
                }
                else if (accessMode.HasFlag(AssetAccessMode.UNKNOWN))
                {
                    AssetInfo r = GetAssetInfoFromAssetBundle(assetName);
                    if (r != null && r.IsLegal)
                        return r;
                    else
                        return GetAssetInfoFromResource(assetName);
                }
                else if (accessMode.HasFlag(AssetAccessMode.REMOTE_ASSET_BUNDLE))
                {
                    AssetInfo r = GetAssetInfoFromAssetBundle(assetName, true);
                    if (r != null && r.IsLegal)
                        return r;
                    else
                        return GetAssetInfoFromAssetBundle(assetName);
                }
                return null;
            }

            /// <summary>
            /// 同步加载资源对象。
            /// </summary>
            /// <typeparam name="T">目标资产类型。</typeparam>
            /// <param name="assetName">资产路径字符串。</param>
            /// <param name="mode">访问模式。</param>
            /// <returns>加载的资产对象。</returns>
            public T Load<T>(
                string assetName,
                AssetAccessMode mode = AssetAccessMode.UNKNOWN)
                where T : Object
            {
                
                AssetInfo info = GetAssetInfo(assetName, mode);
                if (info == null || !info.IsLegal)
                    return null;
                
                if (info.AssetType == AssetTypeEnum.RESOURCE)
                {
                    T o = ResourcesManager.Instance.GetResouceObject<T>(info.AssetPath[0]);
                    if (o != null)
                    {
                        return o;
                    }
                      
                    return ResourcesManager.Instance.Load<T>(info.AssetPath[0]);
                }
                else if (info.AssetType == AssetTypeEnum.ASSET_BUNDLE)
                {
#if UNITY_EDITOR
                    if (_isEditorMode)
                    {
                        var assetPaths = UnityEditor.AssetDatabase.GetAssetPathsFromAssetBundleAndAssetName(info.AbName, assetName);
                        return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(assetPaths[0]);
                    }
#endif
                    AssetBundleLoader ab = AssetBundleManager.Instance.GetAssetBundleLoader(info.AssetBundlePath);
                    if (ab == null ||
                        ab.AssetBundleContent == null)
                    {
                        AssetBundleManager.Instance.Load(assetName, info);
                        ab = AssetBundleManager.Instance.GetAssetBundleLoader(info.AssetBundlePath);
                    }
                
                    T o = AssetBundleManager.Instance.GetAssetObject<T>(info.AssetPath[0]);
                    if (o != null)
                    {
                        return o;
                    }
                    
                    ab.Expand();
                    return AssetBundleManager.Instance.GetAssetObject<T>(info.AssetPath[0]);
                }

                return null;
            }
            
            /// <summary>
            /// 同步加载资源文件夹。
            /// </summary>
            /// <param name="assetName">资产路径字符串。</param>
            /// <param name="mode">访问模式。</param>
            public void LoadDir(
                string assetName,
                AssetAccessMode mode = AssetAccessMode.UNKNOWN)
            {
                AssetInfo info = GetAssetInfo(assetName, mode);
                if (info == null || !info.IsLegal)
                    return;
                
                if (info.AssetType == AssetTypeEnum.RESOURCE)
                {
                    LogF8.LogAsset("Resources不支持加载文件夹功能");
                }
                else if (info.AssetType == AssetTypeEnum.ASSET_BUNDLE)
                {
#if UNITY_EDITOR
                    if (_isEditorMode)
                    {
                        return;
                    }
#endif
                    foreach (var assetPath in info.AssetPath)
                    {
                        string abName = Path.ChangeExtension(assetPath, null).Replace(URLSetting.AssetBundlesPath, "").ToLower();
                        AssetBundleLoader ab = AssetBundleManager.Instance.GetAssetBundleLoader(info.AssetBundlePathWithoutAb + abName);
                        if (ab == null || ab.AssetBundleContent == null)
                        {
                            AssetBundleManager.Instance.Load(Path.GetFileNameWithoutExtension(assetPath), 
                                new AssetInfo(info.AssetType, new []{assetPath}, info.AssetBundlePathWithoutAb, abName));
                        }
                    }
                }
            }
            
            /// <summary>
            /// 同步加载资源对象。
            /// </summary>
            /// <param name="assetName">资产路径字符串。</param>
            /// <param name="assetType">目标资产类型。</param>
            /// <param name="mode">访问模式。</param>
            /// <returns>加载的资产对象。</returns>
            public Object Load(
                string assetName,
                System.Type assetType,
                AssetAccessMode mode = AssetAccessMode.UNKNOWN)
            {
                AssetInfo info = GetAssetInfo(assetName, mode);
                if (info == null || !info.IsLegal)
                    return null;

                if (info.AssetType == AssetTypeEnum.RESOURCE)
                {
                    Object o = ResourcesManager.Instance.GetResouceObject(info.AssetPath[0], assetType);
                    if (o != null)
                    {
                        return o;
                    }

                    return ResourcesManager.Instance.Load(info.AssetPath[0], assetType);
                }
                else if (info.AssetType == AssetTypeEnum.ASSET_BUNDLE)
                {
#if UNITY_EDITOR
                    if (_isEditorMode)
                    {
                        var assetPaths = UnityEditor.AssetDatabase.GetAssetPathsFromAssetBundleAndAssetName(info.AbName, assetName);
                        return UnityEditor.AssetDatabase.LoadAssetAtPath(assetPaths[0], assetType);
                    }
#endif
                    AssetBundleLoader ab = AssetBundleManager.Instance.GetAssetBundleLoader(info.AssetBundlePath);
                    if (ab == null ||
                        ab.AssetBundleContent == null)
                    {
                        AssetBundleManager.Instance.Load(assetName, info);
                        ab = AssetBundleManager.Instance.GetAssetBundleLoader(info.AssetBundlePath);
                    }
            
                    Object o = AssetBundleManager.Instance.GetAssetObject(info.AssetPath[0], assetType);
                    if (o != null)
                    {
                        return o;
                    }
                
                    ab.Expand();
                    return AssetBundleManager.Instance.GetAssetObject(info.AssetPath[0], assetType);
                }

                return null;
            }

            /// <summary>
            /// 同步加载资源对象。
            /// </summary>
            /// <param name="assetName">资产路径字符串。</param>
            /// <param name="mode">访问模式。</param>
            /// <returns>加载的资产对象。</returns>
            public Object Load(
                string assetName,
                AssetAccessMode mode = AssetAccessMode.UNKNOWN)
            {
                AssetInfo info = GetAssetInfo(assetName, mode);
                if (info == null || !info.IsLegal)
                    return null;

                if (info.AssetType == AssetTypeEnum.RESOURCE)
                {
                    Object o = ResourcesManager.Instance.GetResouceObject(info.AssetPath[0]);
                    if (o != null)
                    {
                        return o;
                    }

                    return ResourcesManager.Instance.Load(info.AssetPath[0]);
                }
                else if (info.AssetType == AssetTypeEnum.ASSET_BUNDLE)
                {
#if UNITY_EDITOR
                    if (_isEditorMode)
                    {
                        var assetPaths = UnityEditor.AssetDatabase.GetAssetPathsFromAssetBundleAndAssetName(info.AbName, assetName);
                        return UnityEditor.AssetDatabase.LoadAssetAtPath<Object>(assetPaths[0]);
                    }
#endif
                    AssetBundleLoader ab = AssetBundleManager.Instance.GetAssetBundleLoader(info.AssetBundlePath);
                    if (ab == null ||
                        ab.AssetBundleContent == null)
                    {
                        AssetBundleManager.Instance.Load(assetName, info);
                        ab = AssetBundleManager.Instance.GetAssetBundleLoader(info.AssetBundlePath);
                    }
            
                    Object o = AssetBundleManager.Instance.GetAssetObject(info.AssetPath[0]);
                    if (o != null)
                    {
                        return o;
                    }
                
                    ab.Expand();
                    return AssetBundleManager.Instance.GetAssetObject(info.AssetPath[0]);
                }

                return null;
            }
            
            /// <summary>
            /// 异步加载资产对象。
            /// </summary>
            /// <typeparam name="T">目标资产类型。</typeparam>
            /// <param name="assetName">资产路径字符串。</param>
            /// <param name="callback">异步加载完成时的回调函数。</param>
            /// <param name="mode">访问模式。</param>
            public void LoadAsync<T>(
                string assetName,
                OnAssetObject<T> callback = null,
                AssetAccessMode mode = AssetAccessMode.UNKNOWN)
                where T : Object
            {
                AssetInfo info = GetAssetInfo(assetName, mode);
                if (info == null || !info.IsLegal)
                {
                    End();
                    return;
                }

                if (info.AssetType == AssetTypeEnum.RESOURCE)
                {
                    T o = ResourcesManager.Instance.GetResouceObject<T>(info.AssetPath[0]);
                    if (o != null)
                    {
                        End(o);
                        return;
                    }
                    ResourcesManager.Instance.LoadAsync<T>(info.AssetPath[0], callback);
                }
                else if (info.AssetType == AssetTypeEnum.ASSET_BUNDLE)
                {
#if UNITY_EDITOR
                    if (_isEditorMode)
                    {
                        var assetPaths = UnityEditor.AssetDatabase.GetAssetPathsFromAssetBundleAndAssetName(info.AbName, assetName);
                        T o = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(assetPaths[0]);
                        End(o);
                        return;
                    }
#endif
                    AssetBundleLoader ab = AssetBundleManager.Instance.GetAssetBundleLoader(info.AssetBundlePath);
                    if (ab == null || ab.AssetBundleContent == null || ab.GetDependentNamesLoadFinished() < ab.AddDependentNames())
                    {
                        AssetBundleManager.Instance.LoadAsync(assetName, info, (b) => {
                            End(AssetBundleManager.Instance.GetAssetObject<T>(info.AssetPath[0]));
                        });
                        return;
                    }
                    else
                    {
                        T o = AssetBundleManager.Instance.GetAssetObject<T>(info.AssetPath[0]);
                        if (o != null)
                        {
                            End(o);
                            return;
                        }
                        
                        ab.Expand();
                        End(AssetBundleManager.Instance.GetAssetObject<T>(info.AssetPath[0]));
                    }
                }

                void End(T o = null)
                {
                    callback?.Invoke(o);
                }
            }
                        
            /// <summary>
            /// 异步加载资产文件夹。
            /// </summary>
            /// <param name="assetName">资产路径字符串。</param>
            /// <param name="callback">异步加载完成时的回调函数。</param>
            /// <param name="mode">访问模式。</param>
            public void LoadDirAsync(
                string assetName,
                Action callback = null,
                AssetAccessMode mode = AssetAccessMode.UNKNOWN)
            {
                AssetInfo info = GetAssetInfo(assetName, mode);
                if (info == null || !info.IsLegal)
                {
                    End();
                    return;
                }

                if (info.AssetType == AssetTypeEnum.RESOURCE)
                {
                    LogF8.LogAsset("Resources不支持加载文件夹功能");
                }
                else if (info.AssetType == AssetTypeEnum.ASSET_BUNDLE)
                {
#if UNITY_EDITOR
                    if (_isEditorMode)
                    {
                        End();
                        return;
                    }
#endif
                    int assetCount = 0;
                    foreach (var assetPath in info.AssetPath)
                    {
                        string abName = Path.ChangeExtension(assetPath, null).Replace(URLSetting.AssetBundlesPath, "").ToLower();
                        AssetBundleLoader ab = AssetBundleManager.Instance.GetAssetBundleLoader(info.AssetBundlePathWithoutAb + abName);
                        if (ab == null || ab.AssetBundleContent == null || ab.GetDependentNamesLoadFinished() < ab.AddDependentNames())
                        {
                            AssetBundleManager.Instance.LoadAsync(Path.GetFileNameWithoutExtension(assetPath), 
                                new AssetInfo(info.AssetType, new []{assetPath}, info.AssetBundlePathWithoutAb, abName), (b) =>
                            {
                                if (++assetCount >= info.AssetPath.Length)
                                {
                                    End();
                                }
                            });
                        }
                        else
                        {
                            Object o = AssetBundleManager.Instance.GetAssetObject(assetPath);
                            if (o != null)
                            {
                                if (++assetCount >= info.AssetPath.Length)
                                {
                                    End();
                                }
                                continue;
                            }
                            
                            ab.Expand();
                            if (++assetCount >= info.AssetPath.Length)
                            {
                                End();
                            }
                        }
                    }
                }

                void End()
                {
                    callback?.Invoke();
                }
            }
            
            /// <summary>
            /// 异步加载资产对象。
            /// </summary>
            /// <param name="assetName">资产路径字符串。</param>
            /// <param name="assetType">目标资产类型。</param>
            /// <param name="callback">异步加载完成时的回调函数。</param>
            /// <param name="mode">访问模式。</param>
            public void LoadAsync(
                string assetName,
                System.Type assetType,
                OnAssetObject<Object> callback = null,
                AssetAccessMode mode = AssetAccessMode.UNKNOWN)
            {
                AssetInfo info = GetAssetInfo(assetName, mode);
                if (info == null || !info.IsLegal)
                {
                    End();
                    return;
                }

                if (info.AssetType == AssetTypeEnum.RESOURCE)
                {
                    Object o = ResourcesManager.Instance.GetResouceObject(info.AssetPath[0], assetType);
                    if (o != null)
                    {
                        End(o);
                        return;
                    }
                    ResourcesManager.Instance.LoadAsync(info.AssetPath[0], assetType, callback);
                }
                else if (info.AssetType == AssetTypeEnum.ASSET_BUNDLE)
                {
#if UNITY_EDITOR
                    if (_isEditorMode)
                    {
                        var assetPaths = UnityEditor.AssetDatabase.GetAssetPathsFromAssetBundleAndAssetName(info.AbName, assetName);
                        Object o = UnityEditor.AssetDatabase.LoadAssetAtPath(assetPaths[0], assetType);
                        End(o);
                        return;
                    }
#endif
                    AssetBundleLoader ab = AssetBundleManager.Instance.GetAssetBundleLoader(info.AssetBundlePath);
                    if (ab == null ||
                        ab.AssetBundleContent == null)
                    {
                        AssetBundleManager.Instance.LoadAsync(assetName, info, (b) => {
                            End(AssetBundleManager.Instance.GetAssetObject(info.AssetPath[0], assetType));
                        });
                        return;
                    }
                    else
                    {
                        Object o = AssetBundleManager.Instance.GetAssetObject(info.AssetPath[0], assetType);
                        if (o != null)
                        {
                            End(o);
                            return;
                        }
            
                        ab.Expand();
                        End(AssetBundleManager.Instance.GetAssetObject(info.AssetPath[0], assetType));
                    }
                }

                void End(Object o = null)
                {
                    callback?.Invoke(o);
                }
            }
            
            /// <summary>
            /// 异步加载资产对象。
            /// </summary>
            /// <param name="assetName">资产路径字符串。</param>
            /// <param name="callback">异步加载完成时的回调函数。</param>
            /// <param name="mode">访问模式。</param>
            public void LoadAsync(
                string assetName,
                OnAssetObject<Object> callback = null,
                AssetAccessMode mode = AssetAccessMode.UNKNOWN)
            {
                AssetInfo info = GetAssetInfo(assetName, mode);
                if (info == null || !info.IsLegal)
                {
                    End();
                    return;
                }

                if (info.AssetType == AssetTypeEnum.RESOURCE)
                {
                    Object o = ResourcesManager.Instance.GetResouceObject(info.AssetPath[0]);
                    if (o != null)
                    {
                        End(o);
                        return;
                    }
                    ResourcesManager.Instance.LoadAsync(info.AssetPath[0], callback);
                }
                else if (info.AssetType == AssetTypeEnum.ASSET_BUNDLE)
                {
#if UNITY_EDITOR
                    if (_isEditorMode)
                    {
                        var assetPaths = UnityEditor.AssetDatabase.GetAssetPathsFromAssetBundleAndAssetName(info.AbName, assetName);
                        Object o = UnityEditor.AssetDatabase.LoadAssetAtPath<Object>(assetPaths[0]);
                        End(o);
                        return;
                    }
#endif
                    AssetBundleLoader ab = AssetBundleManager.Instance.GetAssetBundleLoader(info.AssetBundlePath);
                    if (ab == null ||
                        ab.AssetBundleContent == null)
                    {
                        AssetBundleManager.Instance.LoadAsync(assetName, info, (b) => {
                            End(AssetBundleManager.Instance.GetAssetObject(info.AssetPath[0]));
                        });
                        return;
                    }
                    else
                    {
                        Object o = AssetBundleManager.Instance.GetAssetObject(info.AssetPath[0]);
                        if (o != null)
                        {
                            End(o);
                            return;
                        }
            
                        ab.Expand();
                        End(AssetBundleManager.Instance.GetAssetObject(info.AssetPath[0]));
                    }
                }

                void End(Object o = null)
                {
                    callback?.Invoke(o);
                }
            }
            
            
            private AssetInfo GetAssetInfoFromResource(string path)
            {
                if (ResourceMap.Mappings.TryGetValue(path, out string value))
                {
                    return new AssetInfo(AssetTypeEnum.RESOURCE, new []{value}, null, null);
                }
                return null;
            }
            
            private AssetInfo GetAssetInfoFromAssetBundle(string path, bool remote = false)
            {
                if (AssetBundleMap.Mappings.TryGetValue(path, out AssetBundleMap.AssetMapping assetmpping))
                {
                    if (remote)
                    {
                        return new AssetInfo(AssetTypeEnum.ASSET_BUNDLE, assetmpping.AssetPath, AssetBundleManager.GetRemoteAssetBundleCompletePath(), assetmpping.AbName);
                    }
                    else
                    {
                        return new AssetInfo(AssetTypeEnum.ASSET_BUNDLE, assetmpping.AssetPath, AssetBundleManager.GetAssetBundleCompletePath(), assetmpping.AbName);
                    }
                }
                return null;
            }
            
            /// <summary>
            /// 通过资源名称同步卸载。
            /// </summary>
            /// <param name="assetName">资源名称。</param>
            /// <param name="unloadAllRelated">
            /// 如果设置为 true，将卸载目标依赖的所有资源，
            /// 否则只卸载目标资源本身。
            /// </param>
            public void Unload(string assetName, bool unloadAllRelated = false)
            {
#if UNITY_EDITOR
                if (_isEditorMode)
                {
                    return;
                }
#endif
                AssetInfo ab = GetAssetInfoFromAssetBundle(assetName);
                if (ab != null && ab.IsLegal)
                {
                    AssetBundleManager.Instance.Unload(ab.AssetBundlePath, unloadAllRelated);
                }
                AssetInfo abRemote = GetAssetInfoFromAssetBundle(assetName, true);
                if (abRemote != null && abRemote.IsLegal)
                {
                    AssetBundleManager.Instance.Unload(abRemote.AssetBundlePath, unloadAllRelated);
                }
                AssetInfo res = GetAssetInfoFromResource(assetName);
                if (res != null && res.IsLegal)
                {
                    ResourcesManager.Instance.Unload(res.AssetPath[0]);
                }
            }
            
            /// <summary>
            /// 通过资源名称异步卸载。
            /// </summary>
            /// <param name="assetName">资源名称。</param>
            /// <param name="unloadAllRelated">
            /// 如果设置为 true，将卸载目标依赖的所有资源，
            /// 否则只卸载目标资源本身。
            /// </param>
            /// <param name="callback">异步卸载完成时的回调函数。</param>
            public void UnloadAsync(string assetName, bool unloadAllRelated = false, AssetBundleLoader.OnUnloadFinished callback = null)
            {
#if UNITY_EDITOR
                if (_isEditorMode)
                {
                    return;
                }
#endif
                AssetInfo ab = GetAssetInfoFromAssetBundle(assetName);
                if (ab != null && ab.IsLegal)
                {
                    AssetBundleManager.Instance.UnloadAsync(ab.AssetBundlePath, unloadAllRelated, callback);
                }
                AssetInfo abRemote = GetAssetInfoFromAssetBundle(assetName, true);
                if (abRemote != null && abRemote.IsLegal)
                {
                    AssetBundleManager.Instance.UnloadAsync(abRemote.AssetBundlePath, unloadAllRelated, callback);
                }
            }
            
            /// <summary>
            /// 通过资源名称获取加载器的加载进度。
            /// 正常值范围从 0 到 1。
            /// 但如果没有加载器，则返回 -1。
            /// </summary>
            /// <param name="assetName">资源名称。</param>
            /// <returns>加载进度。</returns>
            public float GetLoadProgress(string assetName)
            {
#if UNITY_EDITOR
                if (_isEditorMode)
                {
                    return 1f;
                }
#endif
                float progress = 2.1f;

                List<string> assetBundlePaths = new List<string>();

                AssetInfo ab = GetAssetInfoFromAssetBundle(assetName);
                if (ab != null && ab.IsLegal)
                {
                    if (ab.AssetPath.Length > 1)
                    {
                        foreach (var assetPath in ab.AssetPath)
                        {
                            string abName = Path.ChangeExtension(assetPath, null).Replace(URLSetting.AssetBundlesPath, "").ToLower();
                            assetBundlePaths.Add(ab.AssetBundlePathWithoutAb + abName);
                        }
                    }
                    else
                    {
                        assetBundlePaths.Add(ab.AssetBundlePath);
                    }
                }

                AssetInfo abRemote = GetAssetInfoFromAssetBundle(assetName, true);
                if (abRemote != null && abRemote.IsLegal)
                {
                    if (abRemote.AssetPath.Length > 1)
                    {
                        foreach (var assetPath in abRemote.AssetPath)
                        {
                            string abName = Path.ChangeExtension(assetPath, null).Replace(URLSetting.AssetBundlesPath, "").ToLower();
                            assetBundlePaths.Add(abRemote.AssetBundlePathWithoutAb + abName);
                        }
                    }
                    else
                    {
                        assetBundlePaths.Add(abRemote.AssetBundlePath);
                    }
                }

                AssetInfo res = GetAssetInfoFromResource(assetName);
                if (res != null && res.IsLegal)
                {
                    float resProgress = ResourcesManager.Instance.GetLoadProgress(res.AssetPath[0]);
                    if (resProgress > -1f)
                    {
                        progress = Mathf.Min(progress, resProgress);
                    }
                }

                foreach (string assetBundlePath in assetBundlePaths)
                {
                    float bundleProgress = AssetBundleManager.Instance.GetLoadProgress(assetBundlePath);
                    if (bundleProgress > -1f)
                    {
                        progress = Mathf.Min(progress, bundleProgress);
                    }
                }

                if (progress >= 2f)
                {
                    progress = -1f;
                }

                return progress;
            }
            
            /// <summary>
            /// 获取所有加载器的加载进度。
            /// 正常值范围从 0 到 1。
            /// 但如果没有加载器，则返回 -1。
            /// </summary>
            /// <returns>加载进度。</returns>
            public float GetLoadProgress()
            {
#if UNITY_EDITOR
                if (_isEditorMode)
                {
                    return 1f;
                }
#endif
                float progress = 2.1f;
                float abProgress = AssetBundleManager.Instance.GetLoadProgress();
                if (abProgress > -1f)
                {
                    progress = Mathf.Min(progress, abProgress);
                }
                float resProgress = ResourcesManager.Instance.GetLoadProgress();
                if (resProgress > -1f)
                {
                    progress = Mathf.Min(progress, resProgress);
                }
                if (progress >= 2f)
                {
                    progress = -1f;
                }
                return progress;
            }
    }
}