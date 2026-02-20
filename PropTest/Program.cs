using System;
using System.Reflection;
using Windows.Media.Audio;

namespace Inspector
{
    class Program
    {
        static void Main()
        {
            var type = typeof(AudioPlaybackConnection);
            var ev = type.GetEvent("StateChanged");
            Console.WriteLine("EventHandler Type: " + ev.EventHandlerType.Name);
            var invoke = ev.EventHandlerType.GetMethod("Invoke");
            if (invoke != null)
            {
                var parameters = invoke.GetParameters();
                Console.WriteLine("Arg 2 Type: " + parameters[1].ParameterType.Name);
                
                var argType = parameters[1].ParameterType;
                foreach (var prop in argType.GetProperties())
                {
                    Console.WriteLine(" - " + prop.Name);
                }
            }
        }
    }
}
