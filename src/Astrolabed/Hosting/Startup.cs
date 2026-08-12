using System;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Astrolabed.Hosting;

public static class Startup
{
    public static string[] Arguments { get; private set; } = Array.Empty<string>();

    public static IHost BuildHost(string[] args)
    {
        Console.WriteLine("""

   _____            __                .__        ___.              .___
  /  _  \   _______/  |________  ____ |  | _____ \_ |__   ____   __| _/
 /  /_\  \ /  ___/\   __\_  __ \/  _ \|  | \__  \ | __ \_/ __ \ / __ | 
/    |    \\___ \  |  |  |  | \(  <_> )  |__/ __ \| \_\ \  ___// /_/ | 
\____|__  /____  > |__|  |__|   \____/|____(____  /___  /\___  >____ | 
        \/     \/                               \/    \/     \/     \/ 
Your navigator for the Internet.
https://guidcruncher.github.io/astrolabed/

""");

        Arguments = args;
        var cmd = new ConfigurationBuilder()
              .AddCommandLine(args, new Dictionary<string, string>
              {
                  ["--config"] = "ConfigPath",
                  ["--env"] = "DOTNET_ENVIRONMENT",
                  ["--listen"] = "ListenOverride",
                  ["--resolver"] = "ResolverOverride",
                  ["--log-level"] = "Logging:Level"
              })
              .Build();

        return HostBuilderFactory.Build(args, cmd);
    }
}
