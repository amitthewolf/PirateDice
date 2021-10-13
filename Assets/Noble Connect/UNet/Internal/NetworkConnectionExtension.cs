using System.Reflection;
using UnityEngine.Networking;

#pragma warning disable 0618 

namespace NobleConnect.UNet
{
    public static class NetworkConnectionExtension
    {
        /// <summary>Get a PlayerController by id</summary>
        /// <remarks>
        /// This is needed for the NobleNetworkLobbyManager because Unity does not expose the GetPlayerController method publicly.
        /// So instead use call it via reflection using this extension method.
        /// </remarks>
        /// <param name="conn">The network connection</param>
        /// <param name="playerControllerId">The id of the PlayerController to get</param>
        /// <param name="playerController">The PlayerController is returned here.</param>
        /// <returns>True if the PlayerController is found. False otherwise</returns>
        public static bool GetPlayerController(this NetworkConnection conn, short playerControllerId, out PlayerController playerController)
        {
            MethodInfo dynMethod = conn.GetType().GetMethod("GetPlayerController", BindingFlags.NonPublic | BindingFlags.Instance);

            object[] parameters = new object[] { playerControllerId, null };
            bool result = (bool)dynMethod.Invoke(conn, parameters);

            playerController = result ? (PlayerController)parameters[1] : null;

            return result;
        }
    }
}