# Seq.App.Slack
[![NuGet](https://img.shields.io/nuget/v/Seq.App.Slack.svg?style=flat-square)](https://www.nuget.org/packages/Seq.App.Slack/)

An app for Seq (http://getseq.net) that forwards messages to Slack.

### Getting started

 1. Install the app into Seq through the Seq UI: _Settings_ > _Apps_ > _Install from Nuget_. The package id is _Seq.App.Slack_.
 2. In Slack, select _Manage apps_ > _Search App Directory_ > _"Incoming WebHooks"
 3. Add a new incoming webhook configuration and copy the _Webhook URL_
 4. Back in Seq, under _Settings_ > _Apps_, select _Add Instance_ next to the Slack app icon
 5. Configure the app instance, providing the webhook URL

