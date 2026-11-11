if (args.Length == 0 || (args.Length == 1 && args[0] is "--help" or "-h"))
{
    Console.WriteLine("""
        RazorFolio (rf)

        Project scaffold only. Site commands are not implemented yet.

        Planned commands:
          rf new <directory>
          rf dev --site <directory> [--host 127.0.0.1] [--port 8080]
          rf build --site <directory> [--output public]
          rf preview --site <directory> [--directory public] [--port 8080]
        """);

    return 0;
}

Console.Error.WriteLine("Site commands are not implemented yet. Run rf --help for the planned interface.");
return 2;
