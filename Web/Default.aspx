<%@ Page Language="C#" AutoEventWireup="true"  CodeFile="Default.aspx.cs" Inherits="_Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>Untitled Page</title>
    <script language="javascript">
    function HandleKeyDown(e,obj)
    {
        
     var key, target;

     /* Event-Objekt in Erfahrung bringen */
     if (!e) e = window.event;

     /* Tastencode in Erfahrung bringen */
     if (e.keyCode) key = e.keyCode;
     else if (e.which) key = e.which;
     else return;
     
     
    var input=document.meta.input;
    if(key==' ')
    {
        if(document.selection)
        {
//            alert("hello");
            obj.selection = document.selection.createRange();
            obj.selection.text = "\t";
            event.returnValue = false;
        }
        else
        {
//            alert(input.selectionStart);
//            alert(input.selectionEnd);
            var ta=input;
            var bits=(new RegExp('([\x00-\xff]{'+ta.selectionStart+'})([\x00-\xff]{'+(ta.selectionEnd - ta.selectionStart)+'})([\x00-\xff]*)')).exec(ta.value);
            input.value=bits[1]+"\t"+bits[3]; 
               document.meta.input.focus();

            e.returnValue=false;
        }
    }

     /* Element in Erfahrung bringen, bei dem der Event passierte */
    // if (e.target) target = e.target;
    // else if (e.srcElement) target = e.srcElement;
    // else return;

     /* Prüfung, ob das Target-Element ein input- oder textarea-Element ist */
    // if (target.type && (target.type == "textarea" || target.type == "text")) {
     // Alternativ:
     // if (target.nodeName && (target.nodeName.toLowerCase() == "input" || arget.nodeName.toLowerCase() == "textarea")) {
      // Target-Element ist input oder textarea.
      // Breche Event-Verarbeitung ab. (optional)
      if (e.stopPropagation) e.stopPropagation();
      else if (typeof(e.cancelBubble) == "boolean") e.cancelBubble = true;
      return;
 }

 /* Verarbeitung des Tastendrucks */
 // ...
//       alert(e.which);
//       if (e.which == 9)// && event.srcElement == obj) {
//       {
//          alert("yippiieh");
//          obj.selection = document.selection.createRange();
//          obj.selection.text = "\t";
////          obj.selection.text = String.fromCharCode(tabKeyCode);
//          event.returnValue = false;
//       }
//    }
    </script>
</head>
<body >
    <form id="meta" runat="server">
    <div>
    <asp:TextBox onkeydown="HandleKeyDown(event,this);" onkeyup="document.meta.input.focus();" ID="input" runat=server TextMode=MultiLine Columns="40" Rows="10" Width="386px"></asp:TextBox>&nbsp;
        <br />
        <br />
        <asp:Button ID="execute" runat="server" Height="41px" OnClick="execute_Click" Text="Execute"
            Width="97px" TabIndex="-1" />
        <br />
        <br />
        <asp:Label ID="output" runat="server" Height="170px" Width="393px"></asp:Label></div>
    </form>
</body>
</html>
