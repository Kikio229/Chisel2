using DynEngine;

namespace ConsoleApp1;

internal class Program
{
    static void Main(string[] args)
    {
        // youd have a way better and smarter way to deconstruct this lol
        string loadFrom = args != null ? Path.GetFullPath(args[0]) : throw new Exception();

        var context = new GameLoadContext(loadFrom);
        var assembly = context.LoadFromAssemblyPath(loadFrom);

        var gameType = assembly.GetTypes().FirstOrDefault(t => typeof(IDynamicGame).IsAssignableFrom(t) && !t.IsInterface);

        var game = (IDynamicGame)Activator.CreateInstance(gameType!)!;
        game.OnStartup();

        Thread.Sleep(1000);

        Console.ReadKey();
    }
}
