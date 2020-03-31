using System;

namespace Mops.Client.Core
{
    /// <summary>
    /// Data that makes up a signaler message
    /// </summary>
    /// <remarks>
    /// Note: the same data is used for transmitting and receiving
    /// </remarks>
    [Serializable]
    public class SignallingMessage
    {
        /// <summary>
        /// Possible message types as-serialized on the wire
        /// </summary>
        public enum WireMessageType
        {
            /// <summary>
            /// An unrecognized message
            /// </summary>
            Unknown = 0,
            /// <summary>
            /// A SDP offer message
            /// </summary>
            Offer,
            /// <summary>
            /// A SDP answer message
            /// </summary>
            Answer,
            /// <summary>
            /// A trickle-ice or ice message
            /// </summary>
            Ice
        }

        /// <summary>
        /// Convert a message type from <see xref="string"/> to <see cref="WireMessageType"/>.
        /// </summary>
        /// <param name="stringType">The message type as <see xref="string"/>.</param>
        /// <returns>The message type as a <see cref="WireMessageType"/> object.</returns>
        public static WireMessageType WireMessageTypeFromString(string stringType)
        {
            if (string.Equals(stringType, "offer", StringComparison.OrdinalIgnoreCase))
            {
                return WireMessageType.Offer;
            }
            else if (string.Equals(stringType, "answer", StringComparison.OrdinalIgnoreCase))
            {
                return WireMessageType.Answer;
            }
            throw new ArgumentException($"Unkown signaler message type '{stringType}'");
        }

        /// <summary>
        /// The message type
        /// </summary>
        public WireMessageType MessageType;

        /// <summary>
        /// The primary message contents
        /// </summary>
        public string Data;

        /// <summary>
        /// The data separator needed for proper ICE serialization
        /// </summary>
        public string IceDataSeparator;
    }
}
