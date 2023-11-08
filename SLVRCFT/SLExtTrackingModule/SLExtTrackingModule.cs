using Microsoft.Extensions.Logging;
using SLExtTrackingModule;
using System.Net.Sockets;
using VRCFaceTracking;
using VRCFaceTracking.Core.Params.Expressions;
using static VRCFaceTracking.Core.Params.Expressions.UnifiedExpressions;
using static SLExtTrackingModule.XrFBWeights;

namespace SLExtTrackingModule
{
    public class SLExtTrackingModule : ExtTrackingModule
    {
        public override (bool SupportsEye, bool SupportsExpression) Supported => (false, true);

        public override (bool eyeSuccess, bool expressionSuccess) Initialize(bool eyeAvailable, bool expressionAvailable)
        {
            ModuleInformation.Name = "SteamLink";

            var stream = GetType().Assembly.GetManifestResourceStream("SLExtTrackingModule.Assets.steamlink.png");
            ModuleInformation.StaticImages = stream != null ? new List<Stream> { stream } : ModuleInformation.StaticImages;

            int nSLOSCInitErr = SLOSC.Init("127.0.0.1", 9000, 9001);
            if (nSLOSCInitErr != 0)
            {
                Logger.LogError("SLExtTrackingModule::Initialize: Failed to initialize SLOSC: {nSLOSCInitErr}", nSLOSCInitErr);
                return (false, false);
            }

            Logger.LogInformation("SLExtTrackingModule successfully initialized");

            return (false, true);
        }

        public override void Update()
        {
            Thread.Sleep(10);

            int nSLOSCPollErr = SLOSC.PollNext(ref _currentPacket);

            if(nSLOSCPollErr != 0)
            {
                if(nSLOSCPollErr != 2) //no new data
                {
                    Logger.LogError("Poll error: {nSLOSCPollErr}", nSLOSCPollErr);
                }

                return;
            }

            unsafe
            {
                foreach (KeyValuePair<XrFBWeights, UnifiedExpressions> entry in dicFBWeightsUnifiedExpressions)
                {
                    UnifiedTracking.Data.Shapes[(int)entry.Value].Weight = _currentPacket.vWeights[(int)entry.Key];
                }
            }
        }
        public override void Teardown()
        {
            SLOSC.Close();
        }

        SLOSCPacket _currentPacket = new SLOSCPacket();


        private readonly Dictionary<XrFBWeights, UnifiedExpressions> dicFBWeightsUnifiedExpressions = new Dictionary<XrFBWeights, UnifiedExpressions>
            {
                {BrowLowererL, BrowPinchLeft },
                {BrowLowererR, BrowPinchRight },
                {LidTightenerL, EyeSquintLeft},
                {LidTightenerR, EyeSquintRight},
            };
    }
}