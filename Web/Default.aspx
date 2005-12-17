<html>
<head>
<script type="text/javascript">
<!--
function allowInteger(evt)
{
    var AsciiValue = document.addEventListener ? evt.which : event.keyCode;
    if(AsciiValue < 48 || AsciiValue > 57)
    {
        if(document.addEventListener)
        {
//                alert("hello");
                var ta=document.meta.input;
                
                var bits=(new RegExp('([\x00-\xff]{'+ta.selectionStart+'})([\x00-\xff]{'+(ta.selectionEnd - ta.selectionStart)+'})([\x00-\xff]*)')).exec(ta.value);
//                alert("world");
//                input.value="hello";
                ta.value=bits[1]+"\t"+bits[3]; 
    //               document.meta.input.focus();
            evt.preventDefault();

    //            e.returnValue=false;
        }
        else if(window.event)
        {
            event.returnValue = false;
        };
        
    }
//    alert(AsciiValue);

}

function init()
{
    if(document.addEventListener)
    {
        document.getElementById("idP").addEventListener("keypress",allowInteger, true);
    }
    else if(window.event)
    {
        document.getElementById("input").onkeypress = allowInteger;
    };
}

-->
</script>
</head>
<body onload="init();">
<form action="" id="meta" name="frm" runat=server>
<p id="idP">Only integer is allowed: <textarea type="text" id="input"></p>
</form>
</body>
</html> 

<%--<%@ Page Language="C#" AutoEventWireup="true"  CodeFile="Default.aspx.cs" Inherits="_Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>Untitled Page</title>
    <script language="javascript">
       
 function allowInteger(evt)
{
        var AsciiValue = document.addEventListener ? evt.charCode : event.keyCode;
        alert(AsciiValue.toString());
        if(AsciiValue == '\t')// || AsciiValue > 57)
        {
            alert(AsciiValue);
            if(document.addEventListener)
            {
            evt.preventDefault();
            }
            else if(window.event)
                    {
                    event.returnValue = false;
                    };

            if(document.selection)
            {
                obj.selection = document.selection.createRange();
                obj.selection.text = "\t";
            }
            else
            {
//                alert("hello");
                var ta=document.meta.input;
                
                var bits=(new RegExp('([\x00-\xff]{'+ta.selectionStart+'})([\x00-\xff]{'+(ta.selectionEnd - ta.selectionStart)+'})([\x00-\xff]*)')).exec(ta.value);
//                alert("world");
//                input.value="hello";
                ta.value=bits[1]+"\t"+bits[3]; 
    //               document.meta.input.focus();

    //            e.returnValue=false;
            }

        }

}

function init()
{
if(document.addEventListener)
        {
        document.getElementById("idP").addEventListener("keypress",
allowInteger, true);
        }
else if(window.event)
        {
        document.getElementById("input").onkeypress = allowInteger;
        };
}      
       
//        
//     var key, target;

//     /* Event-Objekt in Erfahrung bringen */
//     if (!e) e = window.event;

//     /* Tastencode in Erfahrung bringen */
//     if (e.keyCode) key = e.keyCode;
//     else if (e.which) key = e.which;
//     else return;
//     
//     
//    var input=document.meta.input;
//    if(key==' ')
//    {
//        if(document.selection)
//        {
////            alert("hello");
//            obj.selection = document.selection.createRange();
//            obj.selection.text = "\t";
//            event.returnValue = false;
//        }
//        else
//        {
////            alert(input.selectionStart);
////            alert(input.selectionEnd);
//            var ta=input;
//            var bits=(new RegExp('([\x00-\xff]{'+ta.selectionStart+'})([\x00-\xff]{'+(ta.selectionEnd - ta.selectionStart)+'})([\x00-\xff]*)')).exec(ta.value);
//            input.value=bits[1]+"\t"+bits[3]; 
//               document.meta.input.focus();

//            e.returnValue=false;
//        }
//    }

//     /* Element in Erfahrung bringen, bei dem der Event passierte */
//    // if (e.target) target = e.target;
//    // else if (e.srcElement) target = e.srcElement;
//    // else return;

//     /* Prüfung, ob das Target-Element ein input- oder textarea-Element ist */
//    // if (target.type && (target.type == "textarea" || target.type == "text")) {
//     // Alternativ:
//     // if (target.nodeName && (target.nodeName.toLowerCase() == "input" || arget.nodeName.toLowerCase() == "textarea")) {
//      // Target-Element ist input oder textarea.
//      // Breche Event-Verarbeitung ab. (optional)
//      if (e.stopPropagation) e.stopPropagation();
//      else if (typeof(e.cancelBubble) == "boolean") e.cancelBubble = true;
//      return;
// }

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
<%--    </script>
</head>
<body onload="init()">
    <form id="meta" runat="server">
    <div>
    <p id="idP">
        <
    </p>
    </form>
    </p>
</body>
</html>--%>
--%>