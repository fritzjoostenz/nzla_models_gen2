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
    }

    private void ReadExpressionsData()
    {
        SpreadsheetGear.IWorksheet xlSheet = this.Workbook.Worksheets["expressions"];
        this.ExpressionsData = new jcDataSet();
        ExpressionsData.AddColumn("expression_key");
        ExpressionsData.AddColumn("expression_type");
        ExpressionsData.AddColumn("setup_code");
        ExpressionsData.AddColumn("test_expected");

        int iRow = 1;
        string txt = xlSheet.Cells[iRow, 0].Text;
        while(!string.IsNullOrEmpty(txt))
        {
            Dictionary<string, object> row = new Dictionary<string, object>();
            row["expression_key"] = xlSheet.Cells[iRow, 0].Text;
            row["expression_type"] = xlSheet.Cells[iRow, 1].Text;
            row["setup_code"] = xlSheet.Cells[iRow, 2].Text;
            row["test_expected"] = xlSheet.Cells[iRow, 3].Value;
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

}
