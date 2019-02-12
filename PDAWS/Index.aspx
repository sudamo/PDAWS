<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Index.aspx.cs" Inherits="PDAWS.Index" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8"/>
    <title>车邦扫描枪服务</title>
</head>
<body>
    <form id="form1" runat="server">
        <h3>ERP自定义功能</h3>
        <fieldset>
            <legend>财务</legend>
        </fieldset>
        <br />
        <fieldset>
            <legend>供应链</legend>
                <label>单整单发货<br />
                <asp:Label ID="lblBillNo" runat="server" Text="销售订单号："></asp:Label>
                <asp:TextBox ID="txtBillNo" runat="server" Width="120"></asp:TextBox>
                <asp:RadioButton ID="rbtSingle" GroupName="SingleShipment" Text="整单" Checked="false" runat="server"/>
                <asp:RadioButton ID="rbtNotSingle" GroupName="SingleShipment" Text="非整单" Checked="true" runat="server"/>
                <asp:Button ID="btnSingleShipment" Text="确定" OnClick="btnSingleShipment_Click" runat="server"/><br />
                </label>
                <br />
                <label>可出数量<br />
                <asp:Label ID="lblBillNo2" runat="server" Text="销售订单号："></asp:Label>
                <asp:TextBox ID="txtBillNo2" runat="server" Width="120"></asp:TextBox>
                <asp:Label ID="lblMTL" runat="server" Text="物料编码："></asp:Label>
                <asp:TextBox ID="txtMTL" runat="server" Width="150"></asp:TextBox>
                <asp:Label ID="lblFCanOutQty" runat="server" Text="调整可出数量："></asp:Label>
                <asp:TextBox ID="txtFCanOutQty" runat="server" Width="50"></asp:TextBox>
                <asp:Button ID="btnUpdateCanOutQty" Text="确定" OnClick="btnUpdateCanOutQty_Click" runat="server"/><br />
                </label>
        </fieldset>
        <br />
        <fieldset>
            <legend>生产制造</legend>
        </fieldset>
        <h3>联系方式</h3>
            <address>
                广州市白云区同沙路283号天健创意园8栋302<br />
                4006-998-168<br />
                020-62870908<br />
                邮政编码：510080<br />
                <abbr title="微信服务号：车邦五福金牛 (WeChat:车邦五福金牛)">微信服务号：</abbr>车邦五福金牛<br />
            </address>
        <footer>
            <p>试问岭南应不好？却道，此心安处是吾乡。</p>
            <p>&copy; <%: DateTime.Now.Year %> - 广州车邦 | 大漠</p>
        </footer>
    </form>
</body>
</html>
