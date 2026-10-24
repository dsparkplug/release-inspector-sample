// A program small enough that its build, signature and inspection are the whole point.
string version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown";
Console.WriteLine($"Hello from HelloRelease {version}.");
