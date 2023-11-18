using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SLExtTrackingModule
{
    enum XrFBWeights : int
    {
        BrowLowererL,
        BrowLowererR,
        CheekPuffL,
        CheekPuffR,
        CheekRaiserL,
        CheekRaiserR,
        CheekSuckL,
        CheekSuckR,
        ChinRaiserB,
        ChinRaiserT,
        DimplerL,
        DimplerR,
        EyesClosedL,
        EyesClosedR,
        EyesLookDownL,
        EyesLookDownR,
        EyesLookLeftL,
        EyesLookLeftR,
        EyesLookRightL,
        EyesLookRightR,
        EyesLookUpL,
        EyesLookUpR,
        InnerBrowRaiserL,
        InnerBrowRaiserR,
        JawDrop,
        JawSidewaysLeft,
        JawSidewaysRight,
        JawThrust,
        LidTightenerL,
        LidTightenerR,
        LipCornerDepressoL,
        LipCornerDepressoR,
        LipCornerPullerL,
        LipCornerPullerR,
        LipFunnelerLB,
        LipFunnelerLT,
        LipFunnelerRB,
        LipFunnelerRT,
        LipPressorL,
        LipPressorR,
        LipPuckerL,
        LipPuckerR,
        LipStretcherL,
        LipStretcherR,
        LipSuckLB,
        LipSuckLT,
        LipSuckRB,
        LipSuckRT,
        LipTightenerL,
        LipTightenerR,
        LipsToward,
        LowerLipDepressorL,
        LowerLipDepressorR,
        MouthLeft,
        MouthRight,
        NoseWrinklerL,
        NoseWrinklerR,
        OuterBrowRaiserL,
        OuterBrowRaiserR,
        UpperLidRaiserL,
        UpperLidRaiserR,
        UpperLipRaiserL,
        UpperLipRaiserR,
        XR_FB_WEIGHTS_MAX,
    };

    unsafe struct SLOSCPacket
    {
        public fixed float vEyeGazePoint[3];
        public fixed float vWeights[(int)XrFBWeights.XR_FB_WEIGHTS_MAX];
    }

    internal static class SLOSC
    {
        [DllImport(_sDLLFilePath, CallingConvention = CallingConvention.Cdecl)]
        private extern static unsafe int SLOSCInit(int nInPort, int nOutPort);

        [DllImport(_sDLLFilePath, CallingConvention = CallingConvention.Cdecl)]
        private extern static unsafe int SLOSCPollNext(SLOSCPacket* pPacket);

        [DllImport(_sDLLFilePath, CallingConvention = CallingConvention.Cdecl)]
        private extern static unsafe int SLOSCClose();

        public static int Init(int nInPort, int nOutPort)
        {
            return SLOSCInit(nInPort, nOutPort);
        }

        public static int PollNext(ref SLOSCPacket packet)
        {
            unsafe
            {
                fixed (SLOSCPacket* pPacket = &packet)
                {
                    return SLOSCPollNext(pPacket);
                }
            }
        }

        public static int Close()
        {
            return SLOSCClose();
        }

        private const string _sDLLFilePath = "SLOSCParser.dll";
    }
}
