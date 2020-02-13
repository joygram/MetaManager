using log4net;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;


namespace gen
{
	public class GameMetaEditLogic
	{
		public static GameMetaEditLogic instance = new GameMetaEditLogic();

		ILog m_log = gen.Log.logger("meta.edit");
		string m_project_namespace = "oge";
		public static string m_root_path;

		gen.MetaLogic m_meta_logic = null;

		public string metaFilePath(string meta_name)
		{
			var file_path = m_root_path + "meta_data\\" + meta_name + "@" + m_project_namespace + ".xlsx";
			return file_path;
		}

		//public void makeFilelist(System.IO.DirectoryInfo root, ref dynamic parent_json)
		//{
		//	System.IO.FileInfo[] files = null;
		//	System.IO.DirectoryInfo[] sub_dirs = null;
		//	try
		//	{
		//		var file_pattern = "*@" + m_project_namespace + ".xlsx";
		//		files = root.GetFiles(file_pattern);
		//	}
		//	catch (UnauthorizedAccessException e)
		//	{
		//		m_log.Error(e.Message);
		//	}
		//	catch (System.IO.DirectoryNotFoundException e)
		//	{
		//		m_log.Error(e.Message);
		//	}
		//	if (files != null)
		//	{
		//		sub_dirs = root.GetDirectories();
		//		foreach (System.IO.DirectoryInfo dir_info in sub_dirs)
		//		{
		//			dynamic dir_json = new ExpandoObject();
		//			dir_json.id = dir_info.FullName;
		//			dir_json.value = dir_info.Name + "/";
		//			dir_json.data = new List<dynamic>();
		//			parent_json.data.Add(dir_json);
		//			makeFilelist(dir_info, ref dir_json);
		//		}
		//		foreach (System.IO.FileInfo file_info in files)
		//		{
		//			dynamic file_json = new ExpandoObject();
		//			file_json.id = file_info.FullName;
		//			file_json.value = file_info.Name;
		//			parent_json.data.Add(file_json);
		//		}
		//	}
		//}

		//// [ {id:, value:, data: []} 
		//string handlerFilelist()
		//{
		//	//var path = this.Request.Query["path"];
		//	var path = new DirectoryInfo(GameMetaEditLogic.m_root_path + "\\meta_data");
		//	dynamic root_json = new ExpandoObject();
		//	root_json.id = "/";
		//	root_json.value = "/";
		//	root_json.open = true;
		//	root_json.data = new List<dynamic>();
		//	makeFilelist(path, ref root_json);
		//	dynamic result_json = new List<dynamic>();
		//	result_json.Add(root_json);
		//	return Newtonsoft.Json.JsonConvert.SerializeObject(result_json);
		//}

		public gen.MetaLogic metaLogic()
		{
			if (null == m_meta_logic)
			{
				m_meta_logic = new gen.MetaLogic(m_root_path);
				m_log.InfoFormat("root_path:{0}", m_root_path);
			}

			return m_meta_logic;
		}

		//make result to gen_result desc
		//todo 생성결과 프로토콜
		public gen.Result createMetaFile(string file_path, string category)
		{
			var gen_result = new gen.Result();

			string meta_file_path = GameMetaEditLogic.m_root_path + "\\meta_data\\" + file_path;

			FileInfo file_info = new FileInfo(meta_file_path);

			gen.MetaLogic logic = metaLogic();

			logic.loadMetaSchema(category);

			// make thrfit json string 
			Thrift.Protocol.TBase meta_container = MetaManager.instance.metaContainer(category);
			if (null == meta_container)
			{
				var err_msg = string.Format("meta container:{0} not exist", category);
				m_log.ErrorFormat(err_msg);
				return gen_result.setFail(err_msg);
			}
			var memory_stream = new System.IO.MemoryStream();
			var transport = new Thrift.Transport.TStreamTransport(memory_stream, memory_stream);
			var protocol = new Thrift.Protocol.TJSONProtocol(transport);
			meta_container.Write(protocol);
			var json_str = Encoding.UTF8.GetString(memory_stream.ToArray());

			logic.makeExcelData(json_str);
			logic.saveToExcel(meta_file_path);
			
			//dynamic result_json = new ExpandoObject();
			//result_json.id = file_info.FullName;
			//result_json.value = file_info.Name;
			//return Newtonsoft.Json.JsonConvert.SerializeObject(result_json);
			return gen_result.setOk();
		}

		public FileInfo saveThriftJsonToExcel(string meta_file_path, string category, string table_data)
		{
			FileInfo file_info = null;
			try
			{
				file_info = new FileInfo(meta_file_path);
				gen.MetaLogic logic = metaLogic();

				logic.saveToExcel(category, table_data, meta_file_path);
			}
			catch (Exception ex)
			{
				file_info = null;
				m_log.ErrorFormat(ex.ToString());
			}

			//생성결과 
			//dynamic result_json = new ExpandoObject();
			//result_json.id = file_info.FullName;
			//result_json.value = file_info.Name;
			//return Newtonsoft.Json.JsonConvert.SerializeObject(result_json);
			return file_info;
		}

		public string pack()
		{
			MetaLogic logic = metaLogic();
			var gen_result = logic.packFileSystem(new DirectoryInfo(GameMetaEditLogic.m_root_path + "\\meta_data\\"));
			return gen_result.toString();
		}

		public gen.Result createNewThriftFile(string category, string file_path)
		{
			var gen_result = new gen.Result();

			Thrift.Protocol.TBase meta_container = MetaManager.instance.metaContainer(category);
			if (null == meta_container)
			{
				return gen_result.setFail(string.Format("meta container:{0} not exist", category));
			}

			FileInfo file_info = new FileInfo(GameMetaEditLogic.m_root_path + "\\meta_data" + file_path);
			Stream file_stream = file_info.Create();
			try
			{
				var transport = new Thrift.Transport.TStreamTransport(file_stream, file_stream);
				var protocol = new Thrift.Protocol.TJSONProtocol(transport);
				meta_container.Write(protocol);
			}
			catch (Exception)
			{

			}
			file_stream.Close();

			return gen_result.setOk();
		}

		public string readExcelToJsonThrift(string file_path)
		{
			var gen_result = readExcel(file_path);
			return gen_result.toJson();
		}

		public gen.Result readExcel(string file_path)
		{
			var meta_logic = metaLogic();

			String metacategory = MetaManager.instance.metaCategory(file_path);
			return meta_logic.loadExcelToThrift(file_path);
		}

		public string readJsonThrift(string file_path)
		{
			String metacategory = MetaManager.instance.metaCategory(file_path);
			Thrift.Protocol.TBase meta_container = MetaManager.instance.metaContainer(metacategory);
			if (null == meta_container)
			{
				//log error
				// meta can not create or error
				return "";
			}
			//thrift 
			//read from file 
			{
				var file_stream = File.Open(file_path, FileMode.Open);
				if (file_stream == null)
				{
					// process error
					return "";
				}
				var transport = new Thrift.Transport.TStreamTransport(file_stream, file_stream);
				var protocol = new Thrift.Protocol.TJSONProtocol(transport);
				meta_container.Read(protocol);
				file_stream.Close();
			}
			//thrift
			//write to memory buffer 
			var return_json = "";
			{
				var mem_stream = new MemoryStream();
				var transport = new Thrift.Transport.TStreamTransport(mem_stream, mem_stream);
				var protocol = new Thrift.Protocol.TJSONProtocol(transport);
				meta_container.Write(protocol);
				return_json = Encoding.UTF8.GetString(mem_stream.ToArray());
			}
			return return_json;
		}

		//string addRow(string file_path, string category, Int32 next_db_id)
		//{
		//	dynamic result_json = new ExpandoObject();
		//	result_json.result = "fail";
		//	result_json.row_id = 0;
		//	result_json.meta_row = "";
		//	string meta_data_name = category + "_meta.data, csGenThrift";
		//	Type meta_data_type = Type.GetType(meta_data_name);
		//	var meta_data = (Thrift.Protocol.TBase)Activator.CreateInstance(meta_data_type);
		//	//var next_db_id = gen.Singleton<gen.DbIdManager>.instance.nextDbId(gen_define.db_id_e.GAME_META_ID);
		//	//m_log.InfoFormat("add new row :{0}", next_db_id);
		//	PropertyInfo meta_id_property = meta_data_type.GetProperty("Meta_id");
		//	meta_id_property.SetValue(meta_data, next_db_id); // new guid
		//	// make thrift data 
		//	var meta_row = "";
		//	{
		//		var mem_stream = new MemoryStream();
		//		var transport = new Thrift.Transport.TStreamTransport(mem_stream, mem_stream);
		//		var protocol = new Thrift.Protocol.TJSONProtocol(transport);
		//		meta_data.Write(protocol);
		//		meta_row = Encoding.UTF8.GetString(mem_stream.ToArray());
		//	}
		//	// make json string
		//	result_json.result = "success";
		//	result_json.row_id = next_db_id;
		//	result_json.meta_row = meta_row;
		//	var return_json = JsonConvert.SerializeObject(result_json);

		//	m_log.InfoFormat("return_json :{0}", return_json);
		//	return return_json;
		//}
	}
}