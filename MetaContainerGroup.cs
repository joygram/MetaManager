using System;
using System.Collections.Generic;
using System.Reflection;

using Thrift.Collections;
using Thrift.Transport;
using Thrift.Protocol;
using System.Diagnostics;

namespace gen
{
	//같은 카테고리의 메타데이터를 통합하여 가지고 있음. 
	public partial class MetaContainerGroup
	{
		public String m_category = "";

		Dictionary<String, TBase> m_hash = new Dictionary<String, TBase>(); // each meta containers

		object m_merged_infos = null; // infos;
		bool m_merged = false;

		public MetaContainerGroup(String category)
		{
			m_category = category;
		}

		public T takeMetaContainer<T>(String name_only)
		{
			return (T)this.takeMetaContainer(name_only);
		}

		public TBase takeMetaContainer(String name_only)
		{
			if (!m_hash.ContainsKey(name_only))
			{
				TBase meta = allocMetaContainer();
				if (null == meta)
				{
					return meta;
				}
				m_hash.Add(name_only, meta);
				return meta;
			}
			return m_hash[name_only];
		}

		public Dictionary<Int32, T> infos<T>()
		{
			if (null == m_merged_infos)
			{
				m_merged_infos = new Dictionary<Int32, T>();
			}
			return m_merged_infos as Dictionary<Int32, T>;
		}
	}
}