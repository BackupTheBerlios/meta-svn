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
//					evt.stopPropagation();
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
		var buttonClicked=false;
		function InsertTab()
		{
			var obj=document.meta.input;
			var ta=document.meta.input; 
			ta.focus();
			if(document.selection)
			{

//					alert("hello");
				obj.selection = document.selection.createRange();
				obj.selection.text = "\t";
//				event.returnValue = false;
//					evt.stopPropagation();
			}
			else //if(document.addEventListener)
			{
				var ta=document.meta.input;                
				var bits=(new RegExp('([\x00-\xff]{'+ta.selectionStart+'})([\x00-\xff]{'+(ta.selectionEnd - ta.selectionStart)+'})([\x00-\xff]*)')).exec(ta.value);
				ta.value=bits[1]+"\t"+bits[3]; 
//				evt.preventDefault();
			}
			var ta=document.meta.input; 
			ta.focus();
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
		function clicked()
		{
//			alert("Clicked");
			buttonClicked=true;
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
		function tab(ta)
		{
			var ta=document.meta.input
			if(ta.createTextRange) 
			{ 
	   			document.selection.createRange().text="\t";
			}
			else if(ta.setSelectionRange)
			{ 
				 var t=ta;
				 var start=t.selectionStart;
				 var end=t.selectionEnd;
				 t.value=t.value.substring(0, start)+"\t"+t.value.substr(end);
//				 t.setSelectionRange(start+1,start+1);
				 var x=5;
				 setTimeout(function(){t.focus();},0);
			}
		}
//		 function tab(ta,evt){
//		   if(ta.createTextRange) { // assume IE's model
//			 (function(){
//			   var rng=null;
//			   ta.onblur=function(){if(rng){rng=null;this.focus();}}
//			   ta.onkeydown=function(){
//				 if(event.keyCode==9 && event.ctrlKey)
//				   (rng=document.selection.createRange()).text="\t";
//			   }
//			   ta.onkeydown();
//			 })();
//		   } else if(ta.setSelectionRange) { // assume Mozilla's model
//			 ta.onkeydown=function(evt){
//			   if(evt.keyCode==9 && evt.ctrlKey) {
//				 var t=this, start=t.selectionStart, end=t.selectionEnd;
//				 t.value=t.value.substring(0, start)+"\t"+t.value.substr(end);
//				 t.setSelectionRange(start+1,start+1);
//				 setTimeout(function(){t.focus();},1);
//			   }
//			 }
//		   } else {
//			 ta.onkeydown=null;
//		   }
//		} 
		-->

		</script>
	</head>
	<body >
		<form action="" id="meta"  name="frm" runat=server>
			<p id="idP">
				<asp:TextBox onkeydown="if(event.keyCode==9){return false;}" id="input" runat=server Height="108px" TextMode="MultiLine" Width="326px"></asp:TextBox>
			</p>
			<p>
				&nbsp;
				<asp:Button AccessKey="E"
				  ID="execute" runat="server" 
				  OnClick="execute_Click" Text="Execute" /></p>
			<p>
				<asp:Label ID="output" runat="server" Height="146px" Width="333px"></asp:Label>&nbsp;</p>
		</form>
	</body>
</html>