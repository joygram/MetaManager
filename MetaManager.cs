using log4net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

namespace gen
{
	public enum load_state_e
	{
		_BEGIN = 0,
		BEFORE_LOAD = 1,
		LOADING = 2,
		COMPLETED = 3,
		_END
	}

	public partial class MetaManager
		: gen.Singleton<MetaManager>
	{
		private Dictionary<String, MetaTable> m_meta_tables = new Dictionary<String, MetaTable>(); // category, MetaContainer

		private FileSystemLoader m_filesystem_loader = new FileSystemLoader();
		private ZipFileLoader m_zipfile_laoder = new ZipFileLoader();

		private gen.LogicTimer m_meta_check_timer = new gen.LogicTimer();
		private FileInfo m_loaded_meta_file_info = null;

		protected ILog m_log = gen.Log.logger("meta");

		public SvnInfo m_svn_info = new SvnInfo(); //오류 방지기능 필요 by joygram 2020/01/30

		//로딩상태 체크를 통해 끊김없이 처리가 진행되도록 지원  by joygram 2020/01/30
		load_state_e m_load_state = load_state_e.BEFORE_LOAD; //로딩중 상태 추가
		Thread m_loader_thread; // 로더 쓰레드 
		byte[] m_zip_bytes; //로더에서 사용할 데이터

		//null방어 
		public String svnInfoString()
		{
			if (null == m_svn_info)
			{
				return "";
			}
			return m_svn_info.serialize();
		}

		public String metaCategory(String filename)
		{
			char[] delimeters = { '@', '.' };
			string[] words = filename.Split(delimeters);
			m_log.Debug(words);
			return words[0];
		}

		// 파일이름의 앞부분만 취한다.
		public String takeNameOnly(String filename)
		{
			char[] delimeters = { '@', '.' };
			string[] words = filename.Split(delimeters);
			m_log.Debug(words);
			return words[0];
		}

		public MetaTable table(String meta_category)
		{
			if (m_meta_tables.ContainsKey(meta_category))
			{
				return m_meta_tables[meta_category];
			}
			MetaTable meta_table = new MetaTable(meta_category);
			m_meta_tables.Add(meta_category, meta_table);
			return meta_table;
		}
		private string takeCategory(string typename)
		{
			string[] delimeters = { "_meta" };
			string[] words = typename.Split(delimeters, System.StringSplitOptions.RemoveEmptyEntries);
			return words[0];
		}
		public string toThriftJsonString(string meta_category)
		{
			var meta_container = metaContainer(meta_category);

			// make thrift data to thrfit json string 
			var memory_stream = new System.IO.MemoryStream();
			var transport = new Thrift.Transport.TStreamTransport(memory_stream, memory_stream);
			var protocol = new Thrift.Protocol.TJSONProtocol(transport);
			meta_container.Write(protocol);
			var json_str = Encoding.UTF8.GetString(memory_stream.ToArray());
			return json_str;
		}
		public Thrift.Protocol.TBase metaContainer(string meta_category)
		{
			return table(meta_category).metaContainer();
		}
		public Dictionary<Int64, T> infos<T>()
		{
			return this.table(takeCategory(typeof(T).ToString())).infos<T>();
		}
		public bool containsMetaId<T>(Int64 meta_id)
		{
			var meta_infos = infos<T>();
			return meta_infos.ContainsKey(meta_id);
		}
		//메타 스키마 타입에서 meta_id기반으로 검색 
		public META_DATA_TYPE findInfo<META_DATA_TYPE>(Int64 meta_id)
		{
			var info_table = infos<META_DATA_TYPE>();
			META_DATA_TYPE info;
			info_table.TryGetValue(meta_id, out info);
			return info;
		}
		// meta_name으로 데이터 검색 by joygram 2020/02/12
		public META_DATA_TYPE findInfoByMetaName<META_DATA_TYPE>(string find_value)
		{
			return this.findInfoByProperty<META_DATA_TYPE, string>("Meta_name", find_value);
		}
		//tid로 데이터 검색 
		public META_DATA_TYPE findInfoByTid<META_DATA_TYPE>(Int64 find_value)
		{
			return this.findInfoByProperty<META_DATA_TYPE, Int64>("Tid", find_value);
		}
		//메타 테이블 데이터의 프로퍼티 값을 리니어하게 검색함. by joygram 2020/02/12
		//TODO 반복 검색을 대비한 테이블 캐싱을 지원한다
		public  META_DATA_TYPE findInfoByProperty<META_DATA_TYPE, PROPERTY_VALUE_TYPE>(string property_name, PROPERTY_VALUE_TYPE find_value)
		{
			var meta_table = table(takeCategory(typeof(META_DATA_TYPE).ToString()));
			//TODO meta_table에 해당 카테고리 디셔너리가 invalidate되어 있으면 캐싱 by joygram 2020/02/12

			var info_hash = infos<META_DATA_TYPE>();

			Type type_metadata = typeof(META_DATA_TYPE);
			PropertyInfo prop_info = type_metadata.GetProperty(property_name);

			bool found = false;
			META_DATA_TYPE info = default(META_DATA_TYPE);
			IEnumerator it = info_hash.GetEnumerator();
			it.Reset();
			while (it.MoveNext())
			{
				var pair = (KeyValuePair<Int64, META_DATA_TYPE>)it.Current;
				info = pair.Value;
				PROPERTY_VALUE_TYPE info_property_value = (PROPERTY_VALUE_TYPE)prop_info.GetValue(info, null);
				if (find_value.Equals(info_property_value))
				{
					found = true;
					break;
				}
			}
			if (found)
			{
				return info;
			}
			return default(META_DATA_TYPE);
		}
  		// 통합한 메타 데이터를 로딩 (경로 메타를 읽지 않는다)
		// 카테고리로 경로를 대체
		public bool loadMetaFromBinaryBuffer(gen_define.thrift_protocol_e protocol_type, string meta_category, byte[] byte_buffer)
		{
			Thrift.Protocol.TBase meta_container = MetaManager.instance.metaContainer(meta_category);
			if (null == meta_container)
			{
				//log error : not exist meta
				return false;
			}
			Thrift.Transport.TMemoryBuffer thrift_memory_transport = new Thrift.Transport.TMemoryBuffer(byte_buffer);
			Thrift.Protocol.TProtocol thrift_protocol = null;
			switch (protocol_type)
			{
				case gen_define.thrift_protocol_e.Binary:
					thrift_protocol = new Thrift.Protocol.TBinaryProtocol(thrift_memory_transport);
					break;
				case gen_define.thrift_protocol_e.Custom:
					break;
				case gen_define.thrift_protocol_e.Json:
					thrift_protocol = new Thrift.Protocol.TJSONProtocol(thrift_memory_transport);
					break;
			}
			if (null == thrift_protocol)
			{
				return false;
			}
			try
			{
				meta_container.Read(thrift_protocol);
			}
			catch (Thrift.Protocol.TProtocolException e)
			{
				gen.Log.logger("meta").ErrorFormat("load meta error:{0}", e);
				return false;
			}
			return true;
		}
		//같은 상태이면 변경안함 
		public void loadFromFileSystem(string root_path)
		{
			if (load_state_e.BEFORE_LOAD != m_load_state)
			{
				return;
			}
			m_load_state = load_state_e.COMPLETED;

			var path_info = new System.IO.DirectoryInfo(root_path);
			m_filesystem_loader.iterateDirectory(path_info);

		}

		// watch_timeout == 0 감시 안함.
		public void loadFromZipFile(string zip_path, UInt16 watch_timeout = 0)
		{
			if(load_state_e.BEFORE_LOAD != m_load_state)
			{      
				return;
			}
			m_load_state = load_state_e.COMPLETED;

			m_loaded_meta_file_info = new FileInfo(zip_path);
			m_zipfile_laoder.loadFromZipFile(zip_path);

			m_svn_info = m_zipfile_laoder.m_svn_info;
			setWatchTimer(watch_timeout);
		}
		public bool isLoadState(load_state_e state)
		{
			return (m_load_state == state);
		}
		//쓰레드 로딩 처리 
		public void loadFromZipBytesAsync(byte[] bytes, bool reload = false)
		{
			if (load_state_e.BEFORE_LOAD != m_load_state &&
				false == reload)
			{
				return;
			}

			m_zip_bytes = bytes;
			m_loader_thread = new Thread(this.loadZipBytes);
			m_loader_thread.Start();
		}

		public void loadFromZipBytes(byte[] bytes, bool reload = false)
		{
			if (load_state_e.BEFORE_LOAD != m_load_state &&
				false == reload)
			{
				return;
			}
			m_load_state = load_state_e.LOADING;
			m_zip_bytes = bytes;
			loadZipBytes();
		}

		void loadZipBytes()
		{
			#warning loadFromZipBytes 결과 확인 및 처리 
			m_zipfile_laoder.loadFromZipBytes(m_zip_bytes);
			m_svn_info = m_zipfile_laoder.m_svn_info;
			m_log.InfoFormat("svn_info:{0}",m_svn_info.ToString());

			m_load_state = load_state_e.COMPLETED;
		}

		public void loadFromRaw(string path)
		{
			return;
		}

		public void setWatchTimer(Int32 timeout)
		{
			if (0 == timeout)
			{
				return;
			}
			m_meta_check_timer.activate(timeout);
		}

		public void tryReloadMetaFile()
		{
			if (null == m_loaded_meta_file_info)
			{
				return;
			}

			//파일이 변경되었는가? 
			if (m_meta_check_timer.m_active && m_meta_check_timer.expired())
			{
				try
				{
					FileInfo current_meta_info = new FileInfo(m_loaded_meta_file_info.FullName);
					if (m_loaded_meta_file_info.LastWriteTime != current_meta_info.LastWriteTime)
					{
						m_log.WarnFormat("try reload meta [{0}], DO NOT USE LIVE SERVICE!!!", m_loaded_meta_file_info.FullName);
						m_load_state = load_state_e.BEFORE_LOAD;

						this.loadFromZipFile(m_loaded_meta_file_info.FullName);
					}
				}
				catch (System.Exception ex)
				{
					m_log.WarnFormat("meta file load failed. try after.... {0}", ex.ToString());					
				}

				m_meta_check_timer.reset();
			}
		}

		public void clear()
		{
			m_meta_tables.Clear();
			m_load_state = load_state_e.BEFORE_LOAD;
		}
	}
}