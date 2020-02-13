using System;
using System.IO;
using System.Text;

namespace gen
{
	internal class ZipFileLoader
	{
		public gen.SvnInfo m_svn_info;

		public void loadFromZipBytes(byte[] bytes)
		{
			ICSharpCode.SharpZipLib.Zip.ZipInputStream zip_stream = new ICSharpCode.SharpZipLib.Zip.ZipInputStream(new MemoryStream(bytes));

			loadFromZipStream(zip_stream);
		}

		public void loadFromZipFile(string zip_path)
		{
			ICSharpCode.SharpZipLib.Zip.ZipInputStream zip_stream = new ICSharpCode.SharpZipLib.Zip.ZipInputStream(File.OpenRead(zip_path));
			loadFromZipStream(zip_stream);
		}

		public void loadFromZipStream(ICSharpCode.SharpZipLib.Zip.ZipInputStream zip_stream)
		{
			ICSharpCode.SharpZipLib.Zip.ZipConstants.DefaultCodePage = 0;
			byte[] data_buffer = null;
			Int32 read_size = 0;

			ICSharpCode.SharpZipLib.Zip.ZipEntry zip_entry;
			while ((zip_entry = zip_stream.GetNextEntry()) != null)
			{
				if (false == zip_entry.IsFile)
				{
					// not a file
					continue;
				}

				// 버젼파일 읽기 
				if (zip_entry.Name == "version")
				{
					data_buffer = new byte[zip_entry.Size]; // replace with buffer_pool
					read_size = zip_stream.Read(data_buffer, 0, (Int32)zip_entry.Size);

					m_svn_info = SvnInfo.deserialize(Encoding.UTF8.GetString(data_buffer));
					continue;
				}
				else if (zip_entry.Name.IndexOf(".gat") == -1)
				{
					// not a gen data 
					continue;
				}

				data_buffer = new byte[zip_entry.Size]; // replace with buffer_pool
				read_size = zip_stream.Read(data_buffer, 0, (Int32)zip_entry.Size);

				String metacategory = MetaManager.instance.metaCategory(zip_entry.Name);
				MetaTable meta_table = MetaManager.instance.table(metacategory);

				Thrift.Protocol.TBase meta_container = meta_table.metaContainer();
				if (null == meta_container)
				{
					// meta can not create or error
					continue;
				}
				System.IO.MemoryStream memory_stream = new System.IO.MemoryStream(data_buffer);
				Thrift.Transport.TTransport transport = new Thrift.Transport.TStreamTransport(memory_stream, memory_stream);
				//Thrift.Protocol.TProtocol protocol = new Thrift.Protocol.TJSONProtocol(transport);
				Thrift.Protocol.TProtocol protocol = new Thrift.Protocol.TBinaryProtocol(transport);
				meta_container.Read(protocol);

			}

		}



	}
}