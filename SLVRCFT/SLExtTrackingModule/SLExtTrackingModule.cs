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
        public override (bool SupportsEye, bool SupportsExpression) Supported => (true, true);

        public override (bool eyeSuccess, bool expressionSuccess) Initialize(bool eyeAvailable, bool expressionAvailable)
        {
            ModuleInformation.Name = "SteamLink";

            var stream = GetType().Assembly.GetManifestResourceStream("SLExtTrackingModule.Assets.steamlink.png");
            ModuleInformation.StaticImages = stream != null ? new List<Stream> { stream } : ModuleInformation.StaticImages;

            int nSLOSCInitErr = SLOSC.Init("127.0.0.1", 9015, 9016);
            if (nSLOSCInitErr != 0)
            {
                Logger.LogError("SLExtTrackingModule::Initialize: Failed to initialize SLOSC: {nSLOSCInitErr}", nSLOSCInitErr);
                return (false, false);
            }

            Logger.LogInformation("SLExtTrackingModule successfully initialized");

            return (true, true);
        }

        private static float CalculateEyeOpenness(float fEyeClosedWeight, float fEyeTightener)
        {
            return Math.Clamp(fEyeClosedWeight + fEyeClosedWeight * fEyeTightener, 0.0f, 1.0f);
        }

        private static unsafe void UpdateEyeTracking(ref SLOSCPacket packet)
        {
            {
                float fAngleX = MathF.Atan2(packet.vEyeGazePoint[0], -packet.vEyeGazePoint[2]);
                float fAngleY = -MathF.Atan2(packet.vEyeGazePoint[1], -packet.vEyeGazePoint[2]);

                float fNmAngleX = fAngleX / (MathF.PI / 2.0f) * 2.0f;
                float fNmAngleY = fAngleY / (MathF.PI / 2.0f) * 2.0f;

                if (float.IsNaN(fNmAngleX))
                {
                    fNmAngleX = 0.0f;
                }
                if (float.IsNaN(fNmAngleY))
                {
                    fNmAngleY = 0.0f;
                }

                UnifiedTracking.Data.Eye.Left.Gaze.x = fAngleX;
                UnifiedTracking.Data.Eye.Left.Gaze.y = fAngleY;

                UnifiedTracking.Data.Eye.Right.Gaze.x = fAngleX;
                UnifiedTracking.Data.Eye.Right.Gaze.y = fAngleY;
            }

            {
                float fLeftOpenness = CalculateEyeOpenness(packet.vWeights[(int)XrFBWeights.EyesClosedL], packet.vWeights[(int)XrFBWeights.LidTightenerL]);
                float fRightOpenness = CalculateEyeOpenness(packet.vWeights[(int)XrFBWeights.EyesClosedR], packet.vWeights[(int)XrFBWeights.LidTightenerR]);

                UnifiedTracking.Data.Eye.Left.Openness = fLeftOpenness;
                UnifiedTracking.Data.Eye.Right.Openness = fRightOpenness;
            }

        }
        private static unsafe void UpdateFaceTracking(ref SLOSCPacket packet)
        {
            foreach (KeyValuePair<XrFBWeights, List<UnifiedExpressions>> entry in dictFBWeightsUnifiedExpressions)
            {
                int nWeightIndex = (int)entry.Key;
                foreach (UnifiedExpressions unifiedExpression in entry.Value)
                {
                    UnifiedTracking.Data.Shapes[(int)unifiedExpression].Weight = packet.vWeights[nWeightIndex];
                }
            }
        }

        public override void Update()
        {
            Thread.Sleep(10);

            int nSLOSCPollErr = SLOSC.PollNext(ref _currentPacket);
            if (nSLOSCPollErr != 0)
            {
                if (nSLOSCPollErr != 2) //no new data
                {
                    Logger.LogError("Poll error: {nSLOSCPollErr}", nSLOSCPollErr);
                }

                return;
            }

            UpdateEyeTracking(ref _currentPacket);
            UpdateFaceTracking(ref _currentPacket);
        }
        public override void Teardown()
        {
            SLOSC.Close();
        }

        SLOSCPacket _currentPacket = new SLOSCPacket();

        private static readonly Dictionary<XrFBWeights, List<UnifiedExpressions>> dictFBWeightsUnifiedExpressions = new Dictionary<XrFBWeights, List<UnifiedExpressions>>
            {
                {UpperLidRaiserL, new List<UnifiedExpressions>{EyeWideLeft}},
                {UpperLidRaiserR, new List<UnifiedExpressions>{EyeWideRight}},
                {LidTightenerL, new List<UnifiedExpressions>{EyeSquintLeft}},
                {LidTightenerR, new List<UnifiedExpressions>{EyeSquintRight}},
                {InnerBrowRaiserL, new List<UnifiedExpressions>{BrowInnerUpLeft}},
                {InnerBrowRaiserR, new List<UnifiedExpressions>{BrowInnerUpRight}},
                {OuterBrowRaiserL, new List<UnifiedExpressions>{BrowOuterUpLeft}},
                {OuterBrowRaiserR, new List<UnifiedExpressions>{BrowOuterUpRight}},
                {BrowLowererL, new List<UnifiedExpressions>{BrowPinchLeft, BrowLowererLeft}},
                {BrowLowererR, new List<UnifiedExpressions>{BrowPinchRight, BrowLowererRight}},
                {JawDrop, new List<UnifiedExpressions>{JawOpen}},
                {JawSidewaysLeft, new List<UnifiedExpressions>{JawLeft}},
                {JawSidewaysRight, new List<UnifiedExpressions>{JawRight}},
                {JawThrust, new List<UnifiedExpressions>{JawForward}},
                {LipsToward, new List<UnifiedExpressions>{MouthClosed}},
                {MouthLeft, new List<UnifiedExpressions>{MouthLowerLeft, MouthUpperLeft}},
                {MouthRight, new List<UnifiedExpressions>{MouthLowerRight, MouthUpperRight}},
            };
    }
}