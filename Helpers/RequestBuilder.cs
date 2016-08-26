using Google.Protobuf;
using PokemonGo.RocketAPI.Enums;
using POGOProtos.Networking;
using POGOProtos.Networking.Envelopes;
using POGOProtos.Networking.Requests;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using PokemonGo.RocketAPI.Extensions;
namespace PokemonGo.RocketAPI.Helpers
{
    public class RequestBuilder
    {
        private readonly string _authToken;
        private readonly AuthType _authType;
        private readonly double _latitude;
        private readonly double _longitude;
        private readonly double _altitude;
        private readonly AuthTicket _authTicket;
        private int _startTime;
        private ulong _nextRequestId;
        private readonly ISettings settings;

        public RequestBuilder(string authToken, AuthType authType, double latitude, double longitude, double altitude, ISettings settings, AuthTicket authTicket = null)
        {
            _authToken = authToken;
            _authType = authType;
            _latitude = latitude;
            _longitude = longitude;
            _altitude = altitude;
            this.settings = settings;
            _authTicket = authTicket;
            _nextRequestId = Convert.ToUInt64(RandomDevice.NextDouble() * Math.Pow(10, 18));
            if (_startTime == 0)
                _startTime = Utils.GetTime(true);
        }

        private Unknown6 GenerateSignature(IEnumerable<IMessage> requests)
        {
            var sig = new Signature();
            sig.TimestampSinceStart = (ulong)(Utils.GetTime(true) - _startTime);
            sig.Timestamp = (ulong)DateTime.UtcNow.ToUnixTime();
            sig.SensorInfo = new Signature.Types.SensorInfo()
            {
                AccelNormalizedX = GenRandom(-0.31110161542892456, 0.1681540310382843),
                AccelNormalizedY = GenRandom(-0.6574847102165222, -0.07290205359458923),
                AccelNormalizedZ = GenRandom(-0.9943905472755432, -0.7463029026985168),
                TimestampSnapshot = (ulong)(Utils.GetTime(true) - _startTime - RandomDevice.Next(100, 400)),
                MagnetometerX = GenRandom(-0.139084026217, 0.138112977147),
                MagnetometerY = GenRandom(-0.2, 0.19),
                MagnetometerZ = GenRandom(-0.2, 0.4),
                AngleNormalizedX = GenRandom(-47.149471283, 61.8397789001),
                AngleNormalizedY = GenRandom(-47.149471283, 61.8397789001),
                AngleNormalizedZ = GenRandom(-47.149471283, 5),
                AccelRawX = GenRandom(0.0729667818829, 0.0729667818829),
                AccelRawY = GenRandom(-2.788630499244109, 3.0586791383810468),
                AccelRawZ = GenRandom(-0.34825887123552773, 0.19347580173737935),
                GyroscopeRawX = GenRandom(-0.9703824520111084, 0.8556089401245117),
                GyroscopeRawY = GenRandom(-1.7470258474349976, 1.4218578338623047),
                GyroscopeRawZ = GenRandom(-0.9681901931762695, 0.8396636843681335),
                AccelerometerAxes = 3
            };

            sig.DeviceInfo = new POGOProtos.Networking.Signature.Types.DeviceInfo();
            if (settings.DeviceId != null) sig.DeviceInfo.DeviceId = settings.DeviceId;
            if (settings.AndroidBoardName != null) sig.DeviceInfo.AndroidBoardName = settings.AndroidBoardName;
            if (settings.AndroidBootloader != null) sig.DeviceInfo.AndroidBootloader = settings.AndroidBootloader;
            if (settings.DeviceBrand != null) sig.DeviceInfo.DeviceBrand = settings.DeviceBrand;
            if (settings.DeviceModel != null) sig.DeviceInfo.DeviceModel = settings.DeviceModel;
            if (settings.DeviceModelIdentifier != null) sig.DeviceInfo.DeviceModelIdentifier = settings.DeviceModelIdentifier;
            if (settings.DeviceModelBoot != null) sig.DeviceInfo.DeviceModelBoot = settings.DeviceModelBoot;
            if (settings.HardwareManufacturer != null) sig.DeviceInfo.HardwareManufacturer = settings.HardwareManufacturer;
            if (settings.HardwareModel != null) sig.DeviceInfo.HardwareModel = settings.HardwareModel;
            if (settings.FirmwareBrand != null) sig.DeviceInfo.FirmwareBrand = settings.FirmwareBrand;
            if (settings.FirmwareTags != null) sig.DeviceInfo.FirmwareTags = settings.FirmwareTags;
            if (settings.FirmwareType != null) sig.DeviceInfo.FirmwareType = settings.FirmwareType;
            if (settings.FirmwareFingerprint != null) sig.DeviceInfo.FirmwareFingerprint = settings.FirmwareFingerprint;

            sig.LocationFix.Add(new POGOProtos.Networking.Signature.Types.LocationFix()
            {
                Provider = "fused",
                Latitude = (float)_latitude,
                Longitude = (float)_longitude,
                Altitude = (float)_altitude,
                HorizontalAccuracy = (float)Math.Round(GenRandom(50, 250), 7),
                VerticalAccuracy = RandomDevice.Next(2, 5),
                //TimestampSnapshot = (ulong)(Utils.GetTime(true) - _startTime - RandomDevice.Next(100, 300)),
                ProviderStatus = 3,
                LocationType = 1
            });

            //Compute 10
            var x = new System.Data.HashFunction.xxHash(32, 0x1B845238);
            var firstHash = BitConverter.ToUInt32(x.ComputeHash(_authTicket.ToByteArray()), 0);
            x = new System.Data.HashFunction.xxHash(32, firstHash);
            var locationBytes = BitConverter.GetBytes(_latitude).Reverse()
                .Concat(BitConverter.GetBytes(_longitude).Reverse())
                .Concat(BitConverter.GetBytes(_altitude).Reverse()).ToArray();
            sig.LocationHash1 = BitConverter.ToUInt32(x.ComputeHash(locationBytes), 0);
            //Compute 20
            x = new System.Data.HashFunction.xxHash(32, 0x1B845238);
            sig.LocationHash2 = BitConverter.ToUInt32(x.ComputeHash(locationBytes), 0);
            //Compute 24
            x = new System.Data.HashFunction.xxHash(64, 0x1B845238);
            var seed = BitConverter.ToUInt64(x.ComputeHash(_authTicket.ToByteArray()), 0);
            x = new System.Data.HashFunction.xxHash(64, seed);
            foreach (var req in requests)
                sig.RequestHash.Add(BitConverter.ToUInt64(x.ComputeHash(req.ToByteArray()), 0));

            //static for now
            sig.Unk22 = ByteString.CopyFrom(new byte[16] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F });


            Unknown6 val = new Unknown6();
            val.RequestType = 6;
            val.Unknown2 = new Unknown6.Types.Unknown2();
            val.Unknown2.Unknown1 = ByteString.CopyFrom(Encrypt(sig.ToByteArray()));
            return val;
        }

        private byte[] Encrypt(byte[] bytes)
        {
            var output = PogoCrypto.HelperCrypto.Extract(bytes);
            return output;
        }

        public RequestEnvelope GetRequestEnvelope(params Request[] customRequests)
        {
            var e = new RequestEnvelope
            {
                StatusCode = 2, //1

                RequestId = _nextRequestId++, //3
                Requests = { customRequests }, //4

                //Unknown6 = , //6
                Latitude = _latitude, //7
                Longitude = _longitude, //8
                Altitude = _altitude, //9
                AuthTicket = _authTicket, //11
                Unknown12 = 989 //12
            };
            e.Unknown6.Add(GenerateSignature(customRequests));
            return e;
        }

        public RequestEnvelope GetInitialRequestEnvelope(params Request[] customRequests)
        {
            var e = new RequestEnvelope
            {
                StatusCode = 2, //1

                RequestId = 1469378659230941192, //3
                Requests = { customRequests }, //4

                //Unknown6 = , //6
                Latitude = _latitude, //7
                Longitude = _longitude, //8
                Altitude = _altitude, //9
                AuthInfo = new POGOProtos.Networking.Envelopes.RequestEnvelope.Types.AuthInfo
                {
                    Provider = _authType == AuthType.Google ? "google" : "ptc",
                    Token = new POGOProtos.Networking.Envelopes.RequestEnvelope.Types.AuthInfo.Types.JWT
                    {
                        Contents = _authToken,
                        Unknown2 = 14
                    }
                }, //10
                Unknown12 = 989 //12
            };
            return e;
        }



        public RequestEnvelope GetRequestEnvelope(RequestType type, IMessage message)
        {
            return GetRequestEnvelope(new Request()
            {
                RequestType = type,
                RequestMessage = message.ToByteString()
            });

        }

        private static readonly Random RandomDevice = new Random();

        public static double GenRandom(double min, double max)
        {
            return RandomDevice.NextDouble() * (max - min) + min;
        }
    }
}