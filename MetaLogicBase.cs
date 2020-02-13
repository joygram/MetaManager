using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;
using System.Text;
using log4net;
using System.Linq;

namespace gen
{
    public static class Ext
    {
        public static T CastTo<T>(this Object o)
        {
            return (T)Convert.ChangeType(o, typeof(T));
        }
    }

	// 하나의 스키마에 대한 처리 
	public class MetaLogicBase
	{
		protected JObject m_schema = null;

		protected JObject m_data_schema;
		protected JObject m_typedefs;
		protected JObject m_structs;

		protected JArray m_excel_header = null;
		//protected JArray m_excel_datas = null;
		protected JObject m_excel_datas = null;

		protected string m_root_dir;

		protected string m_schema_path;

		protected string m_schema_name;
		protected string m_schema_dir;
		protected string m_meta_category;

		protected string m_curr_xl_meta_id = "";

		protected ILog m_log = gen.Log.logger("meta.logic");
		protected gen.Result m_result = new gen.Result();

		public class ThriftHierarchyInfo
		{
			public string m_field_name;
			public string m_field_path;

			public ThriftHierarchyInfo(JObject parent_thrift_info, JObject my_thrift_info, ThriftHierarchyInfo src_info)
			{
				if (null != src_info)
				{
					m_field_name = src_info.m_field_name;
					m_field_path = src_info.m_field_path;
				}

				var column_thrift_name = my_thrift_info["name"].ToString();
				var column_thrift_field_path = my_thrift_info["thrift_index"].ToString();

				if (null == parent_thrift_info)
				{
					m_field_path = column_thrift_field_path;
					m_field_name = column_thrift_name;
				}
				else
				{
					m_field_path = m_field_path + "." + column_thrift_field_path;
					m_field_name = column_thrift_name + "." + m_field_name;
				}
			}

			public string toString()
			{
				return string.Format("name:{0} path:{1}", m_field_name, m_field_path);
			}
		}

		public class ExcelHeaderColumn // header의 한 column
		{
			public string m_id;
			public string m_datatype;
			public string m_elementtype;
			public string m_header;
			public Int32 xlscol = 0;
		}


  

        public MetaLogicBase(string root_dir)
		{
			m_root_dir = root_dir;
		}


		void initialize()
		{
			m_schema = null;
			m_data_schema = null;
			m_typedefs = null;
			m_structs = null;

			m_schema_path = "";
			m_schema_name = "";
			m_schema_dir = "";
			m_meta_category = "";

			m_curr_xl_meta_id = "";
		}
		public static void binaryMetaVersion()
		{
			//var 
		}
		public void setupSchemaInfo(string meta_category)
		{
			m_schema_path = m_root_dir + "//meta_schema//" + meta_category + "_meta.json";
			m_schema_name = Path.GetFileNameWithoutExtension(m_schema_path);
			m_schema_dir = Path.GetFullPath(m_schema_path);

			string[] delimeters = { "_meta" }; // category_meta.json
			m_meta_category = Path.GetFileName(m_schema_name).Split(delimeters, StringSplitOptions.None)[0];
		}
		// from file system 
		public gen.Result loadMetaSchema(string meta_category)
		{
			var gen_result = new gen.Result();
			try
			{
				setupSchemaInfo(meta_category);

				var json_str = File.ReadAllText(@m_schema_path);

				m_schema = JObject.Parse(json_str);
				var meta_data_schema_name = "__" + m_meta_category + "_meta__" + "data";
				var structs = m_schema["structs"].Value<JObject>();
				m_data_schema = structs[meta_data_schema_name].Value<JObject>();

				m_typedefs = m_schema["typedefs"].Value<JObject>();
				m_structs = m_schema["structs"].Value<JObject>();
			}
			catch (Exception ex)
			{
				gen.Log.logger("meta.logic").ErrorFormat("can not load meta:{0},{1}", meta_category, ex);
				return gen_result.setFail(ex.ToString());
			}
			return gen_result.setOk();
		}
		public JObject dataSchema()
		{
			return m_data_schema;
		}
		//excel 시트에서 컨테이너에서 type_id를 얻어냄  
		//엑셀저장시 컨테이너는 datatype<type_id>로 저장함 : 혼합되어 있으므로 xls_data_type_id로 명명하자.
		public string extractExcelListElemTypeId(string xls_data_type_id)
		{
			string thrift_list_elem_type_id = xls_data_type_id;
			char[] delimeters = { '<', '>' };
			string[] words = xls_data_type_id.Split(delimeters);
			if (words.Length > 1)
			{
				thrift_list_elem_type_id = words[1];
			}
			return thrift_list_elem_type_id;
		}
		//Thrift DataType : 데이터(json) 저장에 표현되는 방식 by joygram 2020/02/10
		public bool isPrimitiveThriftDataType(string thrift_datatype)
		{
			switch (thrift_datatype)
			{
				case "i8":
				case "i16":
				case "i32":
				case "i64":
				case "str":
				case "dbl": //datatype 
				case "tf":
				case "enum":
					return true;
			}
			return false;
		}
		//TypeID: IDL정의에 표시되는 것
		public bool isPrimitiveThriftTypeId(string orig_thrift_type_id)
		{
			var thrift_type_id = orig_thrift_type_id;
			var typedef = m_typedefs[thrift_type_id];
			if (null != typedef) //typedef인경우 원래 typeid를 지정 by joygram 2020/02/10 
			{
				thrift_type_id = typedef["typeId"].Value<string>();
			}
			switch (thrift_type_id)
			{
				case "i8":
				case "i16":
				case "i32":
				case "i64":
				case "string":
				case "double": 
				case "bool":
				case "enum":
					return true;
			}
			return false;
		}
		//public bool isThriftTypedefPrimitive(string datatype_name)
		//{
		//	var typedef = m_typedefs[datatype_name];
		//	if (null == typedef)
		//	{
		//		return false;
		//	}
		//	return isThriftTypeIdPrimitive(typedef["typeId"].Value<string>());
		//}

		//typedef primitive인 경우 가져온다.
		public string takeThriftPrimitiveTypeId(string orig_thrift_type_id)
		{
			var thrift_type_id = orig_thrift_type_id;
			var typedef = m_typedefs[thrift_type_id];
			if (null != typedef) //typedef인경우 원래 typeid를 지정 by joygram 2020/02/10 
			{
				thrift_type_id = typedef["typeId"].Value<string>();
			}
			//기본형인 경우 
			if (isPrimitiveThriftTypeId(thrift_type_id))
			{
				return thrift_type_id;
			}
			return orig_thrift_type_id;
		}
		// primitive type id에서 datatype으로 변환하여준다. 
		public string toThriftPrimitiveDataType(string orig_thrift_type_id)
		{
			var thrift_type_id = takeThriftPrimitiveTypeId(orig_thrift_type_id);
			switch (thrift_type_id)
			{
				case "i8":
					return thrift_type_id;
				case "i16":
					return thrift_type_id;
				case "i32":
					return thrift_type_id;
				case "i64":
					return thrift_type_id;
				case "enum":
					return thrift_type_id;
				case "string":
					return "str";
				case "double":
					return "dbl";
				case "bool":
					return "tf";
			}
			return orig_thrift_type_id;
		}


		//typeId는 정의 사용, datatype은 데이터를 저장에 사용 by joygram 2020/02/10 
		//thrift type id를 data type으로 변환하는 함수 
		public string toThriftDataType(string thrift_type_id)
		{
			//var a_thrift_type_id = thrift_type_id;

			// struct의 type인 경우에 사용할 수 있으나 (하지말자)
			//var typedef = m_typedefs[thrift_type_id]; 
			//if (null != typedef) //typedef인경우 원래 typeid를 지정 by joygram 2020/02/10 
			//{
			//	a_thrift_type_id = typedef["typeId"].Value<string>();
			//}

			//기본형인 경우 
			if (isPrimitiveThriftTypeId(thrift_type_id))
			{
				return toThriftPrimitiveDataType(thrift_type_id);
			}

			//콜렉션인 경우 elements 타입id를 뽑아낸다. 
			// 데이터타입을 리턴하고 있음 by joygram 2020/02/10 -> typeid를 리턴하거나 함수가 datatype을 리턴하도록 변경하여야 함. 

			string typename = "rec"; // rec, lst<>, map<> 등.
			// 맨앞의 데이터타입을 얻어내는 일을 수행한다.  by joygram 2020/02/10 
			char[] delimeters = { '<', '>' };
			string[] words = thrift_type_id.Split(delimeters);
			if (words.Length > 1)
			{
				typename = words[0];
			}
			return typename;
		}
		public string toThriftEnumName(string xls_enum_type_id)
		{
			if (isPrimitiveThriftTypeId(xls_enum_type_id))
			{
				return xls_enum_type_id;
			}
			//rec or collections
			string enum_name = xls_enum_type_id;
			char[] delimeters = { '<', '>' };
			string[] words = xls_enum_type_id.Split(delimeters);
			if (words.Length > 1)
			{
				enum_name = words[1];
			}
			return enum_name;
		}

		//public string firstJobjectKey(JObject jobj)
		//{
		//	foreach (var pair in jobj)
		//	{
		//		return pair.Key;
		//	}
		//	return "";
		//}

		//함수 호출이 되었다는 것은 기본 객체가 있다고 판단할 수 있다. 
		private gen.Result makeExcelColumnDataRec(JObject my_column_info, ThriftHierarchyInfo hierarchy_info, ref JObject out_meta_excel_data)
		{
			try
			{
				Int32 next_row_count = 0;

				var column_thrift_info = my_column_info["thrift_info"];
				var column_thrift_type_id = column_thrift_info["typeId"].ToString();

				my_column_info["next_row"] = 1;

				var rec_thrift_info = this.takeThriftStruct(column_thrift_type_id);
				if (null == rec_thrift_info)
				{
					return m_result.setOk(string.Format("thrif_info not found:{0}. just skip.", column_thrift_type_id));
				}
				var thrift_name_path = hierarchy_info.m_field_name;
				var thrift_field_path = hierarchy_info.m_field_path;

				JObject out_excel_rec_obj = new JObject();
				//out_excel_rec_obj["next_row"] = 0; // 활용되는지 확인을 해봐야함. [2018/12/28 by joygram]

				var column_info_values = my_column_info["column_value"];
				if (false == column_info_values.HasValues)
				{
					m_log.WarnFormat("NO COLUMN VALUE HAVE, set row_count to one");
					//out_excel_rec_obj["next_row"] = 1; // 사용하지 않을 것으로 판단함 next_row는  [2019/1/9 by joygram]
				}

				// 구조체에서는 최대 값이 다음 레코드 카운트가 된다. 
				foreach (JProperty rec_field_column_pair in column_info_values) // iterate column 
				{
					string thrift_elem_field_id = rec_field_column_pair.Name;
					JToken meta_column = rec_field_column_pair.Value;
					if (meta_column.First == null)
					{
						gen.Log.logger("exception").DebugFormat("data is null, skip");
						continue;
					}

					string column_thrift_data_type = "";
					JToken column_value = null;
					if (null == meta_column.First)
					{
						var field_thrift_info = rec_thrift_info[thrift_elem_field_id];
						column_thrift_data_type = field_thrift_info["datatype"].ToString();
						m_log.WarnFormat("meta_column.First is null, column_type {0}", column_thrift_data_type);
						continue;
					}
					column_thrift_data_type = ((JProperty)meta_column.First).Name;
					column_value = meta_column[column_thrift_data_type];
					if (column_value == null)
					{
						m_log.WarnFormat("[column_value] is null, column_type {0}", column_thrift_data_type);
						continue; // null value skip
					}

					if (null == rec_thrift_info[thrift_elem_field_id])
					{
						m_log.WarnFormat("NO SCHEMA FOR REC DATA thrift_elem_field_id:{0}, column_type:{1}", thrift_elem_field_id, column_thrift_data_type);
						continue;
					}

					JObject rec_field_column_info = new JObject();
					rec_field_column_info["thrift_info"] = rec_thrift_info[thrift_elem_field_id];
					rec_field_column_info["thrift_field_path"] = thrift_field_path;
					rec_field_column_info["thrift_datatype"] = my_column_info["thrift_datatype"];
					rec_field_column_info["thrift_elem_datatype"] = column_thrift_data_type;
					rec_field_column_info["thrift_elem_field_id"] = thrift_elem_field_id;
					rec_field_column_info["column_value"] = column_value;
					rec_field_column_info["next_row"] = 0; // 0에서 1로 변경 by joygram 2018/12/27

					MetaLogicBase.ThriftHierarchyInfo child_hierarchy_info = new MetaLogicBase.ThriftHierarchyInfo(rec_thrift_info, (JObject)rec_field_column_info["thrift_info"], hierarchy_info); // 생성자로 복제 [2019/1/8 by joygram]
					m_result = makeExcelColumnData(rec_field_column_info, my_column_info, child_hierarchy_info, ref out_excel_rec_obj);
					if (m_result.fail())
					{
						return m_result;
					}

                    next_row_count = Math.Max(next_row_count, rec_field_column_info["next_row"].Value<Int32>()); //rec >> list 일 경우 오류 수정, kangms
				}

				if (0 == next_row_count)
				{
					next_row_count = 1;
				}

                my_column_info["next_row"] = next_row_count; //kangms next_row 수정 !!!
                my_column_info["excel_column_value"] = out_excel_rec_obj;
				out_meta_excel_data[thrift_field_path] = my_column_info;

				return m_result.setOk();
			}
			catch (Exception ex)
			{
				var column_info_log_str = my_column_info.ToString().Replace("{", "{{{{").Replace("}", "}}}}"); //.Replace("\"", "\\\"");
				return m_result.setExceptionOccurred(string.Format("[exception info]:{0}\n[hierarchyInfo]:{1}\n [column_info]:{2}\n\n", ex.ToString(), hierarchy_info.toString(), column_info_log_str));
			}
		}

		private gen.Result makeExcelColumnDataPrimitiveLst(JObject lst_column_info, ThriftHierarchyInfo hierarchy_info, ref JObject out_excel_obj)
		{
			Int32 next_row_count = 0;

			var lst_thrift_info = lst_column_info["thrift_info"];
			var column_thrift_type_id = lst_thrift_info["typeId"].ToString();
			var column_thrift_datatype = lst_thrift_info["datatype"].ToString();

			var thrift_name_path = hierarchy_info.m_field_name;
			var thrift_field_path = hierarchy_info.m_field_path;

			JArray lst = new JArray();
			var meta_lst = lst_column_info["column_value"]; //JArray
			if (null == meta_lst)
			{
				m_log.WarnFormat("my_column_info['column_value'](meta_lst) is null");
				return m_result.setOk();
			}

			Int32 lst_idx = -1;
			string lst_elem_type_id = toJsonThriftName(extractExcelListElemTypeId(column_thrift_type_id));
			var elem_thrift_info = takeThriftStruct(lst_elem_type_id);
			if (null == elem_thrift_info)
			{
				m_log.WarnFormat("lst_elem_type thrif_info not exist {0}", lst_elem_type_id);
				return m_result.setOk();
			}

			lst_column_info["thrift_field_path"] = thrift_field_path;
			Int32 lst_count = meta_lst[1].Value<Int32>(); //thrift_json lst_count position index is 1

			var lst_column_info_str = JsonConvert.SerializeObject(lst_column_info);
			//m_log.WarnFormat("column_thrift_type_id:{0}, lst_column_info_str:{1}", column_thrift_type_id, lst_column_info_str);
			next_row_count = 0;
			foreach (var elem_value in meta_lst) //skip [elem_type, count, ...]    ex: 1, 2, 3, ...
			{
				lst_idx++; //skip elem_type, count
				if (lst_idx < 2)
				{
					continue;
				}

				string thrift_elem_field_id = "1"; //임의로 부여한 field_id `1`사용 
				JObject out_elem_obj = new JObject();

				if (null == elem_thrift_info[thrift_elem_field_id])
				{
					m_log.WarnFormat("NO SCHEMA FOR DATA thrift_field_id:{0}, lst_elem_type:{1}", thrift_elem_field_id, lst_elem_type_id);
					continue;
				}

				JObject elem_column_info = new JObject();
				elem_column_info["thrift_info"] = elem_thrift_info[thrift_elem_field_id]; // gen_define으로 primitive type에 대한 thrift struct를 대체함. 
				elem_column_info["thrift_field_path"] = thrift_field_path;
				elem_column_info["thrift_datatype"] = lst_elem_type_id;
				elem_column_info["thrift_elem_datatype"] = lst_elem_type_id; //datatype이 아니므로 확인이 필요함. (사용하지 않는듯)
				elem_column_info["thrift_elem_field_id"] = thrift_elem_field_id;
				elem_column_info["column_value"] = elem_value;
				elem_column_info["next_row"] = 0; // 0에서 1로 변경 2018/12/27 by joygram 

				MetaLogicBase.ThriftHierarchyInfo child_hierarchy_info = new MetaLogicBase.ThriftHierarchyInfo((JObject)lst_thrift_info, (JObject)elem_column_info["thrift_info"], hierarchy_info); // 생성자를 통해 자식 경로 생성 [2019/1/8 by joygram]
				m_result = makeExcelColumnData(elem_column_info, lst_column_info, child_hierarchy_info, ref out_elem_obj, container_primitive_elem: true);
				if (m_result.fail())
				{
					return m_result;
				}

				//리스트의 총길이를 기록하도록 한다. 
				//var elem_next_row = elem_column_info["next_row"].Value<Int32>();  
				//out_elem_obj["next_row"] = elem_next_row;
				//next_row_count += elem_next_row; //리스트 내용을 모두 합친다. 

				out_elem_obj["next_row"] = 1; //필요한 부분인가?
				next_row_count++; 
				lst.Add(out_elem_obj);
			} // end of foreach lst

			lst_column_info["next_row"] = next_row_count; //kangms next_row 수정 !!!
			lst_column_info["excel_column_value"] = lst;
			out_excel_obj[thrift_field_path] = lst_column_info;

			return m_result.setOk();
		}

		// lst_column_info["next_row"] 총 사용 열이 저장되어야 한다.
		private gen.Result makeExcelColumnDataComplexLst(JObject lst_column_info, ThriftHierarchyInfo hierarchy_info, ref JObject out_excel_data_obj)
		{
			var lst_thrift_info = lst_column_info["thrift_info"];
			var column_thrift_type_id = lst_thrift_info["typeId"].ToString();
			var column_thrift_datatype = lst_thrift_info["datatype"].ToString();

			var thrift_name_path = hierarchy_info.m_field_name;
			var thrift_field_path = hierarchy_info.m_field_path;


			Int32 lst_idx = -1;
			string lst_elem_type_id = toJsonThriftName(extractExcelListElemTypeId(column_thrift_type_id));
			var elem_thrift_info = takeThriftStruct(lst_elem_type_id);
			if (null == elem_thrift_info)
			{
				m_log.WarnFormat("lst_elem_type thrif_info not exist {0}", lst_elem_type_id);
				return m_result.setOk();
			}

			var meta_lst = lst_column_info["column_value"]; //JArray
			if (null == meta_lst)
			{
				m_log.WarnFormat("my_column_info['column_value'](meta_lst) is null");
				return m_result.setOk();
			}

			JArray lst_jarray = new JArray();
			lst_column_info["thrift_field_path"] = thrift_field_path;
			Int32 lst_count = meta_lst[1].Value<Int32>(); //thrift_json lst_count position index is 1

			// 이 값이 최종 반영되는 값임 lst_column_info["next_row"]를 초기화 하면 안된다. ? by joygram 2j018/12/27
			Int32 lst_next_row_count = 0; 

			foreach (var list_elem in meta_lst) // skip [elem_type, count, ...]
			{
				lst_idx++; //skip elem_type, count
				if (lst_idx < 2)
				{
					continue;
				}

				JObject out_excel_elem_data_obj = new JObject();

				// 리스트 새로운 요소 추가시 줄이 늘어나지 않는 현상 수정 2018/12/12 by joygram  
				Int32 highest_column_row_count = 1;
				out_excel_elem_data_obj["next_row"] = highest_column_row_count; //최초생성시 값이 널인 경우 (에디터에서 값을 만들기 어려운 구조이므로 빈객체만 넘어올 수 있다. 이경우 한줄은 확보해주어야 한다.)

				// 구조체의 각 필드의 내용을 엑셀데이터 처리가 가능하도록 변환한다. 
				foreach (JProperty elem_column_property in list_elem) // iterate column  ex: { "1" : {"dbl": 1.0}, "2" : {"dbl" : 2.0}, "3" : {"dbl" : 3.0} } 
				{
					try
					{
						//ex: { "1" : {"dbl" : 1.0 } }
						string thrift_elem_field_id = elem_column_property.Name;
						JToken meta_column = elem_column_property.Value;

						//ex: {"dbl" : 1.0 } //client에서 빈값을 넘겨줄 수 있음. 
						string column_thrift_data_type = ""; // dbl, i32, etc..
						JToken column_value = new JObject();
						if (null == meta_column.First) 
						{
							var field_thrift_info = elem_thrift_info[thrift_elem_field_id];
						}
						else
						{
							column_thrift_data_type = ((JProperty)meta_column.First).Name;
							column_value = meta_column[column_thrift_data_type];
						}

						if (null == elem_thrift_info[thrift_elem_field_id]) //데이터 유, 스키마 무 : 스키마 우선으로 처리한다. 
						{
							m_log.WarnFormat("NO SCHEMA FOR DATA thrift_field_id:{0}, lst_elem_type:{1}", thrift_elem_field_id, lst_elem_type_id);
							continue;
						}

						JObject elem_column_info = new JObject();
						//thrift_info에 없는 데이터가 들어올 경우 경고& 함께 로깅(현재 프로그램 스키마에는 존재하지 않으나 작업데이터(엑셀)에는 포함되어 있는 경우 by joygram 2018/12/14
						elem_column_info["thrift_info"] = elem_thrift_info[thrift_elem_field_id]; //존재하지 않는 경우 널(null) 임.
						elem_column_info["thrift_field_path"] = thrift_field_path;
						elem_column_info["thrift_datatype"] = column_thrift_data_type;  
						elem_column_info["thrift_elem_datatype"] = column_thrift_data_type;
						elem_column_info["thrift_elem_field_id"] = thrift_elem_field_id;
						elem_column_info["column_value"] = column_value;
						elem_column_info["next_row"] = 0; 
						
						MetaLogicBase.ThriftHierarchyInfo child_hierarchy_info = new MetaLogicBase.ThriftHierarchyInfo((JObject)lst_thrift_info, (JObject)elem_column_info["thrift_info"], hierarchy_info); // 생성자를 통해 자식 경로 생성 [2019/1/8 by joygram]
 						m_result = makeExcelColumnData(elem_column_info, lst_column_info, child_hierarchy_info, ref out_excel_elem_data_obj);
						if (m_result.fail())
						{
							m_log.ErrorFormat(m_result.toString());
							return m_result;
						}
						if (elem_column_info["next_row"].Value<Int32>() == 0)
						{
							m_log.WarnFormat("elem_column_info next_row count is zero, thrift_elem_field_id:{0}, highest_row_count:{1}", thrift_elem_field_id, highest_column_row_count);
						}

						// 칼럼중 제일 큰 값을 유지하여 리스트의 excel sheet에서 총 사용길이를 만들어낸다. [2018/12/28 by joygram]
						highest_column_row_count = Math.Max(elem_column_info["next_row"].Value<Int32>(), highest_column_row_count);
						//out_elem_data_obj에 next_row는 필요하지 않다는 판단이 듦 by joygram 2018/12/28 
						out_excel_elem_data_obj["next_row"] = highest_column_row_count;
					}
					catch (SystemException ex)
					{
						m_log.FatalFormat("make data failed. {0}", ex.ToString());
						return m_result.setExceptionOccurred(ex.ToString());
					}
				}//end of foreach column 

				lst_next_row_count += highest_column_row_count;  //리스트의 총길이를 누적하여 계산한다.
				lst_jarray.Add(out_excel_elem_data_obj);
			} // end of foreach lst 

			if (0 == lst_next_row_count)
			{
				lst_next_row_count = 1; //널 데이터라도 중첩리스트일 경우 최소 라인이 필요하여 보정함.  
			}

            //모든 요소의 최대 열의 값을 누적한 값이 되어야 한다.
            lst_column_info["next_row"] = Math.Max(lst_count, lst_next_row_count); //kangms next_row 수정 !!!
            lst_column_info["excel_column_value"] = lst_jarray;
			out_excel_data_obj[thrift_field_path] = lst_column_info;

			//out_meta_excel_data[thrift_field_path] = lst;
			return m_result.setOk();

		}

		// lst_column_info["next_row"]에 최대 사용 열값이 추가되어야 한다.
		private gen.Result makeExcelColumnDataLst(JObject lst_column_info, ThriftHierarchyInfo hierarchy_info, ref JObject out_excel_data_obj)
		{
			var column_thrift_info = lst_column_info["thrift_info"];
			var column_thrift_type_id = column_thrift_info["typeId"].ToString();
			string lst_elem_type_id = toJsonThriftName(extractExcelListElemTypeId(column_thrift_type_id));

			if (isPrimitiveThriftTypeId(lst_elem_type_id))
			{
				return makeExcelColumnDataPrimitiveLst(lst_column_info, hierarchy_info, ref out_excel_data_obj);
			}
			else
			{
				return makeExcelColumnDataComplexLst(lst_column_info, hierarchy_info, ref out_excel_data_obj);
			}
		}

		// column_info는 excel_data_obj에 포함 시킨다. 
		public gen.Result makeExcelColumnData(JObject my_column_info, JObject parent_column_info, ThriftHierarchyInfo hierarchy_info, ref JObject out_excel_data_obj, bool container_primitive_elem = false)
		{
			var column_info_log_str = my_column_info.ToString().Replace("{", "{{{{").Replace("}", "}}}}"); //.Replace("\"", "\\\"");
			m_result.setOk();
			try
			{
				var my_thrift_info = my_column_info["thrift_info"];
				if (null == my_thrift_info)
				{
					m_log.WarnFormat("column_info is null");
					return m_result.setOk();
				} 
				// 기본형 리스트 요소인 경우 최종 보정 - 구조변경으로 보정을 안하고 동작하도록 
				if (container_primitive_elem) //기본형 element에는 thrift field id가 존재하지 않는다.
				{
					// 옆필드에서 정보를 가져오도록 하자. 
					//column_thrift_field_path = my_column_info["thrift_field_path"].ToString(); // 쓰리프트 정보를 참조하지 않고 (가짜이므로) 부모의 정보를 그대로 사용한다.
					//hierarchy_info.m_field_path = column_thrift_field_path;
					//hierarchy_info.m_field_name = column_thrift_name;
				}
				var thrift_field_path = hierarchy_info.m_field_path;
				var column_thrift_datatype = my_thrift_info["datatype"].ToString();
				switch (column_thrift_datatype)
				{
					case "bool": // todo field type, removed
					case "i8":
					case "i16":
					case "i32":
					case "i64":
					case "dbl":
					case "str":
					case "tf":
					case "enum": // 위치 통합 [2019/1/9 by joygram]
						my_column_info["thrift_field_path"] = thrift_field_path;
						my_column_info["excel_column_value"] = my_column_info["column_value"];
						my_column_info["next_row"] = 1;
						out_excel_data_obj[thrift_field_path] = my_column_info;
						break;

					//case "enum":
					//	my_column_info["thrift_field_path"] = thrift_field_path;
					//	my_column_info["next_row"] = 1;
					//	my_column_info["excel_column_value"] = my_column_info["column_value"];
					//	out_excel_data_obj[thrift_field_path] = my_column_info;
					//	break;

					case "rec":
						m_result = makeExcelColumnDataRec(my_column_info, hierarchy_info, ref out_excel_data_obj);
						break;

					case "lst": // lst : ['rec', count, {}, {}, {}]
						try
						{
							// my_column_info["next_row"]에는 최종적으로 추가한 값이 더해져야만 한다. 
							m_result = makeExcelColumnDataLst(my_column_info, hierarchy_info, ref out_excel_data_obj);
							if (m_result.fail())
							{
								m_log.ErrorFormat("[hierarchyInfo]:{0}\n [column_info]:{1}\n", hierarchy_info.toString(), column_info_log_str);
							}
						}
						catch (Exception ex)
						{
							m_result.setExceptionOccurred(string.Format("[exception info]:{0}\n[hierarchyInfo]:{1}\n [column_info]:{2}\n\n", ex.ToString(), hierarchy_info.toString(), column_info_log_str));
						}
						break;

					case "map":
					case "set":
						break;
				}
			}
			catch (Exception ex)
			{
				//gen.Log.logger("exception").FatalFormat("make data failed. \n[{0}]\n\n", ex.ToString());
				return m_result.setExceptionOccurred(string.Format("[exception info]:{0}\n[hierarchyInfo]:{1}\n [column_info]:{2}\n\n", ex.ToString(), hierarchy_info.toString(), column_info_log_str));
			}

			return m_result;
		}

		public JObject makeExcelHeaderColumnInfo(string thrift_field_path, string hier_elem_name, JObject thrift_info)
		{
			var header_column_info = new JObject();
			header_column_info["thrift_field_path"] = thrift_field_path; // 컨테이너 객체의 경우 `.`으로 계층을 표현함.  
			header_column_info["datatype"] = thrift_info["datatype"]; //실제 쓰리프트 데이터 타입 
			header_column_info["elementtype"] = thrift_info["typeId"]; //표현하는 타입(네임스페이스를 포함한 이름)
			header_column_info["header"] = hier_elem_name;
			return header_column_info;
		}

		private JObject takeLstElemSchema(string lst_elem_type_id)
		{
			JObject lst_elem_schema = new JObject();
			if (isPrimitiveThriftTypeId(lst_elem_type_id))
			{
				lst_elem_schema = this.takeThriftStruct(lst_elem_type_id); // 기본형 & typedef타입
			}
			else
			{
				lst_elem_schema = this.takeOrderedThriftStruct(lst_elem_type_id);
			}
			return lst_elem_schema;
		}

		public void makeExcelHeaderColumn(JObject my_thrift_info, JObject parent_thrift_info, ThriftHierarchyInfo hierarchy_info, ref JArray out_meta_columns)
		{

			var thrift_field_datatype = my_thrift_info["datatype"].ToString();
			var thrift_field_type_id = my_thrift_info["typeId"].ToString();
			var thrift_field_typedef = my_thrift_info["typedef"].ToString();

			// ThriftHierarchyInfo으로 이관 [2019/1/9 by joygram]
			//var thrift_field_name = my_thrift_info["name"].ToString();
			//var thrift_field_id = my_thrift_info["thrift_index"].ToString();
			//if (null == parent_thrift_info)
			//{
			//	hierarchy_info.m_field_name = thrift_field_name;
			//	hierarchy_info.m_field_path = thrift_field_id;
			//}
			//else
			//{
			//	hierarchy_info.m_field_name = thrift_field_name + "." + hierarchy_info.m_field_name;
			//	hierarchy_info.m_field_path = hierarchy_info.m_field_path + "." + thrift_field_id;
			//}

			var thrift_field_name = hierarchy_info.m_field_name;
			var thrift_field_path = hierarchy_info.m_field_path;

			JObject header_column_info = null;
			if ("rec" == thrift_field_datatype) //확장 
			{
				header_column_info = new JObject();
				header_column_info["thrift_field_path"] = thrift_field_path;
				header_column_info["datatype"] = thrift_field_datatype;
				header_column_info["elementtype"] = thrift_field_type_id;
				header_column_info["header"] = thrift_field_name;
				out_meta_columns.Add(header_column_info);

				header_column_info = makeExcelHeaderColumnInfo(thrift_field_path, thrift_field_name, my_thrift_info);

				var rec_schema = this.takeOrderedThriftStruct(thrift_field_type_id);
				if (null != rec_schema) //없는 경우 처리?
				{
					foreach (var elem_pair in rec_schema)
					{
						if (elem_pair.Key == "name")
						{
							continue;
						}
						var elem_thrift_info = (JObject)elem_pair.Value;
						MetaLogicBase.ThriftHierarchyInfo child_hierarchyInfo = new MetaLogicBase.ThriftHierarchyInfo(my_thrift_info, elem_thrift_info, hierarchy_info);
						this.makeExcelHeaderColumn(elem_thrift_info, my_thrift_info, child_hierarchyInfo, ref out_meta_columns);
					}
				}
			}
			else if ("lst" == thrift_field_datatype)
			{
				string lst_elem_type_id = extractExcelListElemTypeId(thrift_field_type_id); // 리스트 요소의 typeId를 얻어낸다. 
				header_column_info = new JObject();
				header_column_info["thrift_field_path"] = thrift_field_path;
				header_column_info["datatype"] = thrift_field_datatype;
				header_column_info["elementtype"] = lst_elem_type_id; 
				header_column_info["header"] = thrift_field_name;
				out_meta_columns.Add(header_column_info);

				var lst_elem_schema = this.takeLstElemSchema(lst_elem_type_id);
				foreach (var elem_pair in lst_elem_schema)
				{
					if ("name" == elem_pair.Key)
					{
						continue;
					}
					var elem_thrift_info = (JObject)elem_pair.Value;
					MetaLogicBase.ThriftHierarchyInfo child_hierarchyInfo = new MetaLogicBase.ThriftHierarchyInfo(my_thrift_info, elem_thrift_info, hierarchy_info);
					this.makeExcelHeaderColumn(elem_thrift_info, my_thrift_info, child_hierarchyInfo, ref out_meta_columns);
				}
			}
			else if ("map" == thrift_field_datatype)
			{
				header_column_info = makeExcelHeaderColumnInfo(thrift_field_path, thrift_field_name, my_thrift_info);
				out_meta_columns.Add(header_column_info);
			}
			else if ("set" == thrift_field_datatype)
			{
				header_column_info = makeExcelHeaderColumnInfo(thrift_field_path, thrift_field_name, my_thrift_info);
				out_meta_columns.Add(header_column_info);
			}
			else if ("enum" == thrift_field_datatype)
			{
				header_column_info = makeExcelHeaderColumnInfo(thrift_field_path, thrift_field_name, my_thrift_info);
				out_meta_columns.Add(header_column_info);
			}
			else if ("tf" == thrift_field_datatype)
			{
				header_column_info = makeExcelHeaderColumnInfo(thrift_field_path, thrift_field_name, my_thrift_info);
				out_meta_columns.Add(header_column_info);
			}
			else if ("i16" == thrift_field_datatype)
			{
				header_column_info = makeExcelHeaderColumnInfo(thrift_field_path, thrift_field_name, my_thrift_info);
				out_meta_columns.Add(header_column_info);
			}
			else if ("i32" == thrift_field_datatype)
			{
				header_column_info = makeExcelHeaderColumnInfo(thrift_field_path, thrift_field_name, my_thrift_info);
				out_meta_columns.Add(header_column_info);
			}
			else if ("i64" == thrift_field_datatype)
			{
				header_column_info = makeExcelHeaderColumnInfo(thrift_field_path, thrift_field_name, my_thrift_info);
				out_meta_columns.Add(header_column_info);
			}
			else if ("dbl" == thrift_field_datatype)
			{
				header_column_info = makeExcelHeaderColumnInfo(thrift_field_path, thrift_field_name, my_thrift_info);
				out_meta_columns.Add(header_column_info);
			}
			else if ("str" == thrift_field_datatype)
			{
				header_column_info = makeExcelHeaderColumnInfo(thrift_field_path, thrift_field_name, my_thrift_info);
				out_meta_columns.Add(header_column_info);
			}
			//typedef
			else if ("_meta_id" == thrift_field_typedef)
			{
				header_column_info = makeExcelHeaderColumnInfo(thrift_field_path, thrift_field_name, my_thrift_info);
				out_meta_columns.Add(header_column_info);
			}
			else if ("_meta_tid" == thrift_field_typedef)
			{
				header_column_info = makeExcelHeaderColumnInfo(thrift_field_path, thrift_field_name, my_thrift_info);
				out_meta_columns.Add(header_column_info);
			}
			else if ("_link_id" == thrift_field_typedef)
			{
				header_column_info = makeExcelHeaderColumnInfo(thrift_field_path, thrift_field_name, my_thrift_info);
				out_meta_columns.Add(header_column_info);
			}
			else if ("_mname" == thrift_field_typedef)
			{
				header_column_info = makeExcelHeaderColumnInfo(thrift_field_path, thrift_field_name, my_thrift_info);
				out_meta_columns.Add(header_column_info);
			}
			else if ("_mnote" == thrift_field_typedef)
			{
				header_column_info = makeExcelHeaderColumnInfo(thrift_field_path, thrift_field_name, my_thrift_info);
				out_meta_columns.Add(header_column_info);
			}
			else if (thrift_field_typedef.StartsWith("_link_"))
			{
				gen.Log.logger("meta.logic").InfoFormat("LINK column supported. now {0}", thrift_field_name);
				header_column_info = makeExcelHeaderColumnInfo(thrift_field_path, thrift_field_name, my_thrift_info);
				out_meta_columns.Add(header_column_info);
			}
			else
			{
				gen.Log.logger("meta.logic").ErrorFormat("{0} is unknown", thrift_field_typedef);
			}
		}
		//@todo: 없는경우 오류처리
		public JObject takeOrderedThriftStruct(string struct_name)
		{
			try
			{
				var meta_structs = m_schema["structs"].Value<JObject>(); //["structs"];
				var meta_schema = meta_structs[struct_name];
				//make_ordered_schema 
				JObject ordered_schema = new JObject();
				ordered_schema["name"] = struct_name;
				foreach (JProperty thrift_field_obj in meta_schema)
				{
					var thrift_info = thrift_field_obj.Value;
					string order = thrift_info["order"].Value<string>();
					ordered_schema[order] = thrift_info;
				}
				return ordered_schema;
			}
			catch (Exception ex)
			{
				gen.Log.logger("meta.logger").ErrorFormat("{0} struct making exception, {1}", struct_name, ex.ToString());
			}
			return null;
		}

		public string toUpperCaseFirstChar(string input)
		{
			if (null == input || "" == input)
			{
				return input;
			}

			return input.First().ToString().ToUpper() + input.Substring(1);
		}

		public JObject takeThriftStruct(string struct_type_id)
		{
			if (isPrimitiveThriftTypeId(struct_type_id))
			{
				var primitive_type_id = "__gen_define__" + "_" + takeThriftPrimitiveTypeId(struct_type_id);

				//take thrift schema from gen_define.primitivetype __gen_define__Bool
				//기본형타입의 경우 별도 스키마 정보가 존재하지 않아 gen_define에 기본타입을 부여하여 
				//var primitive_type_id = "__gen_define__" + "_" + struct_name;
				return m_structs[primitive_type_id].Value<JObject>();
			}

			//if (isThriftTypedefPrimitive(struct_name))
			//{
			//	var typedef = m_typedefs[struct_name];
			//	var typedef_type_id = typedef["typeId"].Value<string>();
			//	var primitive_type_id = "__gen_define__" + "_" + typedef_type_id;
			//	return m_structs[primitive_type_id].Value<JObject>();
			//}
			//else if (isThriftTypeIdPrimitive(struct_name))
			//{
			//	//take thrift schema from gen_define.primitivetype __gen_define__Bool
			//	//기본형타입의 경우 별도 스키마 정보가 존재하지 않아 gen_define에 기본타입을 부여하여 
			//	var primitive_type_id = "__gen_define__" + "_" + struct_name;

			//	return m_structs[primitive_type_id].Value<JObject>();
			//}

			var schema_struct = m_structs[struct_type_id];
			if (null == schema_struct)
			{
				gen.Log.logger("meta.logger").ErrorFormat("meta_schema not contains struct:{0}", struct_type_id);
				return null;
			}
			return m_structs[struct_type_id].Value<JObject>();
		}

		public JObject takeEnum(string enum_name)
		{
			try
			{
				var enums = m_schema["enums"].Value<JObject>();
				return enums[enum_name].Value<JObject>();
			}
			catch (Exception ex)
			{
				gen.Log.logger("meta.logic").ErrorFormat("{0} enum:{1} is not exist. if changed enum name, please meta file enum name match to `thrift` define file. {2}", m_meta_category, enum_name, ex.ToString());
				return null;
			}
		}

		void makeExcelHeader()
		{
			m_excel_header = new JArray();

			var meta_namespace = m_schema_name;
			var struct_name = "__" + meta_namespace + "__" + "data";
			var ordered_schema = takeOrderedThriftStruct(struct_name);

			foreach (var schema_elem_pair in ordered_schema)
			{
				try
				{
					if ("name" == schema_elem_pair.Key)
					{
						continue;
					}
					var elem_thrift_info = (JObject)schema_elem_pair.Value;
					MetaLogicBase.ThriftHierarchyInfo hierarchyInfo = new MetaLogicBase.ThriftHierarchyInfo(null, elem_thrift_info, null);
					makeExcelHeaderColumn(elem_thrift_info, null, hierarchyInfo, ref m_excel_header);
				}
				catch (Exception ex)
				{
					gen.Log.logger("meta.logic").FatalFormat(ex.ToString());
				}
			}
			Int32 column_idx = 0;
			foreach (var meta_column in m_excel_header)
			{
				column_idx++;
				meta_column["xls_column"] = column_idx;
			}
		}

		// __namespace__name -> namespace.name
		public string thriftNameToExcel(string thrift_name)
		{
			string excel_name = thrift_name;
			string[] delimeters = { "__" };
			var words = thrift_name.Split(delimeters, StringSplitOptions.RemoveEmptyEntries);
			if (words.Length > 1)
			{
				excel_name = string.Format("{0}.{1}", words[0], words[1]);
			}
			return excel_name;
		}

		// namespace.name -> __namespace__name : json name 
		public string toJsonThriftName(string excel_name)
		{
			string thrift_name = excel_name;
			string[] delimeters = { "." };
			var words = thrift_name.Split(delimeters, StringSplitOptions.None);
			if (words.Length > 1)
			{
				thrift_name = string.Format("__{0}__{1}", words[0], words[1]);
			}
			return thrift_name;
		}

		public JArray excelHeader()
		{
			if (null == m_excel_header)
			{
				makeExcelHeader();
			}
			return m_excel_header;
		}

		string m_current_meta_id_str;
		private gen.Result writeExcelDataLstToWorkSheet(JObject header_column_info, JObject field_data, ref Int32 target_xls_row, ref Int32 highest_target_xls_row)
		{
			var header_column_datatype = header_column_info["datatype"].ToString();
			var xls_col = header_column_info["xls_column"].Value<Int32>();
			var header_name = header_column_info["header"].ToString();
			Int32 last_field_data_row = 0;

			var lst_datas = (JArray)field_data["excel_column_value"];  //데이터가 없는 경우 기록하지 않는다. (메타툴 표현 이슈 체크)
			if (lst_datas.Count == 0)
			{
			//	m_log.WarnFormat("no lst_datas highest_target_xls_row:{0}, target_xls_row:{1}", highest_target_xls_row, target_xls_row);
 				return m_result.setOk();
			}

			applyCellStyleLst(target_xls_row, xls_col);

			Int32 lst_begin_xls_row = target_xls_row;
			//현재 lst_begin_xls_row에 element 쓰기
			var elem_idx = 0;
			Int32 target_idx_row = lst_begin_xls_row;

			// write worksheet header
			foreach (JObject field_column_obj in lst_datas) //write root idx field 
			{
				setCellValue(target_idx_row, xls_col, m_current_meta_id_str + "_" + header_name + "[" + elem_idx + "]");
				target_idx_row += field_column_obj["next_row"].Value<Int32>();
				elem_idx++;
			}

			m_result = writeExcelDataToWorksheet(field_data, ref lst_begin_xls_row, true); // container elem
			if (m_result.fail())
			{
				m_log.WarnFormat(m_result.toString());
			}

			last_field_data_row = target_xls_row + lst_begin_xls_row; //field_data["next_row"].Value<Int32>();
			highest_target_xls_row = Math.Max(highest_target_xls_row, last_field_data_row);
			return m_result.setOk();
		}

		private gen.Result writeExcelDataColumnToWorkSheet(JObject excel_column_data_obj, ref Int32 target_xls_row, ref Int32 highest_target_xls_row, bool container_elem)
		{
			//int row_count = 0;
			foreach (var excel_field_data_obj in excel_column_data_obj)
			{
				if ("meta_id" == excel_field_data_obj.Key) //skip info
				{
					applyCellStyleMetaId(target_xls_row);

					m_current_meta_id_str = excel_field_data_obj.Value.ToString();
					continue;
				}
				// skip data 체크 
				// thrift field path : object pair로 되어 있는 것만 데이터 처리한다. 
				if ("next_row" == excel_field_data_obj.Key)
				{
					continue;
				}

				string field_path = excel_field_data_obj.Key;
				JObject field_data = null;
				try
				{
					field_data = (JObject)excel_field_data_obj.Value;
				}
				catch (Exception ex)
				{
					string field_data_str = JsonConvert.SerializeObject(excel_field_data_obj); //for data check
					var data_str = JsonConvert.SerializeObject(excel_column_data_obj);

					return m_result.setExceptionOccurred(String.Format("field_path:{0}, thrift_field_data:{1}, excel_column_data:{2}, EX:{2}", field_path, gen.Result.escapeBrace(field_data_str), Result.escapeBrace(data_str), ex.ToString()));
				}

				JObject header_column_info = getExcelHeaderColumn(field_path);
				if (null == header_column_info)
				{
					continue; // error column
				}

				// container_elem인지 체크
				var header_column_datatype = header_column_info["datatype"].ToString();
				var xls_col = header_column_info["xls_column"].Value<Int32>();
				var header_name = header_column_info["header"].ToString();
				var elem_type_id = extractExcelListElemTypeId(header_column_info["elementtype"].ToString());
				if (container_elem)
				{
					if (isPrimitiveThriftTypeId(elem_type_id)) //타입보정
					{
						//header_column_datatype = thriftDataType(elem_typeid); //컨테이너 타입도 동일하게 처리하도록 변경하였다.
					}
				}

				if ("meta_id" != header_name)
				{
					applyCellStyleDefault(target_xls_row, xls_col);
				}

				if ("rec" == header_column_datatype)
				{
					applyCellStyleRec(target_xls_row, xls_col);

					Int32 rec_begin_row = target_xls_row;
					var rec_field_data = (JObject)field_data["excel_column_value"];
					if (null != rec_field_data)
					{
						writeExcelDataColumnToWorkSheet(rec_field_data, ref rec_begin_row, ref highest_target_xls_row, container_elem);
						highest_target_xls_row = Math.Max(rec_begin_row, highest_target_xls_row);
					}
				}
				else if ("lst" == header_column_datatype)
				{
					Int32 lst_begin_row = target_xls_row;
					m_result = writeExcelDataLstToWorkSheet(header_column_info, field_data, ref lst_begin_row, ref highest_target_xls_row);
					//m_log.WarnFormat("LST highest_target_xls_row:{0}", highest_target_xls_row);
				}
				else if ("enum" == header_column_datatype)
				{
					applyCellStyleEnum(target_xls_row, xls_col);
					setCellValue(target_xls_row, xls_col, field_data["excel_column_value"].ToString());
				}  
				else if ("dbl" == header_column_datatype)
				{
					applyCellStyleDbl(target_xls_row, xls_col);
					var str_value = field_data["excel_column_value"].ToString();
					if (false == String.IsNullOrEmpty(str_value))
					{
						var value = Double.Parse(str_value);
						setCellValue(target_xls_row, xls_col, value);
					}
				}
				else if ("i64" == header_column_datatype || "i32" == header_column_datatype || "i16" == header_column_datatype || "i8" == header_column_datatype)
				{
					var str_value = field_data["excel_column_value"].ToString();
					if (false == String.IsNullOrEmpty(str_value))
					{
						Int64 value = 0;
						try
						{
							value = Int64.Parse(str_value);
						}
						catch(Exception ex)
						{
							m_log.ErrorFormat("failed converting to str_value:[{0}] to num, ex:{1}", str_value, ex.ToString());
						}
						setCellValue(target_xls_row, xls_col, value);
					}
				}
				else if ("str" == header_column_datatype)
				{
					setCellValue(target_xls_row, xls_col, field_data["excel_column_value"].ToString());
				}
				else
				{
					setCellValue(target_xls_row, xls_col, field_data["excel_column_value"].ToString());
				}
			} // end of column foreach
			return m_result;
		}

		public gen.Result writeExcelDataToWorksheet(JObject excel_data, ref Int32 begin_xls_row, bool container_elem = false)
		{
			try
			{
				if (null == excel_data)
				{
					return m_result.setFail("excel_column_data is null");
				}

				Int32 highest_target_xls_row = begin_xls_row;
				Int32 target_xls_row = begin_xls_row;

				JArray excel_data_array = (JArray)excel_data["excel_column_value"];

				if (false == excel_data_array.HasValues)
				{
					m_log.WarnFormat("excel_data_array is empty: begin_xls_row:{0}", begin_xls_row);
					return m_result.setOk("excel_data_array is empty");
				}

				foreach (JObject excel_column_data_obj in excel_data_array)
				{
					if( excel_column_data_obj.HasValues)
					{
						m_result = writeExcelDataColumnToWorkSheet(excel_column_data_obj, ref target_xls_row, ref highest_target_xls_row, container_elem);
						if (m_result.fail())
						{
							var excel_column_data_obj_str = JsonConvert.SerializeObject(excel_column_data_obj);
							m_log.ErrorFormat("{0}", m_result.toString());
							return m_result;
						}
						target_xls_row += excel_column_data_obj["next_row"].Value<Int32>(); //makeExcelDatas에서 반드시 만들어준다. 
					}  
					else
					{
						target_xls_row += 1; //데이터는 없고 리스트에만 있는 경우 (새로추가 하는 경우 이게 아니더라도 "next_row는 무조건 있어야 하는거아닌가?   
					}

				} // end of row foreach
				begin_xls_row = target_xls_row - 1;
				return m_result.setOk();
			}  
			catch(Exception ex)
			{
				var excel_data_str = JsonConvert.SerializeObject(excel_data);
				var column_value = (JArray)excel_data["excel_column_value"];
				m_log.ErrorFormat("[exception occurred] {0} {1} {2}", excel_data_str, begin_xls_row, ex.ToString());
				return m_result.setExceptionOccurred(String.Format("{0}", ex.ToString()));
			}
		}


		//from thrift hi index 
		public string getFirstIndex(string thrift_index)
		{
			string index = thrift_index;
			char[] delimeters = { '.' };
			string[] words = thrift_index.Split(delimeters);
			if (words.Length > 1)
			{
				index = words[0];
			}
			return index;
		}
		//from thrift hi index
		public string getLastIndex(string thrift_index)
		{
			string index = thrift_index;
			char[] delimeters = { '.' };
			string[] words = thrift_index.Split(delimeters);
			if (words.Length > 1)
			{
				index = words[words.Length - 1];
			}
			return index;
		}
		public gen.Result saveToExcel(string category, string table_data, string file_path)
		{
			m_result = loadMetaSchema(category);
			if (m_result.fail())
			{
				return m_result;
			}
			m_result = makeExcelData(table_data);
			if (m_result.fail())
			{
				return m_result;
			}
			return saveToExcel(file_path);
		}
		// "backup_meta"로 백업 시트 추가 2018/12/12 by joygram 
		public gen.Result updateToExcel(string category, string table_data, string file_path)
		{
			m_result = loadMetaSchema(category);
			if (m_result.fail())
			{
				return m_result;
			}
			m_result = makeExcelData(table_data);
			if (m_result.fail())
			{
				return m_result;
			}
			return updateToExcel(file_path);
		}

		public JObject getExcelHeaderColumn(string thrift_field_path)
		{
			JArray header = excelHeader();
			foreach (var column in header)
			{
				if (thrift_field_path == column["thrift_field_path"].Value<string>())
				{
					return (JObject)column;
				}
			}
			return null;
		}

		private gen.Result makeExcelRowData(JProperty meta_row_pair, ref JObject out_meta_row, ref Int32 next_row_count)
   		{
			var meta_data_schema = dataSchema();

			var meta_id = meta_row_pair.Name;
			var meta_data = meta_row_pair.Value;
			out_meta_row["meta_id"] = meta_id;
			foreach (JProperty meta_column_pair in meta_data) //col, column_pair {thrift_id, {type, value}} : JProperty
			{
				string thrift_field_id = meta_column_pair.Name;
				JToken meta_column = meta_column_pair.Value;
				if (meta_column.First == null)
				{
					gen.Log.logger("exception").DebugFormat("data is null, skip");
					continue;
				}
				try
				{
					var column_thrift_data_type = ((JProperty)meta_column.First).Name;
					var column_value = meta_column[column_thrift_data_type];
					if (null == meta_data_schema[thrift_field_id])
					{
						m_log.WarnFormat("NO SCHEMA FOR DATA thrift_field_id:{0}, column_type:{1}", thrift_field_id, column_thrift_data_type);
						continue;
					}

					JObject column_info = new JObject();
					column_info["thrift_info"] = meta_data_schema[thrift_field_id]; //데이터는 있는데 스키마 정보가 없는 경우 경고 처리 
					column_info["thrift_field_path"] = thrift_field_id;
					column_info["thrift_datatype"] = column_thrift_data_type;
					column_info["thrift_elem_datatype"] = column_thrift_data_type;
					column_info["thrift_elem_field_id"] = 0;
					column_info["column_value"] = column_value;
					column_info["next_row"] = 1; // 0에서 1로 변경 by joygram 2018/12/27

					MetaLogicBase.ThriftHierarchyInfo hierarchyinfo = new MetaLogicBase.ThriftHierarchyInfo(null, (JObject)column_info["thrift_info"], null); // 엔트리 개선  [2019/1/9 by joygram]
					m_result = makeExcelColumnData(column_info, null, hierarchyinfo, ref out_meta_row);
					if (m_result.fail())
					{
						return m_result;
					}
					next_row_count = Math.Max(next_row_count, column_info["next_row"].Value<Int32>());
					out_meta_row["next_row"] = next_row_count; // 한 열의 최종 길이를 반영한다.  [2019/1/14 by joygram]
				}
				catch (Exception ex)
				{
					gen.Log.logger("exception").FatalFormat("{0}\n\n", ex.ToString());
					return m_result.setExceptionOccurred(ex.ToString());
				}
			}
			return m_result;
		}

		public gen.Result makeExcelData(string thrift_json_str)
		{
			m_result.setOk();

			JObject thrift_json = JObject.Parse(thrift_json_str);
			JToken meta_datas = thrift_json["2"]["map"][3]; // JProperty: {{},{}} 
															// excel로 출력할 수 있는 데이터 생성 (row, col)
			var meta_data_schema = dataSchema();
			// make thrift_to_excel_data
			m_excel_datas = new JObject();
			m_excel_datas["excel_column_value"] = new JArray();
			foreach (JProperty meta_row_pair in meta_datas) //row, meta_pair (key, value) : JProperty
			{
  				JObject out_meta_row = new JObject();
				Int32 next_row_count = 0;
				m_result = makeExcelRowData(meta_row_pair, ref out_meta_row, ref next_row_count);
				if (m_result.fail())
				{
					m_log.ErrorFormat(m_result.ToString());
					return m_result;
				}
				var excel_datas = (JArray)m_excel_datas["excel_column_value"];
				excel_datas.Add(out_meta_row);
				//out_meta_row["next_row"] = 1;
				//next_row_count++; 
			}

			//m_excel_datas["next_row"] = 0;
			m_result.m_desc = JsonConvert.SerializeObject(m_excel_datas); //생성 데이터를 받아보자.
			return m_result;
		}

		public JObject excelDatas()
		{
			return m_excel_datas;
		}

		public bool isChildId(string src_hier_idx, string tgt_hier_idx)
		{
			//	m_log.WarnFormat("src:{0}, tgt:{1}", src_hier_idx, tgt_hier_idx);

			var src = src_hier_idx + ".";
			var tgt = tgt_hier_idx + ".";
			var idx = tgt.IndexOf(src);
			//var idx = tgt_hier_idx.IndexOf(src_hier_idx);
			return (idx == 0);
		}

        enum WorksheetNextRowCheckResultCode
        {
                break_for_terminate_in_list = 0
            ,   continue_for_next_row = 1
            ,   success_for_add_value = 2
        }

		private WorksheetNextRowCheckResultCode checkNextRowInWorksheet(Int32 lst_row, Int32 lst_root_row_num, Int32 lst_root_col_num,  string root_lst_idx, Int32 columnDepthCount)
		{
            //check meta id
            var table_meta_id = cellValueString(lst_row, 1);
            if (table_meta_id != "" && table_meta_id != m_curr_xl_meta_id)
            {
                return WorksheetNextRowCheckResultCode.break_for_terminate_in_list;
            }

            var curr_lst_idx = cellValueString(lst_row, lst_root_col_num);

            //check lst curr index
            if (columnDepthCount >= 1 && curr_lst_idx == "")
            {
                return WorksheetNextRowCheckResultCode.continue_for_next_row;
            }

            //check lst current index & root index
            if (columnDepthCount > 1 && lst_root_row_num != lst_row )
            {
                if(root_lst_idx != "" && root_lst_idx == curr_lst_idx )
                {
                    return WorksheetNextRowCheckResultCode.break_for_terminate_in_list;
                }

                if( root_lst_idx == "" && root_lst_idx != curr_lst_idx )
                {
                    return WorksheetNextRowCheckResultCode.break_for_terminate_in_list;
                }
            }

            return WorksheetNextRowCheckResultCode.success_for_add_value;
        }

        private JObject worksheetToThriftColumnPrimitiveLst(ref Int32 xls_row_num, ref Int32 xls_col_num, ref Int32 columnDepthCount)
		{
			var ret_thrift_col_obj = new JObject();

			string thrift_hier_idx = cellValueString(1, xls_col_num); // thrift hierarchy id
			string xls_typeid = cellValueString(2, xls_col_num); // datatype


			// 마지막 채워진 row값 알아내기 !!! 
			Int32 row_count = lastUsedRowNumber(); //마지막 채워진 row값 알아내기 
			Int32 lst_row = xls_row_num;
			Int32 lst_col = 0;

			lst_col = xls_col_num + 1; // 리스트 선언 칼럼 다음에 데이터가 존재 
			var lst_elem_idx = cellValueString(1, lst_col);
			if (!isChildId(thrift_hier_idx, lst_elem_idx))
			{
				return null;
			}

			var thrift_lst_obj = new JArray();
			var elem_type_id = toJsonThriftName(extractExcelListElemTypeId(xls_typeid));
			var elem_thrift_data_type = toThriftDataType(elem_type_id);

			thrift_lst_obj.Add(elem_thrift_data_type);
			thrift_lst_obj.Add(0); // add element count 초기화 (자리를 미리 만듦)

            columnDepthCount++;

            var list_depth_count = columnDepthCount;

            Int32 lst_root_col_num = xls_col_num; //lst_root_col_num으로 부터 끝을 확인한다. 내용이 없거나 인덱스가 0이 되는 경우 
			Int32 lst_root_row_num = xls_row_num;
			var root_lst_idx = cellValueString(lst_root_row_num, lst_root_col_num);

			try
			{
				for (lst_row = xls_row_num; lst_row <= row_count; ++lst_row)
				{
                    var result = this.checkNextRowInWorksheet(lst_row, lst_root_row_num, lst_root_col_num, root_lst_idx, list_depth_count);
                    if (WorksheetNextRowCheckResultCode.break_for_terminate_in_list == result)
                    {
                        break;
                    }
                    else if (WorksheetNextRowCheckResultCode.continue_for_next_row == result)
                    {
                        continue;
                    }

                    var lst_column_obj = new JObject();
					lst_column_obj = worksheetToThriftColumn(ref lst_row, ref lst_col, ref columnDepthCount, true); //lst elem임을 알려야함.
					if (null != lst_column_obj)
					{
						thrift_lst_obj.Add(lst_column_obj[elem_thrift_data_type]); //값만 저장.
					}
				}
				xls_col_num = lst_col;

				thrift_lst_obj[1] = thrift_lst_obj.Count - 2; // list count 업데이트, datatype, count를 제외한 것이 실제 데이터 갯수 
				ret_thrift_col_obj["lst"] = thrift_lst_obj;
				return ret_thrift_col_obj;
			}
			catch (Exception ex)
			{
				m_log.ErrorFormat("r:{0} c:{1} lst_row:{2} lst_col:{3}, {4}", xls_row_num, xls_col_num, lst_row, lst_col, ex.ToString());
				return null;
			}
		}

		// find last col position
		private Int32 findWorksheetLstLastColumn(Int32 xls_col_num, string thrift_hier_idx)
		{
			Int32 lst_col = 0;
			for (lst_col = xls_col_num + 1; ; ++lst_col) // lst 선언 다음 부터 elem type
			{
				var lst_elem_idx = cellValueString(1, lst_col);
				if (!isChildId(thrift_hier_idx, lst_elem_idx))
				{
					return (lst_col - 1);
				}
			}
			return (xls_col_num + 1);

		}

		private JObject worksheetToThriftColumnComplexLst(ref Int32 xls_row_num, ref Int32 xls_col_num, ref Int32 columnDepthCount)
		{
			//m_log.WarnFormat("COMPLEX LST :row{0} col{1} last_row{2}", xls_row_num, xls_col_num, xls_last_row_num);

			var ret_thrift_col_obj = new JObject();

			string thrift_hier_idx = cellValueString(1, xls_col_num); // thrift hierarchy id
			string xls_typeid = cellValueString(2, xls_col_num); // datatype

			// 마지막 채워진 row값 알아내기 !!! 
			Int32 row_count = lastUsedRowNumber(); //마지막 채워진 row값 알아내기 
			Int32 lst_row = xls_row_num;
			Int32 lst_col = 0;

			var thrift_lst_obj = new JArray();
			var elem_type_id = toJsonThriftName(extractExcelListElemTypeId(xls_typeid));
			var elem_thrift_data_type = toThriftDataType(elem_type_id);
			if (false == isPrimitiveThriftDataType(elem_thrift_data_type))
			{
				elem_thrift_data_type = "rec";
				//container타입일 수도 있음. 정확한 타입을 알아내는 함수 제공 필요  
			}
			thrift_lst_obj.Add(elem_thrift_data_type);
			thrift_lst_obj.Add(0); // add element count 초기화 (자리를 미리 만듦)

            columnDepthCount++;

            var list_depth_count = columnDepthCount;

			Int32 lst_root_col_num = xls_col_num; //lst_root_col_num으로 부터 끝을 확인한다. 내용이 없거나 인덱스가 0이 되는 경우 
			Int32 lst_root_row_num = xls_row_num;
			var root_lst_idx = cellValueString(lst_root_row_num, lst_root_col_num);
			try
			{
				Int32 last_lst_col = findWorksheetLstLastColumn(xls_col_num, thrift_hier_idx);

				for (lst_row = xls_row_num; lst_row <= row_count; ++lst_row)
				{
                    var result = this.checkNextRowInWorksheet(lst_row, lst_root_row_num, lst_root_col_num, root_lst_idx, list_depth_count);
                    if (WorksheetNextRowCheckResultCode.break_for_terminate_in_list == result)
                    {
                        break;
                    }
                    else if(WorksheetNextRowCheckResultCode.continue_for_next_row == result)
                    {
                        continue;
                    }

					lst_col = 0;
					var lst_elem_obj = new JObject();
					for (lst_col = xls_col_num + 1; lst_col <= last_lst_col; ++lst_col) // lst 선언 다음 부터 elem type
					{
						var lst_elem_idx = cellValueString(1, lst_col);
						var lst_column_obj = new JObject();
						lst_column_obj = worksheetToThriftColumn(ref lst_row, ref lst_col, ref columnDepthCount);
						if (null != lst_column_obj)
						{
							var last_idx = getLastIndex(lst_elem_idx);
							lst_elem_obj[last_idx] = lst_column_obj;
						}
					}
					var lst_root_col = xls_col_num;
					var lst_root_value = cellValueString(lst_row, lst_root_col);
					if (lst_elem_obj.Count > 0 || false == string.IsNullOrEmpty(lst_root_value)) // 요소데이터가 있거나 리스트 루트 컬럼이 비어있지 않은 경우도 채워줌(빈요소데이터)  2018/07/04 by joygram
					{
						thrift_lst_obj.Add(lst_elem_obj);
                    }
                }
				xls_col_num = last_lst_col; 

				thrift_lst_obj[1] = thrift_lst_obj.Count - 2; // list count 업데이트, datatype, count를 제외한 것이 실제 데이터 갯수 
				ret_thrift_col_obj["lst"] = thrift_lst_obj;
				return ret_thrift_col_obj;
			}
			catch (Exception ex)
			{
				m_log.ErrorFormat("r:{0} c:{1} lst_row:{2} lst_col:{3}, {4}", xls_row_num, xls_col_num, lst_row, lst_col, ex.ToString());
				return null;
			}

		}


		private JObject worksheetToThriftColumnLst(ref Int32 xls_row_num, ref Int32 xls_col_num, ref Int32 columnDepthCount)
		{
			string xls_typeid = cellValueString(2, xls_col_num); // datatype
			var elem_type_id = toJsonThriftName(extractExcelListElemTypeId(xls_typeid));
			var elem_thrift_data_type  = toThriftDataType(elem_type_id);

			if (isPrimitiveThriftDataType(elem_thrift_data_type)) // primitive type : add value directly 
			{
				return worksheetToThriftColumnPrimitiveLst(ref xls_row_num, ref xls_col_num, ref columnDepthCount);
			}
			else //rec만 가능 연이어 lst인경우 처리고려 안되어 있음. 
			{
				var lst_object = worksheetToThriftColumnComplexLst(ref xls_row_num, ref xls_col_num, ref columnDepthCount);
				//m_log.WarnFormat("xls_col_num:{0}", xls_col_num);
				return lst_object;
			}
		}

         
		public JObject worksheetToThriftColumn( ref Int32 xls_row_num, ref Int32 xls_col_num
                                              , ref Int32 columnDepthCount, bool primitive_container_elem = false )
		{
			string thrift_hier_idx = cellValueString(1, xls_col_num); // thrift hierarchy id

			string xls_data_type = cellValueString(2, xls_col_num); // cell에는 datatype<type_id>를 저장한다. list<primitive>인 경우 데이터를 생성하는 셀은 이미 datatype이 포함되어 있다. 

			string thrift_data_type = toThriftDataType(xls_data_type); //컨테이너, enum등은 변환이 필요함 by joygram  
			if (primitive_container_elem) //컨테이너의 경우 요소 타입을 얻어낸다. 기본형도 별도의 필드를 가지게 되었으므로 변환을 하지 않는다. by joygram 2020/02/11
			{
				thrift_data_type = xls_data_type;			
				//xls_data_type = excelToThriftName(thriftListElemTypeId(xls_data_type));
			}

			string column_name = cellValueString(3, xls_col_num); //column_name

			var ret_thrift_col_obj = new JObject();

			var xl_cell_value = cellValue(xls_row_num, xls_col_num);
			bool xl_cell_is_empty = isCellEmpty(xls_row_num, xls_col_num);

			//thrift datatype
			if ("meta_id" == column_name)
			{
				if (false == xl_cell_is_empty) //새로운 row
				{
					m_curr_xl_meta_id = xl_cell_value.ToString();
					ret_thrift_col_obj[thrift_data_type] = xl_cell_value.CastTo<Int32>();
					return ret_thrift_col_obj;
				}
			}
			else if ("rec" == thrift_data_type)
			{
				var thrift_rec_object = new JObject();
				Int32 rec_col = 0;
				for (rec_col = xls_col_num + 1; ; ++rec_col) // rec 선언 다음 부터 elem type
				{
					var rec_elem_idx = cellValueString(1, rec_col);
					if (!isChildId(thrift_hier_idx, rec_elem_idx))
					{
						break;
					}
					var rec_column = new JObject();
					var last_idx = getLastIndex(rec_elem_idx);
					rec_column = worksheetToThriftColumn(ref xls_row_num, ref rec_col, ref columnDepthCount);
					if (null != rec_column)
					{
						thrift_rec_object[last_idx] = rec_column;
					}
				}
				if (thrift_rec_object.Count > 0)
				{
					ret_thrift_col_obj[thrift_data_type] = thrift_rec_object;
				}
				else
				{
					ret_thrift_col_obj = null;
				}
				xls_col_num = rec_col - 1; //칼럼이동, 마지막 사용한 컬럼 보정 ( 루프 나가서 자동증가됨)
				return ret_thrift_col_obj;
			}
			// lst type이 rec인경우와 아닌경우 
			else if ("lst" == thrift_data_type) // {"lst": ["rec", 3", {}, {}, {}]
			{
				return worksheetToThriftColumnLst(ref xls_row_num, ref xls_col_num, ref columnDepthCount);
			}
			else if ("i64" == thrift_data_type)
			{
				if (false == xl_cell_is_empty)
				{
					var val = xl_cell_value.CastTo<Int64>();
					ret_thrift_col_obj[thrift_data_type] = val;
					return ret_thrift_col_obj;
				}
			}
			else if ("i32" == thrift_data_type)
			{
				if (false == xl_cell_is_empty)
				{
					var val = xl_cell_value.CastTo<Int32>();
					ret_thrift_col_obj[thrift_data_type] = val;
					return ret_thrift_col_obj;
				}
			}
			else if ("i16" == thrift_data_type)
			{
				if (false == xl_cell_is_empty)
				{
					var val = xl_cell_value.CastTo<Int16>();
					ret_thrift_col_obj[thrift_data_type] = val;
					return ret_thrift_col_obj;
				}
			}
			else if ("i8" == thrift_data_type)
			{
				// todo cast i8
				if (false == xl_cell_is_empty)
				{
					var val = xl_cell_value.CastTo<Int16>();
					ret_thrift_col_obj[thrift_data_type] = val;
					return ret_thrift_col_obj;
				}
			}
			else if ("dbl" == thrift_data_type)
			{
				if (false == xl_cell_is_empty)
				{
					var val = xl_cell_value.CastTo<Double>();
					ret_thrift_col_obj[thrift_data_type] = Math.Round(val, 4); //소수점 4째 자리까지만 반영
					return ret_thrift_col_obj;
				}
			}
			else if ("tf" == thrift_data_type) //bool
			{
				if (false == xl_cell_is_empty)
				{
					var val = xl_cell_value.CastTo<Int16>();
					ret_thrift_col_obj[thrift_data_type] = val;
					return ret_thrift_col_obj;
				}
			}
			else if ("str" == thrift_data_type)
			{
				if (false == xl_cell_is_empty)
				{
					ret_thrift_col_obj[thrift_data_type] = xl_cell_value.ToString();
					return ret_thrift_col_obj;

				}
			}
			else if ("enum" == thrift_data_type) // 숫자이면 그대로 입력, 문자열인 경우, 스키마 내용 찾기 
			{
				String val;
				if (false == xl_cell_is_empty)
				{
					val = xl_cell_value.ToString();
					if (val == "")
					{
						//col["i32"] = 0;
						return null;
					}

					Int32 enum_value;
					bool is_numeric = int.TryParse(val, out enum_value);
					if (is_numeric)
					{
						ret_thrift_col_obj["i32"] = enum_value;
					}
					else // convert name to enum value
					{
						var elem_name = this.toThriftEnumName(xls_data_type);
						string[] delimeters = { "." }; // category_meta.json
						var words = elem_name.Split(delimeters, StringSplitOptions.None); // 0 : namespace, 1: name

						var json_schema_enum_name = elem_name;
						if (words.Length > 1)
						{
							json_schema_enum_name = "__" + words[0] + "__" + words[1];
						}
						bool matched = false;
						var enum_object = this.takeEnum(json_schema_enum_name);
						foreach (JProperty pair in (JToken)enum_object)
						{
							if (val == pair.Value.ToString())
							{
								ret_thrift_col_obj["i32"] = Int32.Parse(pair.Name); // enum
								matched = true;
								break;
							}
						}
						if (!matched)
						{
							// 못 찾은 경우 sheet를 기준으로 한 값으로 변경을 시도하자.
							Log.logger("meta").ErrorFormat( "can not match {0} enum string !!! : ColumnType({1}), ColumnName({2}) - MetaCategory({3})"
                                                          , val, xls_data_type, column_name
                                                          , m_meta_category );
							ret_thrift_col_obj["i32"] = 0; // error // 우선은 0 값으로 변경하자. 
						}
					}
					return ret_thrift_col_obj;
				}
			}
			return null;
		}

		private bool moveWorksheetNextRow(ref Int32 next_row, Int32 xls_row_num)
		{
			bool has_more_row = false;
			Int32 ws_row_count = lastUsedRowNumber(); //마지막 채워진 row값 알아내기   
			for (next_row = xls_row_num; next_row <= ws_row_count; ++next_row) //find next row
			{
				var row_meta_id = cellValueString(next_row, 1);
				if (row_meta_id != "" &&
					row_meta_id != m_curr_xl_meta_id) // 다음 메타 id가 나타남. 
				{
					has_more_row = true;
					break;
				}
			}
			return has_more_row;
		}

		public gen.Result loadExcelToThrift(string meta_file_path)
		{
			var gen_result = new gen.Result();
			string meta_out_string = "";

			initialize();
			Int32 xls_row_num = 0;
			Int32 xls_col_num = 0;
			try
			{
				var dir_info = new DirectoryInfo(meta_file_path);
				gen_result = this.loadMetaSchema(MetaManager.instance.metaCategory(dir_info.Name));
				if (gen_result.fail())
				{
					return gen_result;
				}
				gen_result = loadWorkSheet(meta_file_path);
				if (gen_result.fail())
				{
					return gen_result;
				}

                Int32 columnDepthCount = 0;

                Int32 ws_col_count = lastUsedColNumber(); //마지막 채워진 col값 알아내기 
				Int32 ws_row_count = lastUsedRowNumber(); //마지막 채워진 row값 알아내기   
				JObject rows = new JObject();
				for (xls_row_num = 4; xls_row_num <= ws_row_count; ++xls_row_num)
				{
					Int32 next_row = 0;
					if (false == moveWorksheetNextRow(ref next_row, xls_row_num))
					{
						break;
					}
					xls_row_num = next_row;

					var row = new JObject();
					string row_id = "";

					for (xls_col_num = 1; xls_col_num <= ws_col_count; ++xls_col_num)
					{
						string xls_thrift_path = cellValueString(1, xls_col_num);
						//string thrift_index = getLastIndex(xls_thrift_path); //thrift_index, 맨 뒷자리만 넣는다. 

						var thrift_column_obj = worksheetToThriftColumn(ref xls_row_num, ref xls_col_num, ref columnDepthCount);
						if (null != thrift_column_obj) // { "i32" : 71650 }
						{
							if (xls_col_num == 1) // c = 1은 고정 필드, meta_id임 
							{
								row_id = (string)thrift_column_obj["i32"];
							}
							//row[thrift_index] = thrift_column_obj;
							row[xls_thrift_path] = thrift_column_obj;
						}
					}
					if (row.Count > 0) 
					{
						rows[row_id] = row;
					}
					string out_string = JsonConvert.SerializeObject(row);
				}
				string rows_out_string = JsonConvert.SerializeObject(rows);

				// meta_infos 
				JObject meta_infos = new JObject();
				JArray meta_table = new JArray();
				meta_table.Add("i32"); //key;
				meta_table.Add("rec"); //value type
				meta_table.Add(rows.Count); //row count
				meta_table.Add(rows);
				meta_infos["map"] = meta_table;

				// make meta container,@todo version도 excel meta_data에 넣어둔다.
				string container_json = @"{1:{rec:{1:{i32:1}}}}";
				JObject meta_container = (JObject)JsonConvert.DeserializeObject(container_json);
				meta_container["2"] = meta_infos;
				meta_out_string = JsonConvert.SerializeObject(meta_container);
			}
			catch (System.IO.IOException ex)
			{
				var log_msg = String.Format("r:{0}, c:{1}, meta_file:{2}, {3}", xls_row_num, xls_col_num, meta_file_path, ex.ToString()); 
				return gen_result.setFail(log_msg);
			}
			catch (System.Exception ex)
			{
				var log_msg = String.Format("r:{0}, c:{1}, meta_file:{2}, {3}", xls_row_num, xls_col_num, meta_file_path, ex.ToString());
				   return gen_result.setFail(log_msg);
			}
			m_log.InfoFormat("meta {0} load completed", meta_file_path);
			return gen_result.setOk(meta_out_string);
		}   

		//schema update함 
		public gen.Result updateSchemaFileSystem(DirectoryInfo root_dir)
		{
			var gen_result = new gen.Result();
			return gen_result;
		}

		public gen.Result updateSchemaDirectory(DirectoryInfo root_dir)
		{
			var gen_result = new gen.Result();
			FileInfo[] files = null;
			DirectoryInfo[] sub_dirs = null;
			try
			{
				var file_pattern = string.Format("*.xlsx");
				files = root_dir.GetFiles(file_pattern);
			}
			catch (UnauthorizedAccessException e)
			{
				gen.Log.logger("exception.metalogic").Error(e.ToString());
			}
			catch (System.IO.DirectoryNotFoundException e)
			{
				gen.Log.logger("exception.metalogic").Error(e.ToString());
			}
			if (files != null)
			{
				foreach (var file_info in files)
				{
					gen_result = updateSchema(file_info);
				}

				sub_dirs = root_dir.GetDirectories();
				foreach (var dir_info in sub_dirs)
				{
					gen_result = updateSchemaDirectory(dir_info);
				}
			}
			return gen_result.setOk();
		}
		public gen.Result updateSchema(FileInfo file_info)
		{
			var gen_result = new gen.Result();
			if (".xlsx" != file_info.Extension)
			{
				return gen_result.setOk("just skip");
			}
			// read xlsx and rewrite 
			gen_result = loadExcelToThrift(file_info.FullName);
			if (gen_result.fail())
			{
				return gen_result;
			}
			gen_result = makeExcelData(gen_result.m_desc);
			if (gen_result.fail())
			{
				return gen_result;
			}
			return saveToExcel(file_info.FullName);
		}

		//gatz파일로 pack함. 
		public gen.Result packFileSystem(DirectoryInfo root_dir)
		{
			var int_dir = root_dir.FullName + "\\_gat\\"; //작업 디렉토리 존재하면 제거
			if (Directory.Exists(int_dir))
			{
				Directory.Delete(int_dir, true); // remove old directory
			}
			var file_name = root_dir.FullName + "\\meta.gatz";
			if (File.Exists(file_name))
			{
				File.Delete(file_name);
			}
			var gen_result = makeGatDirectory(root_dir);
			if (gen_result.ok())
			{
				// make svn_info 
				var svn_info = new SvnInfo();
				svn_info.m_svn_dir = root_dir.FullName;
				svn_info.makeInfo();

				var info_txt = svn_info.serialize();
				var file_path = int_dir.ToString() + "version";
				System.IO.File.WriteAllText(file_path, info_txt);

				FileStream zip_file = File.Create(file_name); //경로지정 
				ZipOutputStream zip_stream = new ZipOutputStream(zip_file);
				zip_stream.SetLevel(0);
				//zip_stream.Password = null; 

				gen_result = packDirectory(root_dir, ref zip_stream);

				zip_stream.IsStreamOwner = true;
				zip_stream.Close();
			}
			return gen_result;
		}
		gen.Result packDirectory(DirectoryInfo root_dir, ref ZipOutputStream zip)
		{
			var gen_result = new gen.Result();

			FileInfo[] files = null;
			DirectoryInfo[] sub_dirs = null;

			try
			{
				//var file_pattern = string.Format("*.gat");
				files = root_dir.GetFiles().Where(f => f.FullName.EndsWith(".gat") || (f.FullName.EndsWith("version"))).ToArray();// file_pattern);
			}
			catch (UnauthorizedAccessException e)
			{
				gen.Log.logger("exception.metalogic").Error(e.ToString());
			}
			catch (System.IO.DirectoryNotFoundException e)
			{
				gen.Log.logger("exception.metalogic").Error(e.ToString());
			}

			if (files != null)
			{
				foreach (var file_info in files)
				{
					gen_result = packGat(file_info, ref zip);
				}

				sub_dirs = root_dir.GetDirectories();
				foreach (var dir_info in sub_dirs)
				{
					gen_result = packDirectory(dir_info, ref zip);
				}
			}
			return gen_result.setOk();
		}
		gen.Result packGat(FileInfo file_info, ref ZipOutputStream zip)
		{
			var gen_result = new gen.Result();
			if (".gat" != file_info.Extension && false == file_info.FullName.EndsWith("version"))
			{
				return gen_result.setOk("just skip");
			}

			ZipEntry entry = new ZipEntry(file_info.Name);
			entry.DateTime = file_info.LastWriteTime;
			entry.Size = file_info.Length;
			zip.PutNextEntry(entry);

			byte[] buffer = new byte[81920]; //todo file size check 
			using (FileStream stream_reader = File.OpenRead(file_info.FullName))
			{
				StreamUtils.Copy(stream_reader, zip, buffer);
			}
			zip.CloseEntry();
			return gen_result;
		}


		public string m_project_namespace = "oge";
		gen.Result makeGatDirectory(DirectoryInfo root_dir)
		{
			var gen_result = new gen.Result();
			gen_result.setOk();

			FileInfo[] files = null;
			DirectoryInfo[] sub_dirs = null;

			try
			{
				var file_pattern = string.Format("*@{0}.xlsx", m_project_namespace);
				files = root_dir.GetFiles(file_pattern);
			}
			catch (UnauthorizedAccessException e)
			{
				gen.Log.logger("exception.metalogic").Error(e.ToString());
			}
			catch (System.IO.DirectoryNotFoundException e)
			{
				gen.Log.logger("exception.metalogic").Error(e.ToString());
			}

			if (files != null)
			{
				foreach (var file_info in files)
				{
					gen_result = makeGat(file_info, gen_define.thrift_protocol_e.Binary);
				}

				sub_dirs = root_dir.GetDirectories();
				foreach (var dir_info in sub_dirs)
				{
					gen_result = makeGatDirectory(dir_info);
				}
			}
			return gen_result;
		}

		gen.Result makeGat(FileInfo file_info, gen_define.thrift_protocol_e protocol_type)
		{
			var gen_result = new gen.Result();
			gen_result.setOk();

			if (".xlsx" != file_info.Extension)
			{
				return gen_result.setFail(string.Format("skip non xlsx :{0}", file_info.Name));
			}
			var target_dir = file_info.DirectoryName + "\\_gat\\";
			if (false == Directory.Exists(target_dir))
			{
				Directory.CreateDirectory(target_dir);
			}

			var target_name = target_dir + file_info.Name + ".gat";
			//var target_name = file_info.FullName + ".gat"; //경로와 이름을 별도의 장소로 지정 
			FileInfo taret_file_info = new FileInfo(target_name);

			// take meta category
			String metacategory = MetaManager.instance.metaCategory(taret_file_info.Name);
			Thrift.Protocol.TBase meta_container = MetaManager.instance.metaContainer(metacategory);
			if (null == meta_container)
			{
				return gen_result.setFail(string.Format("can not take meta container: {0} {1}", metacategory, target_name));
			}
			// read json thrfit to container 
			gen_result = loadExcelToThrift(file_info.FullName);
			if (gen_result.fail())
			{
				return gen_result;
			}

			// read from json
			{
				Thrift.Transport.TMemoryBuffer read_memory_transport = new Thrift.Transport.TMemoryBuffer(Encoding.UTF8.GetBytes(gen_result.m_desc));
				Thrift.Protocol.TProtocol read_protocol = new Thrift.Protocol.TJSONProtocol(read_memory_transport);
				meta_container.Read(read_protocol);
			}

			//write to binary thrift 
			System.IO.Stream stream = System.IO.File.Open(target_name, FileMode.Create);
			try
			{
				Thrift.Transport.TTransport transport = new Thrift.Transport.TStreamTransport(stream, stream);
				Thrift.Protocol.TProtocol protocol = new Thrift.Protocol.TBinaryProtocol(transport);
				meta_container.Write(protocol);
			}
			catch (Exception ex)
			{
				var err_msg = ex.ToString();
				gen_result.setFail(ex.ToString());
			}
			stream.Close();
			return gen_result;
		}

		// excel cell functions for each library
		virtual public object cellValue(Int32 row, Int32 col)
		{
			return new Object();
		}
		public string cellValueString(Int32 row, Int32 col)
		{
			var cell_value = cellValue(row, col);
			if (null == cell_value)
			{
				return "";
			}
			return cell_value.ToString();
		}
		virtual public bool isCellEmpty(Int32 row, Int32 col) { return true; }
		virtual public void applyCellStyleMetaId(Int32 target_xls_row) {}
		virtual public void setCellValue<T>(Int32 row, Int32 col, T input_value) {}
		virtual protected void applyHeaderStyle(JObject header_column, Int32 xls_column, string parent_field_path) {}
		virtual public Int32 lastUsedRowNumber()
		{
			return 0;
		}
		virtual public Int32 lastUsedColNumber()
		{
			return 0;
		}
		virtual protected gen.Result loadWorkSheet(string meta_file_path)
		{
			return new gen.Result().setOk();
		}

		virtual protected gen.Result writeExcelHeaderToWorksheet() { return m_result.setFail("not implemented"); }
		virtual protected void applyCellStyleDefault(Int32 xls_row, Int32 xls_col) {}
		virtual protected void applyCellStyleRec(Int32 xls_row, Int32 xls_col) {}
		virtual protected void applyCellStyleLst(Int32 xls_row, Int32 xls_col) {}
		virtual protected void applyCellStyleDbl(Int32 xls_row, Int32 xls_col) {}
		virtual protected void applyCellStyleEnum(Int32 xls_row, Int32 xls_col) { }
		virtual protected void applyCellStyleDate(Int32 xls_row, Int32 xls_col) { }

		public gen.Result writeToWorksheet()
		{
			m_result = writeExcelHeaderToWorksheet();
			if (m_result.fail())
			{
				return m_result;
			}

			Int32 target_row = 4;
			return writeExcelDataToWorksheet(excelDatas(), ref target_row);
		}

		virtual public gen.Result saveToExcel(string file_path = "", bool save = true)
		{
			return m_result.setFail("not implemented");
		}
		// update
		virtual public gen.Result updateToExcel(string file_path = "") { return m_result.setFail("not implemented"); }

	}//endof class
}//endof namespace

