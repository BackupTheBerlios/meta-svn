<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Default.aspx.cs" Inherits="_Default" %>

<html>
	<head>
		<script type="text/javascript">
		<!--
		function allowInteger(evt,obj)
		{
			var AsciiValue = document.addEventListener ? evt.which : event.keyCode;
//			alert(AsciiValue);
			if(AsciiValue == 9 || AsciiValue=='\t')
			{
				if(document.selection)
				{
//					alert("hello");
					obj.selection = document.selection.createRange();
					obj.selection.text = "\t";
					event.returnValue = false;
					evt.stopPropagation();
				}
				else //if(document.addEventListener)
				{
					var ta=document.meta.input;                
					var bits=(new RegExp('([\x00-\xff]{'+ta.selectionStart+'})([\x00-\xff]{'+(ta.selectionEnd - ta.selectionStart)+'})([\x00-\xff]*)')).exec(ta.value);
					ta.value=bits[1]+"\t"+bits[3]; 
					evt.preventDefault();
				}
		        
			}
		}
		function whatever(evt,obj)
		{
//			alert("whatever");
			if(document.selection)
			{
//				alert("in");
				allowInteger(evt,obj);
			}
		}
		function init()
		{	if(window.event)
			{
//				document.meta.input.onkeypress = allowInteger;
//				document.getElementById("input").onkeypress = allowInteger;
//				alert("in init");
			}
			else if(document.addEventListener)
			{
				document.getElementById("idP").addEventListener("keypress",allowInteger, true);
			}
//			else ;
		}
		-->
		</script>
	</head>
	<body onload="init();">
		<form action="" id="meta" onkeydown="whatever(event,this)" name="frm" runat=server>
			<p id="idP">
				<asp:TextBox id="input" runat=server>
				</asp:TextBox>
			</p>
		</form>
	</body>
</html>