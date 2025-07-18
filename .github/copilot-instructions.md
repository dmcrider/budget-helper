# Copilot Instructions for Budget Helper

<!-- Use this file to provide workspace-specific custom instructions to Copilot. For more details, visit https://code.visualstudio.com/docs/copilot/copilot-customization#_use-a-githubcopilotinstructionsmd-file -->

## Project Overview
This is a C# .NET 8 console application that integrates with Google Calendar API to track bills and calculate account balances based on payday cycles.

## Key Features
- Google Calendar API integration for accessing "Bills" calendar and default calendar
- Parses bill events in format "XXXX ($YYY)" (e.g., "Spotify ($17)")
- Parses payday events starting with "Payday" (e.g., "Payday ($2500)")
- Calculates account balance before each payday for specified number of cycles
- All calendar events are all-day events

## Technical Details
- Uses Google.Apis.Calendar.v3 for Calendar API access
- Uses Google.Apis.Auth for authentication
- Command line arguments: current account balance and number of payday cycles
- Calculates balance starting from "today" for the specified number of pay cycles

## Code Style
- Follow C# naming conventions
- Use async/await patterns for API calls
- Include proper error handling for API operations
- Use clear, descriptive variable and method names
