<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Index.aspx.cs" Inherits="PDAWS.Index" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head id="head1" runat="server">    
    <title>车邦扫描枪服务</title>
    <link href="Themes/Style/CssStyles.css" rel="stylesheet" />
    <link href="Themes/Style/Styles.css" rel="stylesheet" />
</head>
<body>
    <form id="form1" runat="server">
        <h3>ERP自定义功能</h3>
        <div class="FrameContentSpan">
            <table class="FrameTable">
                <tr>
                    <td class="EditPage_Label">销售订单号：</td>
                    <td class="EditPage_Field">
                        <asp:TextBox ID="txtBillNo" CssClass="textBox" Width="200" Rows="1"  TabIndex="1" runat="server"></asp:TextBox>
                        <asp:RadioButton ID="rbtSingle" GroupName="SingleShipment" Text="整单" Checked="false" TabIndex="2" runat="server"/>
                        <asp:RadioButton ID="rbtNotSingle" GroupName="SingleShipment" Text="非整单" Checked="true" TabIndex="3" runat="server"/>
                        &nbsp
                        <input id="btnOK" type="button" class="button" onclick="OK_Click()" value="确定"/>
                    </td>
                </tr>
            </table>
        </div>
        <hr />
        <address>
            广州市白云区同沙路283号天健创意园8栋302<br />
            4006-998-168<br />
            020-62870908<br />
            邮政编码：510080<br />
            <abbr title="微信服务号：车邦五福金牛 (WeChat:车邦五福金牛)">微信服务号：</abbr>车邦五福金牛<br />
        </address>
        <p>试问岭南应不好？却道，此心安处是吾乡。</p>
        <p>&copy; <%: DateTime.Now.Year %> - 广州车邦 | 大漠</p>
    </form>
    <script type="text/javascript" >
        function OK_Click()
        {
            var billno = "abc";
            var ajax = AjaxCB.SSM("abc", true);
            alert(ajax);
        }
    </script>
</body>
</html>
