using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using log4net;

namespace gen
{
	class FileSystemLoader
	{
		ILog m_log = gen.Log.logger("meta.gen");

		bool loadMeta(System.IO.FileInfo fileinfo, gen_define.thrift_protocol_e protocol_type)
		{
			if (".gen" != fileinfo.Extension)
			{
				return false;
			}
			String metacategory = MetaManager.instance.metaCategory(fileinfo.Name);
			m_log.Debug(fileinfo.FullName);

			MetaTable meta_table = MetaManager.instance.table(metacategory);
			Thrift.Protocol.TBase meta = meta_table.metaContainer();
			if (null == meta)
			{
				m_log.ErrorFormat("cant take meta for category:{0} ", metacategory);
				return false;
			}
			System.IO.Stream stream = System.IO.File.Open(fileinfo.FullName, FileMode.Open);
			Thrift.Transport.TTransport transport = new Thrift.Transport.TStreamTransport(stream, stream);

			Thrift.Protocol.TProtocol protocol = null;
			switch (protocol_type)
			{
				case gen_define.thrift_protocol_e.Binary:
					protocol = new Thrift.Protocol.TBinaryProtocol(transport);
					break;
				case gen_define.thrift_protocol_e.Json:
					protocol = new Thrift.Protocol.TJSONProtocol(transport);
					break;
			}

			if (null == protocol)
			{
				stream.Close();
				return false;
			}

			try
			{
				meta.Read(protocol);
			}
			catch (Thrift.Protocol.TProtocolException e)
			{
				System.Console.WriteLine(e.Message);
				stream.Close();
				return false;
			}
			stream.Close();
			return true;
		}
		public void iterateDirectory(System.IO.DirectoryInfo root)
		{
			System.IO.FileInfo[] files = null;
			System.IO.DirectoryInfo[] sub_dirs = null;
			try
			{
				files = root.GetFiles("*.gen");
			}
			catch (UnauthorizedAccessException e)
			{
				m_log.Error(e.Message);
			}
			catch (System.IO.DirectoryNotFoundException e)
			{
				m_log.Error(e.Message);
			}

			if (files != null)
			{
				foreach (System.IO.FileInfo file_info in files)
				{
					loadMeta(file_info, gen_define.thrift_protocol_e.Json);
				}
				sub_dirs = root.GetDirectories();
				foreach (System.IO.DirectoryInfo dir_info in sub_dirs)
				{
					iterateDirectory(dir_info);
				}
			}
		}
	}
}