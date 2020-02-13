


//main
main_layout =
{
	id: "meta_editor",
	view: "accordion",
	multi: true,
	cols: [
		{
			header: "tables",
			collapsed:false,
			body: meta_tabview,
		},
		{
			id: "files_tab",
			header: "files",
			collapsed: false,
			body: filelist_tree_layout,
		}
	]
};

webix.ready(function ()
{
	webix.codebase = "./webix/codebase/components/ace/";

	console.log("webix_editors", webix.editors);

	webix.ui(main_layout);
	webix.extend($$("meta_editor"), webix.ProgressBar);

	$$("file_list_filter").attachEvent("onTimedKeyPress",
		function ()
		{
			$$("file_list_tree").filter("#value#", this.getValue());
		});

	$$("meta_tabview").removeView("empty_tab");


    //$$("file_list_tree").load(MetaCommon.m_server_url + "/meta/");
	refreshFileList();
});



