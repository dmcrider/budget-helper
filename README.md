# Budget Helper

A C# .NET 8 console application that integrates with Google Calendar to track bills and calculate account balances based on payday cycles.

## Features

- **Google Calendar Integration**: Connects to your Google Calendar to read events
- **Bills Tracking**: Parses bill events from a "Bills" calendar in format "XXXX ($YYY)"
- **Payday Tracking**: Parses payday events from your default calendar starting with "Payday"
- **Balance Calculation**: Calculates your account balance on the day before each payday
- **Multi-cycle Planning**: Supports calculating for multiple payday cycles ahead

## Setup

### 1. Google Calendar API Setup

1. Go to the [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select an existing one
3. Enable the Google Calendar API:
   - Navigate to "APIs & Services" → "Library"
   - Search for "Google Calendar API" and enable it
4. Create a Service Account:
   - Go to "APIs & Services" → "Credentials"
   - Click "Create Credentials" → "Service Account"
   - Enter a service account name and description
   - Click "Create and Continue"
5. Assign a role to the service account:
   - In the "Grant this service account access to project" section
   - Add the "Editor" role or a custom role with Calendar access
   - Click "Continue" and then "Done"
6. Create and download the service account key:
   - Click on the created service account from the credentials list
   - Go to the "Keys" tab
   - Click "Add Key" → "Create new key"
   - Select "JSON" format and click "Create"
   - Save the downloaded JSON file as `credentials.json` in the project root
   - You can reference `credentials.json.sample` for the expected file format
7. Share your calendars with the service account:
   - In Google Calendar, go to your calendar settings
   - Add the service account email (found in the JSON file) as a viewer to both your default calendar and Bills calendar

### 2. Environment Configuration

1. Copy the sample environment file:
   ```bash
   cp .env.sample .env
   ```

2. Edit the `.env` file and set your calendar IDs:
   ```
   DEFAULT_CALENDAR_ID=your-email@gmail.com
   BILLS_CALENDAR_ID=your-bills-calendar-id@group.calendar.google.com
   ```

   To find your calendar IDs:
   - **Default Calendar**: Usually your Gmail address
   - **Bills Calendar**: In Google Calendar, go to Settings → Calendar settings → Integrate calendar → Calendar ID

### 3. Calendar Setup

- **Bills Calendar**: Create a calendar named "Bills" and add all-day events in the format:
  - `Spotify ($17)`
  - `Water Bill ($112.68)`
  - `Mortgage ($1250.00)`

- **Default Calendar**: Add payday events to your default calendar in the format:
  - `Payday ($2500)`
  - `Payday ($2500.00)`

### 4. Build and Run

```bash
dotnet build
dotnet run <current_balance> <payday_cycles>
```

## Usage

```bash
# Calculate balance for 2 payday cycles starting with $1500 current balance
dotnet run 1500.00 2

# Calculate balance for 4 payday cycles starting with $250 current balance
dotnet run 250 4
```

## Example Output

```
Budget calculation starting from 2025-07-17
Current balance: $1500.00
Calculating for 2 payday cycles

=== Payday Cycle 1 ===
Period: 2025-07-17 to 2025-07-30
Payday: 2025-07-31 (+$2500.00)

2025-07-20: Spotify ($17.00) (Balance: $1483.00)
2025-07-25: SRP ($112.68) (Balance: $1370.32)
2025-07-28: Mortgage ($1250.00) (Balance: $120.32)
Balance on 2025-07-30 (day before payday): $120.32
After payday (+$2500.00): $2620.32

=== Payday Cycle 2 ===
Period: 2025-08-01 to 2025-08-14
Payday: 2025-08-15 (+$2500.00)

2025-08-05: Car Payment ($350.00) (Balance: $2270.32)
2025-08-10: Insurance ($125.00) (Balance: $2145.32)
Balance on 2025-08-14 (day before payday): $2145.32
After payday (+$2500.00): $4645.32

Final projected balance: $4645.32
```

## Requirements

- .NET 8.0 or later
- Google Calendar API credentials
- Internet connection for API access

## Authentication

The application uses Google Cloud Service Account authentication. The service account credentials are stored in `credentials.json` and used automatically when the application runs. No browser-based authentication is required.

Make sure to:
1. Keep your `credentials.json` file secure and never commit it to version control
2. Share your Google Calendars with the service account email address
3. Ensure the service account has the necessary permissions to read your calendars

## Notes

- All calendar events must be all-day events
- Bill events must follow the exact format: `Name ($Amount)`
- Payday events must start with "Payday" and follow the format: `Payday ($Amount)`
- The application calculates balances starting from "today"
- Balances are calculated for the day before each payday
