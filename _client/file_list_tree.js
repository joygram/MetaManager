
var file_list_header = { template: "MetaFiles", type: "header" };
var file_list_menu = {
	cols: [
		{ view: "button", label: "Refresh", click: "refreshFileList"},
		{ view: "button", label: "NextId", click: "popupNextId"},
		{ view: "button", label: "Packing", click: "onClickPacking", type:"form" },
		{ view: "button", label: "New", click: "popupNewMetaFile", type: "form" },
	]
};

var file_list_filter = {
	view: "text",
	label: "Find Files",
	id: "file_list_filter",
	labelPosition: "top"
}
var file_list_tree = {
	width: 200,
	container: "box",
	view: "tree",
	select: true,
	gravity: 0.3,
	id: "file_list_tree",

	filterMode: {
		showSubItems: false,
		level: 2
	},

	on: {
		"onItemClick": function (tree_id, e, trg)
		{
			var item = this.getItem(tree_id);
			if (!!item && item.$count < 1) // 있으면 요청 안하기 
			{
				var meta_url = MetaCommon.m_server_url + "/meta/row_list/?path=" + item.id;
				try
				{
					MetaLogic.m_current_meta_path = tree_id;
					addMetaTab(meta_url, item.value, tree_id);
					$$("files_tab").collapse(); // 성공적으로 로딩하면 닫자.
				}
				catch (ex)
				{
				} 
				finally
				{
				}
			}
			else
			{
				console.log("TODO refresh");
			}
		}
	}
};

// web draw layout
var filelist_tree_layout = {
	container: "filelist_tree_layout",
	scroll: true,
	width: 300,
	//height: 300,
	rows: [
		file_list_header,
		file_list_menu,
		file_list_filter,
		file_list_tree
	]
};


function dirname(path)
{
	return path.replace(/\\/g, '/')
		.replace(/\/[^\/]*\//, '');
}

//------------------------------------------
// buttone click handlers 

function createNewMetaFile()
{
	if (!this.getParentView().validate())
	{
		webix.alert("please input all data");
		return;
	}

	var path = $$("file_list_tree").getSelectedId();
	if (path.indexOf(".xlsx") > -1) //파일, 부모 아이디 사용
	{
		var parent_id = $$("file_list_tree").getparent_id(id);
		if (parent_id.isNotEmpty()) 
		{
			path = parent_id;
		}
	}

	if (path == "")
	{
		parent_id = "/";
		path = "/";
	}
	console.log("path:" + path);

	var form_value = this.getParentView().getValues();
	var category = $$("meta_category").getText();
	var project_namespace = 'oge'
	var req_url = MetaCommon.m_server_url + "/meta/file_create/?path=" + path + '\\' + category + '@' + project_namespace + '.xlsx' + '&category=' + category;

	MetaCommon.requestUriAsync(req_url, {
		success: function (response_obj)
		{
			var gen_result = MetaCommon.prepareResponseGenResult(response_obj, true);
			if (gen_result.result = "Ok")
			{
				var file_list = JSON.parse(gen_result.desc);
				$$("file_list_tree").add(file_list, 0, parent_id);
			}
			$$("new_meta_file_window").close(); //실패시에도 닫기
		}
	});
	//var response_obj = MetaCommon.requestUri(req_url);
	//var gen_result = MetaCommon.prepareResponseGenResult(response_obj, true);
	//if (gen_result.result = "Ok")
	//{
	//	var file_list = JSON.parse(gen_result.desc);
	//	$$("file_list_tree").add(file_list, 0, parent_id);
	//}
	//$$("new_meta_file_window").close();
}

function showNewMetaForm(categories)
{
	var form_new_meta = {
		view: "form",
		id: "form_new_meta",

		elements: [
			{
				view: "combo",
				id: "meta_category",

				label: "Category",
				name: "category",
				validate: webix.rules.isNotEmpty,

				options: {
					view: "suggest",
					filter: function (item, entered_value)
					{
						var item_value = item.value.toLowerCase();
						return (item_value.indexOf(entered_value.toLowerCase()) != -1);
					},
					data: categories //gen_result.desc
				}
			},
			{
				view: "button",
				value: "Create",
				align: 'center',
				click: createNewMetaFile
			}
		]
	};

	webix.ui({
		view: "window",
		id: "new_meta_file_window",
		height: 300,
		width:600,
		move: true,
		autofocus: true,
		modal: true,
		position: "center",
		animate: { type: "flip" },

		head: {
			view: "toolbar", cols: [
				{ view: "label", label: "Add new meta file : SELECT category or TYPE file_name" },
				{ view: "icon", icon: "times-circle", click: "$$('new_meta_file_window').close();" }
			]
		},
		body: form_new_meta
	}).show();

}

function popupNewMetaFile()
{
	MetaCommon.requestUriAsync(MetaCommon.m_server_url + "/meta/categories", {
		success: function (response_obj)
		{
			var gen_result = MetaCommon.prepareResponseGenResult(response_obj, true);
			if (gen_result.result = "Ok")
			{
				showNewMetaForm(gen_result.desc);
			}
		}
	});
	//sync : can not popup async.
	//var response_obj = MetaCommon.requestUri(MetaCommon.m_server_url + "/meta/categories");
	//var gen_result = MetaCommon.prepareResponseGenResult(response_obj, true);
	//if (gen_result.result != "Ok")
	//{
	//	//$$("form_new_meta").parse(gen_result.desc);
	//}
	//showNewMetaForm(gen_result.desc);
}

function popupNextId()
{
	function getNextMetaId()
	{
		var req_url = MetaCommon.m_server_url + "/meta/next_id";
		var next_meta_id = MetaCommon.requestUri(req_url);

		console.log("next_meta_id:", next_meta_id);

		var text_next_meta_id = $$("text_next_meta_id");
		text_next_meta_id.setValue(next_meta_id);
		text_next_meta_id.refresh();
	}
	
	webix.ui({
		view: "window",
		id: "window_next_meta_id",
		height: 300,
		width: 400,
		move: true,
		autofocus: true,
		modal: true,
		position: "center",

		head: {
			view: "toolbar", cols: [
				{ view: "label", label: "click next meta id" },
				{ view: "icon", icon: "times-circle", click: "$$('window_next_meta_id').close();" }
			]
		},
		body: {
			view: "form",
			id: "form_next_meta_id",
			elements: [
				{ id: "text_next_meta_id", view: "text", label: "Next Id:", value : ""},
				{
					view: "button",
					value: "Get Next Id",
					align: 'center',
					click: getNextMetaId
				}
			]
		}
	}).show();
}

function removeMetaFile()
{
	var id = $$("file_list_tree").getSelectedId();
	console.log(id);

	waitingBox();
	var req_url = MetaCommon.m_server_url + "/meta/delete/?path=" + id;
	console.log(req_url);

	webix.ajax(req_url, {
		success: function (text, data)
		{
			webix.modalbox.hide(waiting_box);

			$$("file_list_tree").remove(id);
		},
		error: function (text, data)
		{
			webix.modalbox.hide(waiting_box);
		}
	});
}

function onClickPacking() //메타데이터 삭제
{
	var req_url = MetaCommon.m_server_url + "/meta/pack";
	MetaCommon.message_box("PACK", "META TABLE PACKING<br> please wait...");

	MetaCommon.requestUriAsync(req_url, {
		success: function (response_obj)
		{
			console.log("response:", response_obj);

			var gen_result = MetaCommon.prepareResponseGenResult(response_obj, true);
			if (gen_result.result = "Ok")
			{
				webix.alert("Meta Tables PACK completed.");
			}
			else
			{
				webix.alert("Meta Tables PACK failed:" + gen_result.desc);
			}
		}
	});

	//var req_url = MetaCommon.m_server_url + "/meta/pack";
	//var response = MetaCommon.requestUri(req_url);
	//console.log("response:", response);
	//var gen_result = JSON.parse(response);
	//if (gen_result.result == "Ok")
	//{
	//	webix.alert("Meta Tables PACK completed.");
	//}
	//else 
	//{
	//	webix.alert("Meta Tables PACK failed:" + gen_result.desc);
	//}

}

function popupRemoveMetaFile() //메타데이터 삭제
{
	var id = $$("file_list_tree").getSelectedId();
	if (id.indexOf(".xlsx") == -1)
	{
		webix.alert("Please select the file");
		return;
	}
	webix.confirm({
		title: "Are you sure you want to delete?",
		ok: "Yes",
		cancel: "No",
		type: "confirm-error",
		text: "Remove MetaData",
		callback: function (result)
		{
			if (result == true)
			{
				removeMetaFile();
			}
		}
	});
}


function refreshFileList()
{
	console.log("refresh file list")
	var response_obj = MetaCommon.requestUri(MetaCommon.m_server_url + "/meta/file_list");
	var gen_result = MetaCommon.prepareResponseGenResult(response_obj, true);
	if (gen_result.result = "Ok")
	{
		var file_tree = $$("file_list_tree");

		file_tree.clearAll();
		file_tree.parse(gen_result.desc);
	}
	console.log("filelist:", gen_result);
}
