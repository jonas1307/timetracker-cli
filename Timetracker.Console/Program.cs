﻿using CommandLine;
using Timetracker.Options;
using Timetracker.Services;

await Parser.Default.ParseArguments<ConfigOptions, TrackOptions, ActivitiesOptions, AddOptions, SyncOptions>(args)
    .MapResult(
        async (ConfigOptions opts) => await ConfigAction(opts),
        async (TrackOptions opts) => await TrackAction(opts),
        async (ActivitiesOptions opts) => await ActivitiesAction(opts),
        async (AddOptions opts) => await AddActions(opts),
        async (SyncOptions opts) => await SyncActions(opts),
        errs => Task.FromResult(0)
    );

async Task ActivitiesAction(ActivitiesOptions opts)
{
    await ActivityService.SeedActivities();

    var activities = ActivityService.GetActivities();

    Console.WriteLine("The available activities are: ");

    foreach (var item in activities)
    {
        Console.WriteLine(item.Name);
    }
}

async Task ConfigAction(ConfigOptions opts)
{
    // Gets user ID
    Console.WriteLine("Obtaining User Id...");

    var user = await HttpService.GetTimetrackerUser(opts.TimetrackerUrl, opts.TimetrackerBearerToken);

    Console.WriteLine("User ID obtained successfully.");

    // Creates config file
    Console.WriteLine("Creating config file...");

    var config = new Config
    {
        TimetrackerBearerToken = opts.TimetrackerBearerToken,
        TimetrackerUrl = opts.TimetrackerUrl,
        TimetrackerUserId = user.Data.User.Id
    };

    FileService.SaveConfig(config);

    Console.WriteLine("Config file created.");

    // Creates activity file
    Console.WriteLine("Creating activities...");

    await ActivityService.SeedActivities();

    Console.WriteLine("Activities file created.");
}

static async Task TrackAction(TrackOptions opts)
{
    await HttpService.RegisterActivity(opts);

    Console.WriteLine("Activity successfully created.");
}

async Task AddActions(AddOptions opts)
{
    throw new NotImplementedException();
}

async Task SyncActions(SyncOptions opts)
{
    throw new NotImplementedException();
}