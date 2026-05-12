using System.ComponentModel;
public static class ExtensionClass
{
    public static void InvokeIfRequired(this ISynchronizeInvoke obj, MethodInvoker action)
    {
        if (obj.InvokeRequired)
        {
            obj.Invoke(action, null);
        }
        else
        {
            action();
        }
    }
}
