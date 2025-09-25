using System;
using System.Text;
using Gb18030.Shared;

namespace TestWebFormsApp
{
    public partial class ExamplePage : System.Web.UI.Page
    {
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
            // second value for multi-item tests
            // int total = TestStringProvider.LoadAll().Length;
            // var value2 = TestStringProvider.GetByIndex((index + 1) % total);

            LabelTest.Text = value; LabelTest.ToolTip = value; // title attribute
            LiteralTest.Text = value; // literal only has inner text

            TextBoxTest.Text = value; // visible value
            TextBoxTest.Attributes["placeholder"] = value; // attribute for placeholder test (not simultaneously visible but encoded)
            TextBoxTest.ToolTip = value;

            HyperLinkTest.Text = value; HyperLinkTest.ToolTip = value;
            LinkButtonTest.Text = value; LinkButtonTest.ToolTip = value;
            ButtonTest.Text = value; ButtonTest.ToolTip = value;

            CheckBoxTest.Text = value; CheckBoxTest.ToolTip = value;
            RadioButtonTest.Text = value; RadioButtonTest.ToolTip = value;

            DropDownListTest.Items.Clear();
            DropDownListTest.Items.Add(value); // first item
            DropDownListTest.Items.Add(value); //value2); // second item
            DropDownListTest.ToolTip = value;

            ListBoxTest.Items.Clear();
            ListBoxTest.Items.Add(value);
            ListBoxTest.Items.Add(value); //value2);
            ListBoxTest.ToolTip = value;

            BulletedListTest.Items.Clear();
            BulletedListTest.Items.Add(value);
            BulletedListTest.ToolTip = value;
        }
    }
}