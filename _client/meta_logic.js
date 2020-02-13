var g_current_select_row_id = 0;
var curSelectTreeId = "";
var waiting_box;

var MetaLogic = {

	m_containers: {},
	m_current_meta_path: "",

	//없으면 로딩한다. 
	getMetaContainer : function (container_path, create_new)
	{
		meta_container = this.m_containers[container_path];
		if (typeof (meta_container) == 'undefined' || create_new == true)
		{
			console.log("create new container:", container_path);

			this.m_containers[container_path] = new MetaContainer();
			meta_container = this.m_containers[container_path];
			meta_container.loadMeta(container_path, true);
		}

		return this.m_containers[container_path];
	},

	modifiedCount: function()
	{
		var count = 0;
		for (container_idx in this.m_containers)
		{
			if (this.m_containers[container_idx].m_modified)
			{
				count++;
			}
		}

		return count;
	}
};

function MetaContainer() {
    this.m_data_path;
    this.m_meta_category;
	this.m_meta_namespace;
	this.m_meta_struct_name;

    this.m_schema;
	this.m_schema_structs;
	this.m_schema_typedefs;

    this.m_meta_data;
	this.m_meta_rows; //데이터에서 container만 뽑아낸 부분 

	this.m_last_clicked_row_id; //마지막 선택한 row_id
	this.m_last_clicked_row_info;
	this.m_modified = false;

	this.m_webix_header = null;
	this.m_webix_link_header = null;

	this.m_link_containers = {}; //링크 데이터를 포함한 컨테이너 


	this.m_link_target_container = null; //링크 데이터를 저장할 컨테이너.
	this.m_link_target_datatable = null; //링크 데이터를 표시할 데이터 테이블

    this.loadMeta = function (meta_data_path, error_window)
    {
        this.m_data_path = meta_data_path;
        this.m_meta_category = MetaCommon.extractMetaNameFromPath(this.m_data_path);
        if ("" == this.m_meta_category)
        {
            console.log("meta_basename is null");
            return false;
		}
		this.m_meta_namespace = this.m_meta_category + "_meta";
		this.m_meta_struct_name = '__' + this.m_meta_namespace + '__' + 'data'; 

        this.m_schema = MetaCommon.loadMetaSchema(this.m_meta_namespace);
        if (typeof this.m_schema == 'undefined')
        {
            console.log("schema load failed:" + this.m_meta_category);
            return false;
		}

		this.m_schema_typedefs = this.m_schema["typedefs"];
        console.log("success meta schema load:" + this.m_meta_category);        

        this.m_meta_data = MetaCommon.loadMetaData(this.m_data_path, error_window);
        if (typeof this.m_meta_data == 'undefined')
        {
            console.log('meta_data is not exist');
            return false;
        }
        console.log("success meta data load:" + this.m_data_path);

        try {
            this.m_meta_rows = this.m_meta_data[2].map[3]; // meta container root 
        }
        catch (ex) {
            console.log(ex);
            this.m_meta_rows = undefined;
        }
        if (typeof this.m_meta_rows == 'undefined')
        {
            console.log("meta_row is not exist");
        }
        return true;
    };

    this.metaStructs = function ()
    {
        if (typeof this.m_schema.structs == 'undefined')
        {
            return null;
        }
        return this.m_schema.structs;
	};

	this.metaTypedefs = function (typename)
	{
		if (typeof this.m_schema.typedefs == 'undefined')
		{
			return null;
		}
		return this.m_schema.typedefs[typename];
	};

	this.removeThriftNamespace = function (typename)
	{
		console.log("typename:", typename, " namespace:", this.m_meta_namespace);
		return typename.replace("__" + this.m_meta_namespace + "__", "");
	};

    this.metaRows = function ()
    {
        if (typeof this.m_meta_rows == 'undefined')
        {
            return null;
        }
        return this.m_meta_rows;
	};

	this.addMetaRow = function (meta_id, meta_row)
	{
		if (typeof this.m_meta_rows == 'undefined')
		{
			console.log("meta_row is not exist. cannot add");
			return;
		}
		console.log("meta_id:", meta_id, "meta_row:", meta_row);
		this.m_meta_rows[meta_id] = meta_row;
	};
	
	this.metaSchema = function ()
	{
		return this.takeThriftStruct(this.m_meta_struct_name);
	};

	// thrift type_id로 구조체를 검색 by joygram 2020/02/10
	this.takeThriftStruct = function (struct_type_id)
	{
		var meta_structs = this.metaStructs();
		if (null == meta_structs)
		{
			return null;
		}

		var thrift_struct = {};
		if (this.isThriftPrimitiveTypedef(struct_type_id))
		{
			var typedef_type_id = this.metaTypedefs(struct_type_id).typeId;
			var primitive_type_id = "__gen_define__" + "_" + typedef_type_id;

			thrift_struct = meta_structs[primitive_type_id];
			thrift_struct.datatype = thrift_struct[1].datatype;

			if (struct_type_id.includes('_link_')) //링크이면 
			{
				thrift_struct.typedef = struct_type_id;
			}
			else 
			{
				thrift_struct.typedef = thrift_struct[1].typedef;
			}
			console.log("[takeThriftStruct] thruft_struct:", thrift_struct, struct_type_id);

		}     
		//type id로 기본형타입인지 검사하여야 한다. 
		else if (this.isPrimitiveThriftTypeId(struct_type_id))
		{
			var primitive_type_id = "__gen_define__" + "_" + struct_type_id;

			thrift_struct = meta_structs[primitive_type_id];
			thrift_struct.datatype = thrift_struct[1].datatype;
			thrift_struct.typedef = thrift_struct[1].typedef;
		}
		else 
		{
			thrift_struct = meta_structs[struct_type_id]; //구조체 key : value로 나열됨 
			thrift_struct.datatype = "rec";
			thrift_struct.typedef = "rec";
		}

		return thrift_struct;
	};

	this.orderedMetaSchema = function ()
	{
		var struct_name = '__' + this.m_meta_namespace + '__data';
		return this.takeOrderedThriftStruct(this.m_meta_struct_name);
	};

	this.takeOrderedThriftStruct = function (struct_name)
	{
		var meta_structs = this.metaStructs();
		if (null == meta_structs)
        {
            console.log("metaStructs is null");
			return null;
		}
		var meta_schema = meta_structs[struct_name];
		var ordered_schema = {};
		
		ordered_schema['name'] = struct_name;
		for (var thrift_field_id in meta_schema)
		{
			var schema_info = meta_schema[thrift_field_id];
			ordered_schema[schema_info.order] = schema_info;
		}
		return ordered_schema;
	};

	this.selectedColumn = function()
	{
		// row, column 		
	}

	//--------------------------------------------------------------------------------
	//- data table enum 
    this.enumStruct = function (enum_name)
    {
        var enum_struct = this.m_schema.enums[enum_name];
        if (typeof enum_struct == 'undefined')
        {
            return null;
        }
        return enum_struct;
    };
    
    this.makeEnumOptions = function(enum_type_name)
    {
        var options = [];
		var enums = this.enumStruct(enum_type_name);
        for (enum_index in enums)
        {
			var enum_value = enums[enum_index];
            //if (enum_value == '_END' || enum_value == '_BEGIN')
			if (enum_value == '_END')
            {
                continue;
			}
			options.push({id: parseInt(enum_index), value:'[' + enum_value + ' : ' + enum_index + ']'});
		}
		//console.log('enum_options', options);
        return options;
	};

	this.columnCompare = function(value, filter)
	{
		value = value.toString.toLowerCase();
		filter = filter.toString().toLowerCase();
		return (value.indexOf(filter) == 0);
	};

	this.takePrimitiveThrift = function (thrift_type_id)
	{
		// structs에서 
	}

	// [[string, backgorund_color] ... ]
	this.makeHeaderArray = function(header_list)
	{
		var out_list = []
		for (var idx in header_list) 
		{
			var header_text = header_list[idx][0];
			var bg_color = header_list[idx][1];
			if (typeof bg_color == "undefined")
			{
				out_list.push({ text: header_text, css: {"text-align":"center"} });
			}
			else 
			{
				out_list.push({ text: header_text, height: 18, css: { "background": bg_color, "text-align":"center" } });
			}
		}
		return out_list;
	}

	//- webix datatable header column
	this.makeWebixHeaderColumn = function (my_thrift_info, parent_thrift_info, hierarchy_info, out_webix_columns)
	{
		if (null == parent_thrift_info) // for recursive processing
		{
			hierarchy_info.m_field_name = my_thrift_info.name;
			hierarchy_info.m_field_path = my_thrift_info.thrift_index;
		}
		else
		{
			hierarchy_info.m_field_name = my_thrift_info.name + "." + hierarchy_info.m_field_name;
			hierarchy_info.m_field_path = hierarchy_info.m_field_path + "." + my_thrift_info.thrift_index;
		}

		var header_hier_info = {
			m_hier_thrift_field_name: hierarchy_info.m_field_name,
			m_hier_thrift_field_path: hierarchy_info.m_field_path,
			m_hier_thrift_type_id: hierarchy_info.m_thrift_type_id,
			m_thrift_info: my_thrift_info
		}

		//console.log("hier_thrift_info:", hier_thrift_info);
		var thrift_field_name = hierarchy_info.m_field_name;
		var thrift_field_path = hierarchy_info.m_field_path;
		var header_column_info = null;

		if (typeof (my_thrift_info.doc) == 'undefined')
		{
			my_thrift_info.doc = "";
		}

		//console.log("[makeWebixHeaderColumn]", my_thrift_info);

		var thrift_define_name = this.thriftDefineName(my_thrift_info.typeId);
		var tooltip_text = "" + thrift_field_path + ": <span style='color:lightgreen'>" + thrift_define_name + "</span> " + thrift_field_name ;

		//조건을 체크하는 순서에 유의한다. 

		if ('_meta_id' == my_thrift_info.typedef)
		{
            header_column_info = {
				id: thrift_field_path,
				header: this.makeHeaderArray([[thrift_field_path, "#886666"], [thrift_define_name, "#966084"], [thrift_field_name, "#886666"]]),
				css: {"color":"#9b84b2"}, // data css
                template: "{common.treetable()} #value#",
                tooltip: tooltip_text,
                width: 200,
				sort: 'int',

                m_hier_info: header_hier_info,
            };
            out_webix_columns.push(header_column_info);
        }
        else if ('_msort_id' == my_thrift_info.typedef) {
			header_column_info = {
				id: thrift_field_path,
				header: this.makeHeaderArray([[thrift_field_path, "#886666"], [thrift_define_name, "#966084"], [thrift_field_name, "#886666"]]),
                tooltip: tooltip_text,
                adjust: 'header',
                sort: 'int',
                editor: 'text',
				hidden: 'true',

                m_hier_info: header_hier_info,
            };
            out_webix_columns.push(header_column_info);
        }
 		else if ('_mname' == my_thrift_info.typedef) 
		{
			header_column_info = {
				id: thrift_field_path,
				header: this.makeHeaderArray([[thrift_field_path, "#886666"], [thrift_define_name, "#966084"], [thrift_field_name, "#886666"]]),
				css: {"color":"lightgreen"},
				tooltip: tooltip_text,

                minWidth: 80,
                adjust: "data",
                sort: 'string',
				editor: 'text',

                m_hier_info: header_hier_info,
            };
            out_webix_columns.push(header_column_info);
        }
		else if ('_mnote' == my_thrift_info.typedef) 
		{
			header_column_info = {
				id: thrift_field_path,
				header: this.makeHeaderArray([[thrift_field_path, "#886666"], [thrift_define_name, "#966084"], [thrift_field_name, "#886666"]]),
                tooltip: tooltip_text,
                editor: 'popup',
				sort: 'string',

                m_hier_info: header_hier_info,
            };
            out_webix_columns.push(header_column_info);
		}
		else if (this.isLinkDataType(my_thrift_info)) 
		{
			link_meta_name = this.takeLinkMetaName(my_thrift_info);

			header_column_info = {
				id: thrift_field_path,
				header: this.makeHeaderArray([[thrift_field_path, "#886666"], [thrift_define_name, "#966084"], ['~' + thrift_field_name, "#886666"]]),
				css: { "color": "lightblue" },
				tooltip: tooltip_text,
				adjust: 'data',
				sort: 'int',
				editor: 'text', // meta_id / meta_name, not editable 
				editable: false, // 기본 수정은 불가능하다.,
				minWidth: 80,

				m_hier_info: header_hier_info, // hier_thrift_info의 typedef가 link_id이면 링크이다.
				m_link_meta_name: link_meta_name,
				m_is_meta_link: true,
			}; //column
			out_webix_columns.push(header_column_info);
		}
		else if ('rec' == my_thrift_info.datatype) 
		{
            var rec_thrift_info = this.takeOrderedThriftStruct(my_thrift_info.typeId);
			if (null != rec_thrift_info)
			{
				var rec_thrift_field_path = thrift_field_path + '.rec';

				var thrift_define_name = this.thriftDefineName(my_thrift_info.typeId);
				var tooltip_text = "" + rec_thrift_field_path + ": <span style='color:lightgreen'>" + thrift_define_name + "</span>  " + thrift_field_name + "  " + my_thrift_info.doc;
				//rec root header
				header_column_info = {
					id: rec_thrift_field_path,
					header: this.makeHeaderArray([[thrift_field_path, "#664444"], [thrift_define_name, "#744062"], [thrift_field_name, "#340320"]]),
					adjust: "header",
					css: { "text-align": "center" },
					tooltip: tooltip_text,
					m_hier_info: header_hier_info
				};
				out_webix_columns.push(header_column_info);

				var parent_thrift_info = my_thrift_info;
				for (var rec_thrift_field_id in rec_thrift_info) 
				{
					if ("name" == rec_thrift_field_id) 
					{
                        continue;
                    }
					rec_schema_info = rec_thrift_info[rec_thrift_field_id];

                    var child_hierarchyInfo = {
                        m_thrift_type_id: my_thrift_info.typeId,
                        m_field_name: hierarchy_info.m_field_name,
                        m_field_path: rec_thrift_field_path
					}
					this.makeWebixHeaderColumn(rec_schema_info, parent_thrift_info, child_hierarchyInfo, out_webix_columns);
                }
            }
        }
		else if ('lst' == my_thrift_info.datatype)
		{
			var lst_thrift_field_path = thrift_field_path + '.lst';
            var lst_elem_type_id = this.thriftListElemName(my_thrift_info.typeId);

			var thrift_define_name = this.thriftDefineName(my_thrift_info.typeId);
			var tooltip_text = "" + lst_thrift_field_path + ": <span style='color:lightgreen'>" + thrift_define_name + "</span>  " + thrift_field_name + "  " + my_thrift_info.doc;

			//console.log("[makeWebixHeaderColumn] lst", elem_typeid);
			if (this.isPrimitiveThriftTypeId(lst_elem_type_id)) //lst정의 컬럼을 그대로 사용한다. 
			{
				header_column_info = {
					id: lst_thrift_field_path,
					header: this.makeHeaderArray([[thrift_field_path, "#446644"], [thrift_define_name, "#407462"], [thrift_field_name, "#033420"]]),
					adjust: "header",
					editor: "text",
					editable: true,
					tooltip: tooltip_text,
					m_hier_info: header_hier_info
				};

				if (this.isLstElemLinkData(my_thrift_info))
				{
					var link_meta_name = this.takeLinkMetaName(my_thrift_info);
  					header_column_info.editable = false;
					header_column_info.m_link_meta_name = link_meta_name; //name_splited[1];
					header_column_info.m_is_meta_link = true;

					//console.log("[makeWebixHeaderColumn] link_meta_name", link_meta_name);

				}
				out_webix_columns.push(header_column_info);
				//console.log("[makeWebixHeaderColumn] primitive elem_typeid:", elem_typeid);

			}
			else // structs, collections 
			{
				//lst root header
				header_column_info = { 
					id: lst_thrift_field_path, 
					header: this.makeHeaderArray([[thrift_field_path, "#446644"], [thrift_define_name, "#407462"], [thrift_field_name, "#033420"]]),
					adjust: "header",
					tooltip: tooltip_text,
					m_hier_info: header_hier_info
				};
				out_webix_columns.push(header_column_info);
				   
				//console.log("[makeWebixHeaderColumn] elem_typeid:", elem_typeid);

				var elem_thrift_info = this.takeOrderedThriftStruct(lst_elem_type_id);
				if (typeof elem_thrift_info != 'undefined')
				{
					for (var field_id in elem_thrift_info)
					{
						if ("name" == field_id)
						{
							continue;
						}
						var field_thrift_info = elem_thrift_info[field_id];

						var child_hierarchyInfo = {
							m_thrift_type_id: field_thrift_info.typeId,

							m_field_name: hierarchy_info.m_field_name,
							m_field_path: lst_thrift_field_path
						}
						this.makeWebixHeaderColumn(field_thrift_info, my_thrift_info, child_hierarchyInfo, out_webix_columns);
					}
				}
			}
        }
		else if ('map' == my_thrift_info.datatype) 
		{
            header_column_info = {
				id: thrift_field_path + ".map",
				header: this.makeHeaderArray([[thrift_field_path, "#562542"], [thrift_define_name, "#562542"], [thrift_field_name, "#562542"]]),
                tooltip: tooltip_text,
                m_hier_info: header_hier_info
            };
            out_webix_columns.push(header_column_info);
        }
		else if ('set' == my_thrift_info.datatype) 
		{
            header_column_info = {
				id: thrift_field_path + ".set", 
				header: this.makeHeaderArray([[thrift_field_path, "#562542"], [thrift_define_name, "#562542"], [thrift_field_name, "#562542"]]),

                tooltip: tooltip_text,
                m_hier_info: header_hier_info
            };
            out_webix_columns.push(header_column_info);
		}
		else if ('enum' == my_thrift_info.datatype) 
		{
			var enum_options = this.makeEnumOptions(my_thrift_info.typeId); 
            var header_column_info = {
				id: thrift_field_path,
				header: this.makeHeaderArray([[thrift_field_path, "#888866"], [thrift_define_name, "#966084"], ['[' + thrift_field_name + ']', "#562542"]]),
				css: {"color":"#c0b0ff"},
                m_hier_info: header_hier_info,
                tooltip: tooltip_text,
				sort: 'int',
				adjust: "header",
                editor: 'combo',
                options: enum_options,
                suggest: {
                    fitMaster: false,
					width:250,
                    filter: function(item, entered_value) {
                        var item_value = item.value.toLowerCase();
                        return (item_value.indexOf(entered_value.toLowerCase()) != -1);
                    }
                }
			};
			//console.log("enum_header:", header_column_info, enum_options);
            out_webix_columns.push(header_column_info);
        }
        else if ('tf' == my_thrift_info.datatype) //bool
        {
            header_column_info = {
				id: thrift_field_path,
				header: this.makeHeaderArray([[thrift_field_path, "#886666"], [thrift_define_name, "#966084"], ['(' + thrift_field_name + ')', "#562542"]]),
                tooltip: tooltip_text,
                m_hier_info: header_hier_info,

                editor: "checkbox",
                template: "{common.checkbox()}",
                css: { "text-align": "center" },
				adjust: 'header',
				sort: 'int'
			};
			out_webix_columns.push(header_column_info);
		}
		else if ('i8' == my_thrift_info.datatype)
		{
			header_column_info = {
				id: thrift_field_path,
				header: this.makeHeaderArray([[thrift_field_path, "#886666"], [thrift_define_name, "#966084"], [thrift_field_name, "#886666"]]),
				tooltip: tooltip_text,
				m_hier_info: header_hier_info,
				editor: 'text',
				sort: 'int'
			};
			out_webix_columns.push(header_column_info);
		}
		else if ('i16' == my_thrift_info.datatype)
		{
			header_column_info = {
				id: thrift_field_path,
				header: this.makeHeaderArray([[thrift_field_path, "#886666"], [thrift_define_name, "#966084"], [thrift_field_name, "#886666"]]),
				tooltip: tooltip_text,
				m_hier_info: header_hier_info,
				editor: 'text',
				sort: 'int'
			};
			out_webix_columns.push(header_column_info);
		}
		else if ('i32' == my_thrift_info.datatype)
		{
			header_column_info = {
				id: thrift_field_path,
				header: this.makeHeaderArray([[thrift_field_path, "#886666"], [thrift_define_name, "#966084"], [thrift_field_name, "#886666"]]),
				tooltip: tooltip_text,
				m_hier_info: header_hier_info,
				editor: 'text',
				sort: 'int'
			};
			out_webix_columns.push(header_column_info);
		}
		else if ('i64' == my_thrift_info.datatype)
		{
			header_column_info = {
				id: thrift_field_path,
				header: this.makeHeaderArray([[thrift_field_path, "#886666"], [thrift_define_name, "#966084"], [thrift_field_name, "#886666"]]),
				tooltip: tooltip_text,
				m_hier_info: header_hier_info,
				editor: 'text',
				sort: 'int'
			};
			out_webix_columns.push(header_column_info);
		}
		else if ('dbl' == my_thrift_info.datatype)
		{
			header_column_info = {
				id: thrift_field_path,
				header: this.makeHeaderArray([[thrift_field_path, "#886666"], [thrift_define_name, "#966084"], [thrift_field_name, "#886666"]]),

				tooltip: tooltip_text,
				m_hier_info: header_hier_info,
				editor: 'text',
				//format: webix.Number.numToStr({decimalDelimiter:".", decimalSize:3}),
				sort: 'int'
			};
			out_webix_columns.push(header_column_info);

		}
		else if ('str' == my_thrift_info.datatype)
		{
			header_column_info = {
				id: thrift_field_path,
				header: this.makeHeaderArray([[thrift_field_path, "#886666"], [thrift_define_name, "#966084"], [thrift_field_name, "#886666"]]),

				tooltip: tooltip_text,
				m_hier_info: header_hier_info,
				sort: 'string',
				editor: 'text',
			};
			out_webix_columns.push(header_column_info);
		}
	};

	this.takeWebixHeaderColumn = function (thrift_field_path)
	{
		//데이터를 생성할 때 헤더를 매핑해두자. !!!
		for (var column_idx in this.m_webix_header)
		{
			var column_header = this.m_webix_header[column_idx];
			if (column_header.id == thrift_field_path)
			{
				return column_header;
			}
		}
		return null;
	}

	// metaschema -> datatable header 
	this.makeWebixHeader = function (remake)
	{
		if (this.m_webix_header == null || true == remake)
		{
			console.log('makeHeaderWebix');
			var webix_columns = [];

			var ordered_meta_schema = this.orderedMetaSchema();
			for (order_index in ordered_meta_schema)
			{
				if ("name" == order_index)
				{
					continue;
				}

				var hierarchyInfo = {
					m_thrift_type_id: this.m_meta_struct_name,
					m_field_name: '',
					m_field_path: ''
				}
				this.makeWebixHeaderColumn(ordered_meta_schema[order_index], null, hierarchyInfo, webix_columns);
			}
			this.m_webix_header = webix_columns;

			console.log("webix_header:", this.m_webix_header);
		}
 		return this.m_webix_header;
	};

	// meta_schema -> popup datatable header
	this.makeWebixHeaderLink = function ()
	{
		if (this.m_webix_link_header == null)
		{
			console.log('makeHeaderWebixLink');
			var webix_columns = [];
			var ordered_meta_schema = this.orderedMetaSchema();
			for (order_index in ordered_meta_schema)
			{
				if ("name" == order_index)
				{
					continue;
				}
				var elem_typedef = ordered_meta_schema[order_index].typedef;
				if (elem_typedef == "_meta_id" 
					|| elem_typedef == "_sort_id"
					|| elem_typedef == "_mname"
					|| elem_typedef == "_mnote")
				{
					var hierarchyInfo = {
						m_thrift_type_id: this.m_meta_struct_name,
						m_field_name: '',
						m_field_path: ''
					}
					this.makeWebixHeaderColumn(ordered_meta_schema[order_index], null, hierarchyInfo, webix_columns);
				}
			}
			this.m_webix_link_header = webix_columns;
		}
		return this.m_webix_link_header;
	};
	
	//--------------------------------------------------------------------------------
	//- data table rules 
	this.makeDataTableRule = function (my_thrift_info, parent_thrift_info, out_validation_rules)
	{
		var thrift_field_path = my_thrift_info.thrift_index;
		if (null != parent_thrift_info) // for recursive processing
		{
			thrift_field_path = parent_thrift_info.thrift_index + '.' + thrift_field_path;
		}

		var datatype = my_thrift_info.datatype;
		var typedef = my_thrift_info.typedef;
		var is_link = false;

		if (this.isLinkDataType(my_thrift_info))
		{
			is_link = true;
		}

		if ('rec' == datatype)
		{
			var rec_thrift_info = this.takeOrderedThriftStruct(my_thrift_info.typeId);
			if (null != rec_thrift_info)
			{
				for (var rec_thrift_field_id in rec_thrift_info)
				{
					this.makeDataTableRule(rec_thrift_info[rec_thrift_field_id], my_thrift_info, out_validation_rules);
				}
			}
		}
		// typedef 
		else if ('_meta_id' == typedef)
		{
		}
		else if ('_link_id' == typedef)
		{
		}
		else if ('_mname' == typedef)
		{
		}
		else if ('_mnote' == typedef)
		{
		}
		else if ('_msort_id' == typedef)
		{

		}
		else if (is_link)
		{

		}
         // type
		else if ('bool' == datatype)
		{
		}
		else if ('i8' == datatype)
		{
			// range check
			out_validation_rules[thrift_field_path] = function (value)
			{
				return webix.rules.isNumber(value);
			}
		}
		else if ('i16' == datatype)
		{
			// range check
			out_validation_rules[thrift_field_path] = function (value)
			{
				return webix.rules.isNumber(value);
			}
		}
		else if ('i32' == datatype)
		{
			out_validation_rules[thrift_field_path] = function (value)
			{
				return webix.rules.isNumber(value);
			}
		}
		else if ('i64' == datatype)
		{
			out_validation_rules[thrift_field_path] = function (value)
			{
				return webix.rules.isNumber(value);
			}
		}
		else if ('double' == datatype)
		{
			out_validation_rules[thrift_field_path] = function (value)
			{
				return webix.rules.isNumber(value);
			}
		}
		else if ('str' == datatype)
		{
		}
		else if ('lst' == datatype)
		{
			if (this.isLstElemLinkData(my_thrift_info))
			{
				is_link = true;
			}
		}
		else if ('map' == datatype)
		{
		}
		else if ('set' == datatype)
		{
		}
		else if ('enum' == datatype)
		{
		}
	};

	this.makeDataTableRules = function ()
	{
		//console.log('makeDataTableRules');
		var validation_rules = {};
		
		var meta_schema = this.metaSchema();
		for (var thrift_field_id in meta_schema)
		{
			this.makeDataTableRule(meta_schema[thrift_field_id], null, validation_rules);
		}
		return validation_rules;
	};
	
	this.firstObjectKey = function(target_obj)
	{
		if (typeof target_obj == 'undefined' ||
			target_obj == null)
		{
			return null;
		}

		var keys = Object.keys(target_obj);
		return keys[0];
	};

	this.takeDataPath = function()
	{
		var meta_filename = this.m_meta_category + "@oge.xlsx";
		var data_path = this.m_data_path.replace(meta_filename, "");
		return data_path;
	}

	//메타가 변경될 때 link container도 같이 변경 
	this.linkContainer = function(link_typedef)
	{
		// 링크 데이터의 카테고리를 알아낸다.
		var link_meta_name = link_typedef.replace("_link_", "") + "@oge.xlsx";

		// 링크 데이터 경로를 알아 로딩한다. 
		var link_data_path = this.takeDataPath() + link_meta_name;

		var link_container = MetaLogic.getMetaContainer(link_data_path);
		return link_container;
	}

	this.findMetaRowByMetaId = function (meta_id)
	{
		for (row_id in this.m_meta_rows)
		{
			if (row_id == meta_id)
			{
				return this.m_meta_rows[row_id];
			}
		}
		return null;
	}

	this.makeWebixColumnDataLink = function (column_info, thrift_column_data, out_column_data)
	{
		var header_field_path = column_info.header_field_path;
		var data_field_path = column_info.data_field_path;

		var thrift_info = column_info.thrift_info;
		var thrift_typeid = column_info.thrift_typeid;

		var link_meta_name = this.takeLinkMetaName(column_info.thrift_info);
		var link_container = this.linkContainer(link_meta_name);
		//console.log("[makeWebixColumnDataLink]", link_meta_name, thrift_column_data);

		var link_name = '';
		var link_row = link_container.findMetaRowByMetaId(thrift_column_data);
		if (null == link_row)
		{
			link_name = "<span style='color:#704040'>*MIA*</span>";
		}
		else 
		{
			link_name = "<span style='color: #704040'>no name</span>" // meta data는 고정이라고 하자 (아니면 스키마 대조 해서 가져오는 함수 생성)
			var meta_name_field = link_row["3"];
			if (typeof meta_name_field != 'undefined')
			{
				link_name = link_row["3"].str;
			}
		}
		out_column_data[header_field_path] = thrift_column_data + ": " + link_name;
		out_column_data['_datapath_' + header_field_path] = data_field_path;
	}

	this.makeWebixColumnDataRec = function (column_info, out_column_data, out_container_root)
	{
		var thrift_column_datatype = column_info.thrift_info.datatype; //this.firstObjectKey(column_info.thrift_column_data);
		if ('enum' == thrift_column_datatype)
		{
			thrift_column_datatype = 'i32';
		}
		if (typeof column_info.thrift_column_data != 'undefined')
		{
			var thrift_column_data = column_info.thrift_column_data[thrift_column_datatype];
		}
		else    
		{
			thrift_column_data = null;
		}
		var header_field_path = column_info.header_field_path;
		var data_field_path = column_info.data_field_path;
		var thrift_info = column_info.thrift_info;
		var thrift_typeid = column_info.thrift_typeid;
		//--------------------------------------------------
		//column_info.no_rec_path is_collection_root_elem
		var rec_thrift_info = this.takeThriftStruct(thrift_typeid);
		if (null == rec_thrift_info)
		{
			console.log('error takeSchema:' + thrift_info.typeId);
			return;
		}

		var rec_header_field_path = header_field_path;
		var rec_data_field_path = data_field_path;
		//console.log("rec_data_field_path:", rec_data_field_path);
		var rec_path = '.rec.';
		if (true == column_info.no_rec_path) // container바로 아래 붙어있는 경우 .rec를 붙이지 않는다. 
		{
			rec_path = '.';
			//console.log("[makeWebixColumnDataRec]is_collection_root_elem no rec", rec_thrift_info, thrift_typeid);
		}

		var warn_msg = '';
		//rec 컨테이너 일경우와 아닌경우 처리 방식이 달라야    
		for (var rec_field_id in thrift_column_data)
		{
			//데이터에는 있는데 스키마에 없는 경우 : 관련하여 버젼을 확인하고 오류메시지를 또는 경고 메시지를 전달한다.  
			// 1. 스키마 최신, 데이터 구버젼 (비교할 수 있는 정보가 데이터에 존재하지 않는다. 엑셀데이터에 버젼기록이 필요함. 버젼필드 추가)
			// 2. 스키마 최신, 서버로직 구버젼 
			// 3. 스키마 구버젼, 서버로직 신버젼 
			if (typeof rec_thrift_info[rec_field_id] == 'undefined')
			{
				//경고 메시지로 출력 
				warn_msg = "[WARN] SCHEMA rec_thrift_info not have DATA's rec_field_id:" + rec_field_id + ", data and schema not match. please check version and data";
				//console.log(msg);
				continue;
			}

			try
			{
				var rec_column_thrift_info = rec_thrift_info[rec_field_id].typeId;
			}
			catch (ex)
			{
				var err_msg = MetaCommon.exception_message(ex, '[thrift_schema & column_data mismatch]\n column_data exist but schema field not exist\n thrift_typeid:' + thrift_typeid + ' rec_field_id:' + rec_field_id + '\nheader_field_path:' + header_field_path);
				console.log(rec_thrift_info, err_msg);

				throw err_msg;
			}

			var rec_column_info = {
				//header_field_path: rec_header_field_path + '.' + rec_field_id,
				header_field_path: rec_header_field_path + rec_path + rec_field_id,

				data_field_path: rec_data_field_path + rec_path + rec_field_id,
				thrift_column_data: thrift_column_data[rec_field_id],

				thrift_info: rec_thrift_info[rec_field_id],
				thrift_typeid: rec_thrift_info[rec_field_id].typeId,
				no_rec_path: false
			};
			//console.log("rec_column_info:", rec_column_info);

			//루트 데이터에 테이블 스키마 이름 출력 
			if (out_column_data.m_is_container)  
			{
				rec_column_header_field_path = rec_column_info.header_field_path;
				if ('lst' == rec_column_info.thrift_info.datatype) // container일 때 container의 datatype을 붙여준다. 
				{
					rec_column_header_field_path += ".lst";
				}
				var column_header = this.takeWebixHeaderColumn(rec_column_header_field_path);
				if (null != column_header)   
				{
					if ('enum' == column_header.m_hier_info.m_thrift_info.datatype)
					{
						out_container_root[column_header.id] = "-1";
					}
					else 
					{
						var header_text = column_header.header[0].text;
						out_container_root[column_header.id] = "<span style='color:lightblue;background-color:#0a0a53;font-size:12px'>" + header_text + "</span>";
					}
				}
			}
			//console.log("[REC_COLUMN_INFO", rec_column_info);
			this.makeWebixColumnData(rec_column_info, out_column_data, out_container_root);
		}

		if (warn_msg != '')
		{
			webix.message(warn_msg, "warn");
		}

		// meta schema's name
		var thrift_define_name = this.thriftDefineName(thrift_typeid);
		out_column_data[header_field_path] = "<span style='color:#804040;font-size:10px'>{ " + thrift_define_name + " }</span>";
		out_column_data['_datapath_' + header_field_path] = data_field_path;
	}

	this.makeWebixColumnDataLst = function (column_info, out_column_data, out_container_root)
	{
		var thrift_column_datatype = column_info.thrift_info.datatype; //this.firstObjectKey(column_info.thrift_column_data);
		if ('enum' == thrift_column_datatype)
		{
			thrift_column_datatype = 'i32';
		}

		if (typeof column_info.thrift_column_data != 'undefined')
		{
			var thrift_column_data = column_info.thrift_column_data[thrift_column_datatype];
		}
		else 
		{
			thrift_column_data = null;
		}

		var header_field_path = column_info.header_field_path;
		var data_field_path = column_info.data_field_path;
		var thrift_info = column_info.thrift_info;
		var thrift_typeid = column_info.thrift_typeid;
		//--------------------------------------------------


		// ['i32', count]
		var lst_header_field_path = header_field_path + '.lst';
		var lst_data_field_path = data_field_path + '.lst';

		var container_count = 0;
		if (null != thrift_column_data)
		{
			container_count = thrift_column_data[1];
		}

		if (container_count > 0)
		{
			out_column_data[header_field_path] = thrift_column_datatype + '(' + container_count + ')';
		}
		else
		{
			out_column_data[header_field_path] = 'empty_' + thrift_column_datatype;
		}
		out_column_data['_datapath_' + lst_header_field_path] = lst_data_field_path;

		var row_id = out_column_data.m_row_id;//["1"]; // datatable 1,2,3은 메타 테이블에서 예약 번호

		var lst_root_header_field_path = row_id + "." + lst_header_field_path; // 새로운 줄이 추가 될 때 마다 row_id를 추가한다.
		var lst_root_data_field_path = row_id + "." + lst_data_field_path;

		var lst_elem_name = this.listContainerType(column_info.thrift_info.typeId);
		var lst_name = column_info.thrift_info.name;

		// root : 리스트정보 표현
		var out_lst_root = {
			id: lst_root_data_field_path,
			value: lst_name,

			m_row_id: row_id,
			m_is_container: true,
			m_is_container_root: true,
			m_container_header_path: lst_header_field_path,
			m_container_data_path: lst_data_field_path,
			data: []
		}

		var root_column_header = this.takeWebixHeaderColumn(lst_header_field_path);
		if (null != root_column_header) //없으면 안됨.  
		{
			var header_text = root_column_header.header[0].text;
			out_lst_root[root_column_header.id] = "<span style='color:lightpink;font-size:12px;background-color:#0a0a53'>" + header_text + "</span>";
		}
		//1-4 필드는 meta table의 예약 필드
		out_lst_root["1"] = row_id; //
		out_lst_root["3"] = "lst (" + container_count + ")";
		if (container_count == "0")
		{
			out_lst_root["3"] = "empty lst";
		}
		if (null == thrift_column_data) // 리스트 루트까지만 만들어줌
		{
			//console.log("[makeWebixColumnDataLst] thrift_column_data is null : no list, just add lst_root", thrift_column_data);
			out_column_data.data.push(out_lst_root);
			return;
		}

		var lst_datas = thrift_column_data;
		var elem_datatype = lst_datas[0];

		for (var lst_data_idx in lst_datas) //list array iterate
		{
			if (lst_data_idx < 2) //['type', 'count', ...] //datatype count 스킵 
			{
				continue;
			}

			var elem_container_header_path = lst_header_field_path;
			var elem_container_data_path = lst_data_field_path + "." + "[" + lst_data_idx + "]";

			var elem_typeid = this.thriftListElemName(thrift_typeid);
			var elem_thrift_info = this.takeThriftStruct(elem_typeid); 
			var elem_value = {};
			elem_value[elem_datatype] = lst_datas[lst_data_idx];

			//리스트 요소 entrypoint 추가 
			var lst_elem_idx = lst_data_idx - 2;
			var out_lst_elem = {
				id: lst_root_data_field_path + '.[' + lst_data_idx + ']', //id는 uniq해야함. 제대로 나옴 treetable은 data아래 출력이 됨.
				value: lst_name + "[" + lst_elem_idx + "]",
				data: [],

				m_row_id: row_id,
				m_is_container: true,
				m_is_container_root: false,

				m_data_idx: lst_data_idx,
		  		m_container_header_path: lst_header_field_path,
				m_container_data_path: lst_data_field_path
			};

			var column_info = {
				header_field_path: elem_container_header_path, //row id 정보가 없는 header_path
				data_field_path: elem_container_data_path,
				thrift_column_data: elem_value,

				thrift_info: elem_thrift_info,
				thrift_typeid: elem_typeid,
				no_rec_path: true

			};
			this.makeWebixColumnData(column_info,  out_lst_elem, out_lst_root);

			out_lst_root.data.push(out_lst_elem);
		}

		//list elem type id show data cell
		var elem_typeid = this.thriftListElemName(thrift_typeid);

		out_column_data[lst_header_field_path] = "<span style='color:#804040;font-size:10px'>{ " + this.thriftDefineName(elem_typeid) + " }</span>";
		out_column_data['_datapath_' + header_field_path] = data_field_path;

		//push list tree data 
		out_column_data.data.push(out_lst_root);
	}

	//----------------------------------------------------------------------------
	//- data table rows
	// out_webix_datas : 리스트등 컨테이너 데이터 생성에 사용함. 
	this.makeWebixColumnData = function (column_info, out_column_data, out_container_root)
	{
		var thrift_column_datatype = column_info.thrift_info.datatype; //this.firstObjectKey(column_info.thrift_column_data);
		if ('enum' == thrift_column_datatype)
		{
			thrift_column_datatype = 'i32';
		}

		if (typeof column_info.thrift_column_data != 'undefined')
		{
			var thrift_column_data = column_info.thrift_column_data[thrift_column_datatype];
		}
		else 
		{
			thrift_column_data = null;
		}

		var header_field_path = column_info.header_field_path;
		var data_field_path = column_info.data_field_path;

		var thrift_info = column_info.thrift_info;
		var thrift_typeid = column_info.thrift_typeid;

		  
		if (this.isLinkDataType(column_info.thrift_info))
		{
			//console.log("[makeWebixColumnData] link type column:", column_info);
			this.makeWebixColumnDataLink(column_info, thrift_column_data, out_column_data);
			return;
		}

		switch (thrift_column_datatype)
		{
			case 'tf':
			case 'i8':
			case 'i16':
			case 'i32':  
			case 'i64':
			case 'dbl':
			case 'str':
				out_column_data[header_field_path] = thrift_column_data;
				out_column_data['_datapath_' + header_field_path] = data_field_path;
				break;

			case 'rec':
				this.makeWebixColumnDataRec(column_info, out_column_data, out_container_root);
				break;

			case 'lst':
				this.makeWebixColumnDataLst(column_info, out_column_data, out_container_root);
				break;

			case 'map':
				// ['i32', 'rec', count, ...
				var map_count = thrift_column_data[2];
				if (map_count > 0)
				{
					out_column_data[header_field_path] = thrift_column_datatype + '(' + map_count + ')';
				}
				else
				{
					out_column_data[header_field_path] = thrift_column_datatype;
				}
				break;

			case 'set':
				break;
		} // switch case 

	};

	this.checkSchemaData = function(meta_row)
	{
		// 데이터와 스키마가 일치 하지 않습니다. 데이터를 수정하시면 스키마 기준으로 데이터가 저장됩니다.
		var warn_text = "데이터와 스키마가 일치하지 않습니다. meta[" + this.m_data_path + "]의 thrift_index:" + thrift_index + "는 표시 되지 않습니다. 데이터를 저장하면 스키마 기준으로 저장합니다.";
		webix.alert({
			title: "WARN ",
			text: warn_text,
			ok: "Close"
		});
	};
	 	
	this.makeWebixRowData = function (row_id, meta_row, out_webix_datas)
	{
		var meta_schema = this.metaSchema();
		var out_webix_data_row = {
			id: row_id,
			value: row_id,
			m_row_id : row_id,
			data:[]
		};
		// 스키마에 없는 경우: 스키마가 오래됐거나 갱신되었거나
		// 헤더중심으로 데이터를 생성하도록 하자 (메타 에디터는 데이터가 없이 스키마로도 항목이 필요함- 컨테이너처리 )
		//for (var thrift_field_id in meta_row)
		for (var thrift_field_id in meta_schema)
		{
			var thrift_info = meta_schema[thrift_field_id];
			if (typeof (thrift_info) == 'undefined')
			{
				console.log("meta_row's thrift_index:", thrift_field_id, " is not exist. data not show.");
				continue;
			}
			var column_info = {
				header_field_path: thrift_field_id, //데이터 테이블 출력 
				data_field_path: thrift_field_id, //쓰리프트 데이터 저장경로
				thrift_column_data : meta_row[thrift_field_id],

				thrift_info: meta_schema[thrift_field_id],
				thrift_typeid: meta_schema[thrift_field_id].typeId,
				no_rec_path : false // struct lst인경우 최초 path에 rec가 들어가지 않는다.
			};
			this.makeWebixColumnData(column_info, out_webix_data_row);    
		}
		out_webix_datas.push(out_webix_data_row);
	};    

	this.makeWebixData = function ()
	{
		var out_webix_datas = [];
		for (row_id in this.m_meta_rows)
		{
			this.makeWebixRowData(row_id, this.m_meta_rows[row_id], out_webix_datas);
		}
		return out_webix_datas;
	};

	this.getFirstIndex = function (thrift_hier_index)
	{
		var index = thrift_hier_index;

		var words = thrift_hier_index.split('.');
		if (words.length > 1)
		{
			index = words[0];
		}
		return index;
	};

	this.getLastIndex = function (thrift_hier_index)
	{
		var index = thrift_hier_index;

		var words = thrift_hier_index.split('.');
		if (words.length > 1)
		{
			index = words[words.length-1];
		}
		return index;
	};

	// typeid to primitive thrift datatype 
	this.toThriftRawDataType = function (a_thrift_typeid)
	{
		var thrift_typeid = a_thrift_typeid;

		if ("enum" == thrift_typeid)
		{
			return "i32";
		}

		if (this.isPrimitiveThriftTypeId(a_thrift_typeid))
		{
			// typedef typeId를 datatype으로 변환해줄 필요가 있다. 
			//(typedef를 다른 용도로 사용하지 않도록 하자.기본형만 typedef를 사용)
			var typedef = this.m_schema_typedefs[thrift_typeid];
			if (typeof typedef != 'undefined')
			{
				thrift_typeid = typedef["typeId"];
			}
			switch (thrift_typeid)
			{
				case "i8":
				case "i16":
				case "i32":
				case "i64":
					return thrift_typeid;
				case "enum":
					return "i32";
				case "bool":
					return "tf";
				case "byte":
					return "i8";
				case "double":
					return "dbl";
				case "string":
					return "str";
				default:
					console.log("ERROR:[toThriftRawDataType]type_id is unknown type", typedef["typeId"]);
					break;
			}
		}

		// 2019/01/07 기본 데이터타입 변경 
		console.log("[toThriftRawDataType] thrift_typeid:", thrift_typeid);
		if (true == thrift_typeid.includes("list<"))
		{
			return "lst";
		}
		if (true == thrift_typeid.includes("map<"))
		{
			return "map";
		}
		if (true == thrift_typeid.includes("set<"))
		{
			return "set";
		}
		 
		//lst, rec
		//return datatype;
		return "rec";
	};
	

	// datatable cell 접근권한을 확인하기 위한 용도로 사용.
	this.makeRowNodeInfo = function(row_id)
	{
		var meta_schema = this.metaSchema();
		// row_id가 복합 lst_root_node 이면 해당 부분은 수정안함. 
		var node_info = {
			m_meta_row_id: -1,

			m_id: -1,
			m_thrift_datatype: "rec",
			m_thrift_info: meta_schema,

			m_parent_id: -1,
			m_parent_datatype: "rec",
			m_parent_thrift_info: {},

			m_root_id: "",
			m_is_lst_root: false,
			m_is_elem: false,
			m_is_elem_primitive: false,
		}

		var id_array = row_id.split(".");
		//console.log("[makeRowNodeInfo] id_array:", id_array, "row_id", row_id);
		for (var i in id_array)
		{
			var id = id_array[i];
			if (i == 0) // row_id set and skip
			{
				node_info.m_meta_row_id = id;
				continue;
			}
			if (id == 'lst')
			{
				//마지막인 'lst' (컨테이너로 끝나면) 루트가 되는 것이다. 
				var is_last_index = (i == id_array.length - 1);
				if (is_last_index)
				{
					node_info.m_is_lst_root = true;
				}
				continue;
			}

			if(id == 'rec')		//kangms add
			{
				continue;
			}

			node_info.m_id = id;
			if (this.isLstElemIdx(id))
			{
				var lst_elem_type_id = this.listContainerType(node_info.m_parent_thrift_info.typeId);
				//console.log("[makeRowNodeInfo]", lst_container_type);
				if (this.isPrimitiveThriftTypeId(lst_elem_type_id))
				{
					node_info.m_thrift_info = this.takeThriftStruct(lst_elem_type_id);
					node_info.m_thrift_datatype = this.toThriftRawDataType(lst_elem_type_id);
					node_info.m_is_elem_primitive = true;
					node_info.m_id = this.toThriftLstElemIdx(id);
					console.log("[makeRowNodeInfo] primitive type", lst_elem_type_id, "node_info", node_info);
				}
				else 
				{
					node_info.m_thrift_info = this.takeThriftStruct(lst_elem_type_id);
					node_info.m_thrift_datatype = node_info.m_thrift_info.datatype;
					node_info.m_id = this.toThriftLstElemIdx(id);
					console.log("[makeRowNodeInfo] thrift data", lst_elem_type_id, "node_info", node_info);
				}
				node_info.m_is_elem = true;
			}
			else 
			{
  				node_info.m_thrift_info = meta_schema[id];
				node_info.m_thrift_datatype = node_info.m_thrift_info.datatype;
			}

			// set parent info 
			var is_last_index = (i == id_array.length-1);
			if (is_last_index)
			{
				if ("lst" == node_info.m_thrift_datatype)
				{
					node_info.m_is_lst_root = true;
				}					
			}
			else
			{
				// make root id 
				if ("" == node_info.m_root_id)
				{
					node_info.m_root_id = id;
				}
				else
				{
					node_info.m_root_id = node_info.m_root_id + "." + id;
				}

				var field_info = meta_schema[id];
				if ('undefined' == typeof field_info)
				{
					continue; //존재하지 않는 정보 (요소 인덱스인 경우 무시)
				}

				node_info.m_parent_id = id;
				node_info.m_parent_datatype = meta_schema[id].datatype;
				node_info.m_parent_thrift_info = meta_schema[id];

				// get next meta_schema
				switch (meta_schema[id].datatype)
				{
					case "rec":
						meta_schema = this.takeThriftStruct(meta_schema[id].typeId);
						break;
					case "lst":
						meta_schema = this.takeThriftStruct(this.thriftListElemName(meta_schema[id].typeId));
						break;
				}
			}
		} // for (var i in id_array)
		return node_info;
	};

	this.isLstElemCell = function (row_id, column_id)
	{
		var in_lst = false;
		var schema = this.metaSchema();
		var hier_ids = column_id.split(".");
		console.log("hier_ids", hier_ids);
		for (var i in hier_ids)
		{

			var thrift_field_id = hier_ids[i];
			var thrift_info = schema[thrift_field_id];
			if (typeof thrift_info == 'undefined') // column header에 rec, lst등이 들어갈 경우 스키마 검색에서 제외한다. by joygram 2018/12/28
			{
				continue;
			}

			if ("rec" == thrift_info.datatype)
			{
				schema = this.takeThriftStruct(thrift_info.typeId);
			}
			else if ("lst" == thrift_info.datatype)
			{
				var lst_elem_type_id = this.listContainerType(thrift_info.typeId);
				if (false == this.isPrimitiveThriftTypeId(lst_elem_type_id))
				{
					schema = this.takeThriftStruct(lst_elem_type_id);
				}
				return true;
			}
		}
		return false;
	};

	this.isLstElemIdx = function(fields_idx)
	{
		return fields_idx.startsWith("[");
	};

	this.toThriftLstElemIdx = function(row_lst_elem_idx)
	{
		var lst_idx = parseInt(row_lst_elem_idx.split(/[\[\]]/)[1]) + 2; //extract elem_idx
		return lst_idx;		
	};

	this.makeThriftDataPathCode = function(datapath, meta_row)
	{
		console.log("[makeThriftDataPathCode] datapath:", datapath, "meta_row:", meta_row);
		  
		var thrift_datapath_str = "meta_row";
		var prev_row_str = "";

		var fields = datapath.split(".");
		for (var idx in fields)
		{
			
			if (this.isLstElemIdx(fields[idx])) // []이 존재하는 경우 붙이기 생략 by joygram 2018/12/20 
			{
				thrift_datapath_str += fields[idx];
			}
			else
			{
				thrift_datapath_str += '["' + fields[idx] + '"]';
			}

			if (typeof eval(thrift_datapath_str) == 'undefined')
			{
				if (fields[idx] == "lst") //어떤 lst타입인가 ? primitive or rec or container 
				{
					eval(thrift_datapath_str + "= [];");
					//lst_obj[0] // datatype // set lst elem datatype
					eval(thrift_datapath_str + "[1] = 0;"); // 초기 카운트 세팅 
				}
				else if (fields[idx] == "rec") //rec가 들어가는가. 보강 필요. rec를 현재는 안넣음 
				{
					eval(thrift_datapath_str + "= {};");
				}
				else 
				{
					//바로 전 경로가 lst인 경우 새로 요소를 추가하는 것이다. (thrift_json lst container 카운트 증가)
					var is_lst = idx > 0 && fields[idx - 1] == "lst";
					if (is_lst)
					{
						//eval(prev_row_str + "[1]++;"); //카운트 증가  
					}
					eval(thrift_datapath_str + "= {}"); //현재 경로에 객체 할당.
				}
				console.log("[makeThriftDataPathCode] undefined data_path:", thrift_datapath_str);
			}
			prev_row_str = thrift_datapath_str;
		}
		return thrift_datapath_str;
	};

	this.makeThriftDataPathCode4Update = function(webix_header_column_id, webix_row_id, meta_row)
	{
		console.log("[makeThriftDataPathCode4Update] column_id:", webix_header_column_id, " row_id:", webix_row_id, " meta_row:", meta_row);
		  
		//make row "[x]" list from webix_row_id
		var row_pos_list = new Array();
	
		var row_fields = webix_row_id.split(".");
		for (var idx in row_fields)
		{
			if (this.isLstElemIdx(row_fields[idx]))	//[] exist check
			{
				row_pos_list.push(row_fields[idx]);
			}	
		}
		
		//get "lst" count from webix_header_column_id
		var list_count = 0;
		var count_result = webix_header_column_id.match(/lst/g);
		if(count_result != null) 
		{
			list_count = count_result.length;
		}
		
		//make column + row id to thrift_data_path_str
		var thrift_data_path_str = "meta_row";
		var is_change_row_pos_for_lst = false;

		var column_fields = webix_header_column_id.split(".");
		for (var idx in column_fields)
		{
			if( column_fields[idx] == "lst" )
			{				
				is_change_row_pos_for_lst = true;
				
				thrift_data_path_str += '["' + column_fields[idx] + '"]';
			}
			else if( column_fields[idx] == "rec" )
			{
				thrift_data_path_str += '["' + column_fields[idx] + '"]';	
			}
			else
			{
				if(    ! isNaN(parseInt(column_fields[idx]) )
					&& is_change_row_pos_for_lst === true )
				{
					thrift_data_path_str += row_pos_list[count_result.length - list_count];
					thrift_data_path_str += '["' +  column_fields[idx] + '"]';
					
					list_count--;
					is_change_row_pos_for_lst = false;
				}
				else
				{					
					thrift_data_path_str += '["' + column_fields[idx] + '"]';				
				}
			}

			if (typeof eval(thrift_data_path_str) == 'undefined')
			{
				if (column_fields[idx] == "lst")
				{
					eval(thrift_data_path_str + "= [];");
					eval(thrift_data_path_str + "[1] = 0;");//set count data in "lst"
				}
				else if (column_fields[idx] == "rec")
				{
					eval(thrift_data_path_str + "= {};");
				}
				else
				{
					eval(thrift_data_path_str + "= {}");
				}
				console.log("[makeThriftDataPathCode4Update] undefined thrift_data_path_str:", thrift_data_path_str);
			}

		}

		if( true === is_change_row_pos_for_lst )
		{
			thrift_data_path_str += row_pos_list[count_result.length - list_count];

			list_count--;
			is_change_row_pos_for_lst = false;			
		}

		return thrift_data_path_str;
	};

	// 복합 구조체의 데이터 추가 
	this.addLstElemComplex = function (webix_row, webix_header)
	{
		var data = {};

		var column_header = this.takeWebixHeaderColumn(webix_row.m_container_header_path);
		if (null == column_header)
		{
			console.log("[addLstElemComplex] error: no column_header by header_path:", header_path);
			return data;
		}
			
		console.log("[addLstElem] column_header:", webix_header);
	
		var elem_data_type = "";
		
		if(typeof (column_header.m_hier_info.m_thrift_info.type.elemTypeId) != "undefined")
		{
			elem_data_type = column_header.m_hier_info.m_thrift_info.type.elemTypeId;			
		}
		else
		{
			elem_data_type = column_header.m_hier_info.m_thrift_info.type.typeId;
		}
	
		var thrift_struct = this.takeThriftStruct(elem_data_type);
		if (null == thrift_struct)
		{
			return data;
		}

		console.log("[addLstElem] elem_typid:", elem_data_type, "thrift_struct:", thrift_struct);
		for (var idx in thrift_struct)
		{
			var field_info = thrift_struct[idx];
			if (typeof field_info.datatype == 'undefined')
			{
				continue;
			}

			var elem_obj = {};
			console.log("[addLstElem] field_info:", field_info);
			switch (field_info.datatype)
			{
				case "enum":
					elem_obj["i32"] = 0;
					data[idx] = elem_obj;
					break;

				case "i8":
				case "i16":
				case "i32":
				case "i64":
					elem_obj[field_info.datatype] = 0;
					data[idx] = elem_obj;
					break;

				case "dbl":
					elem_obj[field_info.datatype] = 0.0;
					data[idx] = elem_obj;
					break;

				case "str":
					elem_obj[field_info.datatype] = "";
					data[idx] = elem_obj;
					break;

				case "lst": //for lst.lst
					if (this.isPrimitiveThriftTypeId(field_info.elemTypeId))
					{
						var elem_data_type = this.toThriftRawDataType(field_info.elemTypeId);
						elem_obj["lst"] = [elem_data_type, 0];
						console.log("[addLstElem] data_type", elem_data_type, " field_info:", field_info);
					}
					else
					{
						elem_obj["lst"] = ["rec", 0];
						console.log("[addLstElem] no primitive type", field_info.elemTypeId);
					}
					data[idx] = elem_obj;
					break;

				default:
					console.log("[addLstElem] not support make data yet.:", field_info.datatype);
					break;
			}
		}
		return data;
	}

	// 리스트 루트를 항상 받는다. 
	// 데이터에 리스트 항목추가
	//ex: meta_row["11"]["lst"]["1"]++;  //카운트 증가
	//    meta_row["11"]["lst"].push({}) //신규 리스트 추가 
	this.addLstElem = function (webix_row)
	{
		var row_node_info = this.makeRowNodeInfo(webix_row.id);  
		var meta_row = this.m_meta_rows[row_node_info.m_meta_row_id];

		var datapath = webix_row.m_container_data_path; 
		var meta_row = this.m_meta_rows[row_node_info.m_meta_row_id];

		var thrift_datapath_str = this.makeThriftDataPathCode(datapath, meta_row);

		var target_lst = eval(thrift_datapath_str);

		console.log("[addLstElem] webix_row", webix_row, "target_lst", target_lst, "thrift_datapath_str", thrift_datapath_str);

		var webix_header = this.makeWebixHeader();
		var column_header = this.takeWebixHeaderColumn(webix_row.m_container_header_path);
		if (null == column_header)
		{
			return target_lst;
		}

		target_lst[1]++;

		//리스트가 비어있는 경우 데이터 타입이 존재하지 않으므로 데이터 타입을 찾아낸다. 
		var elem_typeid = column_header.m_hier_info.m_thrift_info.type.elemTypeId;
		var elem_raw_datatype = this.toThriftRawDataType(elem_typeid);

		target_lst[0] = elem_raw_datatype;
		console.log("[addLstElem] target_lst[0]:", target_lst[0]);

		//기본형 타입인 경우 별도 처리를 수행해야 한다. 현재를 record중심으로 되어있음.
		if (this.isPrimitiveThriftDataType(elem_raw_datatype)) //typedef가 primitive로 동작하지 않음.
		{
			//target_lst[0] = root_node_info.m_thrift_datatype;
			target_lst.push(0);
		}
		else 
		{
			var data = this.addLstElemComplex(webix_row, webix_header);	//kangms remove
			//var data = this.addLstElemComplex(webix_row.m_container_header_path, webix_header, meta_row);		//kangms add
			target_lst.push(data);
			console.log("[addLstElem] complex target_lst:", target_lst);
		}
	};
     
	//ex: meta_row["11"]["lst"]["1"]--; //카운트 감소
	//    meta_row["11"]["lst"].splice(m_data_idx, 1); //요소 제거 스크립트     
	this.delLstElem = function (webix_row)
	{
		var row_node_info = this.makeRowNodeInfo(webix_row.id); 
		// list elem인경우 m_is_elem 검사, m_id 리스트 인덱스, m_parent_id : 쓰리프트 리스트 진입점 
		if (false == row_node_info.m_is_elem)
		{
			console.log("not lst elem, can not delete");
			return false;
		}

		var datapath = webix_row.m_container_data_path; //중첩 리스트인경우 경로 다시 검증.
		var meta_row = this.m_meta_rows[row_node_info.m_meta_row_id];
		var thrift_datapath_str = this.makeThriftDataPathCode(datapath, meta_row);

		console.log("[delLstElem] webix_row:", webix_row, "thrift_datapath_str", thrift_datapath_str);

		var target_lst = eval(thrift_datapath_str);
		if (target_lst[1] > 0) 		//update count
		{
			target_lst[1]--;
		}
		target_lst.splice(webix_row.m_data_idx, 1);

		console.log("REMOVED meta_row:", meta_row);
		return true;
	};

	/**
	컨테이너에 링크타입이 포함되어 있느지 확인하는 용도로 사용한다. 
	typedef: primitive인경우 _link_포함여부 

	typedef: lst 인경우 
	type:
		elemTypeId: _link_포함여부 
	*/
	this.isLinkDataType = function(thrift_info)
	{
		if (typeof thrift_info.typedef == 'undefined')
		{
			return false;
		}

		// primitive type 인경우 
		if (thrift_info.typedef.includes('_link_'))
		{
			return true;
		}
		return false;
	}

	this.isLstElemLinkData = function (thrift_info)
	{
		if (typeof thrift_info.typedef == 'undefined')
		{
			return false;
		}

		// 리스트 요소타입이 링크인경우 
		if ('lst' == thrift_info.typedef) //map, set
		{
			if (thrift_info.type.elemTypeId.includes('_link_'))
			{
				return true;
			}
		}
		return false;
	}

	this.hasLinkData = function (thrift_info)
	{
		if (this.isLinkDataType(thrift_info)) 
		{
			return true;
		}
		else if (this.isLstElemLinkData(thrift_info))
		{
			return true;
		}
		return false;
	}

	this.takeLinkMetaName = function (thrift_info)
	{
		if (typeof thrift_info.typedef == 'undefined')
		{
			return '';
		}

		if ('lst' == thrift_info.typedef)
		{
			var name_splited = thrift_info.type.elemTypeId.split('_link_');
			return name_splited[1];
		}
		var name_splited = thrift_info.typedef.split('_link_');
		return name_splited[1];
	}

	this.makeUpdateColumnData = function (webix_column_data, column_thrift_info, node_info)
	{ 
		console.log("[makeUpdateColumnData]:", webix_column_data, column_thrift_info, node_info);

		if (this.hasLinkData(column_thrift_info)) // 링크에서는 데이터를 분리함: `link_meta_id : string_name`
		{
			var value = webix_column_data.split(":")[0]; //앞자리만 가져오게 한다.
			console.log("[makeUpdateColumnData] link data value:", value, " weibx_column_data", webix_column_data);
			webix_column_data = value;
		}

		// 업데이트 데이터 만들기 // make column data 
		var thrift_datatype = this.toThriftRawDataType(column_thrift_info.datatype);
		if (node_info.m_is_elem_primitive)
		{ //lst_elem_type_id
			var lst_elem_name = this.thriftListElemName(column_thrift_info.typeId);
			thrift_datatype = this.toThriftRawDataType(lst_elem_name);
		}

		var meta_column_value = {};
		switch (thrift_datatype)
		{
			case 'i8':
			case 'i16':
			case 'i32':
			case 'i64':
			case 'tf':
			case 'enum':
				meta_column_value[thrift_datatype] = parseInt(webix_column_data);
				break;

			case 'dbl':
				meta_column_value[thrift_datatype] = parseFloat(webix_column_data);
				break;

			case 'str':
				meta_column_value[thrift_datatype] = webix_column_data;
				break;
	
			default:
				console.log("unknown datatype: thrift_datatype", thrift_datatype, "webix_column_data:", webix_column_data, "column_thrit_info:", column_thrift_info, "node_info:", node_info);
				break;
		} // switch case 

		if (node_info.m_is_elem_primitive) //primitive type인경우 값만 리턴하도록 한다.
		{
			return meta_column_value[thrift_datatype];
		}

		console.log("[makeUpdateColumnData] meta_column_value:", meta_column_value);

		return meta_column_value;
	};

	this.makeDataPath = function (webix_header_column, webix_row)
	{
		var header_id = webix_header_column.id;
		var column_thrift_info = webix_header_column.m_hier_info.my_thrift_info;

		console.log("[makeDataPath] header_id", header_id, " column_thrift_info:", column_thrift_info);
	}

	// RowNodeInfo와 webix_header_column를 조합하여 정보를 수집한다.
	this.updateMetaColumn = function (webix_header_column, webix_row)
	{
		this.makeDataPath(webix_header_column, webix_row);

		var webix_column_data = webix_row[webix_header_column.id];
		if (typeof webix_column_data === "undefined")
		{
			console.log("no webix_column for ", webix_header_column.id);
			return;
		}

		var column_thrift_info = webix_header_column.m_hier_info.m_thrift_info;

		//struct나 복합 구조체의 lst_root인 경우 필요한 갱신 데이터가 아님, 기본형 타입은 바로 아래 출력  
		if ("lst" == column_thrift_info.datatype)
		{
			var lst_elem_type_id = this.thriftListElemName(column_thrift_info.typeId); //extract elem type id 
			if (false == this.isPrimitiveThriftTypeId(lst_elem_type_id))
			{
				console.log("lst root not meta data, just skip. datatype is lst", webix_header_column.m_hier_info);
				return;
			}
		}
				
		var row_node_info = this.makeRowNodeInfo(webix_row.id);

        if (row_node_info.m_is_elem) // 데이터 테이블의 열이 리스트 데이터인 경우 
        {
            //if (!webix_header_column.id.startsWith(row_node_info.m_root_id)) // lst_elem인 경우 자신의 root가 아닌 것은 갱신하지 않음. //루트id에 점을 찍을 필요가 있음. 
            if (false == MetaCommon.isColumnNodeElement(row_node_info.m_root_id, webix_header_column.id, row_node_info.m_is_elem_primitive)) {
                console.log("webix_header_column.id:", webix_header_column.id, " is not lst " + row_node_info.m_root_id + "elem");
                return;
            }

            if (typeof datapath == 'undefined') //기존 데이터가 존재하지 않으므로 데이터 path를 생성해야함. 
            {
                var elem_datapath = webix_row['_datapath_' + webix_row.m_container_header_path];

                if (row_node_info.m_is_elem_primitive) {
                    console.log("[updateMetaColumn] is primitive row_node_info", row_node_info);
                    elem_datapath = webix_row.m_container_data_path + '.' + webix_row.m_data_idx;
                }
                else {
                    var last_column = webix_header_column.id.replace(webix_row.m_container_header_path, ""); //단순 치환이 아니고 rec가 있는 경우 rec를 포함시켜주어야 한다. 
                    elem_datapath += last_column;
                }
                datapath = elem_datapath;
            }
        }

		//데이터테이블의 열이 컨테이너 열이 아닌경우 데이터는 갱신처리를 시도하지 않는다. 
		//칼럼이 list루트인가?
		console.log("column_thrift_info", column_thrift_info);
		if ('rec' == column_thrift_info.datatype ) //rec root or lst column(non-lst row)
		{
			console.log("rec root or non-container row not update. just skip");
			return;
		}
        if ('lst' == column_thrift_info.datatype && false == row_node_info.m_is_elem_primitive)
        {
            console.log("rec root or non-container row not update. just skip");
            return;
        }

		var meta_row = this.m_meta_rows[row_node_info.m_meta_row_id];   
		var meta_column_value = this.makeUpdateColumnData(webix_column_data, column_thrift_info, row_node_info);
		
		//var thrift_datapath_code = this.makeThriftDataPathCode(datapath, meta_row); 						//kangms remove
		
		var thrift_datapath_code = this.makeThriftDataPathCode4Update(webix_header_column.id, webix_row.id, meta_row); 	//kangms add
		eval(thrift_datapath_code + ' = ' + JSON.stringify(meta_column_value));								//kangms add
	};

	// add row할 때 이부분 호출이 필요하지 않을까?
	this.updateMetaRow = function (webix_row)
	{
		var webix_table_header = this.makeWebixHeader();
		for (header_idx in webix_table_header)
		{
			if ("id" == header_idx)
			{
				continue;
			}

			var webix_header_column = webix_table_header[header_idx];
			if (!(webix_header_column.id in webix_row)) //webix data가 존재하는 경우에만 처리
			{     
				continue;
			}
		
			if (webix_header_column.m_hier_info.m_thrift_info.name == "meta_id") //meta_id는 갱신 안함 
			{
				continue;
			}
			this.updateMetaColumn(webix_header_column, webix_row);
		} 
		return true;
	};
	
	this.updateMetaRows = function (data_table_rows)
	{
		for (row_index in data_table_rows)
		{
			this.updateMetaRow(data_table_rows[row_index]);
		}
	};

	//function isPrimitiveThriftDataType(thrift_data_type) { 
	this.isPrimitiveThriftDataType = function (thrift_data_type) {
		
		switch (thrift_data_type) {
			case 'byte':
			case 'tf':
			case 'i8':
			case 'i16':
			case 'i32':
			case 'i64':
			case 'dbl':
			case 'str':
			case 'enum':
				return true;
		}
		return false;
	}

	//thrift typeid는 idl에 표현되는 변수의 형태임   
	this.isPrimitiveThriftTypeId = function(orig_thrift_type_id)
	{
		var thrift_type_id = orig_thrift_type_id; 

		//typedef이면 검증하고 싶은 type id를 얻어낸다. 
		var typedef = this.metaTypedefs(thrift_type_id);
		if (null != typedef) {
			thrift_type_id = typedef["typeId"];
		}

		switch (thrift_type_id)
		{
			case 'byte':
			case 'tf':
			case 'i8':
			case 'i16':
			case 'i32':
			case 'i64':
			case 'enum':
			case 'double':
			case 'string':
				return true;  
		}
		return false;
	}
 

	//typedef에는 datatype이 아닌 정의에 사용하는 typeid를 사용한다.
	//기본형타입의 정의를 잘못 리턴 
	this.isThriftPrimitiveTypedef = function (datatype)
	{
		var typedef = this.m_schema_typedefs[datatype];
		if (typeof typedef == 'undefined')
		{
			return false;
		}

		switch (typedef["typeId"])
		{
			case 'byte':
			case 'bool':
			case 'i8':
			case 'i16':
			case 'i32':
			case 'i64':
			case 'double':
			case 'string':
			case 'enum':
				return true;

			default:
				break;
		}
	}

	this.makeTreeNode = function (node_param, parent_node)
	{
		switch (node_param.node_type)
		{
			case 'rec':
				this.processTreeNodeRecord(node_param, parent_node);
				break;

			case 'lst':
				this.processTreeNodeList(node_param, parent_node);
				break;

			case 'map':
				// ['i32', 'rec', count, ...
				break;

			case 'set':
				break;
		} // switch case 
	};
	
	this.containerName = function(name)
	{
		return name.replace(/__/gi, '::').replace('::', '');
	}
	this.containerNameOnly = function (name)
	{
		return name.replace(/__.*__/gi, '');
	}
	this.listContainerDisplayName = function (name)
	{
		return this.containerName(name).replace(/list<(.*)>/gi, '[$1]');
	}
	this.thriftDefineName = function (thrift_json_field_name)
	{
		return thrift_json_field_name.replace(/__/gi, '.').replace(".", "");
	}

	this.listContainerType = function (name)
	{
		return name.replace(/list<(.*)>/gi, '$1');
	}
	this.thriftListElemName = function(list_type_name) //elem
	{
		//list<list<a>>
		var list_elem_name = list_type_name;
		var words = list_type_name.split(/[<,>]/); 
		if (words.length > 1)
		{
			list_elem_name = words[1];
		}
		return list_elem_name;
	};

	this.saveDataTable = function (update_datas)
	{
		this.updateMetaRows(update_datas); //실패시 데이터 시트 이슈
				
		var req_path = MetaCommon.m_server_url + "/meta/file_save/";
		var req_data = "path=" + encodeURIComponent(this.m_data_path) + '&category=' + encodeURIComponent(this.m_meta_category) + '&data=' + encodeURIComponent(JSON.stringify(this.m_meta_data)); //개별 데이터별 인코딩하도록 수정 

		console.log("save data:", this.m_meta_data);

		MetaCommon.postFormAsync(req_path, req_data, {
			success: function (response_obj)
			{
				var gen_result = MetaCommon.prepareResponseGenResult(response_obj, true);
				if (gen_result.result != "Ok")
				{

				}
				console.log('save completed.', response_obj);
			}
		})
	};

}

