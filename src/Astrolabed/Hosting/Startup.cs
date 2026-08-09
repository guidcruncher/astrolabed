using Microsoft.Extensions.Hosting;

namespace Astrolabed.Hosting;

public static class Startup
{
    public static async Task LoadAsync(IHost host)
    {
        Console.WriteLine("""
   _____            __                .__        ___.                  
  /  _  \   _______/  |________  ____ |  | _____ \_ |__   ____   ______
 /  /_\  \ /  ___/\   __\_  __ \/  _ \|  | \__  \ | __ \_/ __ \ /  ___/
/    |    \\___ \  |  |  |  | \(  <_> )  |__/ __ \| \_\ \  ___/ \___ \ 
\____|__  /____  > |__|  |__|   \____/|____(____  /___  /\___  >____  >
        \/     \/                               \/    \/     \/     \/ 
""");

        await Task.CompletedTask;
    }
}
