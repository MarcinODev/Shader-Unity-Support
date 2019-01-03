using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.InteropServices;
using EnvDTE80;
using EnvDTE;

internal class DebugLog
{
    private static Lazy<Action<string>> _Logger = new Lazy<Action<string>>( () => GetWindow().OutputString );

    private static Action<string> Logger
    {
        get { return _Logger.Value; }
    }

    public static void SetLogger( Action<string> logger )
    {
        _Logger = new Lazy<Action<string>>( () => logger );
    }

    public static void Write( string format, params object[] args)
    {
        var message = string.Format( format, args );
        Write( message );
    }

    public static void Write( string message , bool isError = false)
    {
        if(isError)
        {
            Logger( "!+!+!+!+!+!+!+!+!+!+!+!+!+!+!+!+!+!+!+!+!+!+!+!+!+!+!+!+!+!+!+!+!+!+!+!+-\n[" + message + "]\n!+!+!+!+!+!+!+!+!+!+!+!+!+!+!+!+!+!+!+!+!+!+!+!+!+!+!+!+!+!+!+!+!+!+-" + Environment.NewLine );
        }
        else
        {
            Logger( "-------------------------------------------------------------------------\n[" + message + "]\n---------------------------------------------------------------------" + Environment.NewLine );
        }
    }

    public static void Write(Exception e)
    {
        Write(e.ToString(), true);
    }

    private static OutputWindowPane GetWindow()
    {
        var dte = (DTE2) Marshal.GetActiveObject( "VisualStudio.DTE" );
        return dte.ToolWindows.OutputWindow.ActivePane;
    }
}