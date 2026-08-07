using System;
using AnalogClock;

int count = 1;
if (args.Length > 0 && int.TryParse(args[0], out var n) && n > 0)
{
    count = n;
}

for (int i = 0; i < count; i++)
{
    Console.WriteLine(LicenseKey.Generate());
}
