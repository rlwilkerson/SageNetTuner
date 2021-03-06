﻿namespace SageNetTuner.Filters
{
    using System;
    using System.Globalization;

    using NLog;

    using SageNetTuner.Model;

    using Tamarack.Pipeline;

    public class PortFilter : BaseFilter
    {
        public PortFilter(Logger logger)
            : base(logger)
        {
            logger.Trace("PortFilter.ctor()");

        }

        protected override bool CanExecute(RequestContext context)
        {
            return (context.RequestCommand == RequestCommand.Port);
        }

        protected override string OnExecute(RequestContext context)
        {
            Logger.Trace("PortFilter.OnExecute()");

            return context.Settings.Tuner.ListenerPort.ToString(CultureInfo.InvariantCulture); ;
        }
    }
}