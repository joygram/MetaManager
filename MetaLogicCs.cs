using Newtonsoft.Json.Linq;
using System;

using System.IO;
using OfficeOpenXml; //EPPLUS
using OfficeOpenXml.Style;

// handle excel with EPPLUS
namespace gen
{
	public class MetaLogicCs : MetaLogicBase
	{
		ExcelWorksheet m_ws_meta = null; 

		public MetaLogicCs(string root_dir)
			: base(root_dir)
		{

		}
		override protected gen.Result loadWorkSheet(string meta_file_path)
		{
			var gen_result = new gen.Result();
			try
  			{
				var file_info = new FileInfo(meta_file_path);
				var package = new ExcelPackage(file_info);
				m_ws_meta = package.Workbook.Worksheets["meta_data"];
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
			m_ws_meta.View.FreezePanes(3, 3);

			string parent_field_path = "";
			JArray excel_header = excelHeader();
			foreach (JObject header_column in excel_header)
			{
				Int32 xls_column = header_column["xls_column"].Value<Int32>();
				applyHeaderStyle(header_column, xls_column, parent_field_path);
			}
 			m_ws_meta.Cells.AutoFitColumns(); //내용 크기에 맞추기

			var range = m_ws_meta.Cells[3, 1, 3, excelHeader().Count];
			range.AutoFilter = true; // 타이틀에 오토 필터기능 추가  [2019/1/15 by joygram]

			return m_result.setOk();
		}

		override public gen.Result saveToExcel(string file_path = "", bool save = true)
		{
			// json result 생성: 기존파일을 모두 제거하고 새로 생성함.
			m_result.setOk();
			try
			{
				var package = new ExcelPackage();
				m_ws_meta = package.Workbook.Worksheets.Add("meta_data");

				writeToWorksheet();

				string save_file_path = m_schema_name + ".xlsx";
				if (0 != file_path.Length)
				{
					save_file_path = file_path;
				}

				if (save)
				{
					package.SaveAs(new FileInfo(@save_file_path));
				}
			}
			catch (System.Exception ex)
			{
				m_result.setExceptionOccurred(ex.ToString());
				gen.Log.logger("exception").FatalFormat("{0}", m_result.m_desc);
			}
			return m_result;
		}
		override public gen.Result updateToExcel(string file_path = "")
		{
			m_result.setOk();
			try
			{      
				string save_file_path = m_schema_name + ".xlsx";
				if (0 != file_path.Length)
				{
					save_file_path = file_path;
				}


				// load meta worksheet  
				var file_info = new FileInfo(save_file_path);
				var package = new ExcelPackage(file_info);
				m_ws_meta = package.Workbook.Worksheets["meta_data"];

				writeToWorksheet();

				package.Save();

			}
			catch (System.Exception ex)
			{
				m_result.setExceptionOccurred(ex.ToString());
				gen.Log.logger("exception").FatalFormat("{0}", m_result.m_desc);

			}
			return m_result;
		}
		override public object cellValue(Int32 row, Int32 col)
		{
			try
			{
				var cell = m_ws_meta.Cells[row, col];
				if (null == cell)
				{
					m_log.ErrorFormat("cell {0},{1} is null", row, col);
					return new Object();
				}
				return cell.Value;
			}
			catch (System.Exception ex)
			{
				m_log.ErrorFormat("row:{0},col:{1} no data exception, {2}", row, col, ex.ToString());
				return new Object();				
			}
		}
		override public bool isCellEmpty(Int32 row, Int32 col)
		{
			return (null == m_ws_meta.Cells[row, col].Value); 
		}
		public override void setCellValue<T>(Int32 row, Int32 col, T input_value)
		{
			m_ws_meta.Cells[row, col].Value = input_value;
		}
		override public Int32 lastUsedRowNumber()
		{
			return m_ws_meta.Dimension.End.Row;
		}
		override public Int32 lastUsedColNumber()
		{
			return m_ws_meta.Dimension.End.Column;
		}
		override protected void applyCellStyleDefault(Int32 xls_row, Int32 xls_col)
		{
			//PastelOrange
			var cell = m_ws_meta.Cells[xls_row, xls_col];
			cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
			cell.Style.Fill.BackgroundColor.SetColor(00, 0xff, 0xd1, 0xdc); // pastel pink
			cell.Style.Font.Name = "Consolas";
			//cell.AutoFitColumns();
		}
		override protected void applyCellStyleRec(Int32 xls_row, Int32 xls_col)
		{
			//PastelOrange
			var cell = m_ws_meta.Cells[xls_row, xls_col];
			cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
			cell.Style.Fill.BackgroundColor.SetColor(00, 0xff, 0xb3, 0x47);
		}
		override protected void applyCellStyleLst(Int32 xls_row, Int32 xls_col)
		{
			//PastelViolet
			var cell = m_ws_meta.Cells[xls_row, xls_col];
			cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
			cell.Style.Fill.BackgroundColor.SetColor(00, 0xcb, 0x99, 0xc9);
			cell.Style.Font.Name = "Consolas";
		}
		override protected void applyCellStyleDbl(Int32 xls_row, Int32 xls_col)
		{
			var cell = m_ws_meta.Cells[xls_row, xls_col];
			cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
			cell.Style.Numberformat.Format = "#,##0.0000";
			//cell.Style.Font.Name = "Consolas";
		}
		override protected void applyCellStyleEnum(Int32 xls_row, Int32 xls_col)
		{
			// Yellow (NCS)
			var cell = m_ws_meta.Cells[xls_row, xls_col];
			cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
			cell.Style.Fill.BackgroundColor.SetColor(00, 0xff, 0xd3, 0x00);
			//cell.Style.Font.Name = "Consolas";
		}
		override protected void applyCellStyleDate(Int32 xls_row, Int32 xls_col)
		{
			// Yellow (NCS)
			var cell = m_ws_meta.Cells[xls_row, xls_col];
			cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
			cell.Style.Fill.BackgroundColor.SetColor(00, 0xff, 0xd3, 0x00);
			//cell.Style.Font.Name = "Consolas";
		}
		override public void applyCellStyleMetaId(Int32 xls_row)
		{
			// Light coral : f0 80 80 
			var ws = m_ws_meta;

			//Row전체 색상 변경
			{
				var range = m_ws_meta.Cells[xls_row, 1, xls_row, excelHeader().Count];
				range.Style.Fill.PatternType = ExcelFillStyle.Solid;
				range.Style.Fill.BackgroundColor.SetColor(00, 0xff, 0xd1, 0xdc); //Pastel Pink
				range.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
				range.Style.Font.Name = "Consolas";
			}

			// 헤더스타일 
			{
				var cell = m_ws_meta.Cells[xls_row, 1];
				cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
				cell.Style.Fill.BackgroundColor.SetColor(00, 0xf0, 0x80, 0x80); //LightCoral
			}
			{
				var cell = m_ws_meta.Cells[xls_row, 3];
				cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
				cell.Style.Fill.BackgroundColor.SetColor(00, 0xf0, 0x80, 0x80); //LightCoral
			}
		}    
		Int32 m_field_path_color_g = 60;
		Int32 m_field_path_color_b = 60;
		struct XLColor
		{
			public Int32 m_r;
			public Int32 m_g;
			public Int32 m_b;

			public XLColor(Int32 r, Int32 g, Int32 b)
			{
				m_r = r;
				m_g = g;
				m_b = b;
			}
		}
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
			return new XLColor(200, m_field_path_color_g, m_field_path_color_b);
		}
		override protected void applyHeaderStyle(JObject header_column, Int32 xls_col, string parent_field_path)
		{
			var data_type = header_column["datatype"].ToString();
			var elem_type_id = header_column["elementtype"].ToString();
			var thrift_field_path = header_column["thrift_field_path"].ToString();

			var row_01 = m_ws_meta.Cells[1, xls_col];
			var row_02 = m_ws_meta.Cells[2, xls_col];
			var row_03 = m_ws_meta.Cells[3, xls_col];

			row_01.Style.Fill.PatternType = ExcelFillStyle.Solid;
			row_02.Style.Fill.PatternType = ExcelFillStyle.Solid;
			row_03.Style.Fill.PatternType = ExcelFillStyle.Solid;



			row_01.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Dotted);
			row_01.Style.Font.Name = "Consolas";

			XLColor bg_color = thriftFieldBackgroundColor(ref parent_field_path, thrift_field_path);

			row_01.Style.Fill.BackgroundColor.SetColor(0, bg_color.m_r, bg_color.m_g, bg_color.m_b);

			if ("lst" == data_type)
			{
				data_type = string.Format("{0}<{1}>", data_type, thriftNameToExcel(elem_type_id));

				if (isPrimitiveThriftTypeId(elem_type_id))
				{

				}
				row_02.Style.Fill.BackgroundColor.SetColor(00, 0xcb, 0x99, 0xc9); //PastelViolet
				row_03.Style.Fill.BackgroundColor.SetColor(00, 0xcb, 0x99, 0xc9);
			}
			else if ("rec" == data_type)
			{
				data_type = thriftNameToExcel(elem_type_id);

				//Pastel Orange
				row_02.Style.Fill.BackgroundColor.SetColor(00, 0xff, 0xb3, 0x47); //PastelOrange
				row_03.Style.Fill.BackgroundColor.SetColor(00, 0xff, 0xb3, 0x47);
			}
			else if ("enum" == data_type)
			{
				data_type = string.Format("{0}<{1}>", data_type, thriftNameToExcel(elem_type_id));

				//Pastel Blue 
				row_02.Style.Fill.BackgroundColor.SetColor(00, 0xae, 0xc6, 0xcf); //PastelBlue
				row_03.Style.Fill.BackgroundColor.SetColor(00, 0xae, 0xc6, 0xcf);
			}
			else
			{
				row_02.Style.Fill.BackgroundColor.SetColor(00, 0xff, 0x99, 0x99); //LightSalmonPink
				row_03.Style.Fill.BackgroundColor.SetColor(00, 0xff, 0x99, 0x99);
			}

			row_02.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Dotted);
			row_02.Style.Font.Name = "Consolas";

			row_03.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
			row_03.Style.Font.Name = "Consolas";

			setCellValue(1, xls_col, thrift_field_path);
			setCellValue(2, xls_col, data_type);
			setCellValue(3, xls_col, header_column["header"].ToString());
		}
	}
}

