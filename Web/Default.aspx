<html>
<head>
<script type="text/javascript">
<!--
function allowInteger(evt)
{
    var AsciiValue = document.addEventListener ? evt.which : event.keyCode;
    if(AsciiValue == '\t')
    {
        if(document.addEventListener)
        {
            var ta=document.meta.input;                
            var bits=(new RegExp('([\x00-\xff]{'+ta.selectionStart+'})([\x00-\xff]{'+(ta.selectionEnd - ta.selectionStart)+'})([\x00-\xff]*)')).exec(ta.value);
            ta.value=bits[1]+"\t"+bits[3]; 
            evt.preventDefault();
        }
        else if(window.event)
        {
            obj.selection = document.selection.createRange();
            obj.selection.text = "\t";
            event.returnValue = false;
        };
        
    }
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
        <p id="idP">
        <asp:TextBox id="input" runat=server>
        </asp:TextBox></p>

        </form>
        </body>
        </html>