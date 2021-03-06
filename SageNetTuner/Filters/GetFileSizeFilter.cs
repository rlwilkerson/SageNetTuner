namespace SageNetTuner.Filters
{
    using System;
    using System.Globalization;
    using System.IO;

    using NLog;

    using SageNetTuner.Model;

    using Tamarack.Pipeline;

    public class GetFileSizeFilter : BaseFilter
    {
        public GetFileSizeFilter(Logger logger)
            : base(logger)
        {
            logger.Trace("GetFileSizeFilter.ctor()");

        }

        protected override bool CanExecute(RequestContext context)
        {
            return (context.RequestCommand == RequestCommand.GetFileSize);
        }

        protected override string OnExecute(RequestContext context)
        {
            Logger.Trace("GetFileSizeFilter.OnExecute()");

            return GetFileSize(context.CommandArgs[0]).ToString(CultureInfo.InvariantCulture);
        }

        private long GetFileSize(string filename)
        {

            if (File.Exists(filename))
            {
                try
                {
                    var fi = new FileInfo(filename);
                    fi.Refresh();
                    Logger.Trace("GetFileSize(): Filename={0}, Length={1:n0}", filename, fi.Length);
                    return fi.Length;
                }
                catch (Exception e)
                {
                    Logger.Warn(string.Format("Exception getting file size, returning 0: {0}", e.Message), e);
                    return 0;

                }
            }

            Logger.Warn("File does not exist, cannot get file size");
            return 0;
        }

    }
}