<%--
  Import from Word document into ScrewTurn Wiki
  Version 3
  http://chuchuva.com/software/screwturn-wiki-import-from-word/
  License is open source: GNU and MIT.
--%>

<%@ Page Title="Import Microsoft Word Documents" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="True"
    CodeBehind="ImportWord.aspx.cs" Inherits="ScrewTurn.Wiki.ImportWord" %>

<asp:Content ID="Content1" ContentPlaceHolderID="CphMaster" runat="server">
    <h1 class="pagetitlesystem">
        <asp:Literal ID="lblImport" runat="server" Text="Импорт документов Microsoft Word" /></h1>
    <p>
        <asp:Literal ID="lblImportDescription" runat="server" Text="Импорт страницы из документа Microsoft Word" /></p>
    <br />
    <br />
    Type page title:<br />
    <asp:TextBox ID="sPageName" runat="server" Width="20em" />
    <asp:RequiredFieldValidator ID="RequiredFieldValidator1" Text="Type page title" runat="server"
        ControlToValidate="sPageName" ForeColor="Red" Display="Dynamic" />
    <asp:Label ID="lblPageNotOverwritable" Text="The page was not marked as overwritable. Update the page content to include &amp;lt;!--Overwritable--&amp;gt; in the body."
        runat="server" ForeColor="Red" Visible="false" />
    <asp:Label ID="lblAccessDenied" Text="Access was denied to the page. You may not have the edit page permission." runat="server" ForeColor="Red" Visible="false" />
    <br />
    <br />
    Choose Word document:<br />
    <asp:FileUpload ID="fileUpload" runat="server" />
    <asp:RequiredFieldValidator ID="RequiredFieldValidator2" Text="Type page title" runat="server"
        ControlToValidate="fileUpload" ForeColor="Red" Display="Dynamic" />
    <br />
    <br />
    <asp:Button ID="btnImport" Text="Import from this document" runat="server" OnClick="btnImport_Click" />
    <p style="margin: 3em 0 2em 0">
        Word documents must have .docx extension (Microsoft Office 2007 and higher).</p>
    <asp:Label ID="litError" runat="server" ForeColor="Red" Visible="false" />
</asp:Content>
