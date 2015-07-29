<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="AWSBrowser._Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head id="Head1" runat="server">
    <title>My AWS Enabled Application - AWSBrowser</title>
    <link rel="stylesheet" href="styles/styles.css" type="text/css" media="screen" charset="utf-8"/>
</head>
<html>
<body>
<div id="content" class="container">
    <div class="section grid grid5 s3">
        <h2>Amazon S3 Buckets:</h2>
        <ul>
            <asp:Label ID="s3Placeholder" runat="server"></asp:Label>
        </ul>
    </div>
    <div class="section grid grid5 sdb">
        <h2>Amazon SimpleDB Domains:</h2>
        <ul>
            <asp:Label ID="sdbPlaceholder" runat="server"></asp:Label>
                
        </ul>
    </div>
    <div class="section grid grid5 sdb">
        <h2>Amazon RDS Instances:</h2>
        <ul>
            <asp:Label ID="rdsPlaceholder" runat="server"></asp:Label>
        </ul>
    </div>
    <div class="section grid grid5 gridlast ec2">
        <h2>Amazon EC2 Instances:</h2>
        <ul>
            <asp:Label ID="ec2Placeholder" runat="server"></asp:Label>

        </ul>
    </div>
</div>
</body>
</html>
