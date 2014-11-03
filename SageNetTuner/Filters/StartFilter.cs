﻿namespace SageNetTuner.Filters
{
    using System;
    using System.Linq;

    using NLog;

    using SageNetTuner.Model;

    public class StartFilter :  BaseFilter
    {
        private readonly ExecutableProcessCaptureManager _executableProcessCapture;


        public StartFilter(ExecutableProcessCaptureManager executableProcessCaptureManager, Logger logger)
            : base(logger)
        {
            _executableProcessCapture = executableProcessCaptureManager;
        }

        protected override bool CanExecute(RequestContext context)
        {
            return (context.Command == CommandName.Start);
        }

        protected override string OnExecute(RequestContext context)
        {
            return StartRecording(context);
        }

        private string StartRecording(RequestContext context)
        {

            var command = new StartCommand(context.CommandArgs[1], context.CommandArgs[3]);

            Logger.Debug("StartRecording(): {0}", command);

            try
            {
                // Find the requested channel to get the URL
                var ch = (from x in context.Settings.Lineup.Channels where x.GuideNumber == command.Channel select x).FirstOrDefault();
                if (ch != null)
                {
                    Logger.Debug("StartRecording(): Found Requested Channel: GuideName={0}, GuideNumber={1}, URL={2}", ch.GuideName, ch.GuideNumber, ch.URL);

                    _executableProcessCapture.Start(ch, command.FileName);

                    Logger.Trace("StartRecording(): Recording Started");

                    return "OK";
                }
                else
                {
                    Logger.Warn("StartRecording(): Channel not found");
                    return string.Format("ERROR Channel not found in device lineup. {0}", command.Channel);
                }

            }
            catch (Exception ex)
            {
                Logger.Error("StartRecording(): Exception trying start recording", ex);
                return string.Format("ERROR {0}", ex.Message);
            }
        }

    }
}