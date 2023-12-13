using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using JCass_Data.Objects;
using SpreadsheetGear;

namespace ExpressionTester.TestHarness;

internal class ExpressionTestData
{
    SpreadsheetGear.IWorkbook Workbook;

    public jcDataSet ExpressionsData;
    public Dictionary<string, object> RawData;
    public Dictionary<string, object> ParameterData;
   
    public ExpressionTestData(string filePath)
    {
        this.Workbook = SpreadsheetGear.Factory.GetWorkbook(filePath);        
        this.ReadExpressionsData();
        this.RawData = this.ReadDictionary("raw");
        this.ParameterData = this.ReadDictionary("parameters");
        this.ReadLookups("lookups");
    }

    public Dictionary<string, Dictionary<string, object>> Lookups;

    private void ReadExpressionsData()
    {
        SpreadsheetGear.IWorksheet xlSheet = this.Workbook.Worksheets["functions"];
        this.ExpressionsData = new jcDataSet();
        ExpressionsData.AddColumn("function_key");
        ExpressionsData.AddColumn("function_type");
        ExpressionsData.AddColumn("setup_code");
        ExpressionsData.AddColumn("test_expected");
        ExpressionsData.AddColumn("evaluation_stage");
        ExpressionsData.AddColumn("assign_to_param");
       
        Dictionary<string, int> headers = new Dictionary<string, int>();
        int iRow = 0;
        int iCol = 0;   
        string header = xlSheet.Cells[iRow, iCol].Text;
        while (!string.IsNullOrEmpty(header)) 
        {
            headers.Add(header, iCol);
            iCol++;
            header = xlSheet.Cells[iRow, iCol].Text;
        }

        iRow++;
        string txt = xlSheet.Cells[iRow, 0].Text;
        while(!string.IsNullOrEmpty(txt))
        {
            Dictionary<string, object> row = new Dictionary<string, object>();
            row["function_key"] = xlSheet.Cells[iRow, headers["function_key"]].Text;
            row["function_type"] = xlSheet.Cells[iRow, headers["function_type"]].Text;
            row["setup_code"] = xlSheet.Cells[iRow, headers["setup_code"]].Text;
            row["test_expected"] = xlSheet.Cells[iRow, headers["test_expected"]].Value;
            row["evaluation_stage"] = xlSheet.Cells[iRow, headers["evaluation_stage"]].Text;
            row["assign_to_key"] = xlSheet.Cells[iRow, headers["assign_to_key"]].Text;
            this.ExpressionsData.AddRow(row);
                       

            iRow++;
            txt = xlSheet.Cells[iRow, 0].Text;
        }
        
    }

    private Dictionary<string, object> ReadDictionary(string sheetName)
    {        
        SpreadsheetGear.IWorksheet xlSheet = this.Workbook.Worksheets[sheetName];

        Dictionary<string, object> values = new Dictionary<string, object>();
        int iRow = 1;
        string txt = xlSheet.Cells[iRow, 0].Text;
        while (!string.IsNullOrEmpty(txt))
        {
            Dictionary<string, object> row = new Dictionary<string, object>();
            string key = xlSheet.Cells[iRow, 0].Text;
            object value = xlSheet.Cells[iRow, 1].Value;
            values.Add(key, value);

            iRow++;
            txt = xlSheet.Cells[iRow, 0].Text;
        }
        return values;
    }


    private void ReadLookups(string sheetName)
    {
        SpreadsheetGear.IWorksheet xlSheet = this.Workbook.Worksheets[sheetName];

        this.Lookups = new Dictionary<string, Dictionary<string, object>>();
        int iRow = 1;
        string key1 = xlSheet.Cells[iRow, 0].Text;
        while (!string.IsNullOrEmpty(key1))
        {
            if (!this.Lookups.ContainsKey(key1)) { this.Lookups.Add(key1, new Dictionary<string, object>()); }

            string key2 = xlSheet.Cells[iRow, 1].Text;
            object value = xlSheet.Cells[iRow, 2].Value;

            this.Lookups[key1].Add(key2, value);

            iRow++;
            key1 = xlSheet.Cells[iRow, 0].Text;
        }
    }
}
