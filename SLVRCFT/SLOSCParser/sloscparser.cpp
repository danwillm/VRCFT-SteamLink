#include "sloscparser.h"

#include <iostream>
#include <stdio.h>
#include <stdint.h>

#include <map>
#include <mutex>
#include <string>

#define MINIOSC_IMPLEMENTATION
#include "miniosc.h"

#define SLOSC_CHECK(x) \
do { \
	SLOSCResult result = x; \
	if(result != SLOSC_SUCCESS) { \
		return result; \
	} \
} while(false)

static const std::map<std::string, EXrFBWeights> mapXrFBWeightStrings{
	{"/sl/xrfb/facew/BrowLowererL", BrowLowererL },
	{"/sl/xrfb/facew/BrowLowererR", BrowLowererR },
	{"/sl/xrfb/facew/CheekPuffL", CheekPuffL },
	{"/sl/xrfb/facew/CheekPuffR", CheekPuffR },
	{"/sl/xrfb/facew/CheekRaiserL", CheekRaiserL },
	{"/sl/xrfb/facew/CheekRaiserR", CheekRaiserR },
	{"/sl/xrfb/facew/CheekSuckL", CheekSuckL },
	{"/sl/xrfb/facew/CheekSuckR", CheekSuckR },
	{"/sl/xrfb/facew/ChinRaiserB", ChinRaiserB },
	{"/sl/xrfb/facew/ChinRaiserT", ChinRaiserT },
	{"/sl/xrfb/facew/DimplerL", DimplerL },
	{"/sl/xrfb/facew/DimplerR", DimplerR },
	{"/sl/xrfb/facew/EyesClosedL", EyesClosedL },
	{"/sl/xrfb/facew/EyesClosedR", EyesClosedR },
	{"/sl/xrfb/facew/EyesLookDownL", EyesLookDownL },
	{"/sl/xrfb/facew/EyesLookDownR", EyesLookDownR },
	{"/sl/xrfb/facew/EyesLookLeftL", EyesLookLeftL },
	{"/sl/xrfb/facew/EyesLookLeftR", EyesLookLeftR },
	{"/sl/xrfb/facew/EyesLookRightL", EyesLookRightL },
	{"/sl/xrfb/facew/EyesLookRightR", EyesLookRightR },
	{"/sl/xrfb/facew/EyesLookUpL", EyesLookUpL },
	{"/sl/xrfb/facew/EyesLookUpR", EyesLookUpR },
	{"/sl/xrfb/facew/InnerBrowRaiserL", InnerBrowRaiserL },
	{"/sl/xrfb/facew/InnerBrowRaiserR", InnerBrowRaiserR },
	{"/sl/xrfb/facew/JawDrop", JawDrop },
	{"/sl/xrfb/facew/JawSidewaysLeft", JawSidewaysLeft },
	{"/sl/xrfb/facew/JawSidewaysRight", JawSidewaysRight },
	{"/sl/xrfb/facew/JawThrust", JawThrust },
	{"/sl/xrfb/facew/LidTightenerL", LidTightenerL },
	{"/sl/xrfb/facew/LidTightenerR", LidTightenerR },
	{"/sl/xrfb/facew/LipCornerDepressoL", LipCornerDepressoL },
	{"/sl/xrfb/facew/LipCornerDepressoR", LipCornerDepressoR },
	{"/sl/xrfb/facew/LipCornerPullerL", LipCornerPullerL },
	{"/sl/xrfb/facew/LipCornerPullerR", LipCornerPullerR },
	{"/sl/xrfb/facew/LipFunnelerLB", LipFunnelerLB },
	{"/sl/xrfb/facew/LipFunnelerLT", LipFunnelerLT },
	{"/sl/xrfb/facew/LipFunnelerRB", LipFunnelerRB },
	{"/sl/xrfb/facew/LipFunnelerRT", LipFunnelerRT },
	{"/sl/xrfb/facew/LipPressorL", LipPressorL },
	{"/sl/xrfb/facew/LipPressorR", LipPressorR },
	{"/sl/xrfb/facew/LipPuckerL", LipPuckerL },
	{"/sl/xrfb/facew/LipPuckerR", LipPuckerR },
	{"/sl/xrfb/facew/LipStretcherL", LipStretcherL },
	{"/sl/xrfb/facew/LipStretcherR", LipStretcherR },
	{"/sl/xrfb/facew/LipSuckLB", LipSuckLB },
	{"/sl/xrfb/facew/LipSuckLT", LipSuckLT },
	{"/sl/xrfb/facew/LipSuckRB", LipSuckRB },
	{"/sl/xrfb/facew/LipSuckRT", LipSuckRT },
	{"/sl/xrfb/facew/LipTightenerL", LipTightenerL },
	{"/sl/xrfb/facew/LipTightenerR", LipTightenerR },
	{"/sl/xrfb/facew/LipsToward", LipsToward },
	{"/sl/xrfb/facew/LowerLipDepressorL", LowerLipDepressorL },
	{"/sl/xrfb/facew/LowerLipDepressorR", LowerLipDepressorR },
	{"/sl/xrfb/facew/MouthLeft", MouthLeft },
	{"/sl/xrfb/facew/MouthRight", MouthRight },
	{"/sl/xrfb/facew/NoseWrinklerL", NoseWrinklerL },
	{"/sl/xrfb/facew/NoseWrinklerR", NoseWrinklerR },
	{"/sl/xrfb/facew/OuterBrowRaiserL", OuterBrowRaiserL },
	{"/sl/xrfb/facew/OuterBrowRaiserR", OuterBrowRaiserR },
	{"/sl/xrfb/facew/UpperLidRaiserL", UpperLidRaiserL },
	{"/sl/xrfb/facew/UpperLidRaiserR", UpperLidRaiserR },
	{"/sl/xrfb/facew/UpperLipRaiserL", UpperLipRaiserL },
	{"/sl/xrfb/facew/UpperLipRaiserR", UpperLipRaiserR },
};

static miniosc* osc = nullptr;

bool bHasNewData = false;
SLOSCPacket nextPacket = {};

extern "C" __declspec(dllexport) int SLOSCInit(const int nInPort, const int nOutPort) {
#ifdef _DEBUG
	AllocConsole();

	freopen("CONOUT$", "w", stdout);
#endif

	std::cout << "In Port: " << nInPort << " Out Port: " << nOutPort << std::endl;

	int nOscErr = 0;
	osc = minioscInit(nInPort, nOutPort, nullptr, &nOscErr);

	if (nOscErr != 0) {
		std::cout << "miniosc error: " << nOscErr << std::endl;
		return SLOSC_ERROR_MINIOSC_INITIALIZE_FAILED;
	}

	return SLOSC_SUCCESS;
}

#ifdef _DEBUG
static std::chrono::time_point<std::chrono::steady_clock> tpLastNoPacketLog;
static int nPacketCount = 0;
#endif

void rxcb(const char* cpAddress, const char* sType, const void** ppParmaters) {
#ifdef _DEBUG
	if (std::chrono::steady_clock::now() > tpLastNoPacketLog + std::chrono::milliseconds(1000)) {
		std::cout << "Packets since last log: " << nPacketCount << std::endl;
		
		tpLastNoPacketLog = std::chrono::steady_clock::now();
		nPacketCount = 0;
	}
	nPacketCount++;
#endif

	std::string sAddress(cpAddress);
	{
		auto itWeightString = mapXrFBWeightStrings.find(sAddress);
		if (itWeightString != mapXrFBWeightStrings.end()) {
			nextPacket.vWeights[itWeightString->second] = *((float*)ppParmaters[0]); //assume float

			bHasNewData = true;

			return;
		}
	}

	if (sAddress == "/sl/eyeTrackedGazePoint") {
		memcpy(&nextPacket.vEyeGazePoint, (float*)ppParmaters[0], sizeof(float) * 3);

		bHasNewData = true;

		return;
	}
}

extern "C" __declspec(dllexport) int SLOSCPollNext(SLOSCPacket * outSLOSCPacket) {
	int r = minioscPoll(osc, 10, rxcb);
	if (!bHasNewData) {
		return SLOSC_ERROR_NO_NEW_PACKET;
	}

	memcpy(outSLOSCPacket, &nextPacket, sizeof(SLOSCPacket));
	bHasNewData = false;

	return SLOSC_SUCCESS;
}

extern "C" __declspec(dllexport) int SLOSCClose() {
	minioscClose(osc);

	return SLOSC_SUCCESS;
}