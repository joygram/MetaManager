#if !GEN_CLIENT
using Newtonsoft.Json.Linq;
using System.Data;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;

namespace gen
{
	public class MetaLogic : MetaLogicBase
	{
		IXLWorksheet m_ws_meta = null; //closedxml

		public MetaLogic(string root_dir)
			: base(root_dir)
		{

		}
		override protected gen.Result loadWorkSheet(string meta_file_path)
		{
			var gen_result = new gen.Result();
			try
			{
				var workbook = new XLWorkbook(meta_file_path); // open exist file
				workbook.Worksheets.TryGetWorksheet("meta_data", out m_ws_meta);
			}
			catch (Exception ex)
			{
				return gen_result.setFail(String.Format("{0}", ex.ToString()));
			}
			return gen_result.setOk();
		}
		override protected gen.Result writeExcelHeaderToWorksheet()
		{
			if (null == m_ws_meta)
			{
				return m_result.setFail("meta_worksheet is null");
			}

			m_ws_meta.SheetView.Freeze(3, 3);
			IXLWorksheet ws = m_ws_meta;

			string parent_field_path = "";
			JArray excel_header = excelHeader();
			foreach (JObject header_column in excel_header)
			{
				Int32 xls_column = header_column["xls_column"].Value<Int32>();
				applyHeaderStyle(header_column, xls_column, parent_field_path);
			}
			m_ws_meta.Columns().AdjustToContents();
			m_ws_meta.Range(m_ws_meta.Cell(3, 1), m_ws_meta.Cell(3, excelHeader().Count)).SetAutoFilter(); // 타이틀에 필터기능 추가  [2019/1/11 by joygram]

			return m_result.setOk();
		}
		override public gen.Result saveToExcel(string file_path = "", bool save = true)
		{
			// json result 생성 
			m_result.setOk();
			try
			{
				//파일을 읽어 worksheet를 복사 

				var workbook = new XLWorkbook(); // make new file
				m_ws_meta = workbook.Worksheets.Add("meta_data");

				writeToWorksheet();

				string save_file_path = m_schema_name + ".xlsx";
				if (0 != file_path.Length)
				{
					save_file_path = file_path;
				}

				if (save)
				{
					workbook.SaveAs(save_file_path);
					m_log.InfoFormat("save complreted {0}", file_path);
				}
				workbook.Dispose();
			}
			catch (System.Exception ex)
			{
				m_result.setExceptionOccurred(ex.ToString());
				gen.Log.logger("exception").FatalFormat("RESULT:{0}", m_result.m_desc);
			}
			return m_result;
		}


		public void applyCellStyleBackup(IXLWorksheet worksheet)
		{
			var ws = worksheet;
			//Row전체 색상 변경 
			var range = ws.Range(ws.Cell(1, 1).Address, ws.Cell(3, excelHeader().Count).Address);
			range.Style.Fill.BackgroundColor = XLColor.LightGray;
			range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin; ;
			range.Style.Border.InsideBorder = XLBorderStyleValues.Thin; ;
			range.Style.Font.FontName = "Consolas";
			range.Style.Font.FontSize = 9;
		}

		override public gen.Result updateToExcel(string file_path = "")
		{
			try
			{
				string save_file_path = m_schema_name + ".xlsx";
				if (0 != file_path.Length)
				{
					save_file_path = file_path;
				}

				// load meta worksheet  
				var workbook = new XLWorkbook(save_file_path); // open exist file

				//find exist backup & remove
				var backups = new List<string>(); // take backup_ name 
				var e = workbook.Worksheets.GetEnumerator();
				while (e.MoveNext())
				{
					var ws = e.Current;
					if (ws.Name.Contains("backup_"))
					{
						backups.Add(ws.Name);
					}
				}
				foreach (var name in backups)
				{
					workbook.Worksheets.Delete(name);
				}
				// make backup worksheet 
				var ws_meta = workbook.Worksheets.Worksheet("meta_data");
				var now = DateTime.Now;
				var backup_name = String.Format("backup_meta_{0}-{1}-{2}", now.Year, now.Month, now.Day);
				ws_meta.Name = backup_name;
				applyCellStyleBackup(ws_meta);

				m_ws_meta = workbook.Worksheets.Add("meta_data");
				m_ws_meta.Position = 1;

				writeToWorksheet();

				workbook.Save();
				workbook.Dispose();
			}
			catch (System.Exception ex)
			{
				m_result.setExceptionOccurred(ex.ToString());
				gen.Log.logger("exception").FatalFormat("RESULT:{0}", m_result.m_desc);
			}
			return m_result;
		}
		override public object cellValue(Int32 row, Int32 col)
		{
			return m_ws_meta.Cell(row, col).Value;
		}

		override public bool isCellEmpty(Int32 row, Int32 col)
		{
			return m_ws_meta.Cell(row, col).IsEmpty();
		}
		public override void setCellValue<T>(Int32 row, Int32 col, T input_value)
		{
			var cell = m_ws_meta.Cell(row, col);
			cell.SetValue<T>(input_value);
			cell.Style.Font.FontName = "Consolas";
			cell.Style.Font.FontSize = 9;
		}
		override public Int32 lastUsedRowNumber()
		{
			return m_ws_meta.LastRowUsed().RowNumber();
		}
		override public Int32 lastUsedColNumber()
		{
			return m_ws_meta.LastColumnUsed().ColumnNumber();
		}
		override protected void applyCellStyleDefault(Int32 xls_row, Int32 xls_col)
		{
			var cell = m_ws_meta.Cell(xls_row, xls_col);
			cell.Style.Fill.BackgroundColor = XLColor.PastelPink;
			cell.Style.Font.FontName = "Consolas";
			cell.Style.Font.FontSize = 9;
		}
		override protected void applyCellStyleRec(Int32 xls_row, Int32 xls_col)
		{
			var cell = m_ws_meta.Cell(xls_row, xls_col);
			cell.Style.Fill.BackgroundColor = XLColor.PastelOrange;
			cell.Style.Font.FontName = "Consolas";
			cell.Style.Font.FontSize = 9;
		}
		override protected void applyCellStyleLst(Int32 xls_row, Int32 xls_col)
		{
			var cell = m_ws_meta.Cell(xls_row, xls_col);
			cell.Style.Fill.BackgroundColor = XLColor.PastelViolet;
			cell.Style.Font.FontName = "Consolas";
			cell.Style.Font.FontSize = 9;
		}
		override protected void applyCellStyleDbl(Int32 xls_row, Int32 xls_col)
		{
			var cell = m_ws_meta.Cell(xls_row, xls_col);
			cell.Style.NumberFormat.Format = "#,##0.0000";
			cell.Style.Font.FontName = "Consolas";
			cell.Style.Font.FontSize = 9;
		}
		override protected void applyCellStyleEnum(Int32 xls_row, Int32 xls_col)
		{
			var cell = m_ws_meta.Cell(xls_row, xls_col);
			cell.Style.Fill.BackgroundColor = XLColor.YellowNcs;
			cell.Style.Font.FontName = "Consolas";
			cell.Style.Font.FontSize = 9;
		}
		override protected void applyCellStyleDate(Int32 xls_row, Int32 xls_col)
		{
			var cell = m_ws_meta.Cell(xls_row, xls_col);
			cell.Style.Font.FontName = "Consolas";
			cell.Style.Font.FontSize = 9;
		}
		override public void applyCellStyleMetaId(Int32 xls_row)
		{
			var ws = m_ws_meta;
			//Row전체 색상 변경 
			var range = ws.Range(ws.Cell(xls_row, 1).Address, ws.Cell(xls_row, excelHeader().Count).Address);
			range.Style.Fill.BackgroundColor = XLColor.PastelPink;
			range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin; ;
			range.Style.Font.FontName = "Consolas";
			range.Style.Font.FontSize = 9;

			ws.Cell(xls_row, 1).Style.Fill.BackgroundColor = XLColor.LightCoral;
			ws.Cell(xls_row, 3).Style.Fill.BackgroundColor = XLColor.LightCoral;
		}
		Int32 m_field_path_color_g = 60;
		Int32 m_field_path_color_b = 60;
		XLColor thriftFieldBackgroundColor(ref string parent_path, string current_path)
		{
			bool is_child_field = current_path.StartsWith(parent_path + ".");
			if (!is_child_field)
			{
				if (m_field_path_color_g < 200)
				{
					m_field_path_color_g = (m_field_path_color_g + 40) % 255;
				}
				else
				{
					m_field_path_color_b = (m_field_path_color_b + 40) % 255;
				}
				parent_path = current_path;
			}
			return XLColor.FromArgb(200, m_field_path_color_g, m_field_path_color_b);
		}
		override protected void applyHeaderStyle(JObject header_column, Int32 xls_col, string parent_field_path)
		{
			var data_type = header_column["datatype"].ToString();
			var elem_type_id = header_column["elementtype"].ToString();
			var thrift_field_path = header_column["thrift_field_path"].ToString();

			var row_01 = m_ws_meta.Cell(1, xls_col);
			var row_02 = m_ws_meta.Cell(2, xls_col);
			var row_03 = m_ws_meta.Cell(3, xls_col);

			row_01.Style.Border.OutsideBorder = XLBorderStyleValues.Dotted;
			row_01.Style.Font.FontName = "Consolas";
			row_01.Style.Font.FontSize = 9;
			row_01.Style.Fill.BackgroundColor = thriftFieldBackgroundColor(ref parent_field_path, thrift_field_path);
			if ("lst" == data_type)
			{
				data_type = string.Format("{0}<{1}>", data_type, thriftNameToExcel(elem_type_id));

				if (isPrimitiveThriftTypeId(elem_type_id))
				{

				}
				row_02.Style.Fill.BackgroundColor = XLColor.PastelViolet;// PastelPink;
				row_03.Style.Fill.BackgroundColor = XLColor.PastelViolet;
			}
			else if ("rec" == data_type)
			{
				data_type = thriftNameToExcel(elem_type_id);
				row_02.Style.Fill.BackgroundColor = XLColor.PastelOrange;
				row_03.Style.Fill.BackgroundColor = XLColor.PastelOrange;
			}
			else if ("enum" == data_type)
			{
				data_type = string.Format("{0}<{1}>", data_type, thriftNameToExcel(elem_type_id));
				row_02.Style.Fill.BackgroundColor = XLColor.PastelBlue;
				row_03.Style.Fill.BackgroundColor = XLColor.PastelBlue;
			}
			else
			{
				row_02.Style.Fill.BackgroundColor = XLColor.LightSalmonPink;
				row_03.Style.Fill.BackgroundColor = XLColor.LightSalmonPink;
			}

			row_02.Style.Border.OutsideBorder = XLBorderStyleValues.Dotted;
			row_02.Style.Font.FontName = "Consolas";
			row_02.Style.Font.FontSize = 9;

			row_03.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
			row_03.Style.Font.FontName = "Consolas";
			row_03.Style.Font.FontSize = 9;

			setCellValue(1, xls_col, thrift_field_path);
			setCellValue(2, xls_col, data_type);
			setCellValue(3, xls_col, header_column["header"].ToString());
		}

	}
}
#endif //!NET35