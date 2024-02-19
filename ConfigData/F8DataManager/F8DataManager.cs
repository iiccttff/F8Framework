﻿/*
 *   This file was generated by a tool.
 *   Do not edit it, otherwise the changes will be overwritten.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using F8Framework.F8ExcelDataClass;
using F8Framework.Core;

namespace F8Framework.F8DataManager
{
	public class F8DataManager : Singleton<F8DataManager>
	{
		private Sheet1 p_Sheet1;
		private Sheet2 p_Sheet2;
		private LocalizedStrings p_LocalizedStrings;

		public Sheet1Item GetSheet1ByID(Int32 id)
		{
			Sheet1Item t = null;
			p_Sheet1.Dict.TryGetValue(id, out t);
			if (t == null) LogF8.LogError("can't find the id " + id + " in Sheet1");
			return t;
		}

		public Dictionary<int, Sheet1Item> GetSheet1()
		{
			return p_Sheet1.Dict;
		}

		public Sheet2Item GetSheet2ByID(Int32 id)
		{
			Sheet2Item t = null;
			p_Sheet2.Dict.TryGetValue(id, out t);
			if (t == null) LogF8.LogError("can't find the id " + id + " in Sheet2");
			return t;
		}

		public Dictionary<int, Sheet2Item> GetSheet2()
		{
			return p_Sheet2.Dict;
		}

		public LocalizedStringsItem GetLocalizedStringsByID(Int32 id)
		{
			LocalizedStringsItem t = null;
			p_LocalizedStrings.Dict.TryGetValue(id, out t);
			if (t == null) LogF8.LogError("can't find the id " + id + " in LocalizedStrings");
			return t;
		}

		public Dictionary<int, LocalizedStringsItem> GetLocalizedStrings()
		{
			return p_LocalizedStrings.Dict;
		}

		public void LoadAll()
		{
			p_Sheet1 = Load("Sheet1") as Sheet1;
			p_Sheet2 = Load("Sheet2") as Sheet2;
			p_LocalizedStrings = Load("LocalizedStrings") as LocalizedStrings;
		}

		public void RuntimeLoadAll(Dictionary<String, System.Object> objs)
		{
			p_Sheet1 = objs["Sheet1"] as Sheet1;
			p_Sheet2 = objs["Sheet2"] as Sheet2;
			p_LocalizedStrings = objs["LocalizedStrings"] as LocalizedStrings;
		}

		public IEnumerable LoadAllAsync()
		{
			yield return LoadAsync("Sheet1", result => p_Sheet1 = result as Sheet1);
			yield return LoadAsync("Sheet2", result => p_Sheet2 = result as Sheet2);
			yield return LoadAsync("LocalizedStrings", result => p_LocalizedStrings = result as LocalizedStrings);
		}

		private System.Object Load(string name)
		{
			IFormatter f = new BinaryFormatter();
			TextAsset text = AssetManager.Instance.Load<TextAsset>(name);
			using (MemoryStream memoryStream = new MemoryStream(text.bytes))
			{
				return f.Deserialize(memoryStream);
			}
		}
		private IEnumerator LoadAsync(string name, Action<object> callback)
		{
			IFormatter f = new BinaryFormatter();
			var load = AssetManager.Instance.LoadAsyncCoroutine<TextAsset>(name);
			yield return load;
			{
				TextAsset textAsset = AssetManager.Instance.Load<TextAsset>(name);
				if (textAsset != null)
				{
					using (Stream s = new MemoryStream(textAsset.bytes))
					{
						object obj = f.Deserialize(s);
						callback(obj);
					}
				}
			}
		}
	}
}
