using NitroxClient.Communication.Abstract;
using NitroxModel.DataStructures.Util;
using NitroxModel.Networking.Packets;
using NitroxModel.Networking.Session;
using NitroxModel.Server;
using NSubstitute;

namespace Nitrox.Test.Client.Communication.MultiplayerSession
{
    internal static class TestConstants
    {
        public const string TEST_IP_ADDRESS = "#.#.#.#";
        public const int TEST_SERVER_PORT = ServerConstants.DEFAULT_PORT;
        public const ushort TEST_PLAYER_ID = 1;
        public const string TEST_PLAYER_NAME = "TEST";
        public const string TEST_RESERVATION_KEY = "@#*(&";
        public const string TEST_CORRELATION_ID = "CORRELATED";
        public const int TEST_MAX_PLAYER_CONNECTIONS = 100;
        public const SessionReservationState TEST_REJECTION_STATE = SessionReservationState.REJECTED | SessionReservationState.UNIQUE_PLAYER_NAME_CONSTRAINT_VIOLATED;
        public static readonly AuthenticationContext TEST_AUTHENTICATION_CONTEXT = new AuthenticationContext((byte[])[0x01, 0x02, 0x03], Optional.Empty);
        public static readonly SessionPolicy TEST_SESSION_POLICY = new SessionPolicy(1, [], false, TEST_MAX_PLAYER_CONNECTIONS, false);
        public static readonly PlayerSettings TEST_PLAYER_SETTINGS = new PlayerSettings("testname", RandomColorGenerator.GenerateColor());
        public static readonly IMultiplayerSessionConnectionState TEST_CONNECTION_STATE = Substitute.For<IMultiplayerSessionConnectionState>();
    }
}
