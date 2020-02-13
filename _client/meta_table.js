var meta_tabview = {
	view: "tabview",
	id: "meta_tabview",
	tabbar: {
		optionWidth: 210,
	},
	cells: [
		{
            id: "empty_tab",
            width: 5,
            header: "",
            body: {}
		}
	]
};

var meta_table_layout = {
	container: "TableLayout",
	id: "meta_table_layout",
	rows: [
		meta_tabview,
	]
};

function editDataTable(datatable, row_info)
{
	console.log("editDataTable");

	var meta_container = MetaLogic.getMetaContainer(datatable.config.id);
	meta_container.m_last_clicked_row_id = row_info.row;

	var row_node_info = meta_container.makeRowNodeInfo(row_info.row);
	if (row_node_info.m_is_lst_root) 
	{
		console.log("can not modify lst root");
		return false;
	}
	else if (row_node_info.m_is_elem) 
	{
		// 비교하기 전에 데이터를 정리한다. 컨테이너 인덱스 제거한 후 비교할 것 []
		if (false == row_info.column.startsWith(row_node_info.m_root_id)) 
		{
			console.log("[editDataTable] column:", row_info.column, " is not part of element", " row_note_info.m_root_id:", row_node_info.m_root_id);
			return false;
		}
	}
	else // 일반 노드에서는 lst 컬럼을 수정할 수 없다. 
	{
		if (meta_container.isLstElemCell(row_info.row, row_info.column))
		{
			console.log("[editDataTable] row_info:", row_info, " is lst elem. can not modify");
			return false;
		}
	}

	// 정보로 무언가를 할 수 있을까?
	meta_container.m_edit_row_info = row_info;

	var row = datatable.getItem(row_info.row);
	meta_container.m_prev_column_value = Object.assign({}, row[row_info.column]);

    var column_config = datatable.getColumnConfig(row_info.column);
    if (column_config.editor == "checkbox")
    {
        console.log("[editdatatable] checkbox, check only");
        return false;
    }

	if (column_config.m_is_meta_link)
	{
		console.log("[editdatatable] link is not edit but select.");
		return true;
	}

	console.log("try edit");
	datatable.edit(row_info);
	return true;
}

function disableAllListButton(table_id)
{
	$$("btn_lst_add_" + table_id).disable();
	$$("btn_lst_del_" + table_id).disable();
}

function makeLinkSuggest(link_typedef, datatable, meta_container)
{
	// 무엇을 클릭하였는지. 데이터는 무엇인지, 기존에 선택한 id를 강제로 선택하게 할것인지. 아니면 필터할것인지.
	var link_container = meta_container.linkContainer(link_typedef);
	if (null == link_container)
	{
		// alert
		console.log("link_container is not exist.", link_typedef);
		return;
	}

	var selected_item = datatable.getSelectedItem();
	console.log("selected_item:", selected_item);

	var table_id = "link_datatable";
	var link_datatable_toolbar = {
		id: "link_datatable_toolbar",
		view: "toolbar",
		cols: [
			{ //모든 컬럼에 매칭되는 값이 있는 데이터를 필터하는 기능을 수행함. (대소 구분 없음.)
				id: "txt_filter_" + table_id, view: "text", label: "Filter", inputAlign: "left", labelAligh: "left",
				on: {
					onChange: function (new_value, old_value)
					{
						var table_id = this.config.id.replace("txt_filter_", "");
						var link_datatable = $$(table_id);

						if (!new_value || new_value == "")
						{
							link_datatable.filter();
							return;
						}
						var search_txts = new_value.toString().toLowerCase().split(" ").filter(item => item);
						link_datatable.filter(function (row)
						{
							var hit = false;
							for (idx in row)
							{
								var col = row[idx].toString().toLowerCase();
								for (s_idx in search_txts)
								{
									var txt = search_txts[s_idx];
									if (col.indexOf(txt) != -1)
									{
										hit = true;
										break;
									}
								}
								if (true == hit)
								{
									break;
								}
							}
							return hit;
						});
						$$(this.config.id).focus();
					}
				}
			},
			{
				id: "btn_ok_" + table_id, view: "button", label: "SELECT", width: 100, align: "left",
				on: {
					onItemClick: function (id, e)
					{
						var table_id = id.replace("btn_ok_", "");
						var link_datatable = $$(table_id);

						var target_container = link_datatable.config.m_link_target_container;
						if (null == target_container)
						{
							console.log("target container is null");
						}
						var target_datatable = link_datatable.config.m_link_target_datatable;
						if (null == target_datatable)
						{
							console.log("target data table is null");
						}
						var target_row = target_datatable.getSelectedItem();

						var row = link_datatable.getSelectedItem();
						var selected_value = row["1"]; // 고정 정의:바꾸지 않는다.
						var selected_name = row["3"]; // 고정 정의: 바꾸지 않는다.

						console.log("selected link data :", selected_value, " : ", selected_name)

						var target_column = target_row[target_container.m_edit_row_info.column];
						if (null == link_datatable.m_original_column_value) //최초 선택시 원래 값을 저장함.
						{
						}

						var update_datas = [];
						update_datas.push(target_row);
						target_container.saveDataTable(update_datas);
						$$("link_table_window").close();
					}
				}
			},
			{
				id: "btn_cancel_" + table_id, view: "button", label: "CANCEL", width: 100, align: "left",
				on: {
					onItemClick: function (id, e) 
					{
						var table_id = id.replace("btn_cancel_", "");
						// 타겟 메타 테이블의 값을 원래 대로 변경 시킨다.
						// 데이터 테이블은 손볼 것 없음. 
						var link_datatable = $$(table_id);

						var target_container = link_datatable.config.m_link_target_container;
						if (null == target_container)
						{
							console.log("target container is null");
							return;
						}
						var target_datatable = link_datatable.config.m_link_target_datatable;
						if (null == target_datatable)
						{
							console.log("target data table is null");
							return;
						}
						var target_row = target_datatable.getSelectedItem();
						var target_column = target_row[target_container.m_edit_row_info.column];
						if (null != link_datatable.m_original_column_value) //최초 선택시 원래 값을 저장함.
						{
							target_row[target_container.m_edit_row_info.column] = link_datatable.m_original_column_value;
							target_datatable.refresh();
						}

						$$("link_table_window").close();
					}
				}
			}
		]
	};

	console.log("set toolbars");

	//링크 선택은 모달로 하나만 나타나므로 id는 동일한것을 사용하여도 문제는 없을 것이라고 판단함. 
	var link_datatable = {
		id: "link_datatable",
		view: "datatable",
		columns: link_container.makeWebixHeaderLink(),
		data: link_container.makeWebixData(),

		height: 300,
		autowidth: true,
		scroll: true,
		scrollAlignY: true,
		navigation: true,
		editable: false,
		//editaction: "custom",
		select: "cell",
		blockselect: true,
		resizeColumn: true,

		m_meta_container: link_container,

		m_link_target_container: meta_container,
		m_link_target_datatable: datatable,
        m_original_column_value: null,

		on:
		{
			onAfterSelect: function (row_info, preserve) 
			{
				var table_id = this.config.id;
				var target_container = this.config.m_link_target_container;
				if (null == target_container)
				{
					console.log("target container is null");
				}
				var target_datatable = this.config.m_link_target_datatable;
				if (null == target_datatable)
				{
					console.log("target data table is null");
				}
				var target_row = target_datatable.getSelectedItem();

				var row = this.getSelectedItem();
				var selected_value = row["1"]; // 고정 정의:바꾸지 않는다.
				var selected_name = row["3"]; // 고정 정의: 바꾸지 않는다.

				console.log("selected link data :", selected_value, " : ", selected_name)

				var target_column = target_row[target_container.m_edit_row_info.column];
				if (null == this.m_original_column_value) //최초 선택시 원래 값을 저장함.
				{
					this.m_original_column_value = target_column;
				}
				target_row[target_container.m_edit_row_info.column] = selected_value + ":" + selected_name;
				console.log("target_row", target_row, "target_column:", target_column, " original_column_value:", this.m_original_column_value);
				target_datatable.refresh();
			}
		}
	};

	return webix.ui({
		id: "link_table_window",
		view: "window",
		move: true,
		position: "center",
		modal: true,
		body: {
			id: "link_table_layout",
			view: "layout",
			cols: [{
					rows: [
						link_datatable_toolbar,
						link_datatable
					]
			}]
		}
	});

}

var meta_table_eventhandler = {
	onItemClick: function (row_info) 
	{
		var datatable = $$(this.config.id);

		this.config.cellEdit(row_info);
		
	},
	onItemDblClick: function (row_info) 
	{
		var meta_container = MetaLogic.getMetaContainer(this.config.id);
		meta_container.m_last_clicked_row_id = row_info.row;

		console.log("onItemDblClicked", row_info);
    },
    onBeforeSelect: function(row_info, preserve)
	{
		var table_id = this.config.id;
		var datatable = $$(table_id);
		datatable.editCancel();
    },
	onAfterSelect: function (row_info, preserve) 
	{
		var table_id = this.config.id;
		var datatable = $$(table_id);
		var meta_container = MetaLogic.getMetaContainer(this.config.id);
	
		// button ux 
		disableAllListButton(table_id);
		meta_container.m_edit_row_info = row_info; 
		meta_container.m_last_clicked_row_id = row_info.row;

		var row_node_info = meta_container.makeRowNodeInfo(row_info.row);
		if (row_node_info.m_is_lst_root) 
		{
			$$("btn_lst_add_" + table_id).enable();
		}
		else if (row_node_info.m_is_elem) 
		{
			$$("btn_lst_del_" + table_id).enable();
			if (false == row_info.column.startsWith(row_node_info.m_root_id)) 
			{
			}
		}
		$$("btn_row_select_" + table_id).enable();


		// 자신이 수정할 수 있는 위치가 아니면 스크롤 하지 않도록 한다.
		//if (true == datatable.cellEditable(row_info))
		{
		//재귀호출됨. 
			var selected = this.getSelectedItem();
			this.select(selected.id, selected.m_container_header_path); //row: id, col: m_container_header_path 

			var state = datatable.getScrollState();
			var col_width = this.config.columnWidth;
			var col_index = this.getColumnIndex(selected.m_container_header_path);
			//this.scrollTo(col_width * (col_index -3), state.y); // 지정한 위치까지 이동 
		}


    },
    onCheck: function(row_id, col_id, state) //체크 가능여부 체크
	{
		var meta_container = this.config.m_meta_container;
        var row_info = { row: row_id, column: col_id };
        if (false == this.config.nodeEditable(row_info))
        {
            this.blockEvent();
			var item = this.getItem(row_id); //원복 
            item[col_id] = item[col_id] ? 0 : 1;
			this.updateItem(row_id, item);
            this.unblockEvent();
            console.log("check blocked:", row_info);
		}
		else
		{
			var selected = this.getSelectedItem();
			var row_node_info = meta_container.makeRowNodeInfo(row_info.row); //row,col가능 

			var update_datas = [];
			update_datas.push(this.getItem(row_id));
			meta_container.saveDataTable(update_datas);
		}
    },
	// 전체 데이터 invalidate 
	onPaste: function (text) 
	{
		console.log('onPaste' + text);
		var meta_container = MetaLogic.getMetaContainer(this.config.id);
		
		this.refresh();
		this.validate();

		var update_datas = [];
		this.eachRow(function (row_id) 
		{
			row = this.getItem(row_id);
			update_datas.push(row);
        })
		meta_container.updateMetaRows(update_datas);
        //meta_container.saveDataTable(update_datas);
    },
	onAfterLoad: function () 
	{
		if (!this.count()) 
		{
            //this.showOverlay("no data. `addRow` please");
        }
    },
	onAfterEditStop: function (state, editor, ignoreUpdate)
	{
		console.log("after edit stop");
		// undefined 가 empty("")로 변경되는 경우에도 수정이 되지 않은 것으로 판단한다.  
		if (state.value == state.old)
        {
            console.log("no changed");
            return;
		}  
		
		if (typeof state.old == 'undefined' && state.value == "")
		{
			console.log("not update undefined to empty value");
			return;
		}
		var meta_container = MetaLogic.getMetaContainer(this.config.id);
		
		//특정 컬럼만 갱신할 경우 editor.column 정보를 활용 !!!
		
		var update_datas = [];
		update_datas.push(this.getItem(editor.row));
		
		meta_container.saveDataTable(update_datas);
		console.log("--todo update table -> to meta_data requested.", update_datas);
    },
};


function filterDatatable(table_id, filter_text, focus_editor_id)
{
	var datatable = $$(table_id);

	if (!filter_text || "" == filter_text) //리셋 필터
	{
		datatable.filter();
		return;
	}
	var column_filters = datatable.config.m_column_filter;
	var search_txts = filter_text.toString().toLowerCase().split(" ").filter(item => item);

	datatable.filter(function (row)
	{
		var hit = false;
		for (idx in row)
		{
			// field
			if (this.m_column_filter != "" &&
				idx.indexOf(column_filters) == -1)
			{
				continue;
			}

			var column_value = row[idx];
			if ('undefined' == typeof column_value||
				null == column_value)
			{
				continue;
			}

			var column_value = column_value.toString().toLowerCase();
			for (s_idx in search_txts)
			{
				var search_word = search_txts[s_idx];
				if ("" == search_word)
				{
					continue;
				}

				if (datatable.config.m_match_whole_word) //match whole word
				{
					if (new RegExp('\\b' + column_value + '\\b', 'g').test(search_word))
					{
						hit = true;
						break;
					}
				}
				else //부분 매칭 
				{
					if (column_value.indexOf(search_word) != -1)
					{
						hit = true;
						break;
					}
				}
			}
			if (true == hit)
			{
				break;
			}
		}//for
		return hit;
	});



}

function addMetaTab(url, value, data_file_path)
{
	console.log('url:' + url + ', data_file_path:' + data_file_path);

	var meta_tabview = $$("meta_tabview");

	var meta_container = MetaLogic.getMetaContainer(data_file_path, true);

	console.log("meta_container:", meta_container);

	// generate column 
	//try
	{
		var datatable_header = meta_container.makeWebixHeader(true);
		var datatable_rules = meta_container.makeDataTableRules();
		var datatable_rows = meta_container.makeWebixData();
	}
	//catch (ex)
	//{
	//	var err_msg = 'CANNOT OPEN :' + data_file_path + '\n' + ex;
	//	console.log(err_msg);
		
	//	MetaCommon.error_window(err_msg);
	//	return;
	//}
 

	// make id
	var table_id = data_file_path;
	var tab_layout_id = table_id + "_tab";
	var toolbar_id = table_id + "_toolbar";
	var bottombar_id = table_id + "_bottombar";
	var detail_toolbar_id = table_id + "_detail_toolbar";
	var detail_toolbar_label_id = table_id + "_detail_toolbar_label";
	var row_tree_id = table_id + "_row_tree";
	var row_detail_id = table_id + "_row_detail";

	var table_top_bar = {
        id: toolbar_id,
		view: "toolbar",


        cols: [
 

            // list add
            { id: "btn_row_label_" + table_id, view: "label", label: "ROW", width: 50, align: "left" },
			{ id: "btn_row_add_" + table_id, view: "button", label: "Add Row", width: 100, align: "left", type:"form", click: "tableRowAdd" },
            {
				id: "btn_row_select_" + table_id, view: "button", label: "Select Row", width: 100, align: "left", type: "form", disabled: true,
				m_table_id: table_id, //소속된 테이블 id기록 

                on: {
					onItemClick: function (id, e)
					{
						var table_id = id.replace("btn_row_select_", "");
						var datatable = $$(table_id);
						var meta_container = MetaLogic.getMetaContainer(table_id);
						var webix_header = meta_container.makeWebixHeader();

						datatable.clearSelection();

						console.log("table_id:", table_id);
						var row_info = meta_container.m_edit_row_info;

						var first_column_id = webix_header[0].id;
						var last_column_id = webix_header[webix_header.length - 1].id;
						datatable.selectRange(row_info.row, first_column_id, row_info.row, last_column_id); // detect last column id
                    }//onItemClick
                }
            },
            // list command 
            { id: "btn_lst_label_" + table_id, view: "label", label: "LIST", width: 50, align: "left" },
            // button : list add elem
            {
				id: "btn_lst_add_" + table_id, view: "button", label: "Add Elem", width: 100, align: "left", type: "form", disabled: true,
				m_table_id: table_id, //소속된 테이블 id기록 

                on: {
					onItemClick: function (id, e)
					{
						var datatable = $$(this.config.m_table_id);
						var meta_container = MetaLogic.getMetaContainer(table_id);

						var row_info = meta_container.m_edit_row_info;
						var row_node_info = meta_container.makeRowNodeInfo(row_info.row);

						console.log("row_node_info:", row_node_info);
						var meta_row = meta_container.m_meta_rows[row_node_info.m_meta_row_id];

						var selected = datatable.getSelectedItem();//여러개 선택되어 있는경우 리스트를 리턴한다.
						if (typeof (selected) == "undefined")
						{
							console.log("no selection. just skip");
							return;
						}

						if (selected.length > 1)
						{
							selected = selected[0];
						}

						meta_container.addLstElem(selected); 

						//상태저장 후 복원 
						webix.storage.local.put(table_id, datatable.getState());

						datatable.parse(meta_container.makeWebixData());
						datatable.setState(webix.storage.local.get(table_id));

						meta_container.saveDataTable([]); // meta_container에 미리 갱신 되었으므로 저장 요청만 진행.
                    }//onItemClick
                }
            },
            // button : list del elem
			{
				// 삭제가 되면 해당 부분은 리프래시가 되어야 함. meta data의 내용의 일부를 데이터 테이블에 재 반영 시켤 수 있어야 함. 
				id: "btn_lst_del_" + table_id, view: "button", label: "Del Elem", width: 100, align: "left", disabled: true,
				m_table_id: table_id, //소속된 테이블 id기록 

                on: {
					onItemClick: function (id, e)
					{
						var datatable = $$(this.config.m_table_id);
						var meta_container = MetaLogic.getMetaContainer(table_id);

						var selected = datatable.getSelectedItem(); // 체크 : 여러개 선택했을 때 데이터 내용 
						if (typeof (selected) == "undefined")
						{
							this.disable();
							console.log("no selection. just skip");
							return;
						}
						if (selected.length > 1)
						{
							selected = selected[0];
						}

						var deleted = meta_container.delLstElem(selected);
						if (!deleted)
						{
							this.disable();
						}
						console.log("datatable id:", datatable.config.id, " table_id:", table_id);

						//상태 저장 후 복원.
						webix.storage.local.put(table_id, datatable.getState());
						datatable.clearAll();
						datatable.parse(meta_container.makeWebixData());
						datatable.setState(webix.storage.local.get(table_id));

						meta_container.saveDataTable([]); // meta_container에 미리 갱신 되었으므로 저장 요청만 진행.
					}
                }
			},

			{ //필터 하고자 하는 컬럼 선택
				id: "column_filter_" + table_id, view: "text", label: "COLUMN", align: "left", width: 200,
				m_table_id: table_id, //소속된 테이블 id기록 

				on: {
					onChange: function (new_value, old_value)
					{
						var datatable = $$(this.config.m_table_id);
						datatable.config.m_column_filter = new_value;// new_value.toString().split(" ");


						var txt_filter = $$("txt_filter_" + this.config.m_table_id);
						filterDatatable(this.config.m_table_id, txt_filter.getValue(), this.config.id);

					} //onChange 
				} //on
			},

			{ //모든 컬럼에 매칭되는 값이 있는 데이터를 필터하는 기능을 수행함. (대소 구분 없음.)
				id: "txt_filter_" + table_id, view: "text", label: "FILTER", width: 400, 
				m_table_id: table_id, //소속된 테이블 id기록 

				on: {
					onChange: function (new_value, old_value)
					{
						console.log("onchange:", new_value);
						filterDatatable(this.config.m_table_id, new_value, this.config.id);
					} //onChange 
				} //on
			},

			{ //WholeWord 필터
				id: "match_whole_word_" + table_id, view: "checkbox", label: "WholeWord",
				m_table_id: table_id, //소속된 테이블 id기록 

				on: {
					onChange: function (new_value, old_value)
					{
						var datatable = $$(this.config.m_table_id);
						datatable.config.m_match_whole_word = new_value;// new_value.toString().split(" ");

						var txt_filter = $$("txt_filter_" + this.config.m_table_id);
						filterDatatable(this.config.m_table_id, txt_filter.getValue(), this.config.id);
					} //onChange 
				} //on
			},


        ]
    }


	var table_bottom_bar = {
		id: bottombar_id,
		view: "toolbar",
		cols: [

			{
				id: "btn_left_home_" + table_id, view: "button", label: "FIRST", width: 100, align: "left", type: "form",
				on: {
					onItemClick: function (id, e)
					{
						var table_id = id.replace("btn_left_home_", "");
						var datatable = $$(table_id);
						var meta_container = MetaLogic.getMetaContainer(table_id);
						var webix_header = meta_container.makeWebixHeader();

						var state = datatable.getScrollState();
						datatable.scrollTo(0, 0);
					}//onItemClick
				}
			},
			{
				id: "btn_left_scroll2x_" + table_id, view: "button", label: "LEFT x2", width: 100, align: "left", type: "prev",
				on: {
					onItemClick: function (id, e)
					{
						var table_id = id.replace("btn_left_scroll2x_", "");
						var datatable = $$(table_id);
						var meta_container = MetaLogic.getMetaContainer(table_id);
						var webix_header = meta_container.makeWebixHeader();

						var state = datatable.getScrollState();
						datatable.scrollTo(state.x - 400, 0);
					}//onItemClick
				}
			},
			{
				id: "btn_left_scroll_" + table_id, view: "button", label: "LEFT", width: 100, align: "left", type: "prev",
				on: {
					onItemClick: function (id, e)
					{
						var table_id = id.replace("btn_left_scroll_", "");
						var datatable = $$(table_id);
						var meta_container = MetaLogic.getMetaContainer(table_id);
						var webix_header = meta_container.makeWebixHeader();

						var state = datatable.getScrollState();
						datatable.scrollTo(state.x-200, 0);
					}//onItemClick
				}
			},
			{
				id: "btn_right_scroll_" + table_id, view: "button", label: "RIGHT", width: 100, align: "left", type: "next",
				on: {
					onItemClick: function (id, e)
					{
						var table_id = id.replace("btn_right_scroll_", "");
						var datatable = $$(table_id);
						var meta_container = MetaLogic.getMetaContainer(table_id);
						var webix_header = meta_container.makeWebixHeader();

						var state = datatable.getScrollState();
						datatable.scrollTo(state.x+200, 0);

					}//onItemClick
				}
			},
			{
				id: "btn_right_scroll2x_" + table_id, view: "button", label: "RIGHT x2", width: 100, align: "left", type: "next",
				on: {
					onItemClick: function (id, e)
					{
						var table_id = id.replace("btn_right_scroll2x_", "");
						var datatable = $$(table_id);
						var meta_container = MetaLogic.getMetaContainer(table_id);
						var webix_header = meta_container.makeWebixHeader();

						var state = datatable.getScrollState();
						datatable.scrollTo(state.x+400, 0);

					}//onItemClick
				}
			},
			{
				id: "btn_left_end_" + table_id, view: "button", label: "LAST", width: 100, align: "left", type: "form",
				on: {
					onItemClick: function (id, e)
					{
						var table_id = id.replace("btn_left_end_", "");
						var datatable = $$(table_id);
						var meta_container = MetaLogic.getMetaContainer(table_id);
						var webix_header = meta_container.makeWebixHeader();

						var state = datatable.getScrollState();
						datatable.scrollTo(30000, 0);
					}//onItemClick
				}
			},

			{ id: "table_label_" + table_id, view: "label", label: data_file_path, align: "right" },

		]
	}

	var meta_table_view = {
		id: table_id,
		view: "treetable",

		headerRowHeight: 30,
		rowHeight: 25, 

		columns: datatable_header,
		rules: datatable_rules,
		data: datatable_rows,

		leftSplit: 4, // 완쪽 고정 

		undo: true,
		scroll: true,
		//scrollAlignY: true,
		navigation: true,
		editable: true,
		tooltip: true,
		editaction: "custom",
		footer: false, 
		select: "cell",
		multiselect: true,
		blockselect: true,

		//hover: "data_table_hover",

		clipboard: "block",
		resizeColumn: true,
        on: meta_table_eventhandler,

		// custom type 
		m_meta_container: meta_container, 
		m_column_filter: "",
		m_match_whole_word: 0,

		//----- config method --------
		nodeEditable: function (row_info) 
		{ //리스트 구성요소 중 편집가능한 것인가?
			var datatable = $$(this.id);
			var meta_container = this.m_meta_container;

			console.log("[nodeEditable]", row_info);
			//헤더와 컬럼을 같이 비교하여야 한다. (ROW 리스트 루트인가, COL 리스트 루트인가 )

            var row_node_info = meta_container.makeRowNodeInfo(row_info.row);
            if (row_node_info.m_is_lst_root) {
				console.log("[nodeEditable] can not modify lst root");
                return false;
            }
			else if (row_node_info.m_is_elem)
			{
				//var column_path = row_info.column.replace(/\.lst/gi, '');
                //var root_path = row_node_info.m_root_id.replace(/\.\[[0-9]*\]/gi, '');
                if (false == MetaCommon.isColumnNodeElement(row_node_info.m_root_id, row_info.column, row_node_info.m_is_elem_primitive))
				{
					//row_info column 30.lst.3.lst.2 -> 30.3.2, root_node_id : 30.[2].3 -> 30.3
					console.log("[nodeEditable] row_info:", row_info, " is not part of element, root_node_info.m_root_id:", row_node_info.m_root_id, " row_node_info:", row_node_info);
                    return false;
                }
            }
            else // 일반 노드에서는 lst 컬럼을 수정할 수 없다. 
            {
                if (meta_container.isLstElemCell(row_info.row, row_info.column))
                {
					console.log("[nodeEditable] nodeEditable row_info:", row_info, " is lst elem. can not modify");
                    return false;
                }
			}

			console.log("[nodeEditable] node editable:", row_info);
            return true;
        },
		cellEditable: function (row_info) //config 위치함.
		{
			var datatable = $$(this.id);

            var meta_container = this.m_meta_container;
            if (false == this.nodeEditable(row_info))
            {
				if(row_info.column == 1)
				{
					datatable.scrollTo(30000, 0);

					var selected_id = datatable.getSelectedId();
					datatable.showCell(row_info.row, selected_id.column);	
				}
            	
                return false;
			}

			// check column cell editable
			var column_path_array = row_info.column.split(".");
			var column_is_lst_root = false;
			for (var i in column_path_array)
			{
				var id = column_path_array[i];
				if (id == 'lst')
                {
                    var row_node_info = meta_container.makeRowNodeInfo(row_info.row);
                    if (true == row_node_info.m_is_elem_primitive)
                    {
                        continue;
                    }
					//마지막인 'lst' (컨테이너로 끝나면) 루트가 되는 것이다. 
					var is_last_index = (i == column_path_array.length - 1);
					if (is_last_index)
					{
						column_is_lst_root = true;
					}
					continue;
				}
			}
			if (true == column_is_lst_root)
			{
				console.log("[cellEditable] column lst root not editable");
				return false;
			}


            meta_container.m_last_clicked_row_id = row_info.row;
			meta_container.m_edit_row_info = row_info;

            var row = datatable.getItem(row_info.row);
            meta_container.m_prev_column_value = Object.assign({}, row[row_info.column]);

            var column_config = datatable.getColumnConfig(row_info.column);
			if (column_config.editor == "checkbox") 
			{
                console.log("[cellEditable] checkbox, check only");
                return false;
			}

			if (column_config.m_is_meta_link)
			{
				console.log("[cellEditable] link is not edit but select.");
				return true;
			}

            return true;
        },
		cellEdit: function (row_info)
		{
			var datatable = $$(this.id);
			var meta_container = this.m_meta_container;

			if (false == this.cellEditable(row_info))
			{
				console.log("[cellEdit] not editable:", row_info);
                return;
			}
			
            //링크 편집
            var column_config = datatable.getColumnConfig(row_info.column);
            if (column_config.m_is_meta_link) {

				var suggest = makeLinkSuggest(column_config.m_link_meta_name, datatable, meta_container);
                suggest.show();
                return;
            }
            datatable.edit(row_info);
        }
	}
	var meta_tab_layout = {
		id: tab_layout_id,
		view: "layout",
		cols: [
			{
				rows: [
					table_top_bar,
					meta_table_view,
					table_bottom_bar
				]
			}
		]
	}
	var meta_table_tab = {
		close: true,
		autoheight: true,
		header: value.replace("@oge.xlsx", ""),
		body: meta_tab_layout
    };

    meta_tabview.addView(meta_table_tab);
	meta_tabview.setValue(tab_layout_id);// activate tab

	// add data table keyboard shortcut.
	var new_table = $$(table_id);
	webix.UIManager.addHotKey("enter", function (datatable)
	{
        var pos = datatable.getSelectedId();
        datatable.config.cellEdit(pos);
	}, new_table);

	new_table.attachEvent('unload', function () 
	{
		console.log("unload save state");
		webix.storage.local.put(table_id, this.getState());
	});

	//에디트 상태 저장 by joygram 2018/03/06
	new_table.attachEvent("onItemClick", function (id, e, node)
	{
		webix.storage.local.put(this.config.id, this.getState());
	});
	var table_state = webix.storage.local.get(table_id);
	if (table_state)
	{
		//구조체가 바뀌면 반영이 안되는 문제가 있음.
		//new_table.setState(table_state);
	}
}

function tableSave()
{
	var meta_tabview = $$("meta_tabview");

	var tab_id = meta_tabview.getValue();
	var meta_table_id = tab_id.replace("_tab", "");

	var meta_table = $$(meta_table_id);

	var meta_container = MetaLogic.getMetaContainer(meta_table_id);

	meta_container.saveDataTable([]);
}

function tableRowAdd() //데이터 테이블 Row 추가
{
	var meta_tabview = $$("meta_tabview");

	var tab_id = meta_tabview.getValue();
	var table_id = tab_id.replace("_tab", "");

	var datatable = $$(table_id);

    //메타 컨테이너 받아내기 
	var meta_container = MetaLogic.getMetaContainer(table_id);
	var category = meta_container.m_meta_category;

	var req_url = MetaCommon.m_server_url + "/meta/row_add/?path=" + table_id + "&category=" + category;
	var response_obj = MetaCommon.requestUri(req_url);

	var response = MetaCommon.prepareResponse(response_obj, true);
	// todo change gen_result :://result, row_id, meta_data
	if (response.result != "success")
	{
		console.log("ERROR");
		return;
	}
	meta_row = JSON.parse(response.meta_row);
	meta_container.addMetaRow(response.row_id, meta_row);

	//상태저장 후 복원 
	webix.storage.local.put(table_id, datatable.getState());
	datatable.clearAll();
	datatable.parse(meta_container.makeWebixData());
	datatable.setState(webix.storage.local.get(table_id));

	meta_container.saveDataTable([]); // meta_container에 미리 갱신 되었으므로 저장 요청만 진행.
}
