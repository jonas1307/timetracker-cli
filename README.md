# timetracker-cli

A .NET 10 command-line tool for logging and managing time entries in [7pace Timetracker](https://www.7pace.com).

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A 7pace Timetracker instance with a valid Bearer Token

## Build

```bash
dotnet build Timetracker.Console
dotnet publish Timetracker.Console -c Release
```

## First-time setup

Before using any command, run `config` to authenticate and cache your activity types locally:

```bash
timetracker config --url https://<company>.timehub.7pace.com --token <bearer-token>
```

---

## Commands

- [config](#config)
- [activities](#activities)
- [add](#add)
- [list](#list)
- [update](#update)
- [copy](#copy)
- [delete](#delete)
- [import](#import)

---

### config

Configure the connection to your Timetracker instance.

| Option | Short | Required | Description |
|---|---|---|---|
| `--url` | `-u` | yes* | Base URL of your Timetracker instance |
| `--token` | `-t` | yes* | Bearer token for authentication |
| `--show` | | no | Display current config (token masked) |
| `--reset` | | no | Delete all local config and activity cache |

*Required when setting up or updating credentials.

```bash
# Initial setup
timetracker config --url https://acme.timehub.7pace.com --token eyJ...

# View current config
timetracker config --show

# Remove all local config
timetracker config --reset
```

---

### activities

List available activity types. Uses a local cache populated during `config`.

| Option | Short | Required | Description |
|---|---|---|---|
| `--sync` | | no | Refresh the activity type cache from the server before listing |

```bash
# List cached activity types
timetracker activities

# Sync from server then list
timetracker activities --sync
```

---

### add

Create a new time entry.

| Option | Short | Required | Description |
|---|---|---|---|
| `--date` | `-d` | yes | Date: `YYYY/MM/DD`, `today`, or `yesterday` |
| `--work-item` | `-w` | yes | Work Item ID |
| `--length` | `-l` | yes | Duration in hours (e.g. `0.5`, `1.5`) |
| `--type` | `-t` | yes | Activity type name (see `activities`) |
| `--comment` | `-c` | no | Comment for the entry |
| `--hour` | `-h` | no | Start time in `HH:MM` format (default: `09:00`) |
| `--dry-run` | | no | Preview the entry locally without submitting |

```bash
# Log 2 hours of development on today
timetracker add -d today -w 12345 -l 2 -t Development -c "Feature X"

# Log half an hour of a meeting starting at 14:00
timetracker add -d 2026/06/19 -w 12345 -l 0.5 -t Meeting -h 14:00

# Preview before submitting
timetracker add -d today -w 12345 -l 1 -t Development --dry-run
```

---

### list

List time entries for a period.

| Option | Short | Required | Description |
|---|---|---|---|
| `--from` | `-f` | no | Start date (default: today) |
| `--to` | `-t` | no | End date (default: today) |
| `--month` | `-m` | no | Month in `YYYY/MM` format |
| `--work-item` | `-w` | no | Filter by Work Item ID |
| `--output` | `-o` | no | Output format: `json` (batch-upload compatible) or `csv` |
| `--today` | | no | Shortcut for today's entries |
| `--yesterday` | | no | Shortcut for yesterday's entries |
| `--week` | | no | Entries for the current week (Mon–Sun) |
| `--last-week` | | no | Entries for the previous week (Mon–Sun) |
| `--this-month` | | no | Entries for the current month |
| `--last-month` | | no | Entries for the previous month |
| `--summary` | | no | Daily summary instead of individual entries |
| `--ids` | | no | Show entry IDs instead of comments |

All period shortcuts (`--today`, `--yesterday`, `--week`, `--last-week`, `--this-month`, `--last-month`) and `--month` are mutually exclusive and cannot be combined with `--from` or `--to`.

```bash
# Today's entries
timetracker list --today

# Yesterday's entries
timetracker list --yesterday

# Current week
timetracker list --week

# Previous week
timetracker list --last-week

# Current month
timetracker list --this-month

# Previous month
timetracker list --last-month

# Specific date range
timetracker list -f 2026/06/01 -t 2026/06/30

# Monthly summary by day
timetracker list --month 2026/06 --summary

# Filter by work item and show IDs (for delete/update)
timetracker list --week --work-item 12345 --ids

# Export week as JSON for batch upload
timetracker list --week --output json > worklogs.json

# Export month as CSV
timetracker list --this-month --output csv > worklogs.csv
```

---

### update

Update one or more fields of an existing time entry. Only the fields provided are changed.

| Option | Short | Required | Description |
|---|---|---|---|
| `--id` | `-i` | yes | ID of the entry to update |
| `--date` | `-d` | no | New date |
| `--work-item` | `-w` | no | New Work Item ID |
| `--length` | `-l` | no | New duration in hours |
| `--type` | `-t` | no | New activity type |
| `--comment` | `-c` | no | New comment |
| `--hour` | `-h` | no | New start time |

```bash
# Fix the duration of an entry
timetracker update -i <id> -l 3

# Change the work item and comment
timetracker update -i <id> -w 99999 -c "Moved to correct item"
```

---

### copy

Duplicate an existing entry to a target date (defaults to today). Preserves work item, duration, activity type, comment, and start time.

| Option | Short | Required | Description |
|---|---|---|---|
| `--id` | `-i` | yes | ID of the entry to copy |
| `--date` | `-d` | no | Target date (default: `today`) |

```bash
# Copy a recurring entry to today
timetracker copy -i <id>

# Copy to a specific date
timetracker copy -i <id> -d 2026/06/20
```

---

### delete

Delete a time entry by its ID.

| Option | Short | Required | Description |
|---|---|---|---|
| `--id` | `-i` | yes | ID of the entry to delete |

```bash
timetracker delete -i <id>
```

---

### import

Import multiple time entries from a JSON file. The file must be an array of worklog objects, compatible with the output of `list --output json`.

| Option | Short | Required | Description |
|---|---|---|---|
| `--file` | `-f` | yes | Path to the JSON file |
| `--dry-run` | | no | Preview entries locally without submitting |

```bash
# Preview what would be imported
timetracker import --file worklogs.json --dry-run

# Import
timetracker import --file worklogs.json
```

---

## Common workflows

### Find and delete an entry

```bash
# List today's entries with IDs
timetracker list --today --ids

# Delete the target entry
timetracker delete -i <id>
```

### Fix an entry logged to the wrong work item

```bash
timetracker list --today --ids
timetracker update -i <id> -w <correct-work-item>
```

### Copy a recurring daily entry

```bash
# Find the original entry ID once
timetracker list -f 2026/06/01 -t 2026/06/01 --ids

# Copy it every day
timetracker copy -i <id>
```

### Export, edit, and re-import a week

```bash
# Export
timetracker list --week --output json > worklogs.json

# Edit worklogs.json as needed, then preview
timetracker import --file worklogs.json --dry-run

# Import
timetracker import --file worklogs.json
```

### Monthly summary

```bash
timetracker list --month 2026/06 --summary
```
