<%@ Page Language="C#" AutoEventWireup="true" CodeFile="DatabaseTest.aspx.cs" Inherits="DatabaseTest" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>Database Test</title>
</head>
<body>

    <form id="form1" runat="server">
  <div>
      &nbsp;
              This should display up to 10 sites<div>
                  <asp:SqlDataSource ID="SqlDataSource1" runat="server" ConnectionString="<%$ ConnectionStrings:OdmConnection %>"
            SelectCommand="SELECT TOP 10 [st_id], [st_name] FROM [stations]"></asp:SqlDataSource>
                  <asp:GridView ID="GridView1" runat="server" AutoGenerateColumns="False" DataSourceID="SqlDataSource1">
                      <Columns>
                          <asp:BoundField DataField="st_id" HeaderText="SiteCode" ReadOnly="True" SortExpression="st_id" />
                          <asp:BoundField DataField="st_name" HeaderText="SiteName" SortExpression="st_id" />
                      </Columns>
                  </asp:GridView>
                  <div>
                      This should display  up to 10 variables
                  </div>
                  <asp:SqlDataSource ID="SqlDataSource2" runat="server" ConnectionString="<%$ ConnectionStrings:OdmConnection %>"
            SelectCommand="SELECT TOP 10 [var_id], [var_name] FROM [variables] WHERE var_id=8"></asp:SqlDataSource>
                  <asp:GridView ID="GridView2" runat="server" DataSourceID="SqlDataSource2">
                  </asp:GridView>
                  <div>
                      This should display up to 10 records from the series catalog</div>
                  <asp:SqlDataSource ID="SqlDataSource3" runat="server" ConnectionString="<%$ ConnectionStrings:OdmConnection %>"
            SelectCommand="SELECT TOP 10 [st_id], [st_name] FROM [stations]"></asp:SqlDataSource>
                  <asp:GridView ID="GridView3" runat="server" DataSourceID="SqlDataSource3">
                  </asp:GridView>
              </div>
          
  
  </div>
    </form>
</body>
</html>
