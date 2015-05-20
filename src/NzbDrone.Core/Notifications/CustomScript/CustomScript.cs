using System.Collections.Generic;
using FluentValidation.Results;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Notifications.CustomScript
{
    public class CustomScript : NotificationBase<CustomScriptSettings>
    {
        private readonly ICustomScriptService _customScriptService;

        public CustomScript(ICustomScriptService customScriptService)
        {
            _customScriptService = customScriptService;
        }

        public override string Link
        {
            get { return "http://wiki.sonarr.tv/"; }
        }

        public override void OnGrab(string message)
        {
        }

        public override void OnDownload(DownloadMessage message)
        {
            _customScriptService.OnDownload(message.Series, message.EpisodeFile, Settings);
        }

        public override void AfterRename(Series series)
        {
            //TODO: After rename needs to be optional before we can enable this
            //If this is enabled after merge we need to enable after rename for everything except for Custom Script
//            _customScriptService.AfterRename(series, Settings);
        }

        public override string Name
        {
            get
            {
                return "Custom Script";
            }
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            return new ValidationResult(failures);
        }
    }
}
