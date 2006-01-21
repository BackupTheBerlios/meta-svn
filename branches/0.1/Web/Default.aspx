<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Default.aspx.cs" Inherits="_Default" %>

<html>
	<head>
		<script type="text/javascript">
		<!--
		function insertTab(ta)
		{
			if(ta.createTextRange) 
			{ 
	   			document.selection.createRange().text="\t";
			}
			else if(ta.setSelectionRange)
			{ 
				 var start=ta.selectionStart;
				 var end=ta.selectionEnd;
				 ta.value=ta.value.substring(0, start)+"\t"+ta.value.substr(end);
				 ta.setSelectionRange(start+1,start+1);
			}		
		}
		function tab()
		{
			var ta=document.meta.input
			if(ta.createTextRange) 
			{ 
	   			insertTab(ta);
			}
			else if(ta.setSelectionRange)
			{ 
				 var t=ta;
				 insertTab(ta);
				 setTimeout(function(){t.focus();},0);
			}
		}
		function enter()
		{
			
			var text=document.meta.input.value;
			var lines=text.split("\n");
			var line=lines[lines.length-1];
			var i=0
			for(;i<line.length&&line.charCodeAt(i)==9;i++)
			{
			}
			setTimeout(
				function()
				{
					for(var y=0;y<i;y++)
					{
						insertTab(document.meta.input);
					}
				},
				0
			);
			
		}
		function submit()
		{
			document.meta.submit();
//			__doPostBack('__Page','');
//			__doPostBack('testButton','');
//			__doPost
			//document.execute.click();
		}
		-->

		</script>
	</head>
	<body >
		<form action="" id="meta"  name="frm" runat=server>
			<p id="idP">
				&nbsp;
				<asp:TextBox onkeydown="if(event.keyCode==9){tab();return false;} else if(event.keyCode==13){if(!event.ctrlKey){enter();} else {submit()}}" id="input" runat=server Height="279px" TextMode="MultiLine" Width="476px"></asp:TextBox>
			</p>
			<p>
				&nbsp;
				<asp:Button AccessKey="E"
				  ID="execute" runat="server" 
				  OnClick="execute_Click" Text="Run" />
				</p>
			<p>
				&nbsp;
				<asp:Label ID="output" runat="server" Height="220px" Width="476px"></asp:Label>&nbsp;</p>
		</form>
	</body>
</html>