using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.Text.RegularExpressions;
using DotNetEnv;

class Program
{
    // If modifying these scopes, delete your previously saved credentials
    // at ~/.credentials/calendar-dotnet-quickstart.json
    static string[] Scopes = { CalendarService.Scope.CalendarReadonly };
    static string ApplicationName = "Budget Helper";

    static async Task Main(string[] args)
    {
        // Load environment variables from .env file
        Env.Load();
        
        bool summaryMode = false;
        var filteredArgs = args.Where(arg => arg != "--summary").ToArray();
        
        if (args.Contains("--summary"))
        {
            summaryMode = true;
        }

        decimal currentBalance = 500m; // Default value
        int paydayCycles = 3; // Default value

        if (filteredArgs.Length == 0)
        {
            Console.WriteLine("Using default values: current balance = $500.00, payday cycles = 3");
        }
        else if (filteredArgs.Length == 2)
        {
            if (!decimal.TryParse(filteredArgs[0], out currentBalance))
            {
                Console.WriteLine("Error: Invalid current balance amount.");
                return;
            }

            if (!int.TryParse(filteredArgs[1], out paydayCycles) || paydayCycles <= 0)
            {
                Console.WriteLine("Error: Invalid number of payday cycles.");
                return;
            }
        }
        else
        {
            Console.WriteLine("Usage: budget-helper [--summary] [<current_balance> <payday_cycles>]");
            Console.WriteLine("Example: budget-helper (uses defaults: $500.00, 3 cycles)");
            Console.WriteLine("Example: budget-helper 1500.00 2");
            Console.WriteLine("Example: budget-helper --summary 1500.00 2");
            return;
        }

        try
        {
            var service = GetCalendarService();
            var calculator = new BudgetCalculator(service);
            
            if (summaryMode)
            {
                await calculator.CalculateBalancesSummary(currentBalance, paydayCycles);
            }
            else
            {
                await calculator.CalculateBalances(currentBalance, paydayCycles);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static CalendarService GetCalendarService()
    {
        GoogleCredential credential;

        using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
        {
            credential = GoogleCredential.FromStream(stream)
                .CreateScoped(Scopes);
        }

        var service = new CalendarService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = ApplicationName,
        });

        return service;
    }
}
