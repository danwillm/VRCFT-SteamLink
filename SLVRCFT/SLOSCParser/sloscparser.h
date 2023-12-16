#pragma once

enum EXrFBWeights {
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
	LipCornerDepressorL,
	LipCornerDepressorR,
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
	ToungeTipInterdental,
	ToungeTipAlveolar,
	FrontDorsalPalate,
	MidDorsalPalate,
	BackDorsalVelar,
	ToungeOut,
	ToungeRetreat,
	XR_FB_WEIGHTS_MAX,
};

struct SLOSCPacket {
	float vEyeGazePoint[3];
	float vWeights[XR_FB_WEIGHTS_MAX];
};

enum SLOSCResult {
	SLOSC_SUCCESS,
	SLOSC_ERROR_MINIOSC_INITIALIZE_FAILED,
	SLOSC_ERROR_NO_NEW_PACKET,
};