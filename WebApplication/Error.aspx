<%@ Page Language="C#" MasterPageFile="~/MasterPageSA.master" AutoEventWireup="true" Inherits="ScrewTurn.Wiki.Error" Title="Untitled Page" Culture="auto" meta:resourcekey="PageResource1" UICulture="auto" Codebehind="Error.aspx.cs" %>

<asp:Content ID="ctnError" ContentPlaceHolderID="CphMasterSA" Runat="Server">

	<h1 class="pagetitlesystem"><asp:Literal ID="lblError" runat="server" Text="System Error" meta:resourcekey="lblErrorResource1" EnableViewState="False" /></h1>
	<table width="98%">
		<tr>
			<td>
				<img src="Images/Error.png" alt="Error" />
			</td>
			<td>
				<p>
					<asp:Literal ID="lblDescription" runat="server" 
						Text="We're sorry, an error occurred while processing your request. The error information has been registered and it will be investigated.<br />Please restart from the <a href=&quot;Default.aspx&quot;>Main Page</a>." 
						meta:resourcekey="lblDescriptionResource1" EnableViewState="False" />
				</p>
			</td>
		</tr>
	</table>

</asp:Content>
