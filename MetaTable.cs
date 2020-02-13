using System;
using System.Collections.Generic;
using System.Reflection;

using Thrift.Collections;
using Thrift.Transport;
using Thrift.Protocol;
using System.Diagnostics;

namespace gen
{
	// 하나의 파일만 관리하도록 변경 
	public partial class MetaTable
	{
		public String m_category = "";

		public gen_define.MetaHeader m_binary_header = new gen_define.MetaHeader(); //최초 생성시 보관함. 

		TBase m_meta_container = null;
		object m_meta_infos = null; // infos; 실제 데이터를 담고 있는 해쉬

		//인덱스를 동시 갱신할 수 있는 interface를 만들어야 함. 다중 인덱스 기반 준비 by joygram 2020/02/12 
		object m_meta_tid_infos = null; // tid기반 테이블 
		object m_meta_name_infos = null;// meta_name 기반 테이블 

		public MetaTable(String category)
		{
			m_category = category;
		}

		//최초 생성시, 각 카테고리 에 맞는 id의 더미 데이터를 입력해주자. 
		public TBase allocMetaContainer()
		{
			try
			{
				string container_name = m_category + "_meta.container,csGenThrift";
				Type meta_container_type = Type.GetType(container_name);
				return (TBase)Activator.CreateInstance(meta_container_type);
			}
			catch (System.ArgumentNullException)
			{
				gen.Log.logger("meta.table").ErrorFormat("allocate meta container:`{0}` failed. Remove data `{0}.xlsx` or Add `{0}_meta.cs` to csGenThrift", m_category);
				return null;
			}
			catch (Exception ex)
			{
				// 메타 컨테이너 타입이 라이브러리안에 존재하지 않는다. 즉, 스키마가 존재하지 않는 메타데이터 임 by joygrm 2020/01/31 
				gen.Log.logger("meta.table").ErrorFormat("meta container:`{0}` allocate failed. \n {1}", m_category, ex.ToString());
				return null;
			}
		}

		public T metaContainer<T>()
		{
			return (T)this.metaContainer();
		}

		public gen_define.MetaHeader takeMetaHeader()
		{
			if (null == m_meta_container)
			{
				return null;
			}

			Type container_type = m_meta_container.GetType();
			PropertyInfo property_info = container_type.GetProperty("Header");
			return (gen_define.MetaHeader)property_info.GetValue(m_meta_container, null);
		}

		void takeBinaryVersion()
		{
			if (null == m_meta_container)
			{
				return;
			}
			var meta_header = takeMetaHeader();
			m_binary_header.Version = meta_header.Version;
			m_binary_header.BuildInfo = meta_header.BuildInfo;
			m_binary_header.BuildPath = meta_header.BuildPath;
			gen.Log.logger("sys").WarnFormat("META TABLE {0} BINARY HEADER, VERSION:{1}", m_category, m_binary_header.Version);
		}

		public TBase metaContainer()
		{
			try
			{
				if (null == m_meta_container)
				{
					TBase meta_container = allocMetaContainer();
					if (null == meta_container)
					{
						return meta_container;
					}
					m_meta_container = meta_container;
					takeBinaryVersion();
				}
			}
			catch (Exception ex)
			{
				gen.Log.logger("exception.meta").Error(ex.ToString());
				return null;
			}

			return m_meta_container;
		}

		public Dictionary<Int64, T> infos<T>()
		{
			var meta_container = metaContainer();
			if (null == meta_container)
			{
				return null;
			}

			Type container_type = meta_container.GetType();
			PropertyInfo property_info = container_type.GetProperty("Infos");
			m_meta_infos = property_info.GetValue(meta_container, null);

			return m_meta_infos as Dictionary<Int64, T>;
		}

		//setup tidInfos 
		//setup metaNameInfos 


		public Int32 metaVersion() //로딩하기전 사용 기본 버젼 즉, 바이너리 버젼
		{
			return 0;
		}

	}
}