using System;
using System.Collections.Generic;
using System.Text;
using Gb18030.Shared;

namespace TestWebFormsApp
{
    public partial class DataControls : System.Web.UI.Page
    {
        private class Row { public string Value { get; set; } }

        protected void Page_Load(object sender, EventArgs e)
        {
            int index = int.Parse(Request.QueryString["i"] ?? "0");
            string enc = Request.QueryString["enc"];
            if (!string.IsNullOrEmpty(enc))
            {
                try { Response.ContentEncoding = Encoding.GetEncoding(enc); }
                catch { }
            }
            string overrideString = Request.QueryString["s"];

            var value = overrideString ?? TestStringProvider.GetByIndex(index);
            // int total = TestStringProvider.LoadAll().Length;
            // var value2 = TestStringProvider.GetByIndex((index + 1) % total);
            var data = new List<Row> { new Row { Value = value } };

            // GridView header & caption
            GridViewTest.Caption = value;
            GridViewTest.DataSource = data;
            GridViewTest.DataBind();
            if (GridViewTest.HeaderRow != null && GridViewTest.HeaderRow.Cells.Count > 0)
            {
                GridViewTest.HeaderRow.Cells[0].Text = value; //value2; // header text different from cell value
                GridViewTest.HeaderRow.Cells[0].Attributes["id"] = "GridViewTestHeader";
            }
            // NOTE: GridView does not expose a CaptionRow API; the caption element will be discovered by the test driver via the <caption> tag under the table.

            // Repeater header literal
            RepeaterTest.DataSource = data;
            RepeaterTest.DataBind();
            var header = RepeaterTest.Controls.Count > 0 ? RepeaterTest.Controls[0].FindControl("RepeaterHeader") as System.Web.UI.HtmlControls.HtmlGenericControl : null;
            if (header != null) header.InnerText = value;

            // DataList header (use HeaderTemplate programmatically)
            DataListTest.DataSource = data;
            DataListTest.DataBind();
            // No built-in header id; could prepend a header item but skipping for now.

            // ListView layout, we can tag first item after DataBind
            ListViewTest.DataSource = data;
            ListViewTest.DataBind();

            // DetailsView header cell
            DetailsViewTest.DataSource = data;
            DetailsViewTest.DataBind();
            var dvheader = DetailsViewTest.Controls.Count > 0 ? DetailsViewTest.Controls[0].FindControl("DetailsViewTestHeader") as System.Web.UI.HtmlControls.HtmlGenericControl : null;
            if (dvheader != null) dvheader.InnerText = value;

            // FormView header via viewstate-coded caption simulation: wrap in span id inside template not present now; instead set main container id attribute if present
            FormViewTest.DataSource = data;
            FormViewTest.DataBind();
        }
    }
}