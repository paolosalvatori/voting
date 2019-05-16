#region Using Directives
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using System;
#endregion

namespace VotingWeb
{
    public class TelemetryInitializer : ITelemetryInitializer
    {
        #region Private Constants
        private const string RoleName = "VotingWeb";
        #endregion

        #region Public Methods
        public void Initialize(ITelemetry telemetry)
        {
            if (string.IsNullOrWhiteSpace(telemetry.Context.Cloud.RoleName))
            {
                telemetry.Context.Cloud.RoleName = RoleName;
            }
            if (string.IsNullOrWhiteSpace(telemetry.Context.Cloud.RoleInstance))
            {
                telemetry.Context.Cloud.RoleInstance = $"{RoleName}-{Guid.NewGuid()}";
            }
        } 
        #endregion
    }
}
