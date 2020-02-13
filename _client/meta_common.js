// ui components 
var Resizer = { view: "resizer" };
var thrift_list_offset = 2;

function deepClone(src_obj)
{
	return JSON.parse(JSON.stringify(src_obj));
}

function strCopy(src_str)
{
	return (' ' + src_str).slice(1);
}

function refrash() //툴 새로고침
{
    top.document.location.reload();
}

function waitingBox()
{
    waiting_box = webix.modalbox({
        text: "..........",
        left: -1000,
        width: 0,
        height: 0
    });
}

function resizeView(component_name, width, height)
{
    $$(component_name).config.height = height;
    webix.event(window, "resize", function ()
    { $$(component_name).adjust(); })
}


function isNumber(s)
{
	s += ''; // 문자열로 변환
	s = s.replace(/^\s*|\s*$/g, ''); // 좌우 공백 제거
	if (s == '' || isNaN(s)) return false;
	return true;
}

var MetaCommon = 
{
	 //m_server_url : "http://localhost:8000",
	 m_server_url : "",



	 toUpperCaseFirstChar: function (target_str)
	 {
		 return target_str.charAt(0).toUpperCase() + target_str.slice(1);
	 },

	 isColumnNodeElement: function (root_path, column_path, is_primitive)
     {
		 var root_path = root_path.replace(/\.\[[0-9]*\]/gi, '');
		 //var column_path = column_path.replace(/\.lst/gi, '');			//kangms remove
		 var column_path = column_path.replace(/(\.lst)|(\.rec)/gi, '');	//kangms add
         // 기본형 리스트일경우는 동일 패스이다.
         if (root_path == column_path && true == is_primitive) {
             return true;
         }
		 return column_path.startsWith(root_path + ".");
	 },

	// meta_category@project_namespace.xlsx
    extractMetaNameFromPath: function (path)
    {
        var filename_words = path.replace(/^.*[\\\/]/, '').split("@");
        console.log("filename:" + filename_words + " length:" + filename_words.length);
        
        if (filename_words.length == 2) // gen filename
		{
			console.log("meta_name:" + filename_words[0]);
			return filename_words[0];
        }
        return "";
	 },

	message_box: function (title, message)
	{
		MetaCommon.close_message_box();
		webix.ui({
			id: "message_box",
			view: "window",
			position: "top",
			move: true,
			modal: true,

			width: 600,
			head: title,
			body: {
				template: "<center>" + message + "</center>"
			}
		}).show();
	},
	  
	close_message_box: function ()
	{
		var modal_window = $$("message_box");
		if (typeof modal_window == 'undefined')
		{
			return;
		}
		modal_window.close();
	},
        
    requestUri: function (uri)
	{
		var encoded_uri = encodeURI(uri);

		//XMLHttpRequest : xhr
		console.log("requestUri:" + encoded_uri);
		var xhr = webix.ajax().sync().get(encoded_uri);

		if (typeof xhr.response.statusCode != 'undefined' || xhr.response.statusCode == 500)
		{
			xhr.error = true;
			webix.alert(xhr.rsponse.details); //statusCode, message, details 
		}
		console.log("xhr", xhr);
		return xhr.response; 
	}, 

	requestUriAsync: function (uri, callback)
	{
		MetaCommon.show_progress();

		//XMLHttpRequest : xhr
		webix.ajax().get(encodeURI(uri), {
			error: function (text, data, xhr)
			{
				xhr.error = true;
				webix.alert(xhr.details);
				MetaCommon.hide_progress();
			},
			success: function (text, data, xhr)
			{
				callback.success(xhr.response);
				MetaCommon.hide_progress();
				console.log("xhr", xhr);
			}
		});
	},


	postForm: function (req_path, req_data)
	{
		//XMLHttpRequest : xhr
		var xhr = webix.ajax().sync().post(encodeURI(req_path), encodeURI(req_data));
		console.log("xhr:", xhr);

		if (typeof xhr.response.statusCode != 'undefined' || xhr.response.statusCode == 500)
		{
			xhr.error = true;
			webix.alert(xhr.rsponse.details); //statusCode, message, details 
		}

		console.log("xhr", xhr);
		return xhr.response;
	}, 

	// params: req_data 는 encodeURIComponent() 된 데이터를 받는다.
	postFormAsync: function (req_path, req_data, callback)
	{
		MetaCommon.show_progress();

		// TODO: check req_data is encode?

		//XMLHttpRequest : xhr
		webix.ajax().post(encodeURI(req_path), req_data, {
			error: function (text, data, xhr)
			{
				xhr.error = true;
				webix.alert(xhr.details);
				MetaCommon.hide_progress();
			},
			success: function (text, data, xhr)
			{
				callback.success(xhr.response);
				MetaCommon.hide_progress();
			}
		});
	},

	// gen_result json  
	prepareResponseGenResult: function (response_obj, error_window)
	{
		try
		{
			var gen_result = JSON.parse(response_obj);
			if (gen_result.result != "Ok")
			{
				if (true == error_window)
				{
					MetaCommon.error_window(gen_result, true);
				}
			}
		}
		catch (ex)
		{
			gen_result.result = "Fail";
			gen_result.code = "internal exception :" + ex;
			gen_result.desc = response_obj;

			if (true == error_window)
			{
				MetaCommon.error_window(gen_result, true);
			}
		}
		return gen_result;
	},

	// static content json
	prepareResponse: function (response_obj, error_window)
	{
		try
		{
			var response = JSON.parse(response_obj);
		}
		catch (ex)
		{
			if (true == error_window)
			{
				MetaCommon.error_window(ex, true);
			}
			return;
		}
		return response;
	},
    
    loadMetaSchema: function (schema_name)
	{
		var req_uri = MetaCommon.m_server_url + "/meta_schema/" + schema_name + ".json";
		var response_obj = this.requestUri(req_uri);

		var response = MetaCommon.prepareResponse(response_obj, true);
		console.log("loadMetaSchema", response);
        return response;
    },

    loadMetaData: function (meta_path, error_window)
    {
        var req_uri = MetaCommon.m_server_url + "/meta/row_list/?path=" + meta_path;
        console.log("loadmeta req_uri:" + req_uri);

		var response_obj = this.requestUri(req_uri);
		var gen_result = MetaCommon.prepareResponseGenResult(response_obj, error_window);

		console.log("loadMeta result:", gen_result);
		if (gen_result.result != "Ok")
		{
			return;
		}

		//meta server로 부터 받은 결과를 객체로 변환 
		var content = JSON.parse(gen_result.desc);
		console.log("content:", content);

        return content;
	}, 

	show_progress: function ()
	{
		//$$("meta_editor").disable(); //고민해보자
		$$("meta_editor").showProgress({
			type: "icon",
			//type: "top",
			delay: 100000
		});
	},

	hide_progress: function ()
	{
		//$$("meta_editor").enable();
		$$("meta_editor").hideProgress();

		var message_box = $$("message_box");
		if (typeof message_box != 'undefined')
		{
			message_box.close();
		}
	},

	exception_message: function (ex, message)
	{
		return message + "\nEXCEPTION:" + ex.toString() + "\nSTACK\n" + (new Error).stack;
	},

	error_window: function (error_message, is_gen_result)
	{
		var message_text = error_message
		if (true == is_gen_result)
		{
			message_text = "ERR CODE: " + error_message.code + "\n" + error_message.desc;
		}
		console.log(error_message);
		webix.ui({
			view: "window",
			id: "error_message_window",
			height: 300,
			width:400,
			move: true,
			autofocus: true,
			modal: true,
			position: "center",
			animate: { type: "flip" },

			head: {
				view: "toolbar", cols: [
					{ view: "label", label: "MESSAGE" },
					{ view: "icon", icon: "times-circle", click: "$$('error_message_window').close();" }
				]
			},
			body: {
				view: "textarea",
				scroll:"y",
				value: message_text
			}
		}).show();
	}, 
};


var a = {
	"result":"Fail",
	"code": 0,
	"desc": "'D:\_project\onnet_engine_client\gen_meta_tool\meta_data\map@oge.xlsx' 파일은 다른 프로세스에서 사용 중이므로 프로세스에서 액세스할 수 없습니다."
}