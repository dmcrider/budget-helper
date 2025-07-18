using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using System.Text.RegularExpressions;

public class BudgetCalculator
{
    private readonly CalendarService _service;
    private readonly string _defaultCalendarId;
    private readonly string _billsCalendarId;
    private readonly Regex _billPattern = new Regex(@"^(.+?)\s*(?:\(\$|\$)(\d{1,3}(?:,\d{3})*(?:\.\d{2})?|\d+(?:\.\d{2})?)\)?$");
    private readonly Regex _paydayPattern = new Regex(@"^Payday \(\$(\d+(?:\.\d{2})?)\)$");

    public BudgetCalculator(CalendarService service)
    {
        _service = service;
        _defaultCalendarId = Environment.GetEnvironmentVariable("DEFAULT_CALENDAR_ID") ?? 
            throw new InvalidOperationException("DEFAULT_CALENDAR_ID environment variable is required");
        _billsCalendarId = Environment.GetEnvironmentVariable("BILLS_CALENDAR_ID") ?? 
            throw new InvalidOperationException("BILLS_CALENDAR_ID environment variable is required");
    }

    public async Task CalculateBalances(decimal currentBalance, int paydayCycles)
    {
        var today = DateTime.Today;
        Console.WriteLine($"Budget calculation starting from {today:yyyy-MM-dd}");
        Console.WriteLine($"Current balance: ${currentBalance:F2}");
        Console.WriteLine($"Calculating for {paydayCycles} payday cycles");
        Console.WriteLine();

        // Get payday events to determine the cycle dates
        var paydayEvents = await GetPaydayEvents(today, paydayCycles);
        
        if (paydayEvents.Count == 0)
        {
            Console.WriteLine("No payday events found in the default calendar.");
            return;
        }

        // Get all bill events
        var billEvents = await GetBillEvents(today, paydayEvents.Last().Date);

        // Calculate balance for each payday cycle
        var runningBalance = currentBalance;
        
        for (int cycle = 0; cycle < paydayCycles; cycle++)
        {
            if (cycle >= paydayEvents.Count)
            {
                Console.WriteLine($"Warning: Not enough payday events found for {paydayCycles} cycles.");
                break;
            }

            var payday = paydayEvents[cycle];
            var nextPayday = cycle + 1 < paydayEvents.Count ? paydayEvents[cycle + 1] : null;
            
            // Find bills due in this cycle (from today or last payday to this payday)
            var cycleStart = cycle == 0 ? today : paydayEvents[cycle - 1].Date.AddDays(1);
            var cycleEnd = payday.Date; // Include payday itself
            
            var billsInCycle = billEvents
                .Where(b => b.Date >= cycleStart && b.Date <= cycleEnd)
                .OrderBy(b => b.Date)
                .ToList();

            Console.WriteLine($"=== Payday Cycle {cycle + 1} ===");
            Console.WriteLine($"Period: {cycleStart:yyyy-MM-dd} to {cycleEnd:yyyy-MM-dd}");
            Console.WriteLine($"Payday: {payday.Date:yyyy-MM-dd} (+${payday.Amount:F2})");
            Console.WriteLine();

            // Print column headers
            Console.WriteLine($"{"Date",-12} {"Description",-30} {"Amount",12} {"Balance",12}");
            Console.WriteLine($"{new string('-', 12)} {new string('-', 30)} {new string('-', 12)} {new string('-', 12)}");

            // Process bills in chronological order (excluding payday itself)
            foreach (var bill in billsInCycle.Where(b => b.Date != payday.Date))
            {
                runningBalance -= bill.Amount;
                Console.WriteLine($"{bill.Date,-12:yyyy-MM-dd} {bill.Name,-30} -${bill.Amount,11:F2} ${runningBalance,11:F2}");
            }

            // Show balance on day of payday (before payday income)
            Console.WriteLine($"Balance on {payday.Date:yyyy-MM-dd} (before payday): ${runningBalance:F2}");
            
            // Add payday income
            runningBalance += payday.Amount;
            Console.WriteLine($"After payday (+${payday.Amount:F2}): ${runningBalance:F2}");
            Console.WriteLine();
        }
    }

    public async Task CalculateBalancesSummary(decimal currentBalance, int paydayCycles)
    {
        var today = DateTime.Today;

        // Get payday events to determine the cycle dates
        var paydayEvents = await GetPaydayEvents(today, paydayCycles);
        
        if (paydayEvents.Count == 0)
        {
            Console.WriteLine("No payday events found in the default calendar.");
            return;
        }

        // Get all bill events
        var billEvents = await GetBillEvents(today, paydayEvents.Last().Date);

        // Calculate balance for each payday cycle
        var runningBalance = currentBalance;
        
        for (int cycle = 0; cycle < paydayCycles; cycle++)
        {
            if (cycle >= paydayEvents.Count)
            {
                Console.WriteLine($"Warning: Not enough payday events found for {paydayCycles} cycles.");
                break;
            }

            var payday = paydayEvents[cycle];
            
            // Find bills due in this cycle (from today or last payday to this payday)
            var cycleStart = cycle == 0 ? today : paydayEvents[cycle - 1].Date.AddDays(1);
            var cycleEnd = payday.Date; // Include payday itself
            
            var billsInCycle = billEvents
                .Where(b => b.Date >= cycleStart && b.Date <= cycleEnd)
                .OrderBy(b => b.Date)
                .ToList();

            // Process bills in chronological order (excluding payday itself)
            foreach (var bill in billsInCycle.Where(b => b.Date != payday.Date))
            {
                runningBalance -= bill.Amount;
            }

            // Show balance on day of payday (before payday income)
            Console.WriteLine($"{payday.Date:yyyy-MM-dd}: ${runningBalance:F2}");
            
            // Add payday income for next cycle calculation
            runningBalance += payday.Amount;
        }
    }

    private async Task<List<PaydayEvent>> GetPaydayEvents(DateTime startDate, int maxEvents)
    {
        var events = new List<PaydayEvent>();
        var request = _service.Events.List(_defaultCalendarId);
        request.TimeMinDateTimeOffset = startDate;
        // Assuming bi-weekly pay periods (26 per year), each period is ~14 days
        // Add extra buffer to ensure we capture all needed events
        request.TimeMaxDateTimeOffset = startDate.AddDays(maxEvents * 14 + 30);
        request.ShowDeleted = false;
        request.SingleEvents = true;
        request.MaxResults = 2500;
        request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

        try
        {
            var response = await request.ExecuteAsync();
        
            foreach (var evt in response.Items)
            {
                if (evt.Start?.Date == null) continue; // Skip non-all-day events
                
                var match = _paydayPattern.Match(evt.Summary ?? "");
                if (match.Success)
                {
                    if (decimal.TryParse(match.Groups[1].Value, out decimal amount))
                    {
                        events.Add(new PaydayEvent
                        {
                            Date = DateTime.Parse(evt.Start.Date),
                            Amount = amount
                        });
                    }
                }
            }
        } catch (Exception ex)
        {
            Console.WriteLine($"Error fetching payday events: {ex.Message}");
        }

        return events.OrderBy(e => e.Date).Take(maxEvents).ToList();
    }

    private async Task<List<BillEvent>> GetBillEvents(DateTime startDate, DateTime endDate)
    {
        var events = new List<BillEvent>();
        
        var request = _service.Events.List(_billsCalendarId);
        request.TimeMinDateTimeOffset = startDate;
        request.TimeMaxDateTimeOffset = endDate.AddDays(1); // Include the end date
        request.ShowDeleted = false;
        request.SingleEvents = true;
        request.MaxResults = 2500;
        request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

        var response = await request.ExecuteAsync();
        
        foreach (var evt in response.Items)
        {
            if (evt.Start?.Date == null) continue; // Skip non-all-day events
            
            var match = _billPattern.Match(evt.Summary ?? "");
            if (match.Success)
            {
                if (decimal.TryParse(match.Groups[2].Value.Replace(",", ""), out decimal amount))
                {
                    events.Add(new BillEvent
                    {
                        Date = DateTime.Parse(evt.Start.Date),
                        Name = match.Groups[1].Value.Trim(),
                        Amount = amount
                    });
                }
            }
        }

        return events.OrderBy(e => e.Date).ToList();
    }
}

public class PaydayEvent
{
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
}

public class BillEvent
{
    public DateTime Date { get; set; }
    public string Name { get; set; } = "";
    public decimal Amount { get; set; }
}
