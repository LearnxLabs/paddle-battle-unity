using Mirror;
using UnityEngine;

public struct CreatePlayerMessage : NetworkMessage
{
    public string nickname;
}