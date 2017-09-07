#include "dependencies.h"

#include <map>
#include <string>

BOOL WINAPI DllMain(HMODULE hModule,
	DWORD  ul_reason_for_call,
	LPVOID lpReserved
)
{
	return TRUE;
}


typedef std::pair<std::string, rtc::scoped_refptr<webrtc::MediaStreamInterface>> MediaStreamPair;

static const char kAudioLabel[] = "audio_label";
static const char kVideoLabel[] = "video_label";
static const char kStreamLabel[] = "stream_label";

static struct PeerConnectionObserver : public webrtc::PeerConnectionObserver
{
	// Triggered when the SignalingState changed.
	virtual void OnSignalingChange(
		webrtc::PeerConnectionInterface::SignalingState new_state)
	{

	}

	// Triggered when renegotiation is needed. For example, an ICE restart
	// has begun.
	virtual void OnRenegotiationNeeded()
	{

	}

	// Called any time the IceConnectionState changes.
	//
	// Note that our ICE states lag behind the standard slightly. The most
	// notable differences include the fact that "failed" occurs after 15
	// seconds, not 30, and this actually represents a combination ICE + DTLS
	// state, so it may be "failed" if DTLS fails while ICE succeeds.
	virtual void OnIceConnectionChange(
		webrtc::PeerConnectionInterface::IceConnectionState new_state)
	{

	}

	// Called any time the IceGatheringState changes.
	virtual void OnIceGatheringChange(
		webrtc::PeerConnectionInterface::IceGatheringState new_state)
	{

	}

	// A new ICE candidate has been gathered.
	virtual void OnIceCandidate(const webrtc::IceCandidateInterface* candidate)
	{

	}

} PeerObserverInstance;
static struct VideoObserver : public rtc::VideoSinkInterface<webrtc::VideoFrame>
{
	virtual void OnFrame(const webrtc::VideoFrame& frame)
	{

	}
} VideoObserverInstance;

static rtc::scoped_refptr<webrtc::PeerConnectionFactoryInterface> FactoryInstance;
static rtc::scoped_refptr<webrtc::PeerConnectionInterface> ConnectionInstance;
static rtc::scoped_refptr<webrtc::VideoTrackInterface> VideoInstance;
static std::map <std::string, rtc::scoped_refptr<webrtc::MediaStreamInterface>> ActiveStreams;

#ifdef __cplusplus
extern "C" {
#endif

	BOOL UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API Start(_In_ char* sdpMessage)
	{
		/*
		
		RTC_DCHECK(peer_connection_factory_.get() != NULL);
		RTC_DCHECK(peer_connection_.get() == NULL);

		webrtc::PeerConnectionInterface::RTCConfiguration config;

		// Try parsing config file.
		std::string configFilePath = GetAbsolutePath("webrtcConfig.json");
		std::ifstream configFile(configFilePath);
		Json::Reader reader;
		Json::Value root = NULL;

		if (configFile.good())
		{

		reader.parse(configFile, root, true);
		if (root.isMember("iceConfiguration"))
		{
		Json::Value iceConfig = root.get("iceConfiguration", NULL);
		if (iceConfig == "relay")
		{
		webrtc::PeerConnectionInterface::IceServer turnServer;
		turnServer.uri = "";
		turnServer.username = "";
		turnServer.password = "";


		if (root.isMember("turnServer"))
		{
		Json::Value jsonTurnServer = root.get("turnServer", NULL);
		if (!jsonTurnServer.isNull())
		{
		turnServer.uri = jsonTurnServer["uri"].asString();
		turnServer.username = jsonTurnServer["username"].asString();
		turnServer.password = jsonTurnServer["password"].asString();
		}
		}

		// if we're given explicit turn creds at runtime, steamroll any config values
		if (!turn_username_.empty() && !turn_password_.empty())
		{
		turnServer.username = turn_username_;
		turnServer.password = turn_password_;
		}

		turnServer.tls_cert_policy = webrtc::PeerConnectionInterface::kTlsCertPolicyInsecureNoCheck;
		config.type = webrtc::PeerConnectionInterface::kRelay;
		config.servers.push_back(turnServer);
		}
		else
		{
		if (iceConfig == "stun")
		{
		webrtc::PeerConnectionInterface::IceServer stunServer;
		stunServer.uri = "";
		if (root.isMember("stunServer"))
		{
		Json::Value jsonTurnServer = root.get("stunServer", NULL);
		if (!jsonTurnServer.isNull())
		{
		stunServer.urls.push_back(jsonTurnServer["uri"].asString());
		config.servers.push_back(stunServer);
		}
		}
		}
		else
		{
		webrtc::PeerConnectionInterface::IceServer stunServer;
		stunServer.urls.push_back(GetPeerConnectionString());
		config.servers.push_back(stunServer);
		}
		}
		}
		}

		webrtc::FakeConstraints constraints;
		if (dtls)
		{
		constraints.AddOptional(webrtc::MediaConstraintsInterface::kEnableDtlsSrtp,"true");
		}
		else
		{
		constraints.AddOptional(webrtc::MediaConstraintsInterface::kEnableDtlsSrtp, "false");
		}

		peer_connection_ = peer_connection_factory_->CreatePeerConnection(
		config, &constraints, NULL, NULL, this);

		return peer_connection_.get() != NULL;

		*/
		webrtc::PeerConnectionInterface::RTCConfiguration config;
		webrtc::FakeConstraints constraints;

		// TODO(bengreenier): populate given above comment

		FactoryInstance = webrtc::CreatePeerConnectionFactory();
		ConnectionInstance = FactoryInstance->CreatePeerConnection(config,
			&constraints,
			NULL,
			NULL,
			&PeerObserverInstance);
		VideoInstance = rtc::scoped_refptr<webrtc::VideoTrackInterface>(
			FactoryInstance->CreateVideoTrack(
				kVideoLabel,
				FactoryInstance->CreateVideoSource(
					std::unique_ptr<cricket::VideoCapturer>(
						new cricket::FakeVideoCapturer(false)),
					NULL)));

		rtc::scoped_refptr<webrtc::MediaStreamInterface> stream =
			FactoryInstance->CreateLocalMediaStream(kStreamLabel);

		stream->AddTrack(VideoInstance);

		auto result = ConnectionInstance->AddStream(stream);

		ActiveStreams.insert(MediaStreamPair(stream->label(), stream));

		return FactoryInstance.get() != nullptr &&
			ConnectionInstance.get() != nullptr &&
			VideoInstance.get() != nullptr &&
			result;
	}

	BOOL UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API Stop()
	{
		if (VideoInstance.get() != nullptr)
		{
			VideoInstance.release();
		}

		if (ConnectionInstance.get() != nullptr)
		{
			ConnectionInstance->Close();

			ConnectionInstance.release();
		}

		if (FactoryInstance.get() != nullptr)
		{
			FactoryInstance.release();
		}

		return FALSE;
	}

	BOOL UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API GetPrimaryTexture(_In_ UINT32 width, _In_ UINT32 height, _COM_Outptr_ void** texturePtr)
	{
		// TODO(bengreenier): we can render
		rtc::VideoSinkWants videoRequest;

		// TODO(bengreenier): only need this on start (remove from here):
		VideoInstance->AddOrUpdateSink(&VideoObserverInstance, videoRequest);

		// we'll expose the result of the VideoObserverInstance::OnFrame() here

		return TRUE;
	}

#ifdef __cplusplus
}
#endif