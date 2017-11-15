# Aiursoft API

[![Build Status](https://travis-ci.org/AiursoftWeb/API.svg?branch=master)](https://travis-ci.org/AiursoftWeb/API)

All core API implementations for Aiursoft authorization.

## How to run

**We strongly recommend running this app on Windows 10 or Windows Server 2016**

### Dependencies

* SQL Server LocalDb
* .NET Core 2.0 SDK

### Run with command

Please excuse the following commands in the project folder:

    dotnet restore
    dotnet ef database update
    set ASPNETCORE_ENVIRONMENT Development
    dotnet run

### Run in Visual Studio

Please install Visual Studio 2017 with .NET Core development kit.

1. Double click the `.sln` file.
2. Strike `F5`.

## What is stored in its database

API app is the core authentication gateway for all aiursoft web apps.

API app is also the core profile management system for users.

API app also managements users preferred language.

It stored:

1. All users' basic profile for Aiursoft.
2. Access tokens for all apps developed within Aiursoft.
3. OAuth history and auth codes.
4. App grant history and details.

## What is the relationship with other Aiursoft apps

For all apps' information is stored in the developer project, it requires the developer project is well configured.

To grant an app to access Aiursoft APIs, API needs to check its app id and app secret. API will submit those information to developer site to check and grant.

All other Aiursoft apps which require access token will check the app detail with API app, for here stores all access tokens.

## How to contribute

There are many ways to contribute to the project: logging bugs, submitting pull requests, reporting issues, and creating suggestions.

Even if you have push rights on the repository, you should create a personal fork and create feature branches there when you need them. This keeps the main repository clean and your personal workflow cruft out of sight.

We're also interested in your feedback for the future of this project. You can submit a suggestion or feature request through the issue tracker. To make this process more effective, we're asking that these include more information to help define them more clearly.