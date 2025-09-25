<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="DataControls.aspx.cs" Inherits="TestWebFormsApp.DataControls" %>

<!DOCTYPE html>

<html>
<head runat="server">
    <title>GB18030 Data Controls Test</title>
    <meta charset="utf-8" />
</head>
<body>
    <form id="form1" runat="server">
        <div>
            <asp:GridView ID="GridViewTest" runat="server" AutoGenerateColumns="false">
                <Columns>
                    <asp:BoundField DataField="Value" />
                </Columns>
            </asp:GridView>
            <asp:Repeater ID="RepeaterTest" runat="server">
                <HeaderTemplate><span id="RepeaterHeader" runat="server" class="repeater-header"></span></HeaderTemplate>
                <ItemTemplate><span class="repeater-item"><%# Eval("Value") %></span></ItemTemplate>
            </asp:Repeater>
            <asp:DataList ID="DataListTest" runat="server">
                <ItemTemplate><span class="datalist-item"><%# Eval("Value") %></span></ItemTemplate>
            </asp:DataList>
            <asp:ListView ID="ListViewTest" runat="server">
                <ItemTemplate><span class="listview-item"><%# Eval("Value") %></span></ItemTemplate>
            </asp:ListView>
            <asp:DetailsView ID="DetailsViewTest" runat="server" AutoGenerateRows="false">
                <HeaderTemplate><span id="DetailsViewTestHeader" runat="server" class="detailsview-header"></span></HeaderTemplate>
                <Fields>
                    <asp:BoundField DataField="Value" />
                </Fields>
            </asp:DetailsView>
            <asp:FormView ID="FormViewTest" runat="server">
                <ItemTemplate><span class="formview-item"><%# Eval("Value") %></span></ItemTemplate>
            </asp:FormView>
        </div>
    </form>
</body>
</html>