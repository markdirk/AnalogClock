using System;
using AnalogClock;

int count = 1;
if (args.Length > 0 && int.TryParse(args[0], out var n) && n > 0)
{
    count = n;
}

Console.WriteLine($"Generiere {count} Lizenzschlüssel...");
Console.WriteLine();

for (int i = 0; i < count; i++)
{
    Console.WriteLine(LicenseKey.Generate());
}

Console.WriteLine();
Console.WriteLine("Verwendung: KeyGenerator.exe [Anzahl] > keys.txt");

if (args.Length == 0 && !Console.IsInputRedirected)
{
    Console.WriteLine("Beliebige Taste zum Beenden...");
    Console.ReadKey(true);
}
