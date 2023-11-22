using Microsoft.Extensions.Logging;
using SLExtTrackingModule;
using System.Net.Sockets;
using VRCFaceTracking;
using VRCFaceTracking.Core.Params.Expressions;
using static VRCFaceTracking.Core.Params.Expressions.UnifiedExpressions;
using static SLExtTrackingModule.XrFBWeights;
using VRCFaceTracking.Core.Params.Data;

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

            int nSLOSCInitErr = SLOSC.Init(9015, 9016);
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
            return 1.0f - Math.Clamp(fEyeClosedWeight + fEyeClosedWeight * fEyeTightener, 0.0f, 1.0f);
        }

        private static unsafe void UpdateEyeTracking(ref SLOSCPacket packet)
        {
            {
                float fAngleX = MathF.Atan2(packet.vEyeGazePoint[0], -packet.vEyeGazePoint[2]);
                float fAngleY = MathF.Atan2(packet.vEyeGazePoint[1], -packet.vEyeGazePoint[2]);

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
            ref UnifiedExpressionShape[] shapes = ref UnifiedTracking.Data.Shapes;
            
            fixed(float* weights = packet.vWeights) {
                foreach (KeyValuePair<XrFBWeights, List<UnifiedExpressions>> entry in mapDirectXRFBUnifiedExpressions)
                {
                    int nWeightIndex = (int)entry.Key;
                    foreach (UnifiedExpressions unifiedExpression in entry.Value)
                    {
                        shapes[(int)unifiedExpression].Weight = weights[nWeightIndex];
                    }
                }

                shapes[(int)MouthUpperUpLeft].Weight = Math.Max(0f, shapes[(int)MouthUpperUpLeft].Weight - shapes[(int)NoseSneerLeft].Weight);
                shapes[(int)MouthUpperUpRight].Weight = Math.Max(0f, shapes[(int)MouthUpperUpRight].Weight - shapes[(int)NoseSneerRight].Weight);
                shapes[(int)MouthUpperDeepenLeft].Weight = Math.Max(0f, shapes[(int)MouthUpperUpLeft].Weight - shapes[(int)NoseSneerLeft].Weight);
                shapes[(int)MouthUpperUpRight].Weight = Math.Max(0f, shapes[(int)MouthUpperUpRight].Weight - shapes[(int)NoseSneerRight].Weight);
                shapes[(int)MouthUpperDeepenRight].Weight = Math.Max(0f, shapes[(int)MouthUpperUpRight].Weight - shapes[(int)NoseSneerRight].Weight);

                shapes[(int)LipSuckUpperLeft].Weight = Math.Min(1f - (float)Math.Pow(weights[(int)UpperLipRaiserL], 1f / 6f), weights[(int)LipSuckLT]);
                shapes[(int)LipSuckUpperRight].Weight = Math.Min(1f - (float)Math.Pow(weights[(int)UpperLipRaiserR], 1f / 6f), weights[(int)LipSuckRT]);
            }

        }

        public override void Update()
        {
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

        //based on https://docs.google.com/spreadsheets/d/118jo960co3Mgw8eREFVBsaJ7z0GtKNr52IB4Bz99VTA/edit#gid=0
        private static readonly Dictionary<XrFBWeights, List<UnifiedExpressions>> mapDirectXRFBUnifiedExpressions = new Dictionary<XrFBWeights, List<UnifiedExpressions>>
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
                
                {MouthLeft, new List<UnifiedExpressions>{MouthLowerLeft, MouthUpperLeft}},
                {MouthRight, new List<UnifiedExpressions>{MouthLowerRight, MouthUpperRight}},
                
                {ChinRaiserT, new List<UnifiedExpressions>{MouthRaiserUpper} },
                {ChinRaiserB, new List<UnifiedExpressions>{MouthRaiserLower} },
                
                {DimplerL, new List<UnifiedExpressions>{MouthDimpleLeft} },
                {DimplerR, new List<UnifiedExpressions>{MouthDimpleRight} },

                {LipsToward, new List<UnifiedExpressions>{MouthClosed}},
                {LipCornerPullerL, new List<UnifiedExpressions>{ MouthCornerPullLeft, MouthCornerSlantLeft} },
                {LipCornerPullerR, new List<UnifiedExpressions>{ MouthCornerPullRight, MouthCornerSlantRight} },
                {LipCornerDepressoL, new List<UnifiedExpressions>{ MouthFrownLeft} },
                {LipCornerDepressoR, new List<UnifiedExpressions>{ MouthFrownRight} },
                {LowerLipDepressorL, new List<UnifiedExpressions>{ MouthLowerDownLeft} },
                {LowerLipDepressorR, new List<UnifiedExpressions>{ MouthLowerDownRight} },
                {UpperLipRaiserL, new List<UnifiedExpressions>{ MouthUpperUpLeft} }, //something odd here
                {UpperLipRaiserR, new List<UnifiedExpressions>{ MouthUpperUpRight } }, //something odd here
                {LipTightenerL, new List<UnifiedExpressions>{MouthTightenerLeft} },
                {LipTightenerR, new List<UnifiedExpressions>{MouthTightenerRight} },
                {LipPressorL, new List<UnifiedExpressions>{MouthPressLeft} },
                {LipPressorR, new List<UnifiedExpressions>{MouthPressRight} },
                {LipStretcherL, new List<UnifiedExpressions>{MouthStretchLeft} },
                {LipStretcherR, new List<UnifiedExpressions>{MouthStretchRight} },
                {LipPuckerL, new List<UnifiedExpressions>{ LipPuckerLowerLeft, LipPuckerUpperLeft } },
                {LipPuckerR, new List<UnifiedExpressions>{ LipPuckerLowerRight, LipPuckerUpperRight } },
                {LipFunnelerLB, new List<UnifiedExpressions>{LipFunnelLowerLeft} },
                {LipFunnelerLT, new List<UnifiedExpressions>{LipFunnelUpperLeft} },
                {LipFunnelerRB, new List<UnifiedExpressions>{LipFunnelLowerRight} },
                {LipFunnelerRT, new List<UnifiedExpressions>{LipFunnelUpperRight} },
                {LipSuckLB, new List<UnifiedExpressions>{LipSuckLowerLeft} },
                {LipSuckLT, new List<UnifiedExpressions>{LipSuckUpperLeft} },
                {LipSuckRB, new List<UnifiedExpressions>{LipSuckLowerRight} },
                {LipSuckRT, new List<UnifiedExpressions>{LipSuckUpperLeft} },

                {CheekPuffL, new List<UnifiedExpressions>{CheekPuffLeft} },
                {CheekPuffR, new List<UnifiedExpressions>{CheekPuffRight} },
                {CheekSuckL, new List<UnifiedExpressions>{CheekSuckLeft} },
                {CheekSuckR, new List<UnifiedExpressions>{CheekSuckRight} },
                {CheekRaiserL, new List<UnifiedExpressions>{CheekSquintLeft} },
                {CheekRaiserR, new List<UnifiedExpressions>{CheekSquintRight} },

                {NoseWrinklerL, new List<UnifiedExpressions>{NoseSneerLeft} },
                {NoseWrinklerR, new List<UnifiedExpressions>{NoseSneerRight} },
            };
    }
}