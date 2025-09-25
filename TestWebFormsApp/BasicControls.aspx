<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="BasicControls.aspx.cs" Inherits="TestWebFormsApp.ExamplePage" %>

<!DOCTYPE html>

<html>
<head runat="server">
    <title>GB18030 Basic Controls Test</title>
    <meta charset="utf-8" />
</head>
<body>
    <form id="form1" runat="server">
        <div>
            <asp:Label ID="LabelTest" runat="server" />
            <span id="LiteralTestContainer"><asp:Literal ID="LiteralTest" runat="server" /></span>
            <asp:TextBox ID="TextBoxTest" runat="server" />
            <asp:HyperLink ID="HyperLinkTest" runat="server" NavigateUrl="#" />
            <asp:LinkButton ID="LinkButtonTest" runat="server" />
            <asp:Button ID="ButtonTest" runat="server" />
            <asp:CheckBox ID="CheckBoxTest" runat="server" Text="" />
            <asp:RadioButton ID="RadioButtonTest" runat="server" Text="" GroupName="g" />
            <asp:DropDownList ID="DropDownListTest" runat="server" />
            <asp:ListBox ID="ListBoxTest" runat="server" />
            <asp:BulletedList ID="BulletedListTest" runat="server" />
        </div>
    </form>
</body>
</html>